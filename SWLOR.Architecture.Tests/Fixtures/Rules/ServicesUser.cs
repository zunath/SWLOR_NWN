using SWLOR.Architecture.Tests.Fixtures.Rules.Composition;

namespace SWLOR.Architecture.Tests.Fixtures.Rules;

/// <summary>An instance class that reaches a service through the static accessor instead of its constructor.</summary>
public class ServicesUser
{
    public IFixtureContract Use() => FixtureServices.Fixture;
}
