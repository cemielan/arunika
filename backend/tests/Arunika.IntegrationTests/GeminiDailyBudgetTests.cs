using Arunika.Infrastructure.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Arunika.IntegrationTests;

/// <summary>
/// Regression cover for the free-tier daily allowance surviving a restart.
/// The daily counter used to live only in the limiter's memory, so a redeploy
/// reset the pipeline's view of the day to zero while Google kept counting —
/// which is how the console reported 501/500 on a model this app believed still
/// had budget. These tests run against real Postgres because the guarantee
/// being tested is the atomic insert-or-conditional-increment itself.
/// </summary>
[Collection(PostgresCollection.Name)]
public class GeminiDailyBudgetTests(PostgresContainerFixture fixture)
{
    private static readonly DateOnly Today = new(2026, 9, 11);

    // The container is shared by the whole collection, so every case works on a
    // model name no other case touches.
    private static string UniqueModel() => $"test-model-{Guid.NewGuid():N}";

    private GeminiUsageStore Store() => new(fixture.ConnectionString);

    private GeminiRateLimiter Limiter(string model, int rpd) => new(
        Options.Create(new GeminiOptions
        {
            ApiKey = "test-key",
            Model = model,
            FallbackModels = [],
            MaxRequestsPerMinute = 1000,
            QuotaSafetyPercent = 100,
            MaxSlotWait = TimeSpan.FromMilliseconds(50),
            ModelQuotas = new Dictionary<string, ModelQuota>
            {
                [model] = new() { Rpm = 1000, Tpm = 250_000, Rpd = rpd },
            },
        }),
        NullLogger<GeminiRateLimiter>.Instance,
        Store());

    [Fact]
    public async Task TryReserveAsync_StopsAtTheDailyLimit()
    {
        var store = Store();
        var model = UniqueModel();

        Assert.Equal(1, await store.TryReserveAsync(model, Today, 2));
        Assert.Equal(2, await store.TryReserveAsync(model, Today, 2));

        // Nothing left, and — crucially — nothing reserved either, so a rejected
        // call cannot inflate the count past the published allowance.
        Assert.Null(await store.TryReserveAsync(model, Today, 2));
        Assert.Equal(2, await store.GetCountAsync(model, Today));
    }

    [Fact]
    public async Task TryReserveAsync_CountsEachModelAndDaySeparately()
    {
        var store = Store();
        var first = UniqueModel();
        var second = UniqueModel();

        await store.TryReserveAsync(first, Today, 1);

        Assert.Null(await store.TryReserveAsync(first, Today, 1));
        Assert.Equal(1, await store.TryReserveAsync(second, Today, 1));
        Assert.Equal(1, await store.TryReserveAsync(first, Today.AddDays(1), 1));
    }

    [Fact]
    public async Task DailyBudget_SurvivesARestartOfTheLimiter()
    {
        var model = UniqueModel();

        await Limiter(model, rpd: 2).WaitForSlotAsync(model);
        await Limiter(model, rpd: 2).WaitForSlotAsync(model);

        // A third limiter is a third process lifetime: in-memory counting would
        // hand it a full allowance and double-spend the day.
        var restarted = Limiter(model, rpd: 2);
        var ex = await Assert.ThrowsAsync<GeminiModelExhaustedException>(
            () => restarted.WaitForSlotAsync(model));

        Assert.Equal(model, ex.Model);
        Assert.False(restarted.IsAvailable(model));
    }

    [Fact]
    public async Task ConcurrentWorkers_CannotOverspendTheDailyBudget()
    {
        var model = UniqueModel();
        var store = Store();

        var reservations = await Task.WhenAll(
            Enumerable.Range(0, 20).Select(_ => store.TryReserveAsync(model, Today, 5)));

        Assert.Equal(5, reservations.Count(r => r is not null));
        Assert.Equal(5, await store.GetCountAsync(model, Today));
    }
}
