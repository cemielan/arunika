namespace Arunika.Domain.Entities;

public class Keyword
{
    public Guid Id { get; set; }
    public required string Text { get; set; }

    public ICollection<ArticleKeyword> Articles { get; set; } = new List<ArticleKeyword>();
}
