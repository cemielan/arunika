using Arunika.Application.Abstractions;
using Arunika.Infrastructure.AI;
using Arunika.Infrastructure.BackgroundJobs;
using Arunika.Infrastructure.News.FinancialModelingPrep;
using Arunika.Infrastructure.News.Rss;
using Arunika.Infrastructure.Persistence;
using Arunika.Infrastructure.Persistence.Repositories;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        services.Configure<GeminiOptions>(configuration.GetSection(GeminiOptions.SectionName));
        services.AddSingleton<GeminiRateLimiter>();
        services.AddScoped<IAiEnrichmentService, GeminiAiEnrichmentService>();

        services.Configure<FinancialModelingPrepOptions>(configuration.GetSection(FinancialModelingPrepOptions.SectionName));
        services.AddHttpClient<INewsFetcher, FinancialModelingPrepNewsFetcher>((sp, client) =>
        {
            var baseUrl = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<FinancialModelingPrepOptions>>().Value.BaseUrl;
            client.BaseAddress = new Uri(baseUrl);
        });
        services.AddHttpClient<INewsFetcher, CnbcRssNewsFetcher>();

        services.AddScoped<FetchNewsJob>();
        services.AddScoped<EnrichArticleJob>();
        services.AddScoped<RetryFailedEnrichmentJob>();

        services.AddHangfire(hangfire => hangfire
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(pg => pg.UseNpgsqlConnection(connectionString)));
        services.AddHangfireServer();

        return services;
    }
}
