using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arunika.Infrastructure.AI;

/// <summary>
/// Thrown when a model has no request budget left for the current rolling day,
/// or is cooling down after the API itself reported a quota breach. Callers are
/// expected to move on to the next model in the chain instead of waiting: the
/// free-tier daily window only frees up hours later, and blocking on it would
/// pin a Hangfire enrichment worker for the rest of the day.
/// </summary>
public sealed class GeminiModelExhaustedException(string model, string message)
    : InvalidOperationException(message)
{
    public string Model { get; } = model;
}

/// <summary>
/// In-process sliding-window budget for the Gemini free tier, tracked
/// <em>per model</em> from <see cref="GeminiOptions.ModelQuotas"/>, plus a
/// global per-minute ceiling from <see cref="GeminiOptions.MaxRequestsPerMinute"/>.
///
/// Per-model accounting is the point: the free tier bills RPM and RPD against
/// each model separately, so a single shared counter either throttles the
/// high-quota Flash-Lite models down to the 5 RPM of the weakest Flash model,
/// or — as happened before this was split out — lets the primary model blow
/// through its own 15 RPM while the rest of the chain sits idle.
///
/// Budgets are derated by <see cref="GeminiOptions.QuotaSafetyPercent"/> so the
/// pipeline backs off before Google does; a 429 that slips through anyway is
/// reported via <see cref="MarkModelCooldown"/> and parks that one model only.
/// </summary>
public sealed class GeminiRateLimiter
{
    /// <summary>Applied to models absent from <c>ModelQuotas</c> — the lowest free-tier allowance.</summary>
    private static readonly ModelQuota ConservativeDefault = new() { Rpm = 5, Tpm = 250_000, Rpd = 20 };

    private static readonly TimeSpan MinuteWindow = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan DayWindow = TimeSpan.FromDays(1);

    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiRateLimiter> _logger;
    private readonly object _sync = new();
    private readonly Queue<DateTimeOffset> _globalMinuteCalls = new();
    private readonly Dictionary<string, ModelWindow> _windows = [];

    public GeminiRateLimiter(IOptions<GeminiOptions> options, ILogger<GeminiRateLimiter> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Cheap pre-check so callers can skip a model without paying for the wait
    /// loop. A <c>true</c> here is not a reservation — <see cref="WaitForSlotAsync"/>
    /// can still report the model exhausted if another thread spends the budget first.
    /// </summary>
    public bool IsAvailable(string model)
    {
        lock (_sync)
        {
            var now = DateTimeOffset.UtcNow;
            var window = GetWindow(model);
            if (now < window.CooldownUntil)
            {
                return false;
            }

            Trim(window.DayCalls, now, DayWindow);
            return window.DayCalls.Count < EffectiveRpd(model);
        }
    }

    /// <summary>True when at least one model in <paramref name="models"/> still has budget.</summary>
    public bool AnyAvailable(IEnumerable<string> models) => models.Any(IsAvailable);

    /// <summary>
    /// Reserves one request slot for <paramref name="model"/>, waiting out the
    /// per-minute window if necessary.
    /// </summary>
    /// <exception cref="GeminiModelExhaustedException">
    /// The model is in cooldown, has spent its rolling-day budget, or would need
    /// to block longer than <see cref="GeminiOptions.MaxSlotWait"/> for a slot.
    /// </exception>
    public async Task WaitForSlotAsync(string model, CancellationToken cancellationToken = default)
    {
        var maxWait = _options.MaxSlotWait > TimeSpan.Zero ? _options.MaxSlotWait : TimeSpan.FromSeconds(90);
        var deadline = DateTimeOffset.UtcNow + maxWait;

        while (true)
        {
            TimeSpan waitFor;

            lock (_sync)
            {
                var now = DateTimeOffset.UtcNow;
                var window = GetWindow(model);

                if (now < window.CooldownUntil)
                {
                    throw new GeminiModelExhaustedException(model,
                        $"Gemini model {model} is in quota cooldown until {window.CooldownUntil:u}.");
                }

                Trim(window.MinuteCalls, now, MinuteWindow);
                Trim(window.DayCalls, now, DayWindow);
                Trim(_globalMinuteCalls, now, MinuteWindow);

                var rpd = EffectiveRpd(model);
                if (window.DayCalls.Count >= rpd)
                {
                    throw new GeminiModelExhaustedException(model,
                        $"Gemini model {model} has spent its rolling-day budget ({window.DayCalls.Count}/{rpd}).");
                }

                var modelMinuteOk = window.MinuteCalls.Count < EffectiveRpm(model);
                var globalMinuteOk = _globalMinuteCalls.Count < Math.Max(1, _options.MaxRequestsPerMinute);

                if (modelMinuteOk && globalMinuteOk)
                {
                    window.MinuteCalls.Enqueue(now);
                    window.DayCalls.Enqueue(now);
                    _globalMinuteCalls.Enqueue(now);
                    return;
                }

                // Wait only as long as it takes the oldest call in whichever
                // minute window is full to age out of it.
                var oldest = modelMinuteOk ? _globalMinuteCalls.Peek() : window.MinuteCalls.Peek();
                waitFor = MinuteWindow - (now - oldest) + TimeSpan.FromMilliseconds(50);
            }

            if (DateTimeOffset.UtcNow + waitFor > deadline)
            {
                throw new GeminiModelExhaustedException(model,
                    $"Gemini model {model} has no per-minute slot within {maxWait.TotalSeconds:0}s.");
            }

            await Task.Delay(waitFor, cancellationToken);
        }
    }

    /// <summary>
    /// Parks a single model after the API reported a quota breach. Deliberately
    /// scoped to one model: a shared cooldown used to take the whole Gemini
    /// provider offline for an hour because one 20-RPD Flash model ran dry.
    /// </summary>
    public void MarkModelCooldown(string model, TimeSpan duration)
    {
        lock (_sync)
        {
            var window = GetWindow(model);
            var until = DateTimeOffset.UtcNow.Add(duration);
            if (until > window.CooldownUntil)
            {
                window.CooldownUntil = until;
                _logger.LogWarning("Gemini model {Model} parked until {Until:u} after a reported quota breach.",
                    model, until);
            }
        }
    }

    /// <summary>Remaining budget per model, for the admin diagnostics endpoint.</summary>
    public IReadOnlyList<ModelBudget> GetBudgets()
    {
        lock (_sync)
        {
            var now = DateTimeOffset.UtcNow;
            var models = new List<string> { _options.Model };
            models.AddRange(_options.FallbackModels);

            return models
                .Distinct()
                .Select(model =>
                {
                    var window = GetWindow(model);
                    Trim(window.MinuteCalls, now, MinuteWindow);
                    Trim(window.DayCalls, now, DayWindow);

                    return new ModelBudget(
                        model,
                        window.MinuteCalls.Count,
                        EffectiveRpm(model),
                        window.DayCalls.Count,
                        EffectiveRpd(model),
                        now < window.CooldownUntil ? window.CooldownUntil : null);
                })
                .ToList();
        }
    }

    private ModelWindow GetWindow(string model)
    {
        if (!_windows.TryGetValue(model, out var window))
        {
            window = new ModelWindow();
            _windows[model] = window;
        }

        return window;
    }

    private ModelQuota ResolveQuota(string model)
        => _options.ModelQuotas.TryGetValue(model, out var quota) ? quota : ConservativeDefault;

    private int EffectiveRpm(string model) => Derate(ResolveQuota(model).Rpm);

    private int EffectiveRpd(string model) => Derate(ResolveQuota(model).Rpd);

    private int Derate(int limit)
    {
        var percent = Math.Clamp(_options.QuotaSafetyPercent, 1, 100);
        return Math.Max(1, limit * percent / 100);
    }

    private static void Trim(Queue<DateTimeOffset> calls, DateTimeOffset now, TimeSpan window)
    {
        while (calls.Count > 0 && now - calls.Peek() >= window)
        {
            calls.Dequeue();
        }
    }

    private sealed class ModelWindow
    {
        public Queue<DateTimeOffset> MinuteCalls { get; } = new();
        public Queue<DateTimeOffset> DayCalls { get; } = new();
        public DateTimeOffset CooldownUntil { get; set; } = DateTimeOffset.MinValue;
    }
}

/// <summary>Snapshot of one model's remaining free-tier budget.</summary>
public sealed record ModelBudget(
    string Model,
    int UsedThisMinute,
    int MinuteLimit,
    int UsedToday,
    int DailyLimit,
    DateTimeOffset? CooldownUntil);
