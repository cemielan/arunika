namespace Arunika.Domain.Entities;

public class ArticleKeyword
{
    public Guid ArticleId { get; set; }
    public Guid KeywordId { get; set; }

    public Article? Article { get; set; }
    public Keyword? Keyword { get; set; }
}
