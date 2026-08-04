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
        services.AddSingleton<GeminiRateLimiter>();
        services.AddScoped<GeminiAiEnrichmentService>();
        services.AddScoped<BriefingGenerationService>();

        services.Configure<OpenRouterOptions>(configuration.GetSection(OpenRouterOptions.SectionName));
        services.AddScoped<OpenRouterAiEnrichmentService>();
        services.AddScoped<OpenRouterBriefingGenerationService>();

        services.AddScoped<IAiEnrichmentService>(sp =>
        {
            var gemini = sp.GetRequiredService<GeminiAiEnrichmentService>();
            var openRouter = sp.GetRequiredService<OpenRouterAiEnrichmentService>();
            var logger = sp.GetRequiredService<ILogger<CompositeAiEnrichmentService>>();
            return new CompositeAiEnrichmentService([gemini, openRouter], logger);
        });

        services.AddScoped<DailyBriefingService>();

        services.AddScoped<IBriefingGenerationService>(sp =>
        {
            var gemini = sp.GetRequiredService<BriefingGenerationService>();
            var openRouter = sp.GetRequiredService<OpenRouterBriefingGenerationService>();
            var logger = sp.GetRequiredService<ILogger<CompositeBriefingGenerationService>>();
            return new CompositeBriefingGenerationService([gemini, openRouter], logger);
        });

        services.AddHttpClient("openrouter", (sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<OpenRouterOptions>>().Value;
            client.BaseAddress = new Uri(opts.BaseUrl);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {opts.ApiKey}");
        });

        services.Configure<FinancialModelingPrepOptions>(configuration.GetSection(FinancialModelingPrepOptions.SectionName));
        services.AddHttpClient<INewsFetcher, FinancialModelingPrepNewsFetcher>((sp, client) =>
        {
            var baseUrl = sp.GetRequiredService<IOptions<FinancialModelingPrepOptions>>().Value.BaseUrl;
            client.BaseAddress = new Uri(baseUrl);
        });

        RegisterRssFeed(services, "CNBC", "https://www.cnbc.com/id/100003114/device/rss/rss.html");
        RegisterRssFeed(services, "MarketWatch", "https://feeds.marketwatch.com/marketwatch/topstories");
        RegisterRssFeed(services, "Yahoo Finance", "https://finance.yahoo.com/news/rssindex");

        services.AddScoped<FetchNewsJob>();
        services.AddScoped<EnrichArticleJob>();
        services.AddScoped<RetryFailedEnrichmentJob>();
        services.AddScoped<CleanupOldArticlesJob>();
        services.AddScoped<GenerateDailyBriefingJob>();
        services.AddScoped<SendEmailDigestJob>();
        services.AddHostedService<RecurringJobRegistrationService>();

        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.AddHttpClient<BrevoEmailService>(client =>
        {
            client.BaseAddress = new Uri("https://api.brevo.com/v3/");
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddScoped<IEmailService>(sp => sp.GetRequiredService<BrevoEmailService>());

        services.AddHangfire(hangfire => hangfire
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(pg => pg.UseNpgsqlConnection(connectionString)));
        services.AddHangfireServer();

        return services;
    }

    private static void RegisterRssFeed(IServiceCollection services, string sourceName, string feedUrl)
    {
        var clientName = $"rss-{sourceName.ToLowerInvariant().Replace(" ", "-")}";
        services.AddHttpClient(clientName);
        services.AddTransient<INewsFetcher>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(clientName);
            var logger = sp.GetRequiredService<ILogger<RssNewsFetcher>>();
            return new RssNewsFetcher(sourceName, feedUrl, httpClient, logger);
        });
    }
}
