using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The damage inputs of the ability split (<c>Readmes/MaintainabilityRefactorAbilitySplit.md</c>, part 16) over the bench's
/// impact state store, magnitude part and resistance stand-in, with its calls into the stat and combat services answered by
/// the bench, as <see cref="TestAbilityService"/> answered them before the move. The unscaled formula's damage-dealt and
/// damage-taken stages answer as the stage part's double does (<see cref="TestAbilityDamageStageService"/>).
/// </summary>
public sealed class TestAbilityDamageInputService : AbilityDamageInputService
{
    private readonly AbilityBench _bench;

    public TestAbilityDamageInputService(AbilityBench bench, IAbilityImpactState impactState, ICombatAbilityMagnitudeService magnitude, IResistanceService resistance)
        : base(impactState, FacadeForwarder.For<IStatService>(), FacadeForwarder.For<ICombatRatingService>(), FacadeForwarder.For<ICombatActivityService>(), FacadeForwarder.For<ICombatQueriesService>(),
            FacadeForwarder.For<ICombatAbilityBonusService>(), FacadeForwarder.For<ICombatAbilityImpactService>(), FacadeForwarder.For<ICombatDamageModifiersService>(),
            FacadeForwarder.For<ICombatDamageTakenService>(), magnitude, resistance)
    {
        _bench = bench;
    }

    private void Log(string entry) => _bench.Log.Add(entry);

    // The same reads answer the same in the other doubles: the weapon damage and its type in TestAbilityImpactLifecycleService, the stat adjustment and the weapon skills in TestAbilityActivationService, the stat adjustment in
    // TestAbilityDarkForceService and TestAbilityAuraService.
    protected override int GetStatAdjustment(uint creature, StatType stat) => _bench.Stats.GetValueOrDefault((creature, stat));

    protected override int GetCombatImpactWeaponDamage(uint activator, SkillType skillType, bool usesQueuedNaturalWeapon = false, int? triggeringWeaponDamage = null, bool abilityImpact = false) =>
        triggeringWeaponDamage ?? _bench.WeaponDamage;

    protected override CombatDamageType GetCombatImpactWeaponDamageType(uint activator, SkillType skillType, bool usesQueuedNaturalWeapon) => _bench.WeaponDamageType;

    protected override bool IncludesCombatImpactWeaponDamage(SkillType skillType, bool usesQueuedNaturalWeapon) => _bench.IncludesWeaponDamage;

    protected override bool IsWeaponSkillType(SkillType skillType) => _bench.WeaponSkills.Contains(skillType);

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

    protected override int GetNearbyStatusTargetAttackAdjustment(uint creature) => 0;

    protected override int GetLowHPAttackAdjustment(uint attacker) => 0;
}
