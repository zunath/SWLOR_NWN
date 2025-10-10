using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Combat.ValueObjects;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Shared.Domain.Combat.Contracts;

public interface ICombatCalculationService
{
    /// <summary>
    /// Calculates the physical damage dealt by an attacker using a weapon against a defender.
    /// </summary>
    /// <param name="attacker">The object ID of the attacking creature.</param>
    /// <param name="defender">The object ID of the defending creature.</param>
    /// <param name="weapon">The object ID of the weapon being used.</param>
    /// <param name="isCritical">Whether the attack is a critical hit.</param>
    /// <returns>The calculated physical damage amount.</returns>
    int CalculatePhysicalDamage(
        uint attacker,
        uint defender,
        uint weapon,
        bool isCritical);

    /// <summary>
    /// Calculates the force damage dealt by an attacker against a defender.
    /// </summary>
    /// <param name="attacker">The object ID of the attacking creature.</param>
    /// <param name="defender">The object ID of the defending creature.</param>
    /// <param name="dmg">The base damage amount.</param>
    /// <returns>The calculated force damage amount.</returns>
    int CalculateForceDamage(
        uint attacker,
        uint defender,
        int dmg);

    /// <summary>
    /// Calculates the ability damage dealt by an attacker against a defender using a specific skill and damage type.
    /// </summary>
    /// <param name="attacker">The object ID of the attacking creature.</param>
    /// <param name="defender">The object ID of the defending creature.</param>
    /// <param name="dmg">The base damage amount.</param>
    /// <param name="damageType">The type of damage being dealt.</param>
    /// <param name="skillType">The skill type being used for the attack.</param>
    /// <param name="attackerStatType">The ability stat type used by the attacker.</param>
    /// <param name="defenderStatType">The ability stat type used by the defender for defense.</param>
    /// <returns>The calculated ability damage amount.</returns>
    int CalculateAbilityDamage(
        uint attacker,
        uint defender,
        int dmg,
        CombatDamageType damageType,
        SkillType skillType,
        AbilityType attackerStatType,
        AbilityType defenderStatType
    );

    /// <summary>
    /// Calculates the hit type (miss, normal hit, or critical hit) for an attack.
    /// </summary>
    /// <param name="attacker">The object ID of the attacking creature.</param>
    /// <param name="defender">The object ID of the defending creature.</param>
    /// <param name="weapon">The object ID of the weapon being used.</param>
    /// <returns>A HitResult containing the hit rate and determined hit type.</returns>
    HitResult CalculateHitType(
        uint attacker,
        uint defender,
        uint weapon);
}