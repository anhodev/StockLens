using StockLens.Application.Strategies;
using StockLens.Domain.Entities;
using StockLens.Domain.Enums;

namespace StockLens.Domain.Tests;

public class StrategyResolverTests
{
    private static readonly DateOnly Today = new(2026, 7, 15);
    private readonly StrategyResolver _resolver = new();

    private static Vehicle Vehicle() => new()
    {
        Id = Guid.NewGuid(),
        Make = "Ford",
        Model = "F-150",
        Status = VehicleStatus.InStock,
        AcquiredDate = Today.AddDays(-100),
    };

    private static BusinessStrategy Strategy(StrategyScope scope, string key, string name) => new()
    {
        Scope = scope,
        ScopeKey = key,
        Name = name,
        IsActive = true,
        EffectiveFrom = Today.AddDays(-30),
    };

    [Fact]
    public void Resolves_factory_when_only_factory_matches()
    {
        var v = Vehicle();
        var candidates = new[] { Strategy(StrategyScope.Factory, "Ford", "factory") };

        var result = _resolver.Resolve(v, candidates, Today);

        Assert.NotNull(result);
        Assert.Equal(StrategyScope.Factory, result!.MatchedScope);
    }

    [Fact]
    public void VehicleType_overrides_factory()
    {
        var v = Vehicle();
        var candidates = new[]
        {
            Strategy(StrategyScope.Factory, "Ford", "factory"),
            Strategy(StrategyScope.VehicleType, BusinessStrategy.VehicleTypeKey("Ford", "F-150"), "type"),
        };

        var result = _resolver.Resolve(v, candidates, Today);

        Assert.Equal(StrategyScope.VehicleType, result!.MatchedScope);
        Assert.Equal("type", result.Strategy.Name);
    }

    [Fact]
    public void Vehicle_scope_overrides_everything()
    {
        var v = Vehicle();
        var candidates = new[]
        {
            Strategy(StrategyScope.Factory, "Ford", "factory"),
            Strategy(StrategyScope.VehicleType, BusinessStrategy.VehicleTypeKey("Ford", "F-150"), "type"),
            Strategy(StrategyScope.Vehicle, v.Id.ToString(), "specific"),
        };

        var result = _resolver.Resolve(v, candidates, Today);

        Assert.Equal(StrategyScope.Vehicle, result!.MatchedScope);
        Assert.Equal("specific", result.Strategy.Name);
    }

    [Fact]
    public void Ignores_inactive_and_expired_strategies()
    {
        var v = Vehicle();
        var inactive = Strategy(StrategyScope.Factory, "Ford", "inactive");
        inactive.IsActive = false;
        var expired = Strategy(StrategyScope.VehicleType, BusinessStrategy.VehicleTypeKey("Ford", "F-150"), "expired");
        expired.EffectiveTo = Today.AddDays(-1);

        var result = _resolver.Resolve(v, new[] { inactive, expired }, Today);

        Assert.Null(result);
    }

    [Fact]
    public void Returns_null_when_nothing_matches()
    {
        var v = Vehicle();
        var candidates = new[] { Strategy(StrategyScope.Factory, "Toyota", "other-make") };

        Assert.Null(_resolver.Resolve(v, candidates, Today));
    }

    [Fact]
    public void Picks_most_recent_when_multiple_at_same_scope()
    {
        var v = Vehicle();
        var older = Strategy(StrategyScope.Factory, "Ford", "older");
        older.EffectiveFrom = Today.AddDays(-60);
        var newer = Strategy(StrategyScope.Factory, "Ford", "newer");
        newer.EffectiveFrom = Today.AddDays(-5);

        var result = _resolver.Resolve(v, new[] { older, newer }, Today);

        Assert.Equal("newer", result!.Strategy.Name);
    }
}
