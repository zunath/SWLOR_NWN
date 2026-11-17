using System.Collections.Generic;
using NWN.Native.API;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using Player = SWLOR.Game.Server.Entity.Player;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// Stats: the stat adjustment read and its sources, the FP and Stamina pools, the NPC stat skin reads, and the Detection,
    /// Stealth, Lockpicking, Combat Readiness, Control and Craftsmanship reads. Walk mode, the movement rate, maximum HP,
    /// attributes, NPC loading and regeneration are <c>ICreatureStatService</c>'s and the player records fed by worn gear are
    /// <c>IStatRecordService</c>'s (P5-4e).
    /// The ratings and the deflection and Guard chances are <c>ICombatRatingService</c>'s (Combat, P5-4c); what a deflection
    /// sets off is <c>ICombatDeflectionReactionService</c>'s and the healing adjustments are <c>ICombatResourcesService</c>'s
    /// (Combat, P5-4d).
    /// </summary>
    public interface IStatService
    {
        void CacheData();

        StatTypeCategory GetStatTypeCategory(StatType statType);

        StatTypeAggregation GetStatTypeAggregation(StatType statType);

        DeflectionSource GetStatTypeDeflectionSource(StatType statType);

        StatType GetGrantedDeflectionStatType(StatType sourceStatType);

        int AggregateStatAdjustment(StatType statType, int current, int adjustment);

        bool IsBeneficialStatAdjustment(StatType statType, int value);

        /// <summary>
        /// Registers what equipping an item property does to its wearer. <c>EquipmentStats.RegisterStatActions</c> fills
        /// this when the module loads; until then equipping applies nothing.
        /// </summary>
        void RegisterEquipmentStatAction(ItemPropertyType type, StatEquipmentAction action);

        /// <summary>
        /// Finds what equipping an item property does to its wearer.
        /// </summary>
        bool TryGetEquipmentStatAction(ItemPropertyType type, out StatEquipmentAction action);

        /// <summary>
        /// The item property types equipping adds to the wearer.
        /// </summary>
        IReadOnlyCollection<ItemPropertyType> EquipmentStatActionTypes { get; }

        int ScaleEffect(
            int baseAmount,
            int primaryStat,
            float primaryRate = 0.01f,
            int secondaryStat = 0,
            float secondaryRate = 0.005f);

        /// <summary>
        /// Retrieves the maximum FP on a creature.
        /// For players:
        /// Each point of Willpower grants +3 to max FP.
        /// For NPCs:
        /// FP is read from their skin.
        /// </summary>
        /// <param name="creature">The creature object</param>
        /// <param name="dbPlayer">The player entity. If this is not set, a call to the DB will be made. Leave null for NPCs.</param>
        /// <returns>The max amount of FP</returns>
        int GetMaxFP(uint creature, Player dbPlayer = null);

        int GetMaxFP(int baseFP, int willpower, int bonus);

        /// <summary>
        /// Retrieves the current FP on a creature.
        /// </summary>
        /// <param name="creature">The creature to retrieve FP from.</param>
        /// <param name="dbPlayer">The player entity. If this is not set, a call to the DB will be made. Leave null for NPCs.</param>
        /// <returns>The current amount of FP.</returns>
        int GetCurrentFP(uint creature, Player dbPlayer = null);

        int GetAdjustedRequiredFP(uint creature, int requiredFP);

        /// <summary>
        /// Retrieves the maximum STM on a creature.
        /// Each point of Might grants +1.5 to max STM.
        /// </summary>
        /// <param name="creature">The creature object</param>
        /// <param name="dbPlayer">The player entity. If this is not set, a call to the DB will be made. Leave null for NPCs.</param>
        /// <returns>The max amount of STM</returns>
        int GetMaxStamina(uint creature, Player dbPlayer = null);

        int GetMaxStamina(int baseStamina, int might, int bonus);

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
        /// <param name="sendFeedback">Whether to report the actual gain in diagnostic environments. Disable for natural regeneration and rest.</param>
        /// <returns>The amount of FP actually restored after modifiers and the maximum-FP cap.</returns>
        int RestoreFP(uint creature, int amount, Player dbPlayer = null, bool sendFeedback = true);

        /// <summary>
        /// Reduces a creature's FP by a specified amount.
        /// If creature would fall below 0 FP, they will be reduced to 0 instead.
        /// </summary>
        /// <param name="creature">The creature whose FP will be reduced.</param>
        /// <param name="reduceBy">The amount of FP to reduce by.</param>
        /// <param name="dbPlayer">The player entity to modify. If this is not set, a DB call will be made. Leave null for NPCs.</param>
        void ReduceFP(uint creature, int reduceBy, Player dbPlayer = null);

        /// <summary>
        /// Stealth's stamina drain is its only time limit, so a stealthed player gains no Stamina
        /// from any source: natural regeneration, healing over time, field tools, or stances.
        /// </summary>
        bool IsStaminaRestorationBlocked(bool isPlayer, bool inStealth);

        /// <summary>
        /// Restores an entity's Stamina by a specified amount.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="amount">The amount of Stamina to restore.</param>
        /// <param name="dbPlayer">The player entity to modify. If this is not set, a DB call will be made. Leave null for NPCs.</param>
        /// <param name="sendFeedback">Whether to report the actual gain in diagnostic environments. Disable for natural regeneration and rest.</param>
        /// <returns>The amount of Stamina actually restored after the maximum-Stamina cap.</returns>
        int RestoreStamina(uint creature, int amount, Player dbPlayer = null, bool sendFeedback = true);

        /// <summary>
        /// Reduces an entity's Stamina by a specified amount.
        /// If creature would fall below 0 stamina, they will be reduced to 0 instead.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="reduceBy">The amount of Stamina to reduce by.</param>
        /// <param name="dbPlayer">The entity to modify</param>
        void ReduceStamina(uint creature, int reduceBy, Player dbPlayer = null);

        int GetCombatReadinessPercent(uint creature);

        /// <summary>
        /// Retrieves a creature's detection rating, used against Stealth in the opposed stealth detection check.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <returns>The detection rating of a creature.</returns>
        int GetDetection(uint creature);

        int CalculateDetectionRating(
            int perception,
            int willpower,
            int equipmentBonus,
            int adjustment,
            bool detectMode);

        int ApplyNPCDetectionCap(int detection, bool isNPC);

        /// <summary>
        /// Retrieves a creature's stealth rating, used against Detection in the opposed stealth detection check.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <returns>The stealth rating of a creature.</returns>
        /// <summary>
        /// Retrieves a creature's lockpicking: equipment plus perk, status, and temporary adjustments.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <returns>The lockpicking rating of a creature.</returns>
        int GetLockpicking(uint creature);

        int GetStealth(uint creature);

        int CalculateStealthRating(int agility, int equipmentBonus, int adjustment);

        IReadOnlyList<StatAdjustmentSource> GetStatSources(uint creature, StatType payloadStat);

        int GetStatAdjustment(uint creature, StatType stat);

        int GetStatAdjustmentExcludingTemporaryModifiers(uint creature, StatType stat);

        /// <summary>
        /// Retrieves the stats of an NPC. This is determined by several item properties located on the NPC's skin.
        /// If no skin is equipped or the item properties do not exist, an empty NPCStats object will be returned.
        /// </summary>
        /// <returns>An NPCStats object.</returns>
        NPCStats GetNPCStats(uint npc);

        /// <summary>
        /// Reads the NPC stats a stat skin grants. Works on any skin item, equipped or not.
        /// </summary>
        /// <param name="skin">The stat skin item.</param>
        /// <returns>An NPCStats object.</returns>
        NPCStats GetNPCSkinStats(uint skin);

        NPCStats GetNPCStatsNative(CNWSCreature npc);

        /// <summary>
        /// Returns the three-character shortened version of ability names.
        /// </summary>
        /// <param name="type">The type of ability to retrieve.</param>
        /// <returns>A three-character shortened version of the ability name.</returns>
        string GetAbilityNameShort(AbilityType type);

        /// <summary>
        /// Calculates the total Control for a player in a given crafting skill.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="craftingSkillType">The skill to check</param>
        /// <returns>The total control for a player</returns>
        /// <exception cref="ArgumentException">Thrown if a non-crafting skill is passed in.</exception>
        int CalculateControl(uint player, SkillType craftingSkillType);

        /// <summary>
        /// Calculates the total Craftsmanship for a player in a given crafting skill.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="craftingSkillType">The skill to check</param>
        /// <returns>The total Craftsmanship for a player</returns>
        /// <exception cref="ArgumentException">Thrown if a non-crafting skill is passed in.</exception>
        int CalculateCraftsmanship(uint player, SkillType craftingSkillType);

        /// <summary>
        /// Returns the maximum HP a stat skin grants, or 0 when it grants none.
        /// </summary>
        /// <param name="skin">The stat skin item.</param>
        int GetNPCSkinMaxHP(uint skin);
    }
}
