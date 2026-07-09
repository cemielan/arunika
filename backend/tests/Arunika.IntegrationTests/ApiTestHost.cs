using Arunika.Api.Controllers;
using Arunika.Application.Abstractions;
using Arunika.IntegrationTests.Fakes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Arunika.IntegrationTests;

/// <summary>
/// Mounts only the Phase 6 controllers (Arunika.Api's application part) on an
/// in-memory <see cref="TestServer"/>, backed by <see cref="FakeArticleRepository"/>.
/// Deliberately avoids booting the real <c>Program</c> host — that wires up
/// Hangfire + a real Postgres connection string at startup, which isn't
/// available/needed just to test routing, envelope shape, and serialization.
/// </summary>
public sealed class ApiTestHost : IAsyncLifetime
{
    private WebApplication? _app;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services
            .AddControllers()
            .AddApplicationPart(typeof(NewsController).Assembly);
        builder.Services.AddScoped<IArticleRepository, FakeArticleRepository>();

        _app = builder.Build();
        _app.MapControllers();

        await _app.StartAsync();
        Client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
