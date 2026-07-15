namespace Arunika.Infrastructure.AI;

/// <summary>
/// Simple in-process sliding-window rate limiter shared across every
/// <see cref="GeminiAiEnrichmentService"/> call. The Gemini API key this project
/// uses is on the free tier, which only allows ~15 requests/minute per model —
/// without this limiter, a burst of newly-fetched articles (or a batch of retries
/// from <c>RetryFailedEnrichmentJob</c>) blows straight through that quota because
/// Hangfire runs up to 20 <c>EnrichArticleJob</c> workers concurrently. That 429
/// ("quota exceeded") is the actual root cause behind articles that never get a
/// summary/impact score (see design doc §5) — this limiter paces calls so the
/// quota is (almost) never hit in the first place, instead of just retrying after
/// the fact.
/// </summary>
public class GeminiRateLimiter(int maxCallsPerWindow = 12, TimeSpan? window = null)
{
    private readonly TimeSpan _window = window ?? TimeSpan.FromMinutes(1);
    private readonly Queue<DateTime> _callTimestamps = new();
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task WaitForSlotAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            TimeSpan waitFor;
            await _gate.WaitAsync(cancellationToken);
            try
            {
                var now = DateTime.UtcNow;
                while (_callTimestamps.Count > 0 && now - _callTimestamps.Peek() >= _window)
                {
                    _callTimestamps.Dequeue();
                }

                if (_callTimestamps.Count < maxCallsPerWindow)
                {
                    _callTimestamps.Enqueue(now);
                    return;
                }

                waitFor = _window - (now - _callTimestamps.Peek()) + TimeSpan.FromMilliseconds(50);
            }
            finally
            {
                _gate.Release();
            }

            await Task.Delay(waitFor, cancellationToken);
        }
    }
}
