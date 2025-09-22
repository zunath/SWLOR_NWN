using NWN.Native.API;
using SWLOR.Component.Combat.Enums;
using SWLOR.NWN.API.NWScript.Enum;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;
using SavingThrow = SWLOR.NWN.API.NWScript.Enum.SavingThrow;

namespace SWLOR.Component.Combat.Contracts
{
    public interface ICombatService
    {
        /// <summary>
        /// When the module loads, add all valid damage types to the cache.
        /// </summary>
        void LoadDamageTypes();

        /// <summary>
        /// When a player enters the server, apply any defenses towards damage types they don't already have.
        /// </summary>
        void AddDamageTypeDefenses();

        /// <summary>
        /// Retrieves all valid damage types available in the system.
        /// </summary>
        /// <returns>A list of damage types</returns>
        List<CombatDamageType> GetAllDamageTypes();

        /// <summary>
        /// Calculates the minimum and maximum damage possible with the provided stats.
        /// </summary>
        /// <param name="attackerAttack">The attacker's attack rating.</param>
        /// <param name="attackerDMG">The attacker's DMG rating</param>
        /// <param name="attackerStat">The attacker's attack stat value</param>
        /// <param name="defenderDefense">The defender's defense rating.</param>
        /// <param name="defenderStat">The defender's defend stat value</param>
        /// <param name="critical">the critical rating of the attack, or 0 if the attack is not critical.</param>
        /// <param name="deltaCap">Value to cap the lower and upper bounds of stat delta to. For weapons, should be weapon rank.</param>
        /// <returns>A minimum and maximum damage range</returns>
        (int, int) CalculateDamageRange(
            int attackerAttack,
            int attackerDMG,
            int attackerStat,
            int defenderDefense,
            int defenderStat,
            int critical,
            int deltaCap = 0);

        /// <summary>
        /// Calculates the hit rate against a given target.
        /// Range is clamped to values between 20 and 95, inclusive.
        /// </summary>
        /// <param name="attackerAccuracy">The total accuracy of the attacker.</param>
        /// <param name="defenderEvasion">The total evasion of the defender.</param>
        /// <param name="percentageModifier">Modifies the raw hit change by a certain percentage. This is done after all prior calculations.</param>
        /// <returns>The hit rate, clamped between 20 and 95, inclusive.</returns>
        int CalculateHitRate(
            int attackerAccuracy,
            int defenderEvasion,
            int percentageModifier);

        /// <summary>
        /// Calculates the critical hit rate against a given target.
        /// </summary>
        /// <param name="attackerPER">The attacker's perception stat</param>
        /// <param name="defenderMGT">The defender's might stat.</param>
        /// <param name="criticalModifier">A modifier to the critical rating based on external factors.</param>
        /// <returns>The critical rate, in a percentage</returns>
        int CalculateCriticalRate(int attackerPER, int defenderMGT, int criticalModifier);

        /// <summary>
        /// Calculates a random damage amount based on the provided stats of the attacker and defender.
        /// </summary>
        /// <param name="attackerAttack">The attacker's attack rating.</param>
        /// <param name="attackerDMG">The attacker's DMG rating</param>
        /// <param name="attackerStat">The attacker's attack stat value</param>
        /// <param name="defenderDefense">The defender's defense rating.</param>
        /// <param name="defenderStat">The defender's defend stat value</param>
        /// <param name="critical">the critical rating of the attack, or 0 if the attack is not critical.</param>
        /// <param name="deltaCap">Value to cap the lower and upper bounds of stat delta to. For weapons, should be weapon rank.</param>
        /// <returns>A damage value to apply to the target.</returns>
        int CalculateDamage(
            int attackerAttack,
            int attackerDMG,
            int attackerStat,
            int defenderDefense,
            int defenderStat,
            int critical,
            int deltaCap = 0);

        /// <summary>
        /// Return a damage bonus equal to 0.15 of the player's relevant skill or an NPC's level.
        /// This helps abilities as the player progresses. 
        ///
        /// Global scaling on gear is closer to 0.25 DMG per player skill level so low tier abilities will still
        /// become less useful over time, and get replaced by higher tier ones.  But they will have some utility still.
        /// </summary>
        /// <returns> 0.15 * the player's rank in the specified skill, or the level for NPCs.</returns>
        int GetAbilityDamageBonus(uint creature, SkillType skill);

        /// <summary>
        /// On module heartbeat, clear a PC's saved combat facing if they are no longer in combat.
        /// </summary>
        void ClearCombatState();

        /// <summary>
        /// Builds a combat log message based on the provided information.
        /// </summary>
        /// <param name="attacker">The id of the attacker</param>
        /// <param name="defender">The id of the defender</param>
        /// <param name="attackResultType">The type of result. 1, 7 = Hit, 3 = Critical, 4 = Miss</param>
        /// <param name="chanceToHit">The percent chance to hit</param>
        /// <returns></returns>
        string BuildCombatLogMessage(
            uint attacker,
            uint defender,
            int attackResultType,
            int chanceToHit);

        /// <summary>
        /// Builds a combat log message based on the provided information, for native contexts.
        /// </summary>
        /// <param name="attacker">The CNWSCreature of the attacker</param>
        /// <param name="defender">The CNWSCreature of the defender</param>
        /// <param name="attackResultType">The type of result. 1, 7 = Hit, 3 = Critical, 4 = Miss</param>
        /// <param name="chanceToHit">The percent chance to hit</param>
        /// <returns></returns>
        string BuildCombatLogMessageNative(
            CNWSCreature attacker,
            CNWSCreature defender,
            int attackResultType,
            int chanceToHit);

        /// <summary>
        /// Check for weapon type and perk. Returns either the default ability score or the perk replaced ability score if the user has the relevant perk or active stance.
        /// This is currently used for zen marksmanship, strong style, crushing style, and flurry style.
        /// </summary>
        /// <param name="attacker">The attacker to check</param>
        /// <returns>The correct damage ability score, or 0 if a weapon is not equipped.</returns>
        int GetPerkAdjustedAbilityScore(uint attacker);

        /// <summary>
        /// Retrieves the DMG bonus granted by doublehand, Power Attack, and Might scaling.
        /// </summary>
        /// <param name="attacker">The attacker to check</param>
        /// <param name="weaponType">The BaseItem of the weapon held</param>
        /// <returns>The DMG value or 0 if requirements are not met.</returns>
        int GetMiscDMGBonus(uint attacker, BaseItem weaponType);

        /// <summary>
        /// Retrieves the DMG bonus granted by Might scaling on Crushing Style Staves and Strong Style Sabers.
        /// Returns 0 if an invalid weapon is held.
        /// </summary>
        /// <param name="attacker">The attacker to check</param>
        /// <param name="weaponType">The BaseItem of the weapon held</param>
        /// <returns>The DMG value or 0 if requirements are not met.</returns>
        int GetMightDMGBonus(uint attacker, BaseItem weaponType);

        /// <summary>
        /// Retrieves the DMG bonus granted by doublehand.
        /// If attacker does not meet the requirements of Doublehand, 0 will be returned.
        /// </summary>
        /// <param name="attacker">The attacker to check</param>
        /// <returns>The DMG value or 0 if requirements are not met.</returns>
        int GetDoublehandDMGBonus(uint attacker);

        /// <summary>
        /// Retrieves the DMG bonus granted by Power Attack.
        /// </summary>
        /// <param name="attacker">The attacker to check.</param>
        /// <returns>The DMG bonus, or 0 if Power Attack is not enabled.</returns>
        int GetPowerAttackDMGBonus(uint attacker);

        /// <summary>
        /// Retrieves the DMG bonus granted by doublehand.
        /// If attacker does not meet the requirements of Doublehand, 0 will be returned.
        /// Must be called from within a native context.
        /// </summary>
        /// <param name="attacker">The attacker to check</param>
        /// <returns>The DMG value or 0 if requirements are not met.</returns>
        int GetDoublehandDMGBonusNative(CNWSCreature attacker);

        /// <summary>
        /// Determines the DC for an attacker's saving throw.
        /// </summary>
        /// <param name="attacker">The attacker to check.</param>
        /// <param name="type">The type of saving throw.</param>
        /// <param name="baseDC">The base DC amount.</param>
        /// <param name="abilityOverride">Use this to specify a specific ability to be used.</param>
        /// <returns>A DC value with any bonuses applied.</returns>
        int CalculateSavingThrowDC(
            uint attacker,
            SavingThrow type,
            int baseDC,
            AbilityType abilityOverride = AbilityType.Invalid);
    }
}
