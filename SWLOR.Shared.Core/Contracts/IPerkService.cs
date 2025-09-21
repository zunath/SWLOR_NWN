using System.Collections.Generic;
using SWLOR.Shared.Core.Enums;

namespace SWLOR.Shared.Core.Contracts
{
    public interface IPerkService
    {
        void CacheData();
        Dictionary<PerkType, List<PerkTriggerEquippedAction>> GetAllEquipTriggers();
        Dictionary<PerkType, List<PerkTriggerUnequippedAction>> GetAllUnequipTriggers();
        Dictionary<PerkType, List<PerkTriggerPurchasedRefundedAction>> GetAllPurchaseTriggers();
        Dictionary<PerkType, List<PerkTriggerPurchasedRefundedAction>> GetAllRefundTriggers();
        Dictionary<PerkType, PerkDetail> GetAllPerks();
        Dictionary<PerkType, PerkDetail> GetAllActivePerks(PerkGroupType group);
        Dictionary<PerkCategoryType, PerkCategoryAttribute> GetAllPerkCategories();
        Dictionary<PerkCategoryType, PerkCategoryAttribute> GetAllActivePerkCategories();
        Dictionary<PerkCategoryType, PerkCategoryAttribute> GetAllActivePerkCategories(PerkGroupType group);
        Dictionary<PerkType, PerkDetail> GetActivePerksInCategory(PerkCategoryType category);
        Dictionary<PerkType, PerkDetail> GetActivePerksInCategory(PerkGroupType group, PerkCategoryType category);
        PerkDetail GetPerkDetails(PerkType perkType);
        PerkCategoryAttribute GetPerkCategoryDetails(PerkCategoryType categoryType);
        CharacterTypeAttribute GetCharacterType(CharacterType characterType);
        int GetPerkLevelTier(PerkType perkType, int perkLevel);
        int GetPerkLevel(uint creature, PerkType perkType);
        int GetPlayerEffectivePerkLevel(uint player, PerkType perkType);
        void UnlockPerkForPlayer(uint player, PerkType perkType);
        void RemovePerkLevelOnSkillDecay();
        
        // Static properties
        List<PerkType> HeavyArmorPerks { get; }
        List<PerkType> LightArmorPerks { get; }
    }
}
