namespace Arunika.Infrastructure.AI;

/// <summary>
/// In-process sliding-window rate limiter for Gemini API.
/// Enforces both RPM (requests per minute) and RPD (requests per day) limits.
/// Free tier Flash-Lite: 30 RPM, 1000+ RPD. Free tier Flash: 15 RPM, 20 RPD.
/// </summary>
public class GeminiRateLimiter(int maxCallsPerMinute = 12, int maxCallsPerDay = 900, TimeSpan? minuteWindow = null)
{
    private readonly TimeSpan _minuteWindow = minuteWindow ?? TimeSpan.FromMinutes(1);
    private readonly TimeSpan _dayWindow = TimeSpan.FromDays(1);
    private readonly Queue<DateTime> _minuteCalls = new();
    private readonly Queue<DateTime> _dailyCalls = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly object _cooldownGate = new();
    private DateTimeOffset _quotaBlockedUntilUtc = DateTimeOffset.MinValue;

    public bool IsQuotaCoolingDown
    {
        get
        {
            lock (_cooldownGate)
            {
                return DateTimeOffset.UtcNow < _quotaBlockedUntilUtc;
            }
        }
    }

    public void MarkQuotaCooldown(TimeSpan duration)
    {
        lock (_cooldownGate)
        {
            var blockedUntil = DateTimeOffset.UtcNow.Add(duration);
            if (blockedUntil > _quotaBlockedUntilUtc)
            {
                _quotaBlockedUntilUtc = blockedUntil;
            }
        }
    }

    public async Task WaitForSlotAsync(CancellationToken cancellationToken = default)
    {
        if (IsQuotaCoolingDown)
        {
            throw new InvalidOperationException("Gemini quota cooldown is active.");
        }

        while (true)
        {
            TimeSpan waitFor;
            await _gate.WaitAsync(cancellationToken);
            try
            {
                var now = DateTime.UtcNow;

                while (_minuteCalls.Count > 0 && now - _minuteCalls.Peek() >= _minuteWindow)
                {
                    _minuteCalls.Dequeue();
                }

                while (_dailyCalls.Count > 0 && now - _dailyCalls.Peek() >= _dayWindow)
                {
                    _dailyCalls.Dequeue();
                }

                bool minuteOk = _minuteCalls.Count < maxCallsPerMinute;
                bool dailyOk = _dailyCalls.Count < maxCallsPerDay;

                if (minuteOk && dailyOk)
                {
                    _minuteCalls.Enqueue(now);
                    _dailyCalls.Enqueue(now);
                    return;
                }

                if (!dailyOk)
                {
                    var oldestDaily = _dailyCalls.Peek();
                    waitFor = _dayWindow - (now - oldestDaily) + TimeSpan.FromSeconds(10);
                }
                else
                {
                    var oldestMinute = _minuteCalls.Peek();
                    waitFor = _minuteWindow - (now - oldestMinute) + TimeSpan.FromMilliseconds(50);
                }
            }
            finally
            {
                _gate.Release();
            }

            await Task.Delay(waitFor, cancellationToken);
        }
    }
}
