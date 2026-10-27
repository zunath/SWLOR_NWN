using SWLOR.CombatSim.SpaceCombat.Scenario;
using SWLOR.Game.Server.Service;

namespace SWLOR.CombatSim.SpaceCombat.Engine;

/// <summary>
/// The per-shot numbers of a space weapon (plan sections 5 and 9). Hit rate, damage range and the critical rate come
/// from the shared Combat formulas the ground simulation uses; what is here is the space mapping onto them.
/// </summary>
public static class SpaceShotMath
{
    /// <summary>Energy: ship Accuracy + module Accuracy + AGI x 3. Launcher: the same with PER x 3.</summary>
    public static int Accuracy(ShipState attacker, WeaponModuleSpec weapon) =>
        attacker.Spec.Accuracy + weapon.Accuracy + (weapon.Family == ModuleFamily.Energy ? attacker.Pilot.Agi : attacker.Pilot.Per) * 3;

    public static int Attack(ShipState attacker, WeaponModuleSpec weapon) => attacker.Spec.Attack + weapon.Attack;

    /// <summary>The attribute damage scales with: PER for energy weapons, MGT for launchers.</summary>
    public static int DamageAttribute(ShipState attacker, WeaponModuleSpec weapon) =>
        weapon.Family == ModuleFamily.Energy ? attacker.Pilot.Per : attacker.Pilot.Mgt;

    public static ShipSize WeaponSize(ShipState attacker, WeaponModuleSpec weapon) => weapon.Size ?? attacker.Size;

    /// <summary>Size steps the weapon is above (positive) or below (negative) the target; 0 when the weapon ignores size.</summary>
    public static int SizeSteps(ShipState attacker, WeaponModuleSpec weapon, ShipState target) =>
        weapon.IgnoreSizePenalty ? 0 : (int)WeaponSize(attacker, weapon) - (int)target.Size;

    /// <summary>
    /// Combat.CalculateHitRate on accuracy against evasion, with the size penalty as its percentage modifier. The
    /// formula's own 20-95 limits apply after the penalty.
    /// </summary>
    public static int HitChance(SpaceScenarioSpec scenario, ShipState attacker, WeaponModuleSpec weapon, ShipState target)
    {
        var penalty = Math.Max(0, SizeSteps(attacker, weapon, target)) * scenario.SizeHitPenaltyPerStep;
        return Combat.CalculateHitRate(Accuracy(attacker, weapon), target.Evasion, -penalty);
    }

    /// <summary>Damage multiplier for a weapon smaller than its target: (1 - penalty) per step, compounding.</summary>
    public static double SizeDamageMultiplier(SpaceScenarioSpec scenario, ShipState attacker, WeaponModuleSpec weapon, ShipState target) =>
        Math.Pow(1.0 - scenario.SizeDamagePenaltyPerStep, Math.Max(0, -SizeSteps(attacker, weapon, target)));

    /// <summary>
    /// The ground critical rate with module rank / 10 as weapon rank and attacker PER against target VIT. The
    /// game's 5-50 limits apply through Combat.CalculateCriticalRate unless the scenario names its own.
    /// </summary>
    public static int CriticalChance(SpaceScenarioSpec scenario, ShipState attacker, WeaponModuleSpec weapon, ShipState target)
    {
        if (scenario.CriticalRateMinimum == null && scenario.CriticalRateMaximum == null)
            return Combat.CalculateCriticalRate(attacker.Pilot.Per, target.Pilot.Vit, weapon.Rank, 0);

        // Combat.CalculateCriticalRate clamps to its fixed limits, so an override clamps its unclamped sum.
        var skillBonus = Math.Max(0, weapon.Rank / 10);
        var statBonus = Math.Clamp((int)Math.Floor((attacker.Pilot.Per - target.Pilot.Vit) / 5.0f), 0, 3);
        return Math.Clamp(
            Combat.MinimumCriticalRate + skillBonus + statBonus,
            scenario.CriticalRateMinimum ?? 0,
            scenario.CriticalRateMaximum ?? 100);
    }

    /// <summary>The damage formula's delta cap for a weapon of this rank (GetDamageRoll.GetWeaponDeltaCapFromRequiredSkillRank, which is private).</summary>
    public static int DeltaCap(int rank) => rank <= 0 ? 1 : Math.Clamp(rank / 10 + 1, 1, 6);

    /// <summary>Combat.CalculateDamageRange with one ship Attack against one ship Defense and the target's VIT as the defender stat.</summary>
    public static (int Minimum, int Maximum) DamageRange(
        ShipState attacker, WeaponModuleSpec weapon, int bonusDmg, ShipState target, bool critical) =>
        Combat.CalculateDamageRange(
            Attack(attacker, weapon),
            weapon.Dmg + bonusDmg,
            DamageAttribute(attacker, weapon),
            target.Defense,
            target.Pilot.Vit,
            critical ? Combat.StandardCriticalRating : 0,
            DeltaCap(weapon.Rank));

    /// <summary>The share of damage a resistance lets through.</summary>
    public static double ResistanceMultiplier(ShipState target, SpaceDamageType type) => 1.0 - target.Resistance(type) / 100.0;
}
