using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using StockLens.Application.Dtos;
using StockLens.Domain.Enums;

namespace StockLens.Api.Tests;

/// <summary>
/// Covers the vehicle status workflow: each transition's required evidence, the audit trail,
/// and the sale that selling a vehicle creates (and un-selling removes).
/// </summary>
[Collection(ApiCollection.Name)]
public class VehicleStatusApiTests : IAsyncLifetime
{
    private readonly StockLensApiFactory _factory;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public VehicleStatusApiTests(StockLensApiFactory factory) => _factory = factory;

    public Task InitializeAsync() => Task.CompletedTask;

    // Every test here creates a vehicle; reclaim them so the test DB stays clean.
    public Task DisposeAsync() => _factory.CleanupAsync();

    private async Task<VehicleDto> CreateVehicleAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/vehicles", new CreateVehicleRequest(
            StockLensApiFactory.NewTestVin(), "StatusCo", "Workflow", 2023, "Base", "Blue",
            1000, 30000, 25000, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-20)), Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<VehicleDto>(Json))!;
    }

    private async Task<Guid> FirstSalespersonIdAsync(HttpClient client)
    {
        var team = await client.GetFromJsonAsync<List<SalespersonDto>>("/api/salespeople?activeOnly=true", Json);
        return team!.First().Id;
    }

    [Fact]
    public async Task New_vehicle_starts_open()
    {
        var client = _factory.CreateClient();
        var vehicle = await CreateVehicleAsync(client);
        Assert.Equal(VehicleStatus.Open, vehicle.Status);
    }

    // --- Required evidence per transition ---------------------------------

    [Fact]
    public async Task Deposit_requires_amount_and_salesperson()
    {
        var client = _factory.CreateClient();
        var vehicle = await CreateVehicleAsync(client);

        var noEvidence = await client.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/status",
            new ChangeVehicleStatusRequest(VehicleStatus.Deposited), Json);
        Assert.Equal(HttpStatusCode.BadRequest, noEvidence.StatusCode);

        var noSalesperson = await client.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/status",
            new ChangeVehicleStatusRequest(VehicleStatus.Deposited, DepositAmount: 500), Json);
        Assert.Equal(HttpStatusCode.BadRequest, noSalesperson.StatusCode);

        var zeroDeposit = await client.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/status",
            new ChangeVehicleStatusRequest(VehicleStatus.Deposited, DepositAmount: 0,
                SalespersonId: await FirstSalespersonIdAsync(client)), Json);
        Assert.Equal(HttpStatusCode.BadRequest, zeroDeposit.StatusCode);
    }

    [Fact]
    public async Task Hold_requires_a_reason()
    {
        var client = _factory.CreateClient();
        var vehicle = await CreateVehicleAsync(client);

        var noReason = await client.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/status",
            new ChangeVehicleStatusRequest(VehicleStatus.Hold), Json);
        Assert.Equal(HttpStatusCode.BadRequest, noReason.StatusCode);

        var blankReason = await client.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/status",
            new ChangeVehicleStatusRequest(VehicleStatus.Hold, Reason: "   "), Json);
        Assert.Equal(HttpStatusCode.BadRequest, blankReason.StatusCode);
    }

    [Fact]
    public async Task Sold_requires_date_salesperson_and_price()
    {
        var client = _factory.CreateClient();
        var vehicle = await CreateVehicleAsync(client);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var nothing = await client.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/status",
            new ChangeVehicleStatusRequest(VehicleStatus.Sold), Json);
        Assert.Equal(HttpStatusCode.BadRequest, nothing.StatusCode);

        var noPrice = await client.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/status",
            new ChangeVehicleStatusRequest(VehicleStatus.Sold, SoldDate: today,
                SalespersonId: await FirstSalespersonIdAsync(client)), Json);
        Assert.Equal(HttpStatusCode.BadRequest, noPrice.StatusCode);

        var noSalesperson = await client.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/status",
            new ChangeVehicleStatusRequest(VehicleStatus.Sold, SoldDate: today, SalePrice: 29000), Json);
        Assert.Equal(HttpStatusCode.BadRequest, noSalesperson.StatusCode);
    }

    [Fact]
    public async Task Returning_to_open_requires_a_reason()
    {
        var client = _factory.CreateClient();
        var vehicle = await CreateVehicleAsync(client);

        await client.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/status",
            new ChangeVehicleStatusRequest(VehicleStatus.Hold, Reason: "Recon"), Json);

        var noReason = await client.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/status",
            new ChangeVehicleStatusRequest(VehicleStatus.Open), Json);
        Assert.Equal(HttpStatusCode.BadRequest, noReason.StatusCode);
    }

    // --- Happy paths -------------------------------------------------------

    [Fact]
    public async Task Deposit_stores_amount_and_salesperson_on_the_vehicle()
    {
        var client = _factory.CreateClient();
        var vehicle = await CreateVehicleAsync(client);
        var salespersonId = await FirstSalespersonIdAsync(client);

        var response = await client.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/status",
            new ChangeVehicleStatusRequest(VehicleStatus.Deposited,
                DepositAmount: 1500, SalespersonId: salespersonId), Json);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<VehicleDto>(Json);
        Assert.Equal(VehicleStatus.Deposited, updated!.Status);
        Assert.Equal(1500, updated.DepositAmount);
        Assert.False(string.IsNullOrWhiteSpace(updated.SalespersonName));
    }

    [Fact]
    public async Task Hold_clears_any_deposit_because_a_hold_is_not_a_customer_deal()
    {
        var client = _factory.CreateClient();
        var vehicle = await CreateVehicleAsync(client);
        var salespersonId = await FirstSalespersonIdAsync(client);

        await client.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/status",
            new ChangeVehicleStatusRequest(VehicleStatus.Deposited,
                DepositAmount: 1000, SalespersonId: salespersonId), Json);

        var response = await client.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/status",
            new ChangeVehicleStatusRequest(VehicleStatus.Hold, Reason: "Awaiting recon"), Json);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<VehicleDto>(Json);
        Assert.Equal(VehicleStatus.Hold, updated!.Status);
        Assert.Null(updated.DepositAmount);
    }

    [Fact]
    public async Task Selling_creates_a_sale_that_reaches_the_dashboard()
    {
        var client = _factory.CreateClient();
        var vehicle = await CreateVehicleAsync(client);
        var salespersonId = await FirstSalespersonIdAsync(client);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var before = await client.GetFromJsonAsync<DashboardSummaryDto>("/api/dashboard/summary", Json);

        var response = await client.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/status",
            new ChangeVehicleStatusRequest(VehicleStatus.Sold,
                SalePrice: 29500, SoldDate: today, SalespersonId: salespersonId), Json);
        response.EnsureSuccessStatusCode();

        var sold = await response.Content.ReadFromJsonAsync<VehicleDto>(Json);
        Assert.Equal(VehicleStatus.Sold, sold!.Status);
        Assert.Equal(today, sold.SoldDate);

        var after = await client.GetFromJsonAsync<DashboardSummaryDto>("/api/dashboard/summary", Json);
        Assert.Equal(before!.SoldLast30Days + 1, after!.SoldLast30Days);
        Assert.Equal(before.RevenueLast30Days + 29500, after.RevenueLast30Days);
    }

    [Fact]
    public async Task Un_selling_reverses_the_sale_so_revenue_is_not_left_inflated()
    {
        var client = _factory.CreateClient();
        var vehicle = await CreateVehicleAsync(client);
        var salespersonId = await FirstSalespersonIdAsync(client);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var before = await client.GetFromJsonAsync<DashboardSummaryDto>("/api/dashboard/summary", Json);

        await client.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/status",
            new ChangeVehicleStatusRequest(VehicleStatus.Sold,
                SalePrice: 31000, SoldDate: today, SalespersonId: salespersonId), Json);

        // Deal fell through.
        var reopened = await client.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/status",
            new ChangeVehicleStatusRequest(VehicleStatus.Open, Reason: "Finance declined"), Json);
        reopened.EnsureSuccessStatusCode();

        var vehicleAfter = await reopened.Content.ReadFromJsonAsync<VehicleDto>(Json);
        Assert.Equal(VehicleStatus.Open, vehicleAfter!.Status);
        Assert.Null(vehicleAfter.SoldDate);

        var after = await client.GetFromJsonAsync<DashboardSummaryDto>("/api/dashboard/summary", Json);
        Assert.Equal(before!.SoldLast30Days, after!.SoldLast30Days);
        Assert.Equal(before.RevenueLast30Days, after.RevenueLast30Days);
    }

    [Fact]
    public async Task Status_history_records_every_move_with_its_evidence()
    {
        var client = _factory.CreateClient();
        var vehicle = await CreateVehicleAsync(client);
        var salespersonId = await FirstSalespersonIdAsync(client);

        await client.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/status",
            new ChangeVehicleStatusRequest(VehicleStatus.Deposited,
                DepositAmount: 750, SalespersonId: salespersonId, ChangedBy: "An"), Json);
        await client.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/status",
            new ChangeVehicleStatusRequest(VehicleStatus.Hold, Reason: "Customer travelling"), Json);

        var history = await client.GetFromJsonAsync<List<VehicleStatusChangeDto>>(
            $"/api/vehicles/{vehicle.Id}/status-history", Json);

        Assert.Equal(2, history!.Count);

        // Newest first.
        Assert.Equal(VehicleStatus.Hold, history[0].ToStatus);
        Assert.Equal(VehicleStatus.Deposited, history[0].FromStatus);
        Assert.Equal("Customer travelling", history[0].Reason);

        Assert.Equal(VehicleStatus.Deposited, history[1].ToStatus);
        Assert.Equal(VehicleStatus.Open, history[1].FromStatus);
        Assert.Equal(750, history[1].DepositAmount);
        Assert.Equal("An", history[1].ChangedBy);
        Assert.False(string.IsNullOrWhiteSpace(history[1].SalespersonName));
    }

    [Fact]
    public async Task Moving_to_the_same_status_is_rejected()
    {
        var client = _factory.CreateClient();
        var vehicle = await CreateVehicleAsync(client);

        var response = await client.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/status",
            new ChangeVehicleStatusRequest(VehicleStatus.Open, Reason: "no-op"), Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_salesperson_is_rejected()
    {
        var client = _factory.CreateClient();
        var vehicle = await CreateVehicleAsync(client);

        var response = await client.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/status",
            new ChangeVehicleStatusRequest(VehicleStatus.Deposited,
                DepositAmount: 500, SalespersonId: Guid.NewGuid()), Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Status_change_on_unknown_vehicle_returns_not_found()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync($"/api/vehicles/{Guid.NewGuid()}/status",
            new ChangeVehicleStatusRequest(VehicleStatus.Hold, Reason: "x"), Json);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
