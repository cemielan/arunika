using Arunika.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Arunika.Infrastructure.Persistence;

public class ArunikaDbContext(DbContextOptions<ArunikaDbContext> options) : DbContext(options)
{
    public DbSet<NewsSource> NewsSources => Set<NewsSource>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<ArticleAnalysis> ArticleAnalyses => Set<ArticleAnalysis>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Sector> Sectors => Set<Sector>();
    public DbSet<ArticleSectorImpact> ArticleSectorImpacts => Set<ArticleSectorImpact>();
    public DbSet<Keyword> Keywords => Set<Keyword>();
    public DbSet<ArticleKeyword> ArticleKeywords => Set<ArticleKeyword>();
    public DbSet<Briefing> Briefings => Set<Briefing>();
    public DbSet<BriefingItem> BriefingItems => Set<BriefingItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Required for EF.Functions.TrigramsSimilarity, used by ArticleRepository's
        // near-duplicate title check (design doc §6, Phase 4).
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ArunikaDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
