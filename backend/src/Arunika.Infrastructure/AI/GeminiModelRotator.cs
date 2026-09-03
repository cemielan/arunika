using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arunika.Infrastructure.AI;

public sealed class GeminiModelRotator
{
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiModelRotator> _logger;
    private readonly ConcurrentDictionary<string, ModelUsage> _usage = new();
    private int _articlesSinceRotation = 0;
    private readonly object _rotationLock = new();

    public GeminiModelRotator(IOptions<GeminiOptions> options, ILogger<GeminiModelRotator> logger)
    {
        _options = options.Value;
        _logger = logger;

        foreach (var model in GetOrderedModels())
        {
            _usage[model] = new ModelUsage();
        }
    }

    public string GetNextModel()
    {
        lock (_rotationLock)
        {
            var availableModels = GetOrderedModels()
                .Where(m => _usage[m].IsHealthy)
                .ToList();

            if (availableModels.Count == 0)
            {
                _logger.LogWarning("All Gemini models unhealthy, falling back to primary");
                return _options.Model;
            }

            if (_articlesSinceRotation >= 2)
            {
                _articlesSinceRotation = 0;
                RotateToNextHealthyModel(availableModels);
            }

            var selectedModel = availableModels.First();
            _usage[selectedModel].Increment();
            _articlesSinceRotation++;

            _logger.LogDebug("Selected Gemini model {Model} (articles since rotation: {Count})", selectedModel, _articlesSinceRotation);
            return selectedModel;
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
            _logger.LogWarning("Model {Model} marked unhealthy after failure", model);
        }
    }

    public ModelUsageStats GetUsageStats()
    {
        var stats = new ModelUsageStats();
        foreach (var kvp in _usage)
        {
            stats.Models[kvp.Key] = new ModelStat
            {
                TotalCalls = kvp.Value.TotalCalls,
                SuccessCount = kvp.Value.SuccessCount,
                FailureCount = kvp.Value.FailureCount,
                IsHealthy = kvp.Value.IsHealthy
            };
        }
        stats.ArticlesSinceRotation = _articlesSinceRotation;
        return stats;
    }

    private void RotateToNextHealthyModel(List<string> availableModels)
    {
        var currentModel = availableModels.FirstOrDefault(m => _usage[m].TotalCalls > 0);
        if (currentModel == null) return;

        var currentIndex = availableModels.IndexOf(currentModel);
        var nextIndex = (currentIndex + 1) % availableModels.Count;
        var nextModel = availableModels[nextIndex];

        _logger.LogInformation("Rotating from {CurrentModel} to {NextModel} after 2 articles", currentModel, nextModel);
    }

    private List<string> GetOrderedModels()
    {
        var models = new List<string> { _options.Model };
        models.AddRange(_options.FallbackModels);
        return models.Distinct().ToList();
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
        public bool IsHealthy => _consecutiveFailures < 3;

        public void Increment() => Interlocked.Increment(ref _totalCalls);
        public void RecordSuccess()
        {
            Interlocked.Increment(ref _successCount);
            _consecutiveFailures = 0;
        }
        public void RecordFailure()
        {
            Interlocked.Increment(ref _failureCount);
            Interlocked.Increment(ref _consecutiveFailures);
        }
    }
}

public sealed class ModelUsageStats
{
    public Dictionary<string, ModelStat> Models { get; } = new();
    public int ArticlesSinceRotation { get; set; }
}

public sealed class ModelStat
{
    public int TotalCalls { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public bool IsHealthy { get; set; }
}