using Microsoft.AspNetCore.SignalR;
using StockLens.Application.Abstractions;
using StockLens.Application.Dtos;

namespace StockLens.Api.Realtime;

/// <summary>Broadcasts inventory changes to all connected dashboards over SignalR.</summary>
public class SignalRInventoryNotifier : IInventoryNotifier
{
    private readonly IHubContext<InventoryHub> _hub;

    public SignalRInventoryNotifier(IHubContext<InventoryHub> hub) => _hub = hub;

    public Task VehicleChangedAsync(VehicleDto vehicle, CancellationToken ct = default) =>
        _hub.Clients.All.SendAsync(InventoryHub.VehicleChanged, vehicle, ct);

    public Task ActionChangedAsync(VehicleActionDto action, CancellationToken ct = default) =>
        _hub.Clients.All.SendAsync(InventoryHub.ActionChanged, action, ct);

    public Task DashboardChangedAsync(DashboardSummaryDto summary, CancellationToken ct = default) =>
        _hub.Clients.All.SendAsync(InventoryHub.DashboardChanged, summary, ct);
}
