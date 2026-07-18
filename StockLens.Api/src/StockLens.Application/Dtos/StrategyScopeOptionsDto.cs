namespace StockLens.Application.Dtos;

/// <summary>Lightweight vehicle summary used to populate the strategy scope-key picker.</summary>
public record VehicleSummaryDto(string Id, string Vin, string Make, string Model, int Year);

/// <summary>All possible scope-key values grouped by scope level, for the strategy form dropdowns.</summary>
public record StrategyScopeOptionsDto(
    IReadOnlyList<string> Factories,
    IReadOnlyList<string> VehicleTypes,
    IReadOnlyList<VehicleSummaryDto> Vehicles
);
