using Arunika.Domain.Enums;

namespace Arunika.Domain.Entities;

public class NewsSource
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string BaseUrl { get; set; }
    public SourceType SourceType { get; set; }
    public int TrustScore { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Article> Articles { get; set; } = new List<Article>();
}
