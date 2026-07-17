using Microsoft.EntityFrameworkCore;
using StockLens.Application.Abstractions;
using StockLens.Application.Dtos;
using StockLens.Application.Mapping;
using StockLens.Domain.Common;
using StockLens.Domain.Enums;

namespace StockLens.Application.Services;

/// <summary>Builds the dashboard summary (KPIs, top sales, stock-by-make) from the store.</summary>
public class DashboardService
{
    private readonly IApplicationDbContext _db;

    public DashboardService(IApplicationDbContext db) => _db = db;

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var agingCutoff = today.AddDays(-InventoryPolicy.AgingThresholdDays);
        var last30 = today.AddDays(-30);

        var inStock = await _db.Vehicles
            .AsNoTracking()
            .Where(v => v.Status != VehicleStatus.Sold)
            .Select(v => new { v.AcquiredDate, v.ListPrice })
            .ToListAsync(ct);

        var totalInStock = inStock.Count;
        var agingCount = inStock.Count(v => v.AcquiredDate < agingCutoff);
        var totalValue = inStock.Sum(v => v.ListPrice);
        var avgDays = totalInStock == 0
            ? 0
            : inStock.Average(v => today.DayNumber - v.AcquiredDate.DayNumber);

        var recentSales = await _db.Sales
            .AsNoTracking()
            .Where(s => s.SoldDate >= last30)
            .ToListAsync(ct);

        var avgDaysToSell = await _db.Sales.AnyAsync(ct)
            ? await _db.Sales.AverageAsync(s => (double)s.DaysToSell, ct)
            : (double?)null;

        var topSales = await _db.Sales
            .AsNoTracking()
            .Include(s => s.Vehicle)
            .OrderByDescending(s => s.SalePrice)
            .Take(5)
            .ToListAsync(ct);

        // Group by make separately to keep the in-stock projection above lightweight.
        // Project into an anonymous type (EF-translatable) then map to the DTO in memory.
        var byMakeRaw = await _db.Vehicles
            .AsNoTracking()
            .Where(v => v.Status != VehicleStatus.Sold)
            .GroupBy(v => v.Make)
            .Select(g => new { Make = g.Key, Count = g.Count(), StockValue = g.Sum(v => v.ListPrice) })
            .OrderByDescending(m => m.Count)
            .ToListAsync(ct);
        var byMake = byMakeRaw
            .Select(m => new MakeBreakdownDto(m.Make, m.Count, m.StockValue))
            .ToList();

        return new DashboardSummaryDto(
            totalInStock,
            agingCount,
            totalValue,
            Math.Round(avgDays, 1),
            avgDaysToSell.HasValue ? Math.Round(avgDaysToSell.Value, 1) : null,
            recentSales.Count,
            recentSales.Sum(s => s.SalePrice),
            topSales.Select(s => s.ToTopSaleDto()).ToList(),
            byMake);
    }
}
