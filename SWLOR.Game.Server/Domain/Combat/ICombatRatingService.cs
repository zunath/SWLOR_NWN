using NWN.Native.API;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The Attack, Accuracy, Defense and Evasion ratings and the deflection and Guard chances (part 5 of the stat split;
    /// until then members of <c>IStatService</c>).
    /// </summary>
    public interface ICombatRatingService
    {
        /// <summary>
        /// Calculates the attack for a given creature.
        /// </summary>
        /// <param name="creature">The creature to calculate.</param>
        /// <param name="abilityType">The type of ability to use.</param>
        /// <param name="skillType">The type of skill to use.</param>
        /// <param name="attackBonusOverride">Overrides the attack bonus granted by equipment. Usually only used by Space combat.</param>
        /// <returns>The total Attack value of a creature.</returns>
        int GetAttack(uint creature, AbilityType abilityType, SkillType skillType, int attackBonusOverride = 0);

        int GetAttackNative(CNWSCreature creature, BaseItem itemType, AbilityType statOverride = AbilityType.Invalid,
            bool useForceAttack = false, SkillType skillOverride = SkillType.Invalid);

        /// <summary>
        /// Retrieves the raw attack based on the level, stat, and any bonuses.
        /// </summary>
        /// <param name="level">The level (NPC or skill)</param>
        /// <param name="stat">The raw stat points</param>
        /// <param name="bonus">The amount of bonus attack or force attack</param>
        /// <returns></returns>
        int GetAttack(int level, int stat, int bonus);

        /// <summary>
        /// Retrieves the defense value used by the attack-vs-defense damage roll.
        /// Physical and Force equipment bonuses live here; elemental/status mitigation lives in resistance.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <param name="type">The type of damage to retrieve.</param>
        /// <param name="abilityType"></param>
        /// <param name="defenseBonusOverride">Overrides the defense bonus granted by equipment. Usually only used for Space combat.</param>
        /// <returns>The defense value toward a given damage type.</returns>
        int GetDefense(uint creature, CombatDamageType type, AbilityType abilityType, int defenseBonusOverride = 0);

        int CalculateDefense(int defenderStat, int skillLevel, int defenseBonus);

        /// <summary>
        /// Retrieves the native stat value of a given type on a particular creature.
        /// </summary>
        /// <param name="creature">The creature to check</param>
        /// <param name="statType">The type of stat to check</param>
        /// <returns>The stat value of a creature based on the ability type</returns>
        int GetStatValueNative(CNWSCreature creature, AbilityType statType);

        /// <summary>
        /// Retrieves the defense value used by the attack-vs-defense damage roll.
        /// This is specifically for use with Native code and should not be referenced outside of there.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <param name="type">The type of damage to retrieve.</param>
        /// <param name="abilityType"></param>
        /// <returns>The defense value toward a given damage type.</returns>
        int GetDefenseNative(CNWSCreature creature, CombatDamageType type, AbilityType abilityType);

        int GetAttackPercentAdjustment(uint creature, SkillType skillType);

        /// <summary>
        /// Retrieves the accuracy rating of a creature.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <param name="weapon">The weapon being used.</param>
        /// <param name="statOverride">The stat override used to calculate accuracy. This stat will be used instead of whatever stat is defined for the weapon type.</param>
        /// <param name="skillOverride">The skill override used to calculate accuracy. This skill will be used instead of whatever skill is defined for the weapon type.</param>
        /// <param name="skillLevelOverride">Overrides the skill rank or NPC level used in the accuracy calculation.</param>
        /// <param name="ignoreWeaponAccuracyStatOverride">When true, Accuracy Stat item properties do not replace <paramref name="statOverride"/>.</param>
        /// <returns>The accuracy rating for a creature using a specific weapon.</returns>
        int GetAccuracy(
            uint creature,
            uint weapon,
            AbilityType statOverride,
            SkillType skillOverride,
            int skillLevelOverride = -1,
            bool ignoreWeaponAccuracyStatOverride = false);

        /// <summary>
        /// Retrieves the accuracy rating of a creature from a native context.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <param name="weapon">The weapon being used.</param>
        /// <returns>The accuracy rating for a creature using a specific weapon.</returns>
        int GetAccuracyNative(CNWSCreature creature, CNWSItem weapon, SkillType skillOverride = SkillType.Invalid,
            AbilityType accuracyAbilityOverride = AbilityType.Invalid);

        /// <summary>
        /// Gets the calculated accuracy for a given level, stat, and bonus.
        /// </summary>
        /// <param name="level">The level (skill/NPC)</param>
        /// <param name="stat">The raw accuracy stat amount</param>
        /// <param name="bonus">The amount of bonus accuracy.</param>
        /// <returns>The calculated accuracy result.</returns>
        int GetAccuracy(int level, int stat, int bonus);

        /// <summary>
        /// Retrieves a creature's evasion.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <param name="skillOverride">The skill override to use instead of Armor for the purposes of calculating evasion.</param>
        /// <param name="incomingSkillType">The skill type of the incoming attack for conditional evasion modifiers.</param>
        /// <returns>The evasion rating of a creature.</returns>
        int GetEvasion(uint creature, SkillType skillOverride, SkillType incomingSkillType = SkillType.Invalid);

        /// <summary>
        /// Retrieves a creature's evasion rating from a native context.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <returns>The evasion rating of a creature.</returns>
        int GetEvasionNative(CNWSCreature creature, SkillType incomingSkillType = SkillType.Invalid);

        int GetMeleeDeflectionChanceNative(CNWSCreature creature);

        int GetMeleeDeflectionChance(uint creature);

        int GetRangedDeflectionChanceNative(CNWSCreature creature);

        int GetRangedDeflectionChance(uint creature);

        int GetShieldDeflectionChanceNative(CNWSCreature creature);

        int GetShieldDeflectionChance(uint creature);

        int GetGuardChance(uint creature);

        /// <summary>
        /// Retrieves the total percentage adjustment applied to a creature's defense for a damage type.
        /// This combines the general defense adjustment with the type-specific adjustment, including
        /// the shield-only bonus when a shield is equipped.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <param name="type">The damage type.</param>
        /// <returns>The percentage adjustment applied to defense.</returns>
        int GetDefensePercentAdjustment(uint creature, CombatDamageType type);

        /// <summary>
        /// Gets the evasion based on level, stat, and bonuses.
        /// </summary>
        /// <param name="level">The level (skill/NPC)</param>
        /// <param name="stat">The raw agility stat</param>
        /// <param name="bonus">The amount of bonus evasion</param>
        /// <returns></returns>
        int GetEvasion(int level, int stat, int bonus);

        /// <summary>
        /// NPC stat skins carry the complete Evasion budget. Native armor must
        /// not add another budget through inherited equipment, feats or effects.
        /// Stat-based Evasion adjustments remain part of bonus for all creatures.
        /// </summary>
        int GetEvasion(int level, int stat, int bonus, int nativeArmorClass, bool hasNpcStatBudget);
    }
}
