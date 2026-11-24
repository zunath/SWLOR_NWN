using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The damage stages of the ability split (<c>Readmes/MaintainabilityRefactorAbilitySplit.md</c>, part 17) over the damage
/// inputs' double, the bench's impact state store, magnitude part and resistance stand-in, with its calls into the perk and
/// combat services answered by the bench, as <see cref="TestAbilityService"/> answered them before the move.
/// </summary>
public sealed class TestAbilityDamageStageService : AbilityDamageStageService
{
    private readonly AbilityBench _bench;

    public TestAbilityDamageStageService(AbilityBench bench, IAbilityDamageInputService damageInput, IAbilityImpactState impactState, ICombatAbilityMagnitudeService magnitude, IResistanceService resistance)
        : base(damageInput, impactState, FacadeForwarder.For<IPerkService>(), FacadeForwarder.For<ICombatAbilityHitService>(),
            FacadeForwarder.For<ICombatDamageModifiersService>(), FacadeForwarder.For<ICombatDamageTakenService>(), FacadeForwarder.For<ICombatDamageReflectionService>(), FacadeForwarder.For<ICombatFeedbackService>(),
            FacadeForwarder.For<ICombatCriticalHitEffectsService>(), magnitude, resistance)
    {
        _bench = bench;
    }

    private void Log(string entry) => _bench.Log.Add(entry);

    // The magnitude part answers the same rule through TestCombatAbilityMagnitudeService.
    protected override int ApplyForceAffinityMagnitude(uint creature, PerkType perkType, int amount) => amount + 2;

    protected override void ApplyCriticalHitEffects(uint attacker, uint defender, int damage, int criticalRating, bool isSingleTargetImpact, SkillType skillType) =>
        Log($"CriticalEffects:{damage}:{criticalRating}:{isSingleTargetImpact}");

    protected override int ApplyDamageDealtModifiers(uint attacker, uint defender, int damage, SkillType skillType, CombatDamageType damageType, bool isAbilityDamage, bool canApplyRandomFlatBonuses, bool isLandedAttack, AbilityDetail ability, out int targetStatusDamagePercentAdjustment, int abilityDamagePercentAdjustment, int lowHPAbilityDamagePercentAdjustment)
    {
        Log($"DealtModifiers:{damage}:{abilityDamagePercentAdjustment}:{lowHPAbilityDamagePercentAdjustment}");
        targetStatusDamagePercentAdjustment = 0;
        return damage;
    }

    protected override int ApplyIncomingPhysicalToForceConversion(uint attacker, uint defender, CombatDamageType damageType, ref int physicalDamage, int targetStatusDamagePercentAdjustment)
    {
        Log($"ForceConversion:{physicalDamage}");
        return 0;
    }

    protected override int ApplyTypedLeadershipDamageTakenModifier(uint defender, int damage, CombatDamageType damageType)
    {
        Log($"TypedLeadership:{damage}");
        return damage;
    }

    protected override int ApplyDamageTakenModifiers(uint defender, int damage, uint attacker, CombatDamageType damageType, int targetStatusDamagePercentAdjustment, bool typedLeadershipReductionAlreadyApplied)
    {
        Log($"TakenModifiers:{damage}:{typedLeadershipReductionAlreadyApplied}");
        return damage * _bench.IncomingDamageTakenPercent / 100;
    }

    protected override int CalculateAbilityCriticalRating(uint attacker, SkillType skillType, bool isAreaAbility, int criticalRateAdjustment, uint defender)
    {
        Log($"CriticalRating:{isAreaAbility}:{criticalRateAdjustment}");
        return _bench.CriticalRating;
    }

    protected override (int Damage, int CriticalRating, bool WasCriticalDowngraded) CalculateDamageWithCriticalMitigation(uint defender, int attackerAttack, int attackerDMG, int attackerStat, int defenderDefense, int defenderStat, int critical)
    {
        Log($"DamageRoll:{attackerAttack}:{attackerDMG}:{attackerStat}:{defenderDefense}:{defenderStat}:{critical}");
        return (attackerDMG, critical, _bench.CriticalDowngraded);
    }

    protected override void SendIncomingCriticalHitDowngradeFeedback(uint attacker, uint defender) => Log("CriticalDowngradeFeedback");

    protected override int ApplyCriticalDamageModifier(uint attacker, int damage, int criticalRating, SkillType skillType, uint defender, int criticalDamagePercentAdjustment)
    {
        Log($"CriticalDamage:{damage}:{criticalRating}:{criticalDamagePercentAdjustment}");
        return damage;
    }

    protected override void SendAbilityCriticalHitFeedback(uint attacker, uint defender, string abilityName) => Log($"CriticalFeedback:{abilityName}");
}
