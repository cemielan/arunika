using System.Collections.Concurrent;

namespace Arunika.Infrastructure.AI;

/// <summary>
/// Circuit breaker per model to avoid hammering models that are consistently failing.
/// Opens after 3 consecutive failures within 5 minutes, stays open for 10 minutes.
/// </summary>
public sealed class GeminiCircuitBreaker
{
    private const int FailureThreshold = 3;
    private static readonly TimeSpan FailureWindow = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan OpenDuration = TimeSpan.FromMinutes(10);

    private readonly ConcurrentDictionary<string, ModelState> _states = new();

    public bool IsAvailable(string model)
    {
        var state = _states.GetOrAdd(model, _ => new ModelState());
        lock (state.Lock)
        {
            if (state.IsOpen)
            {
                if (DateTimeOffset.UtcNow >= state.OpenUntil)
                {
                    state.IsOpen = false;
                    state.FailureCount = 0;
                    state.FirstFailureTime = null;
                    return true;
                }
                return false;
            }
            return true;
        }
    }

    public void RecordSuccess(string model)
    {
        var state = _states.GetOrAdd(model, _ => new ModelState());
        lock (state.Lock)
        {
            state.FailureCount = 0;
            state.FirstFailureTime = null;
        }
    }

    public void RecordFailure(string model)
    {
        var state = _states.GetOrAdd(model, _ => new ModelState());
        lock (state.Lock)
        {
            var now = DateTimeOffset.UtcNow;
            if (state.FirstFailureTime is null || now - state.FirstFailureTime > FailureWindow)
            {
                state.FirstFailureTime = now;
                state.FailureCount = 1;
            }
            else
            {
                state.FailureCount++;
                if (state.FailureCount >= FailureThreshold)
                {
                    state.IsOpen = true;
                    state.OpenUntil = now.Add(OpenDuration);
                }
            }
        }
    }

    private sealed class ModelState
    {
        public object Lock { get; } = new();
        public int FailureCount { get; set; }
        public DateTimeOffset? FirstFailureTime { get; set; }
        public bool IsOpen { get; set; }
        public DateTimeOffset OpenUntil { get; set; }
    }
}