namespace StockLens.Application.Dtos;

public record TopSaleDto(
    Guid VehicleId,
    string Make,
    string Model,
    int Year,
    decimal SalePrice,
    DateOnly SoldDate,
    int DaysToSell);

public record MakeBreakdownDto(string Make, int Count, decimal StockValue);

public record DashboardSummaryDto(
    int TotalInStock,
    int AgingStockCount,
    decimal TotalStockValue,
    double AverageDaysInInventory,
    double? AverageDaysToSell,
    int SoldLast30Days,
    decimal RevenueLast30Days,
    IReadOnlyList<TopSaleDto> TopSales,
    IReadOnlyList<MakeBreakdownDto> StockByMake);
