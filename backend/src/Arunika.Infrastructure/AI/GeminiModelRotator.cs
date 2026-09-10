using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arunika.Infrastructure.AI;

/// <summary>
/// Round-robins enrichment across the configured Gemini model chain so the
/// free-tier per-model quotas are spent evenly instead of the primary model
/// absorbing every request and tripping its own RPM.
///
/// The rotation advances one position every <see cref="GeminiOptions.ArticlesPerRotation"/>
/// articles. <see cref="GetCandidates"/> returns the whole chain starting at the
/// current position, so a caller that has to fall through past an exhausted or
/// unhealthy model still tries every remaining option before giving up.
/// </summary>
public sealed class GeminiModelRotator
{
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiModelRotator> _logger;
    private readonly ConcurrentDictionary<string, ModelUsage> _usage = new();
    private readonly List<string> _chain;
    private readonly object _rotationLock = new();
    private int _position;
    private int _articlesSinceRotation;

    public GeminiModelRotator(IOptions<GeminiOptions> options, ILogger<GeminiModelRotator> logger)
    {
        _options = options.Value;
        _logger = logger;

        _chain = new List<string> { _options.Model };
        _chain.AddRange(_options.FallbackModels);
        _chain = _chain.Where(m => !string.IsNullOrWhiteSpace(m)).Distinct().ToList();

        if (_chain.Count == 0)
        {
            throw new InvalidOperationException("No Gemini models are configured.");
        }

        foreach (var model in _chain)
        {
            _usage[model] = new ModelUsage();
        }
    }

    /// <summary>The full model chain in preference order, ignoring rotation.</summary>
    public IReadOnlyList<string> Chain => _chain;

    /// <summary>
    /// The chain rotated so the next model to carry load comes first, with
    /// models that have failed repeatedly demoted to the back rather than
    /// dropped — a demoted model is still better than no enrichment at all.
    /// Advances the rotation, so call once per article.
    /// </summary>
    public IReadOnlyList<string> GetCandidates()
    {
        lock (_rotationLock)
        {
            if (_articlesSinceRotation >= Math.Max(1, _options.ArticlesPerRotation))
            {
                _articlesSinceRotation = 0;
                _position = (_position + 1) % _chain.Count;
                _logger.LogDebug("Rotating Gemini model chain to start at {Model}.", _chain[_position]);
            }

            _articlesSinceRotation++;

            var rotated = _chain.Skip(_position).Concat(_chain.Take(_position)).ToList();

            // Healthy models first, order otherwise preserved.
            return rotated
                .OrderByDescending(model => _usage[model].IsHealthy)
                .ToList();
        }
    }

    public void RecordSuccess(string model)
    {
        if (_usage.TryGetValue(model, out var usage))
        {
            usage.RecordSuccess();
        }
    }

    public void RecordFailure(string model)
    {
        if (_usage.TryGetValue(model, out var usage))
        {
            usage.RecordFailure();
            if (!usage.IsHealthy)
            {
                _logger.LogWarning("Gemini model {Model} demoted after {Failures} consecutive failures.",
                    model, usage.ConsecutiveFailures);
            }
        }
    }

    public ModelUsageStats GetUsageStats()
    {
        var stats = new ModelUsageStats();
        foreach (var (model, usage) in _usage)
        {
            stats.Models[model] = new ModelStat
            {
                TotalCalls = usage.TotalCalls,
                SuccessCount = usage.SuccessCount,
                FailureCount = usage.FailureCount,
                IsHealthy = usage.IsHealthy,
            };
        }

        lock (_rotationLock)
        {
            stats.CurrentModel = _chain[_position];
            stats.ArticlesSinceRotation = _articlesSinceRotation;
        }

        return stats;
    }

    private sealed class ModelUsage
    {
        private int _totalCalls;
        private int _successCount;
        private int _failureCount;
        private int _consecutiveFailures;

        public int TotalCalls => _totalCalls;
        public int SuccessCount => _successCount;
        public int FailureCount => _failureCount;
        public int ConsecutiveFailures => _consecutiveFailures;
        public bool IsHealthy => Volatile.Read(ref _consecutiveFailures) < 3;

        public void RecordSuccess()
        {
            Interlocked.Increment(ref _totalCalls);
            Interlocked.Increment(ref _successCount);
            Interlocked.Exchange(ref _consecutiveFailures, 0);
        }

        public void RecordFailure()
        {
            Interlocked.Increment(ref _totalCalls);
            Interlocked.Increment(ref _failureCount);
            Interlocked.Increment(ref _consecutiveFailures);
        }
    }
}

public sealed class ModelUsageStats
{
    public Dictionary<string, ModelStat> Models { get; } = new();
    public string CurrentModel { get; set; } = string.Empty;
    public int ArticlesSinceRotation { get; set; }
}

public sealed class ModelStat
{
    public int TotalCalls { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public bool IsHealthy { get; set; }
}
