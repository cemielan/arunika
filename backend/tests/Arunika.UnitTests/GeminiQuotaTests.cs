using Arunika.Infrastructure.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Arunika.UnitTests;

/// <summary>
/// Regression cover for the free-tier budget accounting. Both components under
/// test previously failed silently — the rotator returned the primary model
/// forever, and the limiter counted every model against one shared allowance —
/// which showed up only as 429s in the Google console.
/// </summary>
public class GeminiQuotaTests
{
    private static GeminiOptions BaseOptions() => new()
    {
        ApiKey = "test-key",
        Model = "model-a",
        FallbackModels = ["model-b", "model-c"],
        ArticlesPerRotation = 2,
        MaxRequestsPerMinute = 1000,
        QuotaSafetyPercent = 100,
        MaxSlotWait = TimeSpan.FromMilliseconds(50),
        // RPM is left generous so the daily allowance is the binding constraint
        // in these tests; the per-minute path is covered separately.
        ModelQuotas = new Dictionary<string, ModelQuota>
        {
            ["model-a"] = new() { Rpm = 10, Tpm = 250_000, Rpd = 3 },
            ["model-b"] = new() { Rpm = 10, Tpm = 250_000, Rpd = 3 },
            ["model-c"] = new() { Rpm = 10, Tpm = 250_000, Rpd = 3 },
        },
    };

    private static GeminiRateLimiter Limiter(GeminiOptions options) =>
        new(Options.Create(options), NullLogger<GeminiRateLimiter>.Instance);

    private static GeminiModelRotator Rotator(GeminiOptions options) =>
        new(Options.Create(options), NullLogger<GeminiModelRotator>.Instance);

    [Fact]
    public void GetCandidates_AdvancesAfterArticlesPerRotation()
    {
        var rotator = Rotator(BaseOptions());

        // ArticlesPerRotation = 2, so the first two articles share a head model
        // and the third moves on. The old implementation pinned every article
        // to the primary model, concentrating the whole load on one quota.
        var first = rotator.GetCandidates()[0];
        var second = rotator.GetCandidates()[0];
        var third = rotator.GetCandidates()[0];

        Assert.Equal("model-a", first);
        Assert.Equal("model-a", second);
        Assert.Equal("model-b", third);
    }

    [Fact]
    public void GetCandidates_ReturnsEveryModelSoFallthroughIsPossible()
    {
        var rotator = Rotator(BaseOptions());

        var candidates = rotator.GetCandidates();

        Assert.Equal(3, candidates.Count);
        Assert.Equal(new[] { "model-a", "model-b", "model-c" }, candidates.Order().ToArray());
    }

    [Fact]
    public void GetCandidates_DemotesRepeatedlyFailingModelWithoutDroppingIt()
    {
        var rotator = Rotator(BaseOptions());

        for (var i = 0; i < 3; i++)
        {
            rotator.RecordFailure("model-a");
        }

        var candidates = rotator.GetCandidates();

        Assert.Equal(3, candidates.Count);
        Assert.Equal("model-a", candidates[^1]);
    }

    [Fact]
    public async Task WaitForSlotAsync_BudgetsEachModelSeparately()
    {
        var limiter = Limiter(BaseOptions());

        // model-a has Rpd 3. Spending it must not touch model-b's allowance.
        for (var i = 0; i < 3; i++)
        {
            await limiter.WaitForSlotAsync("model-a");
        }

        Assert.False(limiter.IsAvailable("model-a"));
        Assert.True(limiter.IsAvailable("model-b"));

        await limiter.WaitForSlotAsync("model-b");
    }

    [Fact]
    public async Task WaitForSlotAsync_ThrowsRatherThanBlockingWhenDailyBudgetIsSpent()
    {
        var limiter = Limiter(BaseOptions());

        for (var i = 0; i < 3; i++)
        {
            await limiter.WaitForSlotAsync("model-a");
        }

        // The previous implementation slept until the 24h window rolled, which
        // pinned a Hangfire worker for the rest of the day.
        var ex = await Assert.ThrowsAsync<GeminiModelExhaustedException>(
            () => limiter.WaitForSlotAsync("model-a"));

        Assert.Equal("model-a", ex.Model);
    }

    [Fact]
    public void MarkModelCooldown_ParksOnlyTheOffendingModel()
    {
        var limiter = Limiter(BaseOptions());

        limiter.MarkModelCooldown("model-a", TimeSpan.FromMinutes(30));

        Assert.False(limiter.IsAvailable("model-a"));
        Assert.True(limiter.IsAvailable("model-b"));
        Assert.True(limiter.AnyAvailable(["model-a", "model-b", "model-c"]));
    }

    [Fact]
    public void QuotaSafetyPercent_DeratesThePublishedLimit()
    {
        var options = BaseOptions();
        options.QuotaSafetyPercent = 50;
        options.ModelQuotas["model-a"] = new ModelQuota { Rpm = 15, Tpm = 250_000, Rpd = 500 };

        var budget = Limiter(options).GetBudgets().Single(b => b.Model == "model-a");

        Assert.Equal(7, budget.MinuteLimit);
        Assert.Equal(250, budget.DailyLimit);
    }

    [Fact]
    public void GetBudgets_FallsBackToTheLowestAllowanceForUnlistedModels()
    {
        var options = BaseOptions();
        options.ModelQuotas.Remove("model-c");

        var budget = Limiter(options).GetBudgets().Single(b => b.Model == "model-c");

        Assert.Equal(5, budget.MinuteLimit);
        Assert.Equal(20, budget.DailyLimit);
    }

    [Fact]
    public async Task WaitForSlotAsync_HonoursTheGlobalPerMinuteCeiling()
    {
        var options = BaseOptions();
        options.MaxRequestsPerMinute = 2;

        var limiter = Limiter(options);
        await limiter.WaitForSlotAsync("model-a");
        await limiter.WaitForSlotAsync("model-b");

        // Both models still have per-model budget, but the shared burst ceiling
        // is spent, so the third call cannot be served inside the wait window.
        await Assert.ThrowsAsync<GeminiModelExhaustedException>(
            () => limiter.WaitForSlotAsync("model-c"));
    }
}
