using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The aura part of the ability split (<c>Readmes/MaintainabilityRefactorAbilitySplit.md</c>, part 8) with its read of the
/// command radius bonus answered by the bench, as <see cref="TestAbilityService"/> answered it before the move.
/// </summary>
public sealed class TestAbilityAuraService : AbilityAuraService
{
    private readonly AbilityBench _bench;

    public TestAbilityAuraService(AbilityBench bench, IStatusEffectService statusEffect, IStatusEffectCatalogService statusCatalog, IPartyRoster party, ICombatPointService combatPoint)
        : base(statusEffect, statusCatalog, party, FacadeForwarder.For<IStatService>(), combatPoint)
    {
        _bench = bench;
    }

    // The activation checks, Dark Force and the damage inputs answer the same read through their own doubles.
    protected override int GetStatAdjustment(uint creature, StatType stat) => _bench.Stats.GetValueOrDefault((creature, stat));
}
