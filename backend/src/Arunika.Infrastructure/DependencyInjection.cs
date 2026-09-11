using Arunika.Application.Abstractions;
using Arunika.Application.Services;
using Arunika.Infrastructure.AI;
using Arunika.Infrastructure.Auth;
using Arunika.Infrastructure.BackgroundJobs;
using Arunika.Infrastructure.Email;
using Arunika.Infrastructure.News.FinancialModelingPrep;
using Arunika.Infrastructure.News.Rss;
using Arunika.Infrastructure.Persistence;
using Arunika.Infrastructure.Persistence.Repositories;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arunika.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing connection string 'DefaultConnection'.");

        services.AddDbContext<ArunikaDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IArticleRepository, ArticleRepository>();
        services.AddScoped<INewsSourceRepository, NewsSourceRepository>();
        services.AddScoped<IArticleAnalysisRepository, ArticleAnalysisRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        services.Configure<SupabaseOptions>(configuration.GetSection(SupabaseOptions.SectionName));

        services.Configure<GeminiOptions>(configuration.GetSection(GeminiOptions.SectionName));
        // Singleton: the free-tier budget is per API key, so every enrichment
        // worker has to spend from one shared set of counters. The daily half of
        // those counters lives in Postgres so it also survives a redeploy and is
        // shared across instances.
        services.AddSingleton(new GeminiUsageStore(connectionString));
        services.AddSingleton<GeminiRateLimiter>();
        services.AddSingleton<GeminiCircuitBreaker>();
        services.AddSingleton<GeminiModelRotator>();
        services.AddScoped<GeminiAiEnrichmentService>();
        services.AddScoped<BriefingGenerationService>();

        services.Configure<OpenRouterOptions>(configuration.GetSection(OpenRouterOptions.SectionName));
        services.AddScoped<OpenRouterAiEnrichmentService>();
        services.AddScoped<OpenRouterBriefingGenerationService>();

        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));

        services.AddScoped<IAiEnrichmentService>(sp =>
        {
            var gemini = sp.GetRequiredService<GeminiAiEnrichmentService>();
            var openRouter = sp.GetRequiredService<OpenRouterAiEnrichmentService>();
            var aiOptions = sp.GetRequiredService<IOptions<AiOptions>>().Value;
            var openRouterFirst = string.Equals(aiOptions.PreferredProvider, "OpenRouter", StringComparison.OrdinalIgnoreCase);
            IAiEnrichmentService[] ordered = openRouterFirst ? [openRouter, gemini] : [gemini, openRouter];
            var logger = sp.GetRequiredService<ILogger<CompositeAiEnrichmentService>>();
            return new CompositeAiEnrichmentService(ordered, logger);
        });

        services.AddScoped<DailyBriefingService>();

        services.AddScoped<IBriefingGenerationService>(sp =>
        {
            var gemini = sp.GetRequiredService<BriefingGenerationService>();
            var openRouter = sp.GetRequiredService<OpenRouterBriefingGenerationService>();
            var aiOptions = sp.GetRequiredService<IOptions<AiOptions>>().Value;
            var openRouterFirst = string.Equals(aiOptions.PreferredProvider, "OpenRouter", StringComparison.OrdinalIgnoreCase);
            IBriefingGenerationService[] ordered = openRouterFirst ? [openRouter, gemini] : [gemini, openRouter];
            var logger = sp.GetRequiredService<ILogger<CompositeBriefingGenerationService>>();
            return new CompositeBriefingGenerationService(ordered, logger);
        });

        services.AddHttpClient("openrouter", (sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<OpenRouterOptions>>().Value;
            client.BaseAddress = new Uri(opts.BaseUrl);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {opts.ApiKey}");
            client.DefaultRequestHeaders.Add("HTTP-Referer", "https://github.com/arunika");
            client.DefaultRequestHeaders.Add("X-Title", "Arunika");
        });

        services.Configure<FinancialModelingPrepOptions>(configuration.GetSection(FinancialModelingPrepOptions.SectionName));
        services.AddHttpClient<INewsFetcher, FinancialModelingPrepNewsFetcher>((sp, client) =>
        {
            var baseUrl = sp.GetRequiredService<IOptions<FinancialModelingPrepOptions>>().Value.BaseUrl;
            client.BaseAddress = new Uri(baseUrl);
        });

        foreach (var (sourceName, feedUrl) in RssFeeds)
        {
            RegisterRssFeed(services, sourceName, feedUrl);
        }

        services.AddScoped<FetchNewsJob>();
        services.AddScoped<EnrichArticleJob>();
        services.AddScoped<RetryFailedEnrichmentJob>();
        services.AddScoped<CleanupOldArticlesJob>();
        services.AddScoped<GenerateDailyBriefingJob>();
        services.AddScoped<SendEmailDigestJob>();
        services.AddHostedService<RecurringJobRegistrationService>();

        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.AddHttpClient<EdgeFunctionEmailService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddScoped<IEmailService>(sp => sp.GetRequiredService<EdgeFunctionEmailService>());

        services.AddHangfire(hangfire => hangfire
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(pg => pg.UseNpgsqlConnection(connectionString)));
        services.AddHangfireServer(options =>
        {
            options.Queues = ["fetch", "default", "enrichment"];
            options.WorkerCount = Environment.ProcessorCount > 2 ? 2 : 1;
        });

        return services;
    }

    /// <summary>
    /// The free RSS feeds Arunika polls. Each <c>SourceName</c> must match a
    /// <c>news_sources.Name</c> seeded in <see cref="Persistence.Configurations.NewsSourceConfiguration"/>
    /// — <see cref="BackgroundJobs.FetchNewsJob"/> looks the source up by name and
    /// skips the feed entirely when no row matches, which fails silently.
    /// RssFeedSeedingTests guards that pairing.
    /// </summary>
    public static readonly (string SourceName, string FeedUrl)[] RssFeeds =
    [
        // Ordered by the TrustScore seeded in NewsSourceConfiguration, highest first.
        ("Federal Reserve Press Releases", "https://www.federalreserve.gov/feeds/press_all.xml"),
        ("WSJ Markets", "https://feeds.content.dowjones.io/public/rss/RSSMarketsMain"),
        ("BBC Business", "https://feeds.bbci.co.uk/news/business/rss.xml"),
        ("Antara Ekonomi", "https://www.antaranews.com/rss/ekonomi.xml"),
        ("CNBC", "https://www.cnbc.com/id/100003114/device/rss/rss.html"),
        ("MarketWatch", "https://feeds.marketwatch.com/marketwatch/topstories"),
        ("CNBC Indonesia Market", "https://www.cnbcindonesia.com/market/rss"),
        ("Kontan Investasi", "https://investasi.kontan.co.id/rss"),
        ("Detik Finance", "https://finance.detik.com/rss"),
        // Dropped sources, all still seeded but inactive so their existing
        // articles keep a valid SourceId: Reuters Business/Markets News (82 —
        // feeds.reuters.com was retired and no longer resolves), Yahoo Finance
        // (70) and GDELT (60) as the lowest-trust feeds, and Nasdaq Markets
        // (70), Investing.com (65) and Seeking Alpha (60), which were never
        // seeded at all.
    ];

    private static void RegisterRssFeed(IServiceCollection services, string sourceName, string feedUrl)
    {
        var clientName = $"rss-{sourceName.ToLowerInvariant().Replace(" ", "-")}";
        services.AddHttpClient(clientName, client =>
        {
            // Some publishers (federalreserve.gov among them) answer 404 to a
            // request with no User-Agent, so every RSS client identifies itself.
            client.DefaultRequestHeaders.UserAgent.ParseAdd("ArunikaNewsBot/1.0 (+https://github.com/Kadmiel/arunika)");
            client.Timeout = TimeSpan.FromSeconds(20);
        });
        services.AddTransient<INewsFetcher>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(clientName);
            var logger = sp.GetRequiredService<ILogger<RssNewsFetcher>>();
            return new RssNewsFetcher(sourceName, feedUrl, httpClient, logger);
        });
    }
}
