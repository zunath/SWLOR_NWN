using SWLOR.Architecture.Tests.Fixtures.Rules.Composition;

namespace SWLOR.Architecture.Tests.Fixtures.Rules;

/// <summary>A facade: allowed to resolve its instance through the accessor.</summary>
public static class AccessorFacade
{
    public static string Get() => FixtureAccessor.Resolve<string>();
}
