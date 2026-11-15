using System.Numerics;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Domain.Space;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The answers of <see cref="TestAbilityService"/> for the damage inputs and stages (parts 16 and 17). When the ability split
/// (<c>Readmes/MaintainabilityRefactorAbilitySplit.md</c>) moves the part, these overrides become its own double; the
/// answers come from the bench, so the tests that set them do not change.
/// </summary>
public sealed partial class TestAbilityService
{
    protected override int GetAttack(uint creature, AbilityType abilityType, SkillType skillType)
    {
        Log($"GetAttack:{creature}:{abilityType}:{skillType}");
        return _bench.AttackValue;
    }

    protected override int GetAttack(int level, int stat, int bonus)
    {
        Log($"GetNpcAttack:{level}:{stat}:{bonus}");
        return _bench.AttackValue;
    }

    protected override int GetDefense(uint creature, CombatDamageType type, AbilityType abilityType)
    {
        Log($"GetDefense:{creature}:{type}:{abilityType}");
        return _bench.DefenseValue;
    }

    protected override NPCStats GetNPCStats(uint npc) => _bench.NpcStats;

    protected override int GetCurrentFP(uint creature) => _bench.CurrentFP;

    protected override int GetMaxFP(uint creature) => _bench.MaxFP;

    protected override int GetCurrentStamina(uint creature) => _bench.CurrentStamina;

    protected override int GetMaxStamina(uint creature) => _bench.MaxStamina;

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

    protected override (int DamageBonus, int HitChancePercentAdjustment, int CriticalDamagePercentAdjustment) GetIdleSkillAbilityBonuses(uint activator, SkillType skillType) => _bench.IdleBonuses;

    protected override int GetAbilityDamageBonus(uint creature, SkillType skill) => 0;

    protected override int GetAbilityDamageFlatAdjustment(uint creature, PerkType perkType, SkillType skillType) => 0;

    protected override int GetCostlyAbilityDamageBonus(uint activator, AbilityDetail ability, SkillType skillType) => 0;

    protected override int GetSameTargetPressureWeaponAbilityDamageBonus(uint attacker, uint target, SkillType skillType) => 0;

    protected override int ApplyTargetStatusAttackModifiers(uint attacker, uint defender, int attack, SkillType skillType) => attack;

    protected override int ApplyStatusSourceDefenseModifiers(uint attacker, uint defender, int defense) => defense;

    protected override int ApplyIncomingPhysicalToForceDefenseConversion(uint defender, CombatDamageType damageType, int physicalDefense, Func<int> forceDefenseProvider) => forceDefenseProvider();

    protected override int GetAbilityDefenseIgnorePercentAdjustment(uint creature, PerkType perkType, SkillType skillType, uint defender) => 0;

    protected override int ApplyDefenseIgnore(int defense, int defenseIgnorePercent)
    {
        Log($"DefenseIgnore:{defense}:{defenseIgnorePercent}");
        return defense;
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

    protected override int GetNearbyStatusTargetAttackAdjustment(uint creature) => 0;

    protected override int GetLowHPAttackAdjustment(uint attacker) => 0;
}
