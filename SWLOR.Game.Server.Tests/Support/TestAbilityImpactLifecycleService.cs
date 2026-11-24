using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The impact lifecycle of the ability split (<c>Readmes/MaintainabilityRefactorAbilitySplit.md</c>, part 5) over the bench's
/// impact state store, melee technique, recorder, status effect and delivery stand-ins, with its calls into the combat services
/// answered by the bench, as <see cref="TestAbilityService"/> answered them before the move.
/// </summary>
public sealed class TestAbilityImpactLifecycleService : AbilityImpactLifecycleService
{
    private readonly AbilityBench _bench;

    public TestAbilityImpactLifecycleService(
        AbilityBench bench,
        IAbilityImpactState impactState,
        IMeleeTechniqueService meleeTechnique,
        ICombatRecorderService combatRecorder,
        IAbilityDamageDeliveryService damageDelivery,
        IStatusEffectCombatEventService statusEffectCombatEvents)
        : base(impactState, meleeTechnique, combatRecorder, damageDelivery, statusEffectCombatEvents, FacadeForwarder.For<ICombatQueriesService>(),
            FacadeForwarder.For<ICombatAbilityBonusService>(), FacadeForwarder.For<ICombatAbilityImpactService>(),
            FacadeForwarder.For<ICombatResourcesService>(), FacadeForwarder.For<ICombatCriticalHitEffectsService>())
    {
        _bench = bench;
    }

    private void Log(string entry) => _bench.Log.Add(entry);

    // The activation part's Force power check answers with the same skill.
    protected override SkillType GetAbilitySkillType(uint creature, AbilityDetail ability) => ability.SkillType;

    protected override int GetAbilityDMG(uint creature, uint weapon, SkillType abilitySkill)
    {
        Log($"GetAbilityDMG:{creature}:{weapon}:{abilitySkill}");
        return _bench.WeaponDamage;
    }

    protected override CombatDamageType GetWeaponDamageType(uint weapon)
    {
        Log($"GetWeaponDamageType:{weapon}");
        return _bench.WeaponDamageType;
    }

    protected override int ConsumeNextAbilityDamageBonus(uint creature, PerkType perkType)
    {
        Log("ConsumeNextAbilityDamageBonus");
        return _bench.NextAbilityDamageBonus;
    }

    protected override (int DamageBonus, int CriticalRatePercentAdjustment, int DefenseIgnorePercentAdjustment) ConsumeNextSkillAbilityBonuses(uint creature, SkillType skillType, StatusEffectCategory abilityStatusCategories)
    {
        Log($"ConsumeNextSkillAbilityBonuses:{abilityStatusCategories}");
        return _bench.NextSkillBonuses;
    }

    protected override int GetPersistentNextSkillAbilityCriticalRateBonus(uint creature, SkillType skillType)
    {
        Log("GetPersistentCriticalRate");
        return _bench.PersistentCriticalRate;
    }

    protected override (int DamageBonus, int CriticalRatePercentAdjustment, int CriticalDamagePercentAdjustment) ConsumeQueuedWeaponAbilityBonuses(uint creature, SkillType skillType)
    {
        Log("ConsumeQueuedWeaponAbilityBonuses");
        return _bench.QueuedBonuses;
    }

    protected override (int DMGBonus, int CriticalRatePercentAdjustment, int EnmityBonus) ConsumeNextAttackGuardedHitBonuses(uint creature)
    {
        Log("ConsumeGuardedHitBonuses");
        return _bench.GuardedHitBonuses;
    }

    protected override int GetStatusAppliedNextAttackDamageBonus(uint creature)
    {
        Log("GetStatusAppliedBonus");
        return _bench.StatusAppliedBonus;
    }

    // The damage inputs have their own (TestAbilityDamageInputService).
    protected override int GetCombatImpactWeaponDamage(uint activator, SkillType skillType, bool usesQueuedNaturalWeapon = false, int? triggeringWeaponDamage = null, bool abilityImpact = false) =>
        triggeringWeaponDamage ?? _bench.WeaponDamage;

    // The damage inputs answer the same through their own (TestAbilityDamageInputService).
    protected override CombatDamageType GetCombatImpactWeaponDamageType(uint activator, SkillType skillType, bool usesQueuedNaturalWeapon) => _bench.WeaponDamageType;

    // The telegraphed impact answers the same through its own (TestAbilityService).
    protected override void ApplyAbilityImpactEffects(uint activator, AbilityImpactSummary summary) =>
        Log($"ApplyAbilityImpactEffects:{activator}:{summary.ImpactedTargetCount}");

    protected override int RefundCriticalRangedAbilityStaminaCost(uint creature, AbilityDetail ability)
    {
        Log($"RefundCritical:{creature}");
        return 0;
    }

    protected override int ConsumePersistentNextSkillAbilityCriticalRateBonus(uint creature, SkillType skillType)
    {
        Log($"ConsumePersistentCriticalRate:{creature}:{skillType}");
        return 0;
    }

    protected override void ApplyNonCriticalAbilityEffects(uint activator, uint target, SkillType skillType) =>
        Log($"NonCriticalEffects:{activator}:{skillType}");
}
