using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Perks: the definition caches, a creature's perk levels, the stat bonuses and granted feats that follow from them,
    /// and refunds. The logic lives in <see cref="IPerkService"/>; the event handlers stay here and every member
    /// forwards to the container's instance.
    /// </summary>
    public static class Perk
    {
        private static IPerkService Instance => FacadeAccessor.Resolve<IPerkService>();

        /// <summary>
        /// When the module loads, cache all perk and character type information.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheData()
        {
            Instance.CacheData();
        }

        [NWNEventHandler(ScriptName.OnModuleCacheAfter)]
        public static void CachePerkFeatData()
        {
            Instance.CachePerkFeatData();
        }

        /// <summary>
        /// Retrieves all of the equip triggers registered by perks.
        /// </summary>
        /// <returns></returns>
        public static Dictionary<PerkType, List<PerkTriggerEquippedAction>> GetAllEquipTriggers()
        {
            return Instance.GetAllEquipTriggers();
        }

        /// <summary>
        /// Retrieves all of the unequip triggers registered by perks.
        /// </summary>
        /// <returns></returns>
        public static Dictionary<PerkType, List<PerkTriggerUnequippedAction>> GetAllUnequipTriggers()
        {
            return Instance.GetAllUnequipTriggers();
        }

        /// <summary>
        /// Retrieves all of the purchase triggers registered by perks.
        /// </summary>
        /// <returns></returns>
        public static Dictionary<PerkType, List<PerkTriggerPurchasedRefundedAction>> GetAllPurchaseTriggers()
        {
            return Instance.GetAllPurchaseTriggers();
        }

        /// <summary>
        /// Retrieves all of the refund triggers registered by perks.
        /// </summary>
        /// <returns></returns>
        public static Dictionary<PerkType, List<PerkTriggerPurchasedRefundedAction>> GetAllRefundTriggers()
        {
            return Instance.GetAllRefundTriggers();
        }

        /// <summary>
        /// Retrieves a list of all perks, including inactive ones.
        /// </summary>
        /// <returns>A list of all perks.</returns>
        public static Dictionary<PerkType, PerkDetail> GetAllPerks()
        {
            return Instance.GetAllPerks();
        }

        public static int GetForceAffinity(uint creature)
        {
            return Instance.GetForceAffinity(creature);
        }

        public static bool TryGetForceSideAffinity(uint creature, PerkType perkType, out int sideAffinity)
        {
            return Instance.TryGetForceSideAffinity(creature, perkType, out sideAffinity);
        }

        public static float GetForceAffinityMagnitudeMultiplier(uint creature, PerkType perkType)
        {
            return Instance.GetForceAffinityMagnitudeMultiplier(creature, perkType);
        }

        public static int GetForceAffinityHitChanceAdjustment(uint creature, PerkType perkType)
        {
            return Instance.GetForceAffinityHitChanceAdjustment(creature, perkType);
        }

        public static float GetForceAffinityRestorativeMagnitudeMultiplier(uint creature, PerkType perkType)
        {
            return Instance.GetForceAffinityRestorativeMagnitudeMultiplier(creature, perkType);
        }

        public static int ApplyForceAffinityMagnitude(uint creature, PerkType perkType, int amount)
        {
            return Instance.ApplyForceAffinityMagnitude(creature, perkType, amount);
        }

        /// <summary>Applies affinity to a heal, shield or regeneration amount, which gains at most +25%.</summary>
        public static int ApplyForceAffinityRestorativeMagnitude(uint creature, PerkType perkType, int amount)
        {
            return Instance.ApplyForceAffinityRestorativeMagnitude(creature, perkType, amount);
        }

        public static float ApplyForceAffinityMagnitude(uint creature, PerkType perkType, float amount)
        {
            return Instance.ApplyForceAffinityMagnitude(creature, perkType, amount);
        }

        public static IReadOnlyList<StatAdjustmentSource> GetStatSources(uint creature, StatType payloadStat)
        {
            return Instance.GetStatSources(creature, payloadStat);
        }

        public static int GetStatBonus(uint creature, StatType stat)
        {
            return Instance.GetStatBonus(creature, stat);
        }

        public static int GetTargetedStatBonus( uint creature, PerkType targetPerkType, StatType primaryPerkStatType, StatType secondaryPerkStatType, StatType adjustmentStatType)
        {
            return Instance.GetTargetedStatBonus(creature, targetPerkType, primaryPerkStatType, secondaryPerkStatType, adjustmentStatType);
        }

        /// <summary>
        /// Retrieves a list of all active perks, excluding inactive ones, by group.
        /// </summary>
        /// <returns>A list of all active perks.</returns>
        public static Dictionary<PerkType, PerkDetail> GetAllActivePerks(PerkGroupType group)
        {
            return Instance.GetAllActivePerks(group);
        }

        /// <summary>
        /// Retrieves a list of all perk categories, including inactive ones.
        /// </summary>
        /// <returns>A list of all perk categories.</returns>
        public static Dictionary<PerkCategoryType, PerkCategoryAttribute> GetAllPerkCategories()
        {
            return Instance.GetAllPerkCategories();
        }

        /// <summary>
        /// Retrieves a list of all active perk categories, excluding inactive ones.
        /// </summary>
        /// <returns>A list of all active perk categories.</returns>
        public static Dictionary<PerkCategoryType, PerkCategoryAttribute> GetAllActivePerkCategories(PerkGroupType group)
        {
            return Instance.GetAllActivePerkCategories(group);
        }

        /// <summary>
        /// Retrieves a list of all active perks by the specified category, by group.
        /// </summary>
        /// <param name="group">The group to filter by.</param>
        /// <param name="category">The category to search by.</param>
        /// <returns>A list of all active perks in the specified category.</returns>
        public static Dictionary<PerkType, PerkDetail> GetActivePerksInCategory(PerkGroupType group, PerkCategoryType category)
        {
            return Instance.GetActivePerksInCategory(group, category);
        }

        /// <summary>
        /// Retrieves details about an individual perk.
        /// </summary>
        /// <param name="perkType">The type of perk to retrieve.</param>
        /// <returns>An object containing a perk's details.</returns>
        public static PerkDetail GetPerkDetails(PerkType perkType)
        {
            return Instance.GetPerkDetails(perkType);
        }

        public static bool TryGetPerkDetails(PerkType perkType, out PerkDetail detail)
        {
            return Instance.TryGetPerkDetails(perkType, out detail);
        }

        public static PerkCategoryType GetPerkCategoryType(PerkType perkType)
        {
            return Instance.GetPerkCategoryType(perkType);
        }

        public static bool IsPerkInCategory(PerkType perkType, int categoryValue)
        {
            return Instance.IsPerkInCategory(perkType, categoryValue);
        }

        /// <summary>
        /// Retrieves details about an individual perk category.
        /// </summary>
        /// <param name="categoryType">The type of category to retrieve.</param>
        /// <returns>An object containing a perk category's details.</returns>
        public static PerkCategoryAttribute GetPerkCategoryDetails(PerkCategoryType categoryType)
        {
            return Instance.GetPerkCategoryDetails(categoryType);
        }

        /// <summary>
        /// Retrieves the detail about a specific character type.
        /// </summary>
        /// <param name="characterType">The character type to retrieve.</param>
        /// <returns>A character type detail.</returns>
        public static CharacterTypeAttribute GetCharacterType(CharacterType characterType)
        {
            return Instance.GetCharacterType(characterType);
        }

        /// <summary>
        /// Retrieves the tier of a specific perk level.
        /// </summary>
        /// <param name="perkType">The type of perk</param>
        /// <param name="perkLevel">The level of the perk</param>
        /// <returns>The tier of the perk level. Returns 0 if unable to be determined.</returns>
        public static int GetPerkLevelTier(PerkType perkType, int perkLevel)
        {
            return Instance.GetPerkLevelTier(perkType, perkLevel);
        }

        /// <summary>
        /// Retrieves the perk level of a creature.
        /// On NPCs, this will retrieve the "PERK_LEVEL_{perkId}" variable, where {perkId} is replaced with the ID of the perk.
        /// If this variable is not set, the max level of the perk will be used instead.
        /// On PCs, this will retrieve the current perk level. It does not take into account any skill decay and should be
        /// treated as a "soft" check as requirements are assumed to have been checked prior.
        /// It is handled this way for performance reasons (checking requirements on perks is very expensive).
        /// If you need to perform a "hard" check on requirements, use GetEffectivePerkLevel instead.
        /// </summary>
        /// <param name="creature">The creature whose perk level will be retrieved.</param>
        /// <param name="perkType">The type of perk to retrieve.</param>
        /// <returns>The perk level of a creature.</returns>
        public static int GetPerkLevel(uint creature, PerkType perkType)
        {
            return Instance.GetPerkLevel(creature, perkType);
        }

        public static IReadOnlyList<FeatType> GetGrantedFeatsForPerkLevel(PerkType perkType, int perkLevel)
        {
            return Instance.GetGrantedFeatsForPerkLevel(perkType, perkLevel);
        }

        public static IReadOnlyList<FeatType> GetCurrentActiveAbilityFeats(PerkType perkType, int perkLevel)
        {
            return Instance.GetCurrentActiveAbilityFeats(perkType, perkLevel);
        }

        public static IReadOnlyList<FeatType> GetAllActiveAbilityFeats(PerkType perkType)
        {
            return Instance.GetAllActiveAbilityFeats(perkType);
        }

        public static RecastGroup GetActiveAbilityRecastGroup(PerkType perkType)
        {
            return Instance.GetActiveAbilityRecastGroup(perkType);
        }

        /// <summary>
        /// Restores native feats from purchased perks after loading or migrating a character file.
        /// This does not repeat purchase triggers or change the player's quickbar layout.
        /// </summary>
        public static void RestorePlayerFeats(uint player)
        {
            Instance.RestorePlayerFeats(player);
        }

        public static void SyncGrantedFeats(uint creature, PerkType perkType, int perkLevel, bool addByLevel)
        {
            Instance.SyncGrantedFeats(creature, perkType, perkLevel, addByLevel);
        }

        /// <summary>
        /// Removes declared self effects and effects granted by the refunded perk's abilities.
        /// Source-owned effects retain their ability identity even when another perk uses the same status class.
        /// </summary>
        public static void RemoveStatusEffectsOnPerkRefund(uint creature, PerkType perkType)
        {
            Instance.RemoveStatusEffectsOnPerkRefund(creature, perkType);
        }

        /// <summary>
        /// Selects distinct ability/status pairs for a perk refund. Deduplication retains separate
        /// abilities that share a status class so each cleanup uses its own originating ability.
        /// </summary>
        public static IReadOnlyList<(AbilityDetail Ability, Type StatusEffectType)> GetSourceOwnedStatusEffectRefunds( IEnumerable<AbilityDetail> abilities, PerkType perkType)
        {
            return Instance.GetSourceOwnedStatusEffectRefunds(abilities, perkType);
        }

        public static bool ShouldEnforceActiveAbilityFeatReplacement(uint creature, PerkType perkType)
        {
            return Instance.ShouldEnforceActiveAbilityFeatReplacement(creature, perkType);
        }

        public static bool IsCurrentActiveAbilityFeat(FeatType feat, PerkType perkType, int perkLevel)
        {
            return Instance.IsCurrentActiveAbilityFeat(feat, perkType, perkLevel);
        }

        /// <summary>
        /// Retrieves a player's effective perk level.
        /// This performs a "hard" check on all perk requirements. This process is VERY expensive so please use sparingly.
        /// It is almost always better to use GetPerkLevel instead of this method.
        /// </summary>
        /// <param name="player">The player whose perk level we're retrieving</param>
        /// <param name="perkType">The type of perk we're retrieving</param>
        /// <returns>The player's effective perk level.</returns>
        public static int GetPlayerEffectivePerkLevel(uint player, PerkType perkType)
        {
            return Instance.GetPlayerEffectivePerkLevel(player, perkType);
        }

        /// <summary>
        /// This will mark a perk as unlocked for a player.
        /// If the perk does not have an "unlock requirement", nothing will happen.
        /// This will do a DB call so be sure to refresh your entity instance after calling this.
        /// </summary>
        /// <param name="player">The player to unlock the perk for</param>
        /// <param name="perkType">The type of perk to unlock for the player</param>
        public static void UnlockPerkForPlayer(uint player, PerkType perkType)
        {
            Instance.UnlockPerkForPlayer(player, perkType);
        }

        /// <summary>
        /// When a skill receives decay, any perks tied to that skill should be checked.
        /// If the player no longer meets the requirements for those perks, they should be reduced in level.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSwlorLoseSkill)]
        public static void RemovePerkLevelOnSkillDecay()
        {
            Instance.RemovePerkLevelOnSkillDecay();
        }
    }
}
