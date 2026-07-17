using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StockLens.Infrastructure.Persistence;

namespace StockLens.Api.Tests;

/// <summary>
/// Boots the real API against an isolated <c>stocklens_test</c> database rather than the
/// development database, so test runs never pollute local/demo inventory. The database is
/// created and seeded on first use by the API's own startup migration + seeding path.
/// </summary>
/// <remarks>
/// Requires the Postgres container from docker-compose to be running. Override the target
/// with the <c>STOCKLENS_TEST_CONNECTION</c> environment variable (e.g. in CI).
/// </remarks>
public class StockLensApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string DefaultTestConnection =
        "Host=localhost;Port=5433;Database=stocklens_test;Username=stocklens;Password=stocklens";

    /// <summary>VIN prefix that marks a row as test-created and therefore safe to remove.</summary>
    public const string TestVinPrefix = "ITEST";

    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("STOCKLENS_TEST_CONNECTION") ?? DefaultTestConnection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("environment", "Development");
        builder.UseSetting("ConnectionStrings:StockLens", ConnectionString);
    }

    /// <summary>Creates a unique VIN that <see cref="CleanupAsync"/> will reclaim.</summary>
    public static string NewTestVin() => TestVinPrefix + Guid.NewGuid().ToString("N")[..11].ToUpperInvariant();

    /// <summary>Start from a known state even if a previous run was interrupted.</summary>
    async Task IAsyncLifetime.InitializeAsync()
    {
        _ = CreateClient(); // forces host start: applies migrations and seeds the test database
        await CleanupAsync();
    }

    /// <summary>
    /// Removes every row this suite created. Actions and sales cascade from the vehicle,
    /// so deleting the vehicle is sufficient.
    /// </summary>
    public async Task CleanupAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var created = await db.Vehicles
            .Where(v => v.Vin.StartsWith(TestVinPrefix))
            .ToListAsync();

        if (created.Count == 0) return;

        db.Vehicles.RemoveRange(created);
        await db.SaveChangesAsync();
    }

    // Explicit implementation: WebApplicationFactory already exposes a DisposeAsync()
    // returning ValueTask, which would otherwise collide with IAsyncLifetime's Task version.
    async Task IAsyncLifetime.DisposeAsync()
    {
        await CleanupAsync();
        await base.DisposeAsync();
    }
}
