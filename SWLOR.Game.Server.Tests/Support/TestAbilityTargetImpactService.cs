using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The per-target resolution of the ability split (<c>Readmes/MaintainabilityRefactorAbilitySplit.md</c>, part 11) over the bench's
/// impact state store and its hit roll, hostile impact, Dark Force and damage parts, with its calls into the combat services
/// answered by the bench, as <see cref="TestAbilityService"/> answered them before the move.
/// </summary>
public sealed class TestAbilityTargetImpactService : AbilityTargetImpactService
{
    private readonly AbilityBench _bench;

    public TestAbilityTargetImpactService(
        AbilityBench bench,
        IAbilityImpactState impactState,
        IAbilityHitRollService hitRoll,
        IAbilityHostileImpactService hostileImpact,
        IAbilityDarkForceService darkForce,
        IAbilityDamageInputService damageInput,
        IAbilityDamageStageService damageStage)
        : base(impactState, hitRoll, hostileImpact, darkForce, damageInput, damageStage, FacadeForwarder.For<ICombatResourcesService>(),
            FacadeForwarder.For<ICombatAbilityImpactService>(), FacadeForwarder.For<ICombatAbilityBonusService>())
    {
        _bench = bench;
    }

    // The hostile impact opens the same scope through its own (TestAbilityHostileImpactService).
    protected override IDisposable BeginDamageDerivedHealing(uint creature, uint target) => _bench.BeginHealingScope(creature, target);

    protected override int GetAbilityImpactBaseDamageBonus(uint activator, uint target, AbilityDetail ability, SkillType skillType) => _bench.FlatBaseDamageBonus;

    protected override int GetAbilityStatusCategoryDamageBonus(uint creature, SkillType skillType, StatusEffectCategory appliedCategories) => 0;
}
