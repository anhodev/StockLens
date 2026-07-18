using System.Globalization;
using Microsoft.EntityFrameworkCore;
using StockLens.Application.Abstractions;
using StockLens.Application.Dtos;
using StockLens.Application.Mapping;
using StockLens.Domain.Common;
using StockLens.Domain.Enums;

namespace StockLens.Application.Services;

/// <summary>Builds the dashboard summary (KPIs, top sales, stock-by-make, sales trend).</summary>
public class DashboardService
{
    /// <summary>Number of calendar months shown on the sales performance chart.</summary>
    private const int TrendMonths = 6;

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
            .Include(s => s.Salesperson)
            .OrderByDescending(s => s.SalePrice)
            .Take(5)
            .ToListAsync(ct);

        var salesTrend = await GetSalesTrendAsync(today, ct);

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
            byMake,
            salesTrend);
    }

    /// <summary>
    /// Units and revenue per calendar month over the trailing window, oldest first.
    /// Months with no sales are returned as zeroes so the chart keeps an even x-axis.
    /// </summary>
    private async Task<IReadOnlyList<SalesTrendPointDto>> GetSalesTrendAsync(
        DateOnly today, CancellationToken ct)
    {
        var firstMonth = new DateOnly(today.Year, today.Month, 1).AddMonths(-(TrendMonths - 1));

        var raw = await _db.Sales
            .AsNoTracking()
            .Where(s => s.SoldDate >= firstMonth)
            .GroupBy(s => new { s.SoldDate.Year, s.SoldDate.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Units = g.Count(),
                Revenue = g.Sum(s => s.SalePrice),
            })
            .ToListAsync(ct);

        var byMonth = raw.ToDictionary(r => (r.Year, r.Month));

        return Enumerable.Range(0, TrendMonths)
            .Select(offset =>
            {
                var month = firstMonth.AddMonths(offset);
                byMonth.TryGetValue((month.Year, month.Month), out var hit);
                return new SalesTrendPointDto(
                    month,
                    month.ToString("MMM", CultureInfo.InvariantCulture),
                    hit?.Units ?? 0,
                    hit?.Revenue ?? 0m);
            })
            .ToList();
    }
}
