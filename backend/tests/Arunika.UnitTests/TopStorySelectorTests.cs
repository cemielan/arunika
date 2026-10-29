using Arunika.Application.Services;
using Arunika.Domain.Entities;

namespace Arunika.UnitTests;

public class TopStorySelectorTests
{
    private static Article MakeArticle(string title, params string[] keywords)
    {
        var article = new Article { Id = Guid.NewGuid(), Title = title, Url = "https://example.test", RawContent = "", DedupeHash = title };
        foreach (var keyword in keywords)
        {
            article.Keywords.Add(new ArticleKeyword { Keyword = new Keyword { Id = Guid.NewGuid(), Text = keyword } });
        }

        return article;
    }

    [Fact]
    public void Select_SkipsLowerRankedStoryAboutSameEvent()
    {
        var articles = new[]
        {
            MakeArticle("Brazilian markets crash", "Brazil", "Lula", "Election", "Stocks"),
            MakeArticle("Markets don't vote for Lula", "brazil", "lula", "election", "Currency"),
            MakeArticle("Gas prices hit high", "Natural Gas", "Europe", "LNG"),
        };

        var picked = TopStorySelector.Select(articles, 10);

        Assert.Equal(["Brazilian markets crash", "Gas prices hit high"], picked.Select(a => a.Title));
    }

    [Fact]
    public void Select_KeepsStoriesSharingOnlyGenericKeywords()
    {
        var articles = new[]
        {
            MakeArticle("Meta raises capital", "AI", "Stocks", "Meta", "Bonds"),
            MakeArticle("Nvidia earnings beat", "AI", "Stocks", "Nvidia", "Earnings"),
        };

        Assert.Equal(2, TopStorySelector.Select(articles, 10).Count);
    }

    [Fact]
    public void Select_StopsAtCount()
    {
        var articles = Enumerable.Range(0, 5).Select(i => MakeArticle($"Story {i}", $"k{i}")).ToList();

        Assert.Equal(3, TopStorySelector.Select(articles, 3).Count);
    }
}
