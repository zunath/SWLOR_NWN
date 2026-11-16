using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// The damage inputs of an ability hit (part 16 of the ability split, Readmes/MaintainabilityRefactorAbilitySplit.md): the
    /// weapon damage and flat bonuses, the attack and the defense of the player and NPC damage formulas, which hand them to
    /// their stages (<see cref="IAbilityDamageStageService"/>), the unscaled damage formula, and the two rules the per-target
    /// resolution reads to pick a formula and a damage type, with the damage type itself.
    /// </summary>
    public interface IAbilityDamageInputService
    {
        /// <summary>Whether an NPC activator's impact asked for NPC stat scaling. A player never scales as an NPC.</summary>
        bool ShouldUseNPCStatScaling(uint activator, bool useNPCStatScaling);

        /// <summary>Whether a Beast Mastery weapon ability hits with the activator's natural weapon.</summary>
        bool UsesQueuedNaturalWeapon(AbilityImpactTracker trackedImpact, SkillType skillType);

        /// <summary>
        /// An impact that adds weapon DMG also takes that weapon's element, matching auto-attacks.
        /// Like native weapon resolution, an element only replaces Physical; authored non-Physical types stay.
        /// </summary>
        CombatDamageType GetWeaponAbilityDamageType(uint activator, SkillType skillType, CombatDamageType damageType);

        /// <summary>
        /// <see cref="GetWeaponAbilityDamageType"/> for the impact the caller already read: the explicit weapon profile's element,
        /// otherwise the authored type unless it is Physical and the impact adds weapon DMG, then the triggering weapon's element.
        /// </summary>
        CombatDamageType ResolveWeaponAbilityDamageType(
            uint activator,
            SkillType skillType,
            CombatDamageType damageType,
            AbilityImpactTracker trackedImpact);

        /// <summary>
        /// The damage of an impact that skips the attack and defense formula: its base damage and captured bonus, through the
        /// damage-dealt modifiers, combat readiness, the physical-to-Force conversion, typed Leadership, resistance and the
        /// damage-taken modifiers. 0 when the impact has no damage.
        /// </summary>
        int CalculateUnscaledCombatImpactDamage(
            uint activator,
            uint target,
            SkillType skillType,
            int baseDamage,
            CombatDamageType damageType,
            int abilityDamagePercentAdjustment = 0,
            int lowHPAbilityDamagePercentAdjustment = 0);

        /// <summary>
        /// The inputs of the player damage formula, up to and including the defense ignore. False when the impact has no
        /// damage, so the hit deals none.
        /// </summary>
        bool TryGetCombatImpactDamageInputs(
            uint activator,
            uint target,
            SkillType skillType,
            int baseDamage,
            CombatDamageType damageType,
            AbilityType combatImpactDamageAbility,
            out AbilityDamageInputs inputs);

        /// <summary>
        /// The inputs of the NPC damage formula (the creature's level and NPC stats, scaled by its ability rank), up to and
        /// including the defense ignore. False when the impact has no damage, so the hit deals none.
        /// </summary>
        bool TryGetNPCCombatImpactDamageInputs(
            uint activator,
            uint target,
            SkillType skillType,
            int baseDamage,
            CombatDamageType damageType,
            AbilityType combatImpactDamageAbility,
            out AbilityDamageInputs inputs);
    }
}
