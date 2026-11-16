using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The magnitude part of the ability split (<c>Readmes/MaintainabilityRefactorAbilitySplit.md</c>, part 4) with its calls into
/// the perk and stat services answered by the bench, as <see cref="TestAbilityService"/> answered them before the move.
/// </summary>
public sealed class TestCombatAbilityMagnitudeService : CombatAbilityMagnitudeService
{
    private readonly AbilityBench _bench;

    public TestCombatAbilityMagnitudeService(AbilityBench bench, IAbilityImpactState impactState)
        : base(impactState, FacadeForwarder.For<IPerkService>(), FacadeForwarder.For<IStatService>())
    {
        _bench = bench;
    }

    protected override int GetCombatReadinessPercent(uint creature) => _bench.CombatReadiness;

    protected override float GetForceAffinityMagnitudeMultiplier(uint creature, PerkType perkType) => _bench.ForceAffinityMultiplier;

    protected override float GetForceAffinityRestorativeMagnitudeMultiplier(uint creature, PerkType perkType) => _bench.ForceAffinityRestorativeMultiplier;

    protected override int ApplyForceAffinityRestorativeMagnitude(uint creature, PerkType perkType, int amount) => amount + 1;

    // The ability damage formulas call the same rule through the damage stage part (TestAbilityDamageStageService).
    protected override int ApplyForceAffinityMagnitude(uint creature, PerkType perkType, int amount) => amount + 2;

    protected override float ApplyForceAffinityMagnitude(uint creature, PerkType perkType, float amount) => amount + 2.5f;
}
