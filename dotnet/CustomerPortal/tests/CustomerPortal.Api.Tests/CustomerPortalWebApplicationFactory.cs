using CustomerPortal.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CustomerPortal.Api.Tests;

/// <summary>
/// Boots the API against an isolated in-memory SQLite database (one open
/// connection per factory instance) — the .NET equivalent of the Java test
/// suite's isolated in-memory H2 context.
/// </summary>
public class CustomerPortalWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public CustomerPortalWebApplicationFactory()
    {
        // Program.cs now requires Jwt:SigningKey to come from the environment
        // (never committed) — this is that environment for the test host,
        // which boots before ConfigureWebHost customizations are applied.
        Environment.SetEnvironmentVariable("Jwt__SigningKey", "test-only-signing-key-do-not-use-elsewhere");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<CustomerPortalDbContext>>();
            services.AddDbContext<CustomerPortalDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
