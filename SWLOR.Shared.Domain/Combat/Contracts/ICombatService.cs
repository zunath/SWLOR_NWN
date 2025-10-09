using NWN.Native.API;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Shared.Domain.Combat.Contracts
{
    public interface ICombatService
    {
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
        int GetMiscDMGBonus(uint attacker, BaseItemType weaponType);

        /// <summary>
        /// Retrieves the DMG bonus granted by Might scaling on Crushing Style Staves and Strong Style Sabers.
        /// Returns 0 if an invalid weapon is held.
        /// </summary>
        /// <param name="attacker">The attacker to check</param>
        /// <param name="weaponType">The BaseItem of the weapon held</param>
        /// <returns>The DMG value or 0 if requirements are not met.</returns>
        int GetMightDMGBonus(uint attacker, BaseItemType weaponType);

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
            SavingThrowCategoryType type,
            int baseDC,
            AbilityType abilityOverride = AbilityType.Invalid);

        bool IsParalyzed(uint creature);
    }
}
