using SWLOR.Architecture.Tests.Fixtures.Rules.Composition;

namespace SWLOR.Architecture.Tests.Fixtures.Rules;

/// <summary>Not a facade, but resolves through the accessor: service location.</summary>
public sealed class AccessorIntruder
{
    public string Get() => FixtureAccessor.Resolve<string>();
}
