using StockLens.Application.Dtos;

namespace StockLens.Application.Abstractions;

/// <summary>
/// Publishes real-time inventory changes to connected dashboards.
/// Implemented in the API layer over SignalR, so Application stays transport-agnostic.
/// </summary>
public interface IInventoryNotifier
{
    Task VehicleChangedAsync(VehicleDto vehicle, CancellationToken ct = default);
    Task ActionChangedAsync(VehicleActionDto action, CancellationToken ct = default);
    Task DashboardChangedAsync(DashboardSummaryDto summary, CancellationToken ct = default);
}
