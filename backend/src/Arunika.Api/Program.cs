using System.Reflection;
using System.Text;
using Arunika.Api.Contracts;
using Arunika.Api.Middleware;
using Arunika.Infrastructure;
using Arunika.Infrastructure.Auth;
using Arunika.Infrastructure.BackgroundJobs;
using Arunika.Infrastructure.Persistence;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// Add services to the container.

var supabaseSection = builder.Configuration.GetSection(SupabaseOptions.SectionName);
var supabaseOptions = supabaseSection.Get<SupabaseOptions>()
    ?? throw new InvalidOperationException("Missing 'Supabase' configuration section.");
if (string.IsNullOrWhiteSpace(supabaseOptions.Url))
{
    throw new InvalidOperationException(
        "Supabase:Url is not configured. Set it via the environment variable (Supabase__Url) " +
        "to your project URL from the Supabase dashboard under Settings -> API.");
}
builder.Services.Configure<SupabaseOptions>(supabaseSection);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Supabase signs tokens with asymmetric keys (RS256) and exposes its
        // public keys via OpenID discovery, so no shared secret is needed:
        // the authority's JWKS is fetched automatically for validation.
        options.Authority = $"{supabaseOptions.Url.TrimEnd('/')}/auth/v1";
        options.Audience = "authenticated";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

var frontendOrigin = builder.Configuration.GetValue<string>("Cors:FrontendOrigin") ?? "http://localhost:3000";
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(frontendOrigin)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.AddAuthorization();

builder.Services.AddControllers();

// Standardize the 400 (invalid model state) response shape to match the
// design doc §7 error envelope, same as the 404/500 shapes below.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var message = string.Join(" ", context.ModelState.Values
            .SelectMany(entry => entry.Errors)
            .Select(error => error.ErrorMessage));
        var envelope = new ApiErrorEnvelope(new ApiErrorDetail(
            "VALIDATION_ERROR",
            string.IsNullOrWhiteSpace(message) ? "Invalid request." : message));
        return new BadRequestObjectResult(envelope);
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Apply pending EF Core migrations automatically on every startup.
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ArunikaDbContext>();
        context.Database.Migrate();
        Log.Information("EF Core migrations applied successfully.");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to apply EF Core migrations on startup. " +
            "The app will start but the database schema may be out of date.");
    }
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // TODO(V2): gate behind auth (IDashboardAuthorizationFilter) before exposing outside Development.
    app.UseHangfireDashboard("/hangfire");
}

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }))
    .WithName("HealthCheck");

// Register recurring Hangfire jobs. On Supabase free tier (or any cold-start
// DB), the first connection may be slow; wrap in try-catch so the app still
// starts even if lock acquisition times out. The Hangfire server will retry
// automatically on its own polling schedule, and the jobs become registered
// on a subsequent successful connection.
using (var scope = app.Services.CreateScope())
{
    try
    {
        var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
        recurringJobManager.AddOrUpdate<FetchNewsJob>(
            "fetch-news",
            job => job.RunAsync(CancellationToken.None),
            "*/30 * * * *");
        recurringJobManager.AddOrUpdate<GenerateDailyBriefingJob>(
            "generate-daily-briefing",
            job => job.RunAsync(CancellationToken.None),
            "0 6 * * *");
        recurringJobManager.AddOrUpdate<CleanupOldArticlesJob>(
            "cleanup-old-articles",
            job => job.RunAsync(CancellationToken.None),
            "0 0 * * *");
        recurringJobManager.AddOrUpdate<RetryFailedEnrichmentJob>(
            "retry-failed-enrichment",
            job => job.RunAsync(CancellationToken.None),
            "*/10 * * * *");
        recurringJobManager.AddOrUpdate<SendEmailDigestJob>(
            "send-email-digest",
            job => job.RunAsync(CancellationToken.None),
            "0 8 * * *");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to register Hangfire recurring jobs on startup. " +
            "The jobs will be registered once the database is reachable.");
    }
}

app.Run();

// Exposed so Arunika.IntegrationTests can boot this app via WebApplicationFactory<Program>.
public partial class Program;
