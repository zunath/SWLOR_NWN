namespace SWLOR.Game.Server.Tests.Support;

// Part 2, StatusEffectCatalogService: the search for the effect definitions.
public sealed partial class TestStatusEffectService
{
    public IEnumerable<Type> DefaultDefinitionTypes() => base.GetStatusEffectTypes();

    protected override IEnumerable<Type> GetStatusEffectTypes() => _bench.DefinitionTypes ?? Array.Empty<Type>();
}
