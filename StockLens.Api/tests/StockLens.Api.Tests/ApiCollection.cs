namespace StockLens.Api.Tests;

/// <summary>
/// Groups every API test class into one xUnit collection sharing a single
/// <see cref="StockLensApiFactory"/>.
/// </summary>
/// <remarks>
/// Necessary for correctness, not just speed: xUnit runs test classes in parallel by default,
/// and these classes share one test database. Cleanup reclaims rows by VIN prefix, so a class
/// finishing would delete vehicles another class was still using. A shared collection forces
/// the classes to run sequentially, and reuses one host instead of booting several.
/// </remarks>
[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<StockLensApiFactory>
{
    public const string Name = "StockLens API";
}
