namespace SWLOR.Architecture.Tests.Fixtures.Rules.Composition;

/// <summary>Stands for the real FacadeAccessor.</summary>
public static class FixtureAccessor
{
    public static T Resolve<T>()
        where T : class => throw new NotSupportedException();
}
