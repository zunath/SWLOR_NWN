using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Part 2 of the status effect split, <c>StatusEffectCatalogService</c>, with the search for the effect definitions answered by the
/// bench (<see cref="StatusEffectBench.DefinitionTypes"/>).
/// </summary>
public sealed class TestStatusEffectCatalogService : StatusEffectCatalogService
{
    private readonly StatusEffectBench _bench;

    public TestStatusEffectCatalogService(StatusEffectBench bench)
    {
        _bench = bench;
    }

    public IEnumerable<Type> DefaultDefinitionTypes() => base.GetStatusEffectTypes();

    protected override IEnumerable<Type> GetStatusEffectTypes() => _bench.DefinitionTypes ?? Array.Empty<Type>();
}
