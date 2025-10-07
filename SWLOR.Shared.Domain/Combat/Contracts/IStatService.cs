using NWN.Native.API;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Combat.ValueObjects;
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
        /// Retrieves the maximum FP on a creature.
        /// For players:
        /// Each Vitality modifier grants +2 to max FP.
        /// For NPCs:
        /// FP is read from their skin.
        /// </summary>
        /// <param name="creature">The creature object</param>
        /// <param name="dbPlayer">The player entity. If this is not set, a call to the DB will be made. Leave null for NPCs.</param>
        /// <returns>The max amount of FP</returns>
        int GetMaxFP(uint creature, Player dbPlayer = null);

        int GetMaxFP(int baseFP, int modifier, int bonus);

        /// <summary>
        /// Retrieves the current FP on a creature.
        /// </summary>
        /// <param name="creature">The creature to retrieve FP from.</param>
        /// <param name="dbPlayer">The player entity. If this is not set, a call to the DB will be made. Leave null for NPCs.</param>
        /// <returns>The current amount of FP.</returns>
        int GetCurrentFP(uint creature, Player dbPlayer = null);

        /// <summary>
        /// Retrieves the maximum STM on a creature.
        /// CON modifier will be checked. Each modifier grants +2 to max STM.
        /// </summary>
        /// <param name="creature">The creature object</param>
        /// <param name="dbPlayer">The player entity. If this is not set, a call to the DB will be made. Leave null for NPCs.</param>
        /// <returns>The max amount of STM</returns>
        int GetMaxStamina(uint creature, Player dbPlayer = null);

        int GetMaxStamina(int baseFP, int modifier, int bonus);

        /// <summary>
        /// Retrieves the current STM on a creature.
        /// </summary>
        /// <param name="creature">The creature to retrieve STM from.</param>
        /// <param name="dbPlayer">The player entity. If this is not set, a call to the DB will be made. Leave null for NPCs.</param>
        /// <returns>The current amount of STM.</returns>
        int GetCurrentStamina(uint creature, Player dbPlayer = null);

        /// <summary>
        /// Restores a creature's FP by a specified amount.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="amount">The amount of FP to restore.</param>
        /// <param name="dbPlayer">The player entity to modify. If this is not set, a call to the DB will be made. Leave null for NPCs.</param>
        void RestoreFP(uint creature, int amount, Player dbPlayer = null);

        /// <summary>
        /// Reduces a creature's FP by a specified amount.
        /// If creature would fall below 0 FP, they will be reduced to 0 instead.
        /// </summary>
        /// <param name="creature">The creature whose FP will be reduced.</param>
        /// <param name="reduceBy">The amount of FP to reduce by.</param>
        /// <param name="dbPlayer">The player entity to modify. If this is not set, a DB call will be made. Leave null for NPCs.</param>
        void ReduceFP(uint creature, int reduceBy, Player dbPlayer = null);

        /// <summary>
        /// Restores an entity's Stamina by a specified amount.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="amount">The amount of Stamina to restore.</param>
        /// <param name="dbPlayer">The player entity to modify. If this is not set, a DB call will be made. Leave null for NPCs.</param>
        void RestoreStamina(uint creature, int amount, Player dbPlayer = null);

        /// <summary>
        /// Reduces an entity's Stamina by a specified amount.
        /// If creature would fall below 0 stamina, they will be reduced to 0 instead.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="reduceBy">The amount of Stamina to reduce by.</param>
        /// <param name="dbPlayer">The entity to modify</param>
        void ReduceStamina(uint creature, int reduceBy, Player dbPlayer = null);

        /// <summary>
        /// After a player's status effects are reassociated,
        /// adjust any food HP if necessary.
        /// </summary>
        void ReapplyFoodHP();

        /// <summary>
        /// Increases or decreases a player's HP by a specified amount.
        /// There is a cap of 255 HP per NWN level. Players are auto-leveled to 40 by default, so this
        /// gives 255 * 40 = 10,200 HP maximum. If the player's HP would go over this amount, it will be set to 10,200.
        /// This method will not persist the changes so be sure you call _db.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="player">The player to adjust</param>
        /// <param name="adjustBy">The amount to adjust by.</param>
        void AdjustPlayerMaxHP(Player entity, uint player, int adjustBy);

        /// <summary>
        /// Modifies a player's maximum FP by a certain amount.
        /// This method will not persist the changes so be sure you call _db.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustPlayerMaxFP(Player entity, int adjustBy, uint player);

        /// <summary>
        /// Modifies a player's maximum STM by a certain amount.
        /// This method will not persist the changes so be sure you call _db.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustPlayerMaxSTM(Player entity, int adjustBy, uint player);

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
        /// Retrieves the total defense toward a specific type of damage.
        /// Physical and Force types include effect bonuses, stats, etc.
        /// Fire/Poison/Electrical/Ice include effect bonuses, stats, etc. at 70% of physical.
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
        /// Retrieves the total defense toward a specific type of damage.
        /// This is specifically for use with Native code and should not be referenced outside of there.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <param name="type">The type of damage to retrieve.</param>
        /// <param name="abilityType"></param>
        /// <returns>The defense value toward a given damage type.</returns>
        int GetDefenseNative(CNWSCreature creature, CombatDamageType type, AbilityType abilityType);

        /// <summary>
        /// Retrieves the stats of an NPC. This is determined by several item properties located on the NPC's skin.
        /// If no skin is equipped or the item properties do not exist, an empty NPCStats object will be returned.
        /// </summary>
        /// <returns>An NPCStats object.</returns>
        NPCStats GetNPCStats(uint npc);

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