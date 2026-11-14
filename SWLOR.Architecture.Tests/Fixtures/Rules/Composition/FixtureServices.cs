namespace SWLOR.Architecture.Tests.Fixtures.Rules.Composition;

/// <summary>Stands for the real GameServices: a static property per removed facade, typed as the service's interface.</summary>
public static class FixtureServices
{
    public static IFixtureContract Fixture => FixtureAccessor.Resolve<IFixtureContract>();

    public static string Name => "not a service";
}
