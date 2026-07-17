using Microsoft.AspNetCore.SignalR;

namespace StockLens.Api.Realtime;

/// <summary>
/// SignalR hub the Angular dashboard connects to for live inventory updates.
/// The server pushes messages; clients don't invoke hub methods directly.
/// </summary>
public class InventoryHub : Hub
{
    public const string VehicleChanged = "VehicleChanged";
    public const string ActionChanged = "ActionChanged";
    public const string DashboardChanged = "DashboardChanged";
}
