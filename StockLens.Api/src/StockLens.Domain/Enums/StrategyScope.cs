namespace StockLens.Domain.Enums;

/// <summary>
/// The breadth a business strategy applies to. Resolution precedence when several
/// strategies match a vehicle is most-specific-first: Vehicle &gt; VehicleType &gt; Factory.
/// </summary>
public enum StrategyScope
{
    /// <summary>Applies to every vehicle of a make. ScopeKey = Make (e.g. "Toyota").</summary>
    Factory = 0,

    /// <summary>Applies to a make+model. ScopeKey = "Make|Model" (e.g. "Toyota|Corolla").</summary>
    VehicleType = 1,

    /// <summary>Applies to a single vehicle. ScopeKey = VehicleId.</summary>
    Vehicle = 2
}
