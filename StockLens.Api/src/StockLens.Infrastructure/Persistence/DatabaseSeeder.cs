using Microsoft.EntityFrameworkCore;
using StockLens.Domain.Entities;
using StockLens.Domain.Enums;

namespace StockLens.Infrastructure.Persistence;

/// <summary>
/// Seeds a representative dealership so the dashboard is populated on first run.
/// Each block is independently idempotent, so an existing database gains only the
/// data it is missing rather than being rebuilt or duplicated.
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>Months of sales history generated for the performance chart.</summary>
    private const int SalesHistoryMonths = 6;

    public static async Task SeedAsync(ApplicationDbContext db, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var rnd = new Random(42);

        var salespeople = await SeedSalespeopleAsync(db, today, ct);
        await SeedInventoryAsync(db, today, rnd, ct);
        await SeedSalesHistoryAsync(db, today, rnd, salespeople, ct);
    }

    // --- Sales team -------------------------------------------------------

    /// <summary>
    /// Seeds the sales team, returning the active roster. Gated on <em>active</em> members so
    /// the inactive "Unassigned" fallback row created by the migration doesn't look like a team.
    /// </summary>
    private static async Task<List<Salesperson>> SeedSalespeopleAsync(
        ApplicationDbContext db, DateOnly today, CancellationToken ct)
    {
        var existing = await db.Salespeople
            .Where(s => s.IsActive)
            .OrderBy(s => s.FullName)
            .ToListAsync(ct);
        if (existing.Count > 0) return existing;

        var team = new List<Salesperson>
        {
            new() { FullName = "Alex Nguyen",   Email = "alex.nguyen@stocklens.test",   Team = "Used",  HireDate = today.AddYears(-4) },
            new() { FullName = "Priya Sharma",  Email = "priya.sharma@stocklens.test",  Team = "New",   HireDate = today.AddYears(-3) },
            new() { FullName = "Marcus Rivera", Email = "marcus.rivera@stocklens.test", Team = "Used",  HireDate = today.AddYears(-2) },
            new() { FullName = "Chloe Dubois",  Email = "chloe.dubois@stocklens.test",  Team = "Fleet", HireDate = today.AddYears(-1) },
            new() { FullName = "Sam Okafor",    Email = "sam.okafor@stocklens.test",    Team = "New",   HireDate = today.AddMonths(-8) },
        };

        await db.Salespeople.AddRangeAsync(team, ct);
        await db.SaveChangesAsync(ct);
        return team;
    }

    // --- In-stock inventory, strategies and actions ------------------------

    private static async Task SeedInventoryAsync(
        ApplicationDbContext db, DateOnly today, Random rnd, CancellationToken ct)
    {
        if (await db.Vehicles.AnyAsync(ct)) return;

        // Ages are spread so a realistic share crosses the 90-day aging line.
        var specs = new (string Make, string Model, int Year, string Trim, string Color, int AgeDays, decimal List, decimal Cost, int Miles, BodyType Body)[]
        {
            ("Toyota",    "Corolla",   2023, "LE",          "White",  12,  22500, 19000, 8200,  BodyType.Sedan),
            ("Toyota",    "RAV4",      2022, "XLE",         "Silver", 47,  31000, 26500, 21000, BodyType.Suv),
            ("Toyota",    "Camry",     2021, "SE",          "Black",  118, 26000, 22000, 34000, BodyType.Sedan),
            ("Honda",     "Civic",     2023, "Sport",       "Blue",   8,   26500, 22800, 5400,  BodyType.Sedan),
            ("Honda",     "CR-V",      2020, "EX",          "Grey",   135, 24000, 20500, 52000, BodyType.Suv),
            ("Honda",     "Accord",    2022, "Touring",     "White",  73,  33000, 28500, 18000, BodyType.Sedan),
            ("Ford",      "F-150",     2021, "Lariat",      "Red",    156, 42000, 36000, 41000, BodyType.Truck),
            ("Ford",      "Explorer",  2022, "XLT",         "Black",  61,  38000, 33000, 27000, BodyType.Suv),
            ("Ford",      "Escape",    2020, "SE",          "Blue",   201, 19500, 16500, 63000, BodyType.Suv),
            ("BMW",       "3 Series",  2021, "330i",        "Black",  99,  38500, 33000, 29000, BodyType.Sedan),
            ("BMW",       "X5",        2022, "xDrive40i",   "White",  33,  61000, 54000, 15000, BodyType.Suv),
            ("Tesla",     "Model 3",   2023, "Long Range",  "White",  5,   41000, 37000, 3200,  BodyType.Sedan),
            ("Tesla",     "Model Y",   2022, "Performance", "Grey",   88,  52000, 46000, 12000, BodyType.Suv),
            ("Chevrolet", "Silverado", 2021, "LT",          "Silver", 172, 39000, 34000, 38000, BodyType.Truck),
            ("Chevrolet", "Equinox",   2023, "LT",          "Red",    26,  27000, 23500, 9000,  BodyType.Suv),
            ("Hyundai",   "Tucson",    2022, "SEL",         "Blue",   112, 28000, 24000, 20000, BodyType.Suv),
            ("Hyundai",   "Elantra",   2023, "SEL",         "White",  40,  21000, 18000, 11000, BodyType.Sedan),
            ("Kia",       "Sportage",  2021, "EX",          "Grey",   145, 25500, 21500, 33000, BodyType.Suv),
            ("Nissan",    "Rogue",     2020, "SV",          "Black",  188, 20500, 17500, 58000, BodyType.Suv),
            ("Nissan",    "Altima",    2022, "SR",          "Silver", 54,  24500, 21000, 22000, BodyType.Sedan),
            ("Volkswagen","Golf GTI",  2022, "Autobahn",    "Red",    64,  32000, 27500, 16000, BodyType.Hatchback),
            ("Chrysler",  "Pacifica",  2021, "Touring L",   "Silver", 129, 33500, 29000, 44000, BodyType.Van),
            ("Subaru",    "Outback",   2022, "Premium",     "Green",  35,  31500, 27000, 19000, BodyType.Wagon),
            ("Ford",      "Mustang",   2023, "GT",          "Red",    22,  47000, 41000, 6100,  BodyType.Coupe),
        };

        var vehicles = specs.Select(s => new Vehicle
        {
            Vin = NewVin(),
            Make = s.Make,
            Model = s.Model,
            Year = s.Year,
            Trim = s.Trim,
            Color = s.Color,
            Mileage = s.Miles,
            BodyType = s.Body,
            ListPrice = s.List,
            Cost = s.Cost,
            Status = VehicleStatus.Open,
            AcquiredDate = today.AddDays(-s.AgeDays),
        }).ToList();

        // A few actions on the oldest aging stock, as a manager would log.
        var aging = vehicles.Where(v => v.IsAgingStock(today)).Take(3).ToList();
        var actions = aging.Select((v, i) => new VehicleAction
        {
            VehicleId = v.Id,
            Vehicle = v,
            ActionType = i switch
            {
                0 => ActionType.PriceReductionPlanned,
                1 => ActionType.MoveToAuction,
                _ => ActionType.Promote,
            },
            Status = ActionStatus.Open,
            Note = i switch
            {
                0 => "Reduce list price by 5% end of month.",
                1 => "Send to Tuesday auction if unsold in 2 weeks.",
                _ => "Feature on homepage carousel.",
            },
            CreatedBy = "manager",
        }).ToList();

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
                Name = "Rogue clearance", Description = "Aging SUV, clear aggressively.",
                TargetDaysToSell = 45, DiscountPercent = 8, EffectiveFrom = today.AddDays(-30),
            },
        };

        var oldest = vehicles.OrderBy(v => v.AcquiredDate).First();
        strategies.Add(new BusinessStrategy
        {
            Scope = StrategyScope.Vehicle, ScopeKey = oldest.Id.ToString(),
            Name = "Manager special", Description = $"Personal push on {oldest.Make} {oldest.Model}.",
            TargetDaysToSell = 20, DiscountPercent = 12, EffectiveFrom = today.AddDays(-10),
        });

        await db.Vehicles.AddRangeAsync(vehicles, ct);
        await db.VehicleActions.AddRangeAsync(actions, ct);
        await db.Strategies.AddRangeAsync(strategies, ct);
        await db.SaveChangesAsync(ct);
    }

    // --- Sales history ----------------------------------------------------

    /// <summary>
    /// Generates sold vehicles and their sales spread across the trailing months so the
    /// performance chart has a real trend. Skipped once historical sales already exist.
    /// </summary>
    private static async Task SeedSalesHistoryAsync(
        ApplicationDbContext db, DateOnly today, Random rnd, List<Salesperson> team, CancellationToken ct)
    {
        // Historical sales already present means this block has run before.
        if (await db.Sales.AnyAsync(s => s.SoldDate < today.AddDays(-45), ct)) return;
        if (team.Count == 0) return;

        var catalogue = new (string Make, string Model, int Year, decimal Price, BodyType Body)[]
        {
            ("Toyota",    "Highlander", 2023, 44000, BodyType.Suv),
            ("Toyota",    "Tacoma",     2022, 39500, BodyType.Truck),
            ("Toyota",    "Corolla",    2022, 21500, BodyType.Sedan),
            ("BMW",       "X3",         2022, 47500, BodyType.Suv),
            ("BMW",       "5 Series",   2021, 45000, BodyType.Sedan),
            ("Tesla",     "Model Y",    2023, 53000, BodyType.Suv),
            ("Tesla",     "Model 3",    2022, 39000, BodyType.Sedan),
            ("Ford",      "Bronco",     2022, 46000, BodyType.Suv),
            ("Ford",      "F-150",      2022, 43500, BodyType.Truck),
            ("Honda",     "Pilot",      2021, 36000, BodyType.Suv),
            ("Honda",     "Civic",      2022, 25500, BodyType.Sedan),
            ("Chevrolet", "Tahoe",      2021, 58000, BodyType.Suv),
            ("Chevrolet", "Equinox",    2022, 26500, BodyType.Suv),
            ("Hyundai",   "Santa Fe",   2023, 33500, BodyType.Suv),
            ("Kia",       "Telluride",  2023, 41000, BodyType.Suv),
            ("Subaru",    "Outback",    2022, 31000, BodyType.Wagon),
        };

        var vehicles = new List<Vehicle>();
        var sales = new List<SalesRecord>();

        // Walk each month oldest → newest with a gentle upward trend, so the chart reads
        // as a dealership improving rather than as noise. Months are anchored to the first
        // of the calendar month (matching how the dashboard buckets the trend) because
        // today-relative offsets would straddle month boundaries and under-fill the window.
        var currentMonth = new DateOnly(today.Year, today.Month, 1);

        for (var monthOffset = SalesHistoryMonths - 1; monthOffset >= 0; monthOffset--)
        {
            var monthStart = currentMonth.AddMonths(-monthOffset);
            var unitsThisMonth = 4 + rnd.Next(0, 4) + (SalesHistoryMonths - 1 - monthOffset) / 2;

            // The in-progress month only has days up to today to sell on.
            var sellableDays = monthOffset == 0
                ? today.Day
                : DateTime.DaysInMonth(monthStart.Year, monthStart.Month);

            for (var i = 0; i < unitsThisMonth; i++)
            {
                var pick = catalogue[rnd.Next(catalogue.Length)];
                var soldDate = monthStart.AddDays(rnd.Next(0, sellableDays));

                var daysToSell = 12 + rnd.Next(0, 55);
                var salePrice = pick.Price - rnd.Next(0, 2500);

                var vehicle = new Vehicle
                {
                    Vin = NewVin(),
                    Make = pick.Make,
                    Model = pick.Model,
                    Year = pick.Year,
                    Trim = string.Empty,
                    Color = "Various",
                    Mileage = rnd.Next(2000, 40000),
                    BodyType = pick.Body,
                    ListPrice = pick.Price,
                    Cost = Math.Round(pick.Price * 0.86m, 2),
                    Status = VehicleStatus.Sold,
                    AcquiredDate = soldDate.AddDays(-daysToSell),
                    SoldDate = soldDate,
                };
                vehicles.Add(vehicle);

                sales.Add(new SalesRecord
                {
                    Vehicle = vehicle,
                    VehicleId = vehicle.Id,
                    SalespersonId = team[rnd.Next(team.Count)].Id,
                    SalePrice = salePrice,
                    SoldDate = soldDate,
                    DaysToSell = daysToSell,
                });
            }
        }

        await db.Vehicles.AddRangeAsync(vehicles, ct);
        await db.Sales.AddRangeAsync(sales, ct);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Generates a unique VIN-shaped identifier.
    /// </summary>
    /// <remarks>
    /// Deliberately not derived from the shared seeded <see cref="Random"/>: the seed blocks
    /// run conditionally, so a skipped block would leave the sequence at a different position
    /// and the next block would regenerate VINs already in the table, violating the unique
    /// index. A GUID keeps VINs unique regardless of which blocks ran.
    /// </remarks>
    private static string NewVin() => Guid.NewGuid().ToString("N")[..17].ToUpperInvariant();
}
