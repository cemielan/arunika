using Arunika.Infrastructure.AI;

namespace Arunika.UnitTests;

public class ImpactScoringRubricTests
{
    [Fact]
    public void ResolveScore_SumsTheFiveComponents()
    {
        var score = ImpactScoringRubric.ResolveScore(18, 14, 11, 9, 12, reportedTotal: 0);

        Assert.Equal(64, score);
    }

    [Fact]
    public void ResolveScore_IgnoresTheModelsOwnArithmeticWhenComponentsArePresent()
    {
        var score = ImpactScoringRubric.ResolveScore(18, 14, 11, 9, 12, reportedTotal: 85);

        Assert.Equal(64, score);
    }

    [Theory]
    [InlineData(99, 14, 11, 9, 12, 71)]  // breadth over its ceiling of 25
    [InlineData(18, 14, 99, 9, 12, 73)]  // surprise over its ceiling of 20
    [InlineData(-5, 14, 11, 9, 12, 46)]  // a negative component
    public void ResolveScore_ClampsEachComponentToItsOwnCeiling(
        int breadth, int magnitude, int surprise, int immediacy, int certainty, int expected)
    {
        var score = ImpactScoringRubric.ResolveScore(breadth, magnitude, surprise, immediacy, certainty, 0);

        Assert.Equal(expected, score);
    }

    [Fact]
    public void ResolveScore_CannotExceedOneHundred()
    {
        var score = ImpactScoringRubric.ResolveScore(25, 25, 20, 15, 15, reportedTotal: 0);

        Assert.Equal(100, score);
    }

    [Fact]
    public void ResolveScore_FallsBackToTheReportedTotalWhenAComponentIsMissing()
    {
        var score = ImpactScoringRubric.ResolveScore(18, null, 11, 9, 12, reportedTotal: 70);

        Assert.Equal(70, score);
    }

    [Theory]
    [InlineData(150, 100)]
    [InlineData(-4, 0)]
    public void ResolveScore_ClampsTheFallbackTotal(int reportedTotal, int expected)
    {
        var score = ImpactScoringRubric.ResolveScore(null, null, null, null, null, reportedTotal);

        Assert.Equal(expected, score);
    }
}
