using System.Reflection;
using Arunika.Api.Contracts;
using Arunika.Api.Middleware;
using Arunika.Infrastructure;
using Arunika.Infrastructure.BackgroundJobs;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
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
            "*/15 * * * *");
        recurringJobManager.AddOrUpdate<RetryFailedEnrichmentJob>(
            "retry-failed-enrichment",
            job => job.RunAsync(CancellationToken.None),
            "*/10 * * * *");
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
