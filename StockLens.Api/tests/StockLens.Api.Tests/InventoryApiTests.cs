using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using StockLens.Application.Dtos;

namespace StockLens.Api.Tests;

/// <summary>
/// Integration tests that boot the real API (and thus the real EF pipeline) via
/// WebApplicationFactory. They require the Postgres container from docker-compose to be up.
/// </summary>
public class InventoryApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public InventoryApiTests(WebApplicationFactory<Program> factory) =>
        _factory = factory.WithWebHostBuilder(b => b.UseSetting("environment", "Development"));

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
        var vin = "IT" + Guid.NewGuid().ToString("N")[..14].ToUpperInvariant();

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
