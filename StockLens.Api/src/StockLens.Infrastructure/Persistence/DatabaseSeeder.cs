using Microsoft.EntityFrameworkCore;
using StockLens.Domain.Entities;
using StockLens.Domain.Enums;

namespace StockLens.Infrastructure.Persistence;

/// <summary>Seeds a representative inventory so the dashboard is populated on first run.</summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, CancellationToken ct = default)
    {
        if (await db.Vehicles.AnyAsync(ct))
            return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var rnd = new Random(42);

        // --- In-stock vehicles, spread across ages so some cross the 90-day aging line. ---
        var specs = new (string Make, string Model, int Year, string Trim, string Color, int Ageeeed, decimal List, decimal Cost, int Miles)[]
        {
            ("Toyota",   "Corolla",  2023, "LE",       "White",  12,  22500, 19000, 8200),
            ("Toyota",   "RAV4",     2022, "XLE",      "Silver", 47,  31000, 26500, 21000),
            ("Toyota",   "Camry",    2021, "SE",       "Black",  118, 26000, 22000, 34000),
            ("Honda",    "Civic",    2023, "Sport",    "Blue",   8,   26500, 22800, 5400),
            ("Honda",    "CR-V",     2020, "EX",       "Grey",   135, 24000, 20500, 52000),
            ("Honda",    "Accord",   2022, "Touring",  "White",  73,  33000, 28500, 18000),
            ("Ford",     "F-150",    2021, "Lariat",   "Red",    156, 42000, 36000, 41000),
            ("Ford",     "Explorer", 2022, "XLT",      "Black",  61,  38000, 33000, 27000),
            ("Ford",     "Escape",   2020, "SE",       "Blue",   201, 19500, 16500, 63000),
            ("BMW",      "3 Series", 2021, "330i",     "Black",  99,  38500, 33000, 29000),
            ("BMW",      "X5",       2022, "xDrive40i","White",  33,  61000, 54000, 15000),
            ("Tesla",    "Model 3",  2023, "Long Range","White", 5,   41000, 37000, 3200),
            ("Tesla",    "Model Y",  2022, "Performance","Grey", 88,  52000, 46000, 12000),
            ("Chevrolet","Silverado",2021, "LT",       "Silver", 172, 39000, 34000, 38000),
            ("Chevrolet","Equinox",  2023, "LT",       "Red",    26,  27000, 23500, 9000),
            ("Hyundai",  "Tucson",   2022, "SEL",      "Blue",   112, 28000, 24000, 20000),
            ("Hyundai",  "Elantra",  2023, "SEL",      "White",  40,  21000, 18000, 11000),
            ("Kia",      "Sportage", 2021, "EX",       "Grey",   145, 25500, 21500, 33000),
            ("Nissan",   "Rogue",    2020, "SV",       "Black",  188, 20500, 17500, 58000),
            ("Nissan",   "Altima",   2022, "SR",       "Silver", 54,  24500, 21000, 22000),
        };

        var vehicles = new List<Vehicle>();
        foreach (var s in specs)
        {
            var acquired = today.AddDays(-s.Ageeeed);
            vehicles.Add(new Vehicle
            {
                Vin = NewVin(rnd),
                Make = s.Make,
                Model = s.Model,
                Year = s.Year,
                Trim = s.Trim,
                Color = s.Color,
                Mileage = s.Miles,
                ListPrice = s.List,
                Cost = s.Cost,
                Status = VehicleStatus.InStock,
                AcquiredDate = acquired,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
        }

        // --- Sold vehicles + matching sales records for the "top sales" panel. ---
        var soldSpecs = new (string Make, string Model, int Year, decimal Sale, decimal Cost, int DaysAgoSold, int DaysToSell)[]
        {
            ("Toyota", "Highlander", 2023, 44000, 38000, 4,  22),
            ("BMW",    "X3",         2022, 47500, 41000, 9,  31),
            ("Tesla",  "Model Y",    2023, 53000, 47000, 2,  15),
            ("Ford",   "Bronco",     2022, 46000, 40000, 18, 40),
            ("Honda",  "Pilot",      2021, 36000, 31000, 12, 55),
            ("Toyota", "Tacoma",     2022, 39500, 34000, 25, 28),
            ("Chevrolet","Tahoe",    2021, 58000, 51000, 6,  47),
            ("Hyundai","Santa Fe",   2023, 33500, 29000, 21, 19),
        };

        var sales = new List<SalesRecord>();
        foreach (var s in soldSpecs)
        {
            var soldDate = today.AddDays(-s.DaysAgoSold);
            var v = new Vehicle
            {
                Vin = NewVin(rnd),
                Make = s.Make,
                Model = s.Model,
                Year = s.Year,
                Trim = "—",
                Color = "Various",
                Mileage = rnd.Next(2000, 40000),
                ListPrice = s.Sale,
                Cost = s.Cost,
                Status = VehicleStatus.Sold,
                AcquiredDate = soldDate.AddDays(-s.DaysToSell),
                SoldDate = soldDate,
            };
            vehicles.Add(v);
            sales.Add(new SalesRecord
            {
                Vehicle = v,
                VehicleId = v.Id,
                SalePrice = s.Sale,
                SoldDate = soldDate,
                DaysToSell = s.DaysToSell,
                SoldBy = "manager",
            });
        }

        // --- Seed a few actions on aging vehicles. ---
        var agingVehicles = vehicles.Where(v => v.IsAgingStock(today)).Take(3).ToList();
        var actions = new List<VehicleAction>();
        foreach (var (v, i) in agingVehicles.Select((v, i) => (v, i)))
        {
            actions.Add(new VehicleAction
            {
                VehicleId = v.Id,
                Vehicle = v,
                ActionType = i == 0 ? ActionType.PriceReductionPlanned
                           : i == 1 ? ActionType.MoveToAuction
                           : ActionType.Promote,
                Status = ActionStatus.Open,
                Note = i == 0 ? "Reduce list price by 5% end of month."
                     : i == 1 ? "Send to Tuesday auction if unsold in 2 weeks."
                     : "Feature on homepage carousel.",
                CreatedBy = "manager",
            });
        }

        // --- Business strategies at each scope. ---
        var strategies = new List<BusinessStrategy>
        {
            new()
            {
                Scope = StrategyScope.Factory, ScopeKey = "Toyota",
                Name = "Toyota fast-turn", Description = "Keep Toyota stock moving quickly.",
                TargetDaysToSell = 60, DiscountPercent = 3, EffectiveFrom = today.AddDays(-120),
            },
            new()
            {
                Scope = StrategyScope.VehicleType, ScopeKey = BusinessStrategy.VehicleTypeKey("Ford", "F-150"),
                Name = "F-150 premium hold", Description = "High-demand truck, hold margin.",
                TargetDaysToSell = 120, DiscountPercent = 1, EffectiveFrom = today.AddDays(-90),
            },
            new()
            {
                Scope = StrategyScope.VehicleType, ScopeKey = BusinessStrategy.VehicleTypeKey("Nissan", "Rogue"),
                Name = "Rogue clearance", Description = "Aging SUV — clear aggressively.",
                TargetDaysToSell = 45, DiscountPercent = 8, EffectiveFrom = today.AddDays(-30),
            },
        };
        // A vehicle-specific strategy on the oldest aging vehicle (overrides its factory/type strategy).
        var oldest = vehicles.Where(v => v.Status == VehicleStatus.InStock)
            .OrderBy(v => v.AcquiredDate).First();
        strategies.Add(new BusinessStrategy
        {
            Scope = StrategyScope.Vehicle, ScopeKey = oldest.Id.ToString(),
            Name = "Manager special", Description = $"Personal push on {oldest.Make} {oldest.Model}.",
            TargetDaysToSell = 20, DiscountPercent = 12, EffectiveFrom = today.AddDays(-10),
        });

        await db.Vehicles.AddRangeAsync(vehicles, ct);
        await db.Sales.AddRangeAsync(sales, ct);
        await db.VehicleActions.AddRangeAsync(actions, ct);
        await db.Strategies.AddRangeAsync(strategies, ct);
        await db.SaveChangesAsync(ct);
    }

    private static string NewVin(Random rnd)
    {
        const string chars = "ABCDEFGHJKLMNPRSTUVWXYZ0123456789";
        return string.Concat(Enumerable.Range(0, 17).Select(_ => chars[rnd.Next(chars.Length)]));
    }
}
