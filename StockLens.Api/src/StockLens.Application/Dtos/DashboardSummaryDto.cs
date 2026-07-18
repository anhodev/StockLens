namespace StockLens.Application.Dtos;

public record TopSaleDto(
    Guid VehicleId,
    string Make,
    string Model,
    int Year,
    decimal SalePrice,
    DateOnly SoldDate,
    int DaysToSell,
    string SoldBy);

public record MakeBreakdownDto(string Make, int Count, decimal StockValue);

/// <summary>One month of sales performance, oldest first.</summary>
public record SalesTrendPointDto(
    /// <summary>First day of the month the point covers.</summary>
    DateOnly Month,
    /// <summary>Short display label, e.g. "Feb".</summary>
    string Label,
    int Units,
    decimal Revenue);

public record DashboardSummaryDto(
    int TotalInStock,
    int AgingStockCount,
    decimal TotalStockValue,
    double AverageDaysInInventory,
    double? AverageDaysToSell,
    int SoldLast30Days,
    decimal RevenueLast30Days,
    IReadOnlyList<TopSaleDto> TopSales,
    IReadOnlyList<MakeBreakdownDto> StockByMake,
    IReadOnlyList<SalesTrendPointDto> SalesTrend);
