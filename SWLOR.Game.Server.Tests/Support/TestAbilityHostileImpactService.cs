using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The hostile impact of the ability split (<c>Readmes/MaintainabilityRefactorAbilitySplit.md</c>, part 12) over the bench's
/// impact state store, observer, combat point and enmity stand-ins and its delivery, status application, Dark Force and visual
/// parts, with its calls into the combat services answered by the bench, as <see cref="TestAbilityService"/> answered them
/// before the move.
/// </summary>
public sealed class TestAbilityHostileImpactService : AbilityHostileImpactService
{
    private readonly AbilityBench _bench;

    public TestAbilityHostileImpactService(
        AbilityBench bench,
        IAbilityImpactState impactState,
        IEnumerable<IAbilityImpactObserver> observers,
        ICombatPointService combatPoint,
        IEnmityChangeService enmity,
        IAbilityDamageDeliveryService damageDelivery,
        IAbilityStatusApplicationService statusApplication,
        IAbilityDarkForceService darkForce,
        IAbilityImpactVisualService impactVisual)
        : base(impactState, observers, combatPoint, enmity, FacadeForwarder.For<ICombatResourcesService>(), FacadeForwarder.For<ICombatAbilityImpactService>(),
            damageDelivery, statusApplication, darkForce, impactVisual)
    {
        _bench = bench;
    }

    private void Log(string entry) => _bench.Log.Add(entry);

    // The per-target resolution opens the same scope through its own (TestAbilityTargetImpactService).
    protected override IDisposable BeginDamageDerivedHealing(uint creature, uint target) => _bench.BeginHealingScope(creature, target);

    protected override bool IsTargetAttackingAllyForStatusAppliedRiders(uint activator, uint target)
    {
        Log("IsTargetAttackingAlly");
        return _bench.AttackingAlly;
    }

    protected override void ApplySuccessfulAbilityImpactRiders(uint activator, uint target, AbilityDetail ability, SkillType skillType, CombatDamageType damageType, int damage, bool statusApplied, Type primaryStatusEffect, IEnumerable<Type> additionalStatusEffects, bool firstHostileAbilityHitDamageBonusApplied, bool isFirstSuccessfulTarget, StatusEffectCategory appliedStatusCategories, bool allowsHitProcs, bool targetWasAttackingAlly) =>
        Log($"ImpactRiders:{target}:{damage}:{statusApplied}:{firstHostileAbilityHitDamageBonusApplied}:{isFirstSuccessfulTarget}:{appliedStatusCategories}:{allowsHitProcs}:{targetWasAttackingAlly}");
}
