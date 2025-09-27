using SWLOR.Shared.Domain.Ability.Delegates;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Perk.ValueObjects;

namespace SWLOR.Shared.Domain.Perk.Contracts
{
    public interface IPerkService
    {
        /// <summary>
        /// Gets the list of heavy armor perks
        /// </summary>
        List<PerkType> HeavyArmorPerks { get; }

        /// <summary>
        /// Gets the list of light armor perks
        /// </summary>
        List<PerkType> LightArmorPerks { get; }

        /// <summary>
        /// When the module loads, cache all perk and character type information.
        /// </summary>
        void CacheData();

        /// <summary>
        /// Retrieves all of the equip triggers registered by perks.
        /// </summary>
        /// <returns></returns>
        Dictionary<PerkType, List<PerkTriggerEquippedAction>> GetAllEquipTriggers();

        /// <summary>
        /// Retrieves all of the unequip triggers registered by perks.
        /// </summary>
        /// <returns></returns>
        Dictionary<PerkType, List<PerkTriggerUnequippedAction>> GetAllUnequipTriggers();

        /// <summary>
        /// Retrieves all of the purchase triggers registered by perks.
        /// </summary>
        /// <returns></returns>
        Dictionary<PerkType, List<PerkTriggerPurchasedRefundedAction>> GetAllPurchaseTriggers();

        /// <summary>
        /// Retrieves all of the refund triggers registered by perks.
        /// </summary>
        /// <returns></returns>
        Dictionary<PerkType, List<PerkTriggerPurchasedRefundedAction>> GetAllRefundTriggers();

        /// <summary>
        /// Retrieves a list of all perks, including inactive ones.
        /// </summary>
        /// <returns>A list of all perks.</returns>
        Dictionary<PerkType, PerkDetail> GetAllPerks();

        /// <summary>
        /// Retrieves a list of all active perks, excluding inactive ones, by group.
        /// </summary>
        /// <returns>A list of all active perks.</returns>
        Dictionary<PerkType, PerkDetail> GetAllActivePerks(PerkGroupType group);

        /// <summary>
        /// Retrieves a list of all perk categories, including inactive ones.
        /// </summary>
        /// <returns>A list of all perk categories.</returns>
        Dictionary<PerkCategoryType, PerkCategoryAttribute> GetAllPerkCategories();

        /// <summary>
        /// Retrieves a list of all active perk categories, excluding inactive ones.
        /// </summary>
        /// <returns>A list of all active perk categories.</returns>
        Dictionary<PerkCategoryType, PerkCategoryAttribute> GetAllActivePerkCategories();

        /// <summary>
        /// Retrieves a list of all active perk categories for a specific group, excluding inactive ones.
        /// </summary>
        /// <param name="group">The group to filter by.</param>
        /// <returns>A list of all active perk categories for the specified group.</returns>
        Dictionary<PerkCategoryType, PerkCategoryAttribute> GetAllActivePerkCategories(PerkGroupType group);

        /// <summary>
        /// Retrieves a list of all active perks by the specified category.
        /// </summary>
        /// <param name="category">The category to search by.</param>
        /// <returns>A list of all active perks in the specified category.</returns>
        Dictionary<PerkType, PerkDetail> GetActivePerksInCategory(PerkCategoryType category);

        /// <summary>
        /// Retrieves a list of all active perks by the specified category and group.
        /// </summary>
        /// <param name="group">The group to filter by.</param>
        /// <param name="category">The category to search by.</param>
        /// <returns>A list of all active perks in the specified category and group.</returns>
        Dictionary<PerkType, PerkDetail> GetActivePerksInCategory(PerkGroupType group, PerkCategoryType category);

        /// <summary>
        /// Retrieves details about an individual perk.
        /// </summary>
        /// <param name="perkType">The type of perk to retrieve.</param>
        /// <returns>An object containing a perk's details.</returns>
        PerkDetail GetPerkDetails(PerkType perkType);

        /// <summary>
        /// Retrieves details about an individual perk category.
        /// </summary>
        /// <param name="categoryType">The type of category to retrieve.</param>
        /// <returns>An object containing a perk category's details.</returns>
        PerkCategoryAttribute GetPerkCategoryDetails(PerkCategoryType categoryType);

        /// <summary>
        /// Retrieves the detail about a specific character type.
        /// </summary>
        /// <param name="characterType">The character type to retrieve.</param>
        /// <returns>A character type detail.</returns>
        CharacterTypeAttribute GetCharacterType(CharacterType characterType);

        /// <summary>
        /// Retrieves the tier of a specific perk level.
        /// </summary>
        /// <param name="perkType">The type of perk</param>
        /// <param name="perkLevel">The level of the perk</param>
        /// <returns>The tier of the perk level. Returns 0 if unable to be determined.</returns>
        int GetPerkLevelTier(PerkType perkType, int perkLevel);

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
        int GetPerkLevel(uint creature, PerkType perkType);

        /// <summary>
        /// Retrieves a player's effective perk level.
        /// This performs a "hard" check on all perk requirements. This process is VERY expensive so please use sparingly.
        /// It is almost always better to use GetPerkLevel instead of this method.
        /// </summary>
        /// <param name="player">The player whose perk level we're retrieving</param>
        /// <param name="perkType">The type of perk we're retrieving</param>
        /// <returns>The player's effective perk level.</returns>
        int GetPlayerEffectivePerkLevel(uint player, PerkType perkType);

        /// <summary>
        /// This will mark a perk as unlocked for a player.
        /// If the perk does not have an "unlock requirement", nothing will happen.
        /// This will do a DB call so be sure to refresh your entity instance after calling this.
        /// </summary>
        /// <param name="player">The player to unlock the perk for</param>
        /// <param name="perkType">The type of perk to unlock for the player</param>
        void UnlockPerkForPlayer(uint player, PerkType perkType);

        /// <summary>
        /// When a skill receives decay, any perks tied to that skill should be checked.
        /// If the player no longer meets the requirements for those perks, they should be reduced in level.
        /// </summary>
        void RemovePerkLevelOnSkillDecay();
    }
}