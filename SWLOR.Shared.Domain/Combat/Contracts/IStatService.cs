using NWN.Native.API;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Shared.Domain.Combat.Contracts
{
    public interface IStatService
    {
        int BaseHP => 70;
        int BaseFP => 10;
        int BaseSTM => 10;

        /// <summary>
        /// When a player enters the server, reapply HP and temporary stats.
        /// </summary>
        void ApplyPlayerStats();


        /// <summary>
        /// After a player's status effects are reassociated,
        /// adjust any food HP if necessary.
        /// </summary>
        void ReapplyFoodHP();


        void ApplyPlayerMovementRate(uint player);

        /// <summary>
        /// Calculates a player's stat based on their skill bonuses, upgrades, etc. and applies the changes to one ability score.
        /// </summary>
        /// <param name="entity">The player entity</param>
        /// <param name="player">The player object</param>
        /// <param name="ability">The ability score to apply to.</param>
        void ApplyPlayerStat(Player entity, uint player, AbilityType ability);

        /// <summary>
        /// Modifies the ability recast reduction of a player by a certain amount.
        /// This method will not persist the changes so be sure you call _db.Set after calling this.
        /// </summary>
        /// <param name="entity">The player entity</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustPlayerRecastReduction(Player entity, int adjustBy);

        /// <summary>
        /// Modifies a player's HP Regen by a certain amount.
        /// This method will not persist the changes so be sure you call _db.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustHPRegen(Player entity, int adjustBy);

        /// <summary>
        /// Modifies a player's FP Regen by a certain amount.
        /// This method will not persist the changes so be sure you call _db.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustFPRegen(Player entity, int adjustBy);

        /// <summary>
        /// Modifies a player's STM Regen by a certain amount.
        /// This method will not persist the changes so be sure you call _db.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustSTMRegen(Player entity, int adjustBy);

        /// <summary>
        /// Modifies a player's defense toward a particular damage type by a certain amount.
        /// This method will not persist the changes so be sure you call _db.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="type">The type of damage</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustDefense(Player entity, CombatDamageType type, int adjustBy);

        /// <summary>
        /// Modifies a player's evasion by a certain amount.
        /// This method will not persist the changes so be sure you call _db.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustEvasion(Player entity, int adjustBy);

        /// <summary>
        /// Modifies a player's attack by a certain amount. Attack affects damage output.
        /// This method will not persist the changes so be sure you call _db.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustAttack(Player entity, int adjustBy);

        /// <summary>
        /// Modifies a player's force attack by a certain amount. Force Attack affects damage output.
        /// This method will not persist the changes so be sure you call _db.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustForceAttack(Player entity, int adjustBy);

        /// <summary>
        /// Modifies a player's control by a certain amount.
        /// This method will not persist the changes so be sure you call _db.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="skillType">The skill type to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustControl(Player entity, SkillType skillType, int adjustBy);

        /// <summary>
        /// Modifies a player's craftsmanship by a certain amount.
        /// This method will not persist the changes so be sure you call _db.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="skillType">The skill type to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustCraftsmanship(Player entity, SkillType skillType, int adjustBy);

        /// <summary>
        /// Modifies a player's CP bonus by a certain amount.
        /// This method will not persist the changes so be sure you call _db.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="skillType">The skill type to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustCPBonus(Player entity, SkillType skillType, int adjustBy);

        /// <summary>
        /// Retrieves the native stat value of a given type on a particular creature.
        /// </summary>
        /// <param name="creature">The creature to check</param>
        /// <param name="statType">The type of stat to check</param>
        /// <returns>The stat value of a creature based on the ability type</returns>
        int GetStatValueNative(CNWSCreature creature, AbilityType statType);

        /// <summary>
        /// Applies the total number of attacks per round to a player.
        /// If a valid weapon is passed in the associated mastery perk will also be checked.
        /// </summary>
        /// <param name="creature">The player to apply attacks to</param>
        /// <param name="rightHandWeapon">The weapon equipped to the right hand.</param>
        /// <param name="offHandItem">The off hand item equipped to the left hand.</param>
        void ApplyAttacksPerRound(uint creature, uint rightHandWeapon, uint offHandItem = OBJECT_INVALID);

        void ApplyCritModifier(uint player, uint rightHandWeapon);

        /// <summary>
        /// Returns the three-character shortened version of ability names.
        /// </summary>
        /// <param name="type">The type of ability to retrieve.</param>
        /// <returns>A three-character shortened version of the ability name.</returns>
        string GetAbilityNameShort(AbilityType type);

        /// <summary>
        /// Calculates the base value for a particular type of saving throw.
        /// This does not factor in stat modifiers.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="type">The type of saving throw.</param>
        /// <param name="offHandItem">The off hand item equipped to the left hand.</param>
        /// <returns>The base saving throw value</returns>
        int CalculateBaseSavingThrow(uint player, SavingThrowCategoryType type, uint offHandItem = OBJECT_INVALID);

        /// <summary>
        /// Stores an NPC's STM and FP as local variables.
        /// Also load their HP per their skin, if specified.
        /// </summary>
        void LoadNPCStats();

        /// <summary>
        /// Restores an NPC's STM and FP.
        /// </summary>
        void RestoreNPCStats(bool outOfCombatRegen);
    }
}