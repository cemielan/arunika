using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arunika.Infrastructure.AI;

/// <summary>
/// Thrown when a model has no request budget left for the current quota day,
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
/// Budget gate for the Gemini free tier, tracked <em>per model</em> from
/// <see cref="GeminiOptions.ModelQuotas"/>, plus a global per-minute ceiling
/// from <see cref="GeminiOptions.MaxRequestsPerMinute"/>.
///
/// Per-model accounting is the point: the free tier bills RPM and RPD against
/// each model separately, so a single shared counter either throttles the
/// high-quota Flash-Lite models down to the 5 RPM of the weakest Flash model,
/// or — as happened before this was split out — lets the primary model blow
/// through its own 15 RPM while the rest of the chain sits idle.
///
/// The two windows are kept in different places on purpose. Per-minute burst
/// smoothing is in-process: losing it on restart costs at most one minute of
/// pacing. The daily count goes to <see cref="GeminiUsageStore"/>, because RPD
/// is billed per API key against a calendar day in US/Pacific and an in-process
/// counter silently resets on every redeploy — the pipeline then believed it
/// had a full allowance while Google kept counting, and the console ended up
/// showing 501/500. Without a store the limiter falls back to in-memory daily
/// counting, which is only good enough for tests.
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

    /// <summary>
    /// Google resets RPD on a calendar day in US/Pacific. Where the IANA database
    /// is unavailable, standard time (never PDT) is used instead: that puts our
    /// boundary up to an hour <em>after</em> Google's, so the count is carried
    /// slightly too long rather than cleared too early.
    /// </summary>
    private static readonly TimeZoneInfo QuotaZone =
        TimeZoneInfo.TryFindSystemTimeZoneById("America/Los_Angeles", out var pacific)
            ? pacific
            : TimeZoneInfo.CreateCustomTimeZone("Gemini-Quota-Day", TimeSpan.FromHours(-8), "Gemini quota day", "Gemini quota day");

    private readonly GeminiOptions _options;
    private readonly GeminiUsageStore? _usageStore;
    private readonly ILogger<GeminiRateLimiter> _logger;
    private readonly object _sync = new();
    private readonly Queue<DateTimeOffset> _globalMinuteCalls = new();
    private readonly Dictionary<string, ModelWindow> _windows = [];

    public GeminiRateLimiter(
        IOptions<GeminiOptions> options,
        ILogger<GeminiRateLimiter> logger,
        GeminiUsageStore? usageStore = null)
    {
        _options = options.Value;
        _logger = logger;
        _usageStore = usageStore;
    }

    /// <summary>
    /// Cheap pre-check so callers can skip a model without paying for the wait
    /// loop, answered from the last count this instance observed. A <c>true</c>
    /// here is not a reservation — <see cref="WaitForSlotAsync"/> re-checks
    /// against the shared store and can still report the model exhausted.
    /// </summary>
    public bool IsAvailable(string model)
    {
        lock (_sync)
        {
            var window = GetWindow(model);
            if (DateTimeOffset.UtcNow < window.CooldownUntil)
            {
                return false;
            }

            // An unknown or stale day means this instance has not spent anything
            // it knows of yet; WaitForSlotAsync will find out from the store.
            return window.Day.Date != QuotaDay() || window.Day.Count < EffectiveRpd(model);
        }
    }

    /// <summary>True when at least one model in <paramref name="models"/> still has budget.</summary>
    public bool AnyAvailable(IEnumerable<string> models) => models.Any(IsAvailable);

    /// <summary>
    /// Reserves one request slot for <paramref name="model"/>, waiting out the
    /// per-minute window if necessary and then spending one call from the shared
    /// daily allowance.
    /// </summary>
    /// <exception cref="GeminiModelExhaustedException">
    /// The model is in cooldown, has spent its daily budget, or would need to
    /// block longer than <see cref="GeminiOptions.MaxSlotWait"/> for a slot.
    /// </exception>
    public async Task WaitForSlotAsync(string model, CancellationToken cancellationToken = default)
    {
        var maxWait = _options.MaxSlotWait > TimeSpan.Zero ? _options.MaxSlotWait : TimeSpan.FromSeconds(90);
        var deadline = DateTimeOffset.UtcNow + maxWait;
        var quotaDay = QuotaDay();
        var rpd = EffectiveRpd(model);

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

                // Fail fast on what we already know before waiting for a minute
                // slot we would only have to throw away.
                if (window.Day.Date == quotaDay && window.Day.Count >= rpd)
                {
                    throw new GeminiModelExhaustedException(model,
                        $"Gemini model {model} has spent its daily budget ({window.Day.Count}/{rpd}).");
                }

                Trim(window.MinuteCalls, now, MinuteWindow);
                Trim(_globalMinuteCalls, now, MinuteWindow);

                var modelMinuteOk = window.MinuteCalls.Count < EffectiveRpm(model);
                var globalMinuteOk = _globalMinuteCalls.Count < Math.Max(1, _options.MaxRequestsPerMinute);

                if (modelMinuteOk && globalMinuteOk)
                {
                    window.MinuteCalls.Enqueue(now);
                    _globalMinuteCalls.Enqueue(now);
                    break;
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

        await ReserveDailyCallAsync(model, quotaDay, rpd, cancellationToken);
    }

    /// <summary>
    /// Spends one call from the daily allowance, outside the lock because the
    /// store is a database round trip. A rejection here has already taken a
    /// per-minute slot, which is deliberate: minute slots age out on their own,
    /// while double-spending the daily allowance does not heal until tomorrow.
    /// </summary>
    private async Task ReserveDailyCallAsync(string model, DateOnly quotaDay, int rpd, CancellationToken cancellationToken)
    {
        if (_usageStore is null)
        {
            lock (_sync)
            {
                var window = GetWindow(model);
                var count = window.Day.Date == quotaDay ? window.Day.Count : 0;
                if (count >= rpd)
                {
                    throw new GeminiModelExhaustedException(model,
                        $"Gemini model {model} has spent its daily budget ({count}/{rpd}).");
                }

                window.Day = new DayUsage(quotaDay, count + 1);
                return;
            }
        }

        var reserved = await _usageStore.TryReserveAsync(model, quotaDay, rpd, cancellationToken);

        lock (_sync)
        {
            // On a rejection the exact shared count is unknown — only that it is
            // at or above the limit. Recording the limit is enough to keep
            // IsAvailable from sending anything else down this model today.
            GetWindow(model).Day = new DayUsage(quotaDay, reserved ?? rpd);
        }

        if (reserved is null)
        {
            throw new GeminiModelExhaustedException(model,
                $"Gemini model {model} has spent its daily budget ({rpd}/{rpd}) for {quotaDay:yyyy-MM-dd} Pacific.");
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

    /// <summary>
    /// Remaining budget per model, for the admin diagnostics endpoint. Daily
    /// figures come from the shared store rather than this instance's cache, so
    /// the endpoint reports the whole deployment's spend and stays right across a
    /// restart — the readings that matter when answering "why did enrichment
    /// stop?". Per-minute figures remain local, which is all they ever were.
    /// </summary>
    public async Task<IReadOnlyList<ModelBudget>> GetBudgetsAsync(CancellationToken cancellationToken = default)
    {
        var quotaDay = QuotaDay();
        var models = new List<string> { _options.Model };
        models.AddRange(_options.FallbackModels);
        var distinct = models.Distinct().ToList();

        var spentToday = new Dictionary<string, int>(distinct.Count);
        foreach (var model in distinct)
        {
            if (_usageStore is not null)
            {
                spentToday[model] = await _usageStore.GetCountAsync(model, quotaDay, cancellationToken);
            }
        }

        lock (_sync)
        {
            var now = DateTimeOffset.UtcNow;

            return distinct
                .Select(model =>
                {
                    var window = GetWindow(model);
                    Trim(window.MinuteCalls, now, MinuteWindow);

                    var usedToday = spentToday.TryGetValue(model, out var stored)
                        ? stored
                        : window.Day.Date == quotaDay ? window.Day.Count : 0;

                    return new ModelBudget(
                        model,
                        window.MinuteCalls.Count,
                        EffectiveRpm(model),
                        usedToday,
                        EffectiveRpd(model),
                        now < window.CooldownUntil ? window.CooldownUntil : null);
                })
                .ToList();
        }
    }

    /// <summary>The calendar day the free-tier RPD allowance is currently being billed against.</summary>
    private static DateOnly QuotaDay() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, QuotaZone).DateTime);

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

    private readonly record struct DayUsage(DateOnly Date, int Count);

    private sealed class ModelWindow
    {
        public Queue<DateTimeOffset> MinuteCalls { get; } = new();
        public DayUsage Day { get; set; }
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
