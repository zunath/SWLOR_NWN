using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The catalog part of the ability split (<c>Readmes/MaintainabilityRefactorAbilitySplit.md</c>, part 1) with its definition
/// search answered by the bench, and a way to put one ability in it without building the real definitions.
/// </summary>
public sealed class TestAbilityCatalogService : AbilityCatalogService
{
    private readonly AbilityBench _bench;

    public TestAbilityCatalogService(AbilityBench bench)
        : base(FacadeForwarder.For<IAbilityTargetingCacheService>())
    {
        _bench = bench;
    }

    private void Log(string entry) => _bench.Log.Add(entry);

    /// <summary>Puts one ability in the catalog, without building the real definitions.</summary>
    public void Register(FeatType feat, AbilityDetail ability) =>
        ((Dictionary<FeatType, AbilityDetail>)GetAllAbilityDetails())[feat] = ability;

    /// <summary>The default of the definition search, for the test that proves it finds the server's definitions.</summary>
    public IEnumerable<Type> DefaultDefinitionTypes() => base.GetAbilityListDefinitionTypes();

    protected override IEnumerable<Type> GetAbilityListDefinitionTypes() => _bench.DefinitionTypes ?? Array.Empty<Type>();

    protected override void CacheAbilityTargeting(IReadOnlyDictionary<FeatType, AbilityDetail> abilities)
    {
        _bench.CachedTargeting = abilities;
        Log("CacheAbilityTargeting");
    }
}
