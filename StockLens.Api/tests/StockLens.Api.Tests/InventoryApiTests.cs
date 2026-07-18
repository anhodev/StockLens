using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using StockLens.Application.Dtos;

namespace StockLens.Api.Tests;

/// <summary>
/// Integration tests that boot the real API (and thus the real EF pipeline) against an
/// isolated test database. They require the Postgres container from docker-compose to be up.
/// </summary>
[Collection(ApiCollection.Name)]
public class InventoryApiTests
{
    private readonly StockLensApiFactory _factory;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public InventoryApiTests(StockLensApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Dashboard_summary_returns_populated_data()
    {
        var client = _factory.CreateClient();

        var summary = await client.GetFromJsonAsync<DashboardSummaryDto>("/api/dashboard/summary", Json);

        Assert.NotNull(summary);
        Assert.True(summary!.TotalInStock > 0);
        Assert.NotEmpty(summary.TopSales);
        Assert.NotEmpty(summary.StockByMake);
    }

    [Fact]
    public async Task Dashboard_sales_trend_covers_a_continuous_month_window()
    {
        var client = _factory.CreateClient();

        var summary = await client.GetFromJsonAsync<DashboardSummaryDto>("/api/dashboard/summary", Json);

        var trend = summary!.SalesTrend;
        Assert.NotEmpty(trend);
        // Oldest first, one point per calendar month, no gaps — the chart depends on it.
        Assert.Equal(trend.OrderBy(p => p.Month).Select(p => p.Month), trend.Select(p => p.Month));
        for (var i = 1; i < trend.Count; i++)
            Assert.Equal(trend[i - 1].Month.AddMonths(1), trend[i].Month);

        Assert.All(trend, p => Assert.True(p.Units >= 0 && p.Revenue >= 0));
        Assert.Contains(trend, p => p.Units > 0); // seeded history must produce real sales
    }

    [Fact]
    public async Task Top_sales_are_attributed_to_a_salesperson()
    {
        var client = _factory.CreateClient();

        var summary = await client.GetFromJsonAsync<DashboardSummaryDto>("/api/dashboard/summary", Json);

        Assert.NotEmpty(summary!.TopSales);
        Assert.All(summary.TopSales, s => Assert.False(string.IsNullOrWhiteSpace(s.SoldBy)));
        // Highest value first.
        Assert.Equal(summary.TopSales.OrderByDescending(s => s.SalePrice).Select(s => s.SalePrice),
                     summary.TopSales.Select(s => s.SalePrice));
    }

    [Fact]
    public async Task Salespeople_endpoint_returns_seeded_team_with_totals()
    {
        var client = _factory.CreateClient();

        var team = await client.GetFromJsonAsync<List<SalespersonDto>>("/api/salespeople?activeOnly=true", Json);

        Assert.NotNull(team);
        Assert.NotEmpty(team!);
        Assert.All(team, p => Assert.True(p.IsActive));
        Assert.All(team, p => Assert.False(string.IsNullOrWhiteSpace(p.FullName)));
        // Seeded sales are distributed across the team, so someone must have sold something.
        Assert.Contains(team, p => p.SalesCount > 0 && p.Revenue > 0);
    }

    [Fact]
    public async Task Aging_endpoint_returns_only_aging_vehicles()
    {
        var client = _factory.CreateClient();

        var aging = await client.GetFromJsonAsync<List<VehicleDto>>("/api/vehicles/aging", Json);

        Assert.NotNull(aging);
        Assert.All(aging!, v => Assert.True(v.IsAgingStock));
        Assert.All(aging!, v => Assert.True(v.DaysInInventory > 90));
    }

    [Fact]
    public async Task Create_vehicle_then_log_action_persists_and_links()
    {
        var client = _factory.CreateClient();
        var vin = StockLensApiFactory.NewTestVin();

        try
        {
            var create = await client.PostAsJsonAsync("/api/vehicles", new CreateVehicleRequest(
                vin, "IntegrationCo", "TestModel", 2023, "Base", "Blue", 1000, 20000, 17000,
                DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-10)), Json);
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);

            var vehicle = await create.Content.ReadFromJsonAsync<VehicleDto>(Json);
            Assert.NotNull(vehicle);

            var action = await client.PostAsJsonAsync($"/api/vehicles/{vehicle!.Id}/actions",
                new CreateActionRequest(Domain.Enums.ActionType.PriceReductionPlanned,
                    Domain.Enums.ActionStatus.Open, "Test action", "tester"), Json);
            Assert.Equal(HttpStatusCode.Created, action.StatusCode);

            var actions = await client.GetFromJsonAsync<List<VehicleActionDto>>(
                $"/api/vehicles/{vehicle.Id}/actions", Json);
            Assert.Single(actions!);
            Assert.Equal("Test action", actions![0].Note);
        }
        finally
        {
            // Clean up even when an assertion above fails, so a red test leaves no residue.
            await _factory.CleanupAsync();
        }
    }

    [Theory]
    // A vehicle reads as "{Year} {Make} {Model}" on screen, so searching that phrase must work.
    [InlineData("2020 Ford Escape")]
    [InlineData("ford escape")]      // case-insensitive
    [InlineData("escape ford")]      // term order must not matter
    [InlineData("  ford   escape ")] // extra whitespace tolerated
    [InlineData("Escape")]
    [InlineData("2020")]             // year-only term
    public async Task Search_matches_across_fields(string term)
    {
        var client = _factory.CreateClient();

        var page = await client.GetFromJsonAsync<PagedResult<VehicleDto>>(
            $"/api/vehicles?search={Uri.EscapeDataString(term)}&pageSize=100", Json);

        Assert.NotNull(page);
        Assert.Contains(page!.Items, v => v is { Year: 2020, Make: "Ford", Model: "Escape" });
    }

    [Fact]
    public async Task Search_terms_narrow_results_rather_than_widen_them()
    {
        var client = _factory.CreateClient();

        var ford = await client.GetFromJsonAsync<PagedResult<VehicleDto>>(
            "/api/vehicles?search=ford&pageSize=100", Json);
        var fordEscape = await client.GetFromJsonAsync<PagedResult<VehicleDto>>(
            "/api/vehicles?search=ford%20escape&pageSize=100", Json);

        // Every term must match (AND), so adding "escape" can only shrink the "ford" set.
        Assert.True(fordEscape!.Total <= ford!.Total);
        Assert.All(fordEscape.Items, v => Assert.Equal("Escape", v.Model));
    }

    [Fact]
    public async Task Search_with_no_match_returns_empty()
    {
        var client = _factory.CreateClient();

        var page = await client.GetFromJsonAsync<PagedResult<VehicleDto>>(
            "/api/vehicles?search=ford%20lamborghini&pageSize=100", Json);

        Assert.Empty(page!.Items);
    }

    [Fact]
    public async Task Search_treats_like_wildcards_literally()
    {
        var client = _factory.CreateClient();

        // "%" must not act as a wildcard that matches everything.
        var page = await client.GetFromJsonAsync<PagedResult<VehicleDto>>(
            "/api/vehicles?search=%25&pageSize=100", Json);

        Assert.Empty(page!.Items);
    }

    [Fact]
    public async Task Invalid_vehicle_returns_validation_problem()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/vehicles", new CreateVehicleRequest(
            "X", "", "", 3000, null, null, -1, -1, 0,
            DateOnly.FromDateTime(DateTime.UtcNow)), Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
