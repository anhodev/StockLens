using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockLens.Application.Abstractions;
using StockLens.Application.Dtos;
using StockLens.Application.Mapping;
using StockLens.Domain.Entities;
using StockLens.Domain.Enums;

namespace StockLens.Application.Services;

public enum StatusChangeOutcome
{
    Success,
    VehicleNotFound,
    SalespersonNotFound,
    AlreadyInStatus,
}

public record StatusChangeResult(StatusChangeOutcome Outcome, VehicleDto? Vehicle = null, string? Message = null);

/// <summary>
/// Applies vehicle status transitions and records why each one happened.
/// </summary>
/// <remarks>
/// Selling a vehicle creates the matching <see cref="SalesRecord"/> so the sale reaches the
/// dashboard; moving a vehicle back out of Sold removes it again, otherwise a cancelled deal
/// would keep inflating revenue and the sales trend forever.
/// </remarks>
public class VehicleStatusService
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<VehicleStatusService> _logger;

    public VehicleStatusService(IApplicationDbContext db, ILogger<VehicleStatusService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<StatusChangeResult> ChangeStatusAsync(
        Guid vehicleId, ChangeVehicleStatusRequest req, CancellationToken ct = default)
    {
        var vehicle = await _db.Vehicles
            .Include(v => v.Actions)
            .Include(v => v.Salesperson)
            .FirstOrDefaultAsync(v => v.Id == vehicleId, ct);

        if (vehicle is null)
            return new StatusChangeResult(StatusChangeOutcome.VehicleNotFound);

        if (vehicle.Status == req.ToStatus)
            return new StatusChangeResult(
                StatusChangeOutcome.AlreadyInStatus,
                Message: $"Vehicle is already {req.ToStatus}.");

        if (req.SalespersonId is { } salespersonId &&
            !await _db.Salespeople.AnyAsync(s => s.Id == salespersonId, ct))
        {
            return new StatusChangeResult(
                StatusChangeOutcome.SalespersonNotFound,
                Message: "The selected salesperson does not exist.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromStatus = vehicle.Status;

        // A vehicle leaving Sold means the deal came undone: drop the sale so revenue,
        // top sales and the trend stop counting it.
        if (fromStatus == VehicleStatus.Sold && req.ToStatus != VehicleStatus.Sold)
        {
            var reversed = await _db.Sales.Where(s => s.VehicleId == vehicle.Id).ToListAsync(ct);
            if (reversed.Count > 0)
            {
                _db.Sales.RemoveRange(reversed);
                _logger.LogInformation(
                    "Vehicle {VehicleId} left Sold; reversed {SaleCount} sale(s)", vehicle.Id, reversed.Count);
            }
        }

        ApplyStatus(vehicle, req, today);

        if (req.ToStatus == VehicleStatus.Sold)
        {
            var soldDate = req.SoldDate!.Value;
            _db.Sales.Add(new SalesRecord
            {
                VehicleId = vehicle.Id,
                SalespersonId = req.SalespersonId!.Value,
                SalePrice = req.SalePrice!.Value,
                SoldDate = soldDate,
                DaysToSell = Math.Max(0, soldDate.DayNumber - vehicle.AcquiredDate.DayNumber),
            });
        }

        _db.VehicleStatusChanges.Add(new VehicleStatusChange
        {
            VehicleId = vehicle.Id,
            FromStatus = fromStatus,
            ToStatus = req.ToStatus,
            Reason = string.IsNullOrWhiteSpace(req.Reason) ? null : req.Reason.Trim(),
            DepositAmount = req.ToStatus == VehicleStatus.Deposited ? req.DepositAmount : null,
            SalePrice = req.ToStatus == VehicleStatus.Sold ? req.SalePrice : null,
            SalespersonId = req.SalespersonId,
            EffectiveDate = req.ToStatus == VehicleStatus.Sold ? req.SoldDate!.Value : today,
            ChangedBy = string.IsNullOrWhiteSpace(req.ChangedBy) ? "manager" : req.ChangedBy.Trim(),
        });

        vehicle.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Vehicle {VehicleId} status {FromStatus} -> {ToStatus} by {ChangedBy}",
            vehicle.Id, fromStatus, req.ToStatus, req.ChangedBy ?? "manager");

        // Re-read the salesperson so the returned DTO carries the new name.
        if (vehicle.SalespersonId is { } id)
            vehicle.Salesperson = await _db.Salespeople.FirstOrDefaultAsync(s => s.Id == id, ct);
        else
            vehicle.Salesperson = null;

        return new StatusChangeResult(StatusChangeOutcome.Success, vehicle.ToDto(today));
    }

    /// <summary>
    /// Sets the vehicle's own state for the target status. Only Deposited carries a deposit,
    /// and only Sold carries a sold date, so stale values are cleared on every other move.
    /// </summary>
    private static void ApplyStatus(Vehicle vehicle, ChangeVehicleStatusRequest req, DateOnly today)
    {
        vehicle.Status = req.ToStatus;

        switch (req.ToStatus)
        {
            case VehicleStatus.Deposited:
                vehicle.DepositAmount = req.DepositAmount;
                vehicle.SalespersonId = req.SalespersonId;
                vehicle.SoldDate = null;
                break;

            case VehicleStatus.Sold:
                vehicle.DepositAmount = null; // rolled into the sale
                vehicle.SalespersonId = req.SalespersonId;
                vehicle.SoldDate = req.SoldDate;
                break;

            case VehicleStatus.Hold:
                // A hold is a dealership decision, not a customer deal.
                vehicle.DepositAmount = null;
                vehicle.SalespersonId = null;
                vehicle.SoldDate = null;
                break;

            case VehicleStatus.Open:
                vehicle.DepositAmount = null;
                vehicle.SalespersonId = null;
                vehicle.SoldDate = null;
                break;
        }
    }

    public async Task<IReadOnlyList<VehicleStatusChangeDto>> GetHistoryAsync(
        Guid vehicleId, CancellationToken ct = default)
    {
        return await _db.VehicleStatusChanges
            .AsNoTracking()
            .Where(c => c.VehicleId == vehicleId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new VehicleStatusChangeDto(
                c.Id, c.VehicleId, c.FromStatus, c.ToStatus, c.Reason,
                c.DepositAmount, c.SalePrice,
                c.Salesperson != null ? c.Salesperson.FullName : null,
                c.EffectiveDate, c.ChangedBy, c.CreatedAt))
            .ToListAsync(ct);
    }
}
