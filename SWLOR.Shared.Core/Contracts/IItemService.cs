using System.Collections.Generic;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.UI.Model;

namespace SWLOR.Shared.Core.Contracts
{
    public interface IItemService
    {
        void CacheData();
        void Load2DACache();
        void LoadItemToDamageStatMapping();
        void LoadItemToAccuracyStatMapping();
        AbilityType GetWeaponDamageAbilityType(BaseItem itemType);
        AbilityType GetWeaponAccuracyAbilityType(BaseItem itemType);
        void UseItem();
        bool CanCreatureUseItem(uint creature, uint item);
        void ReturnItem(uint target, uint item);
        int GetInventoryItemCount(uint obj);
        ArmorType GetArmorType(uint item);
        string GetIconResref(uint item);
        string BuildItemPropertyString(uint item);
        GuiBindingList<string> BuildItemPropertyList(uint item);
        GuiBindingList<string> BuildItemPropertyList(List<ItemProperty> itemProperties);
        string CanBePersistentlyStored(uint player, uint item);
        int GetDMG(uint item);
        int GetCriticalModifier(BaseItem type);
        bool ReduceItemStack(uint item, int reduceBy);
        bool IsLegacyItem(uint item);
        void MarkLegacyItem(uint item);
        InventorySlot GetItemSlot(uint creature, uint item);
        
        // Static properties
        List<BaseItem> WeaponBaseItemTypes { get; }
        List<BaseItem> ArmorBaseItemTypes { get; }
        List<BaseItem> ShieldBaseItemTypes { get; }
        List<BaseItem> VibrobladeBaseItemTypes { get; }
        List<BaseItem> FinesseVibrobladeBaseItemTypes { get; }
        List<BaseItem> LightsaberBaseItemTypes { get; }
        List<BaseItem> HeavyVibrobladeBaseItemTypes { get; }
        List<BaseItem> PolearmBaseItemTypes { get; }
        List<BaseItem> TwinBladeBaseItemTypes { get; }
        List<BaseItem> SaberstaffBaseItemTypes { get; }
        List<BaseItem> KatarBaseItemTypes { get; }
        List<BaseItem> StaffBaseItemTypes { get; }
        List<BaseItem> PistolBaseItemTypes { get; }
        List<BaseItem> ThrowingWeaponBaseItemTypes { get; }
        List<BaseItem> RifleBaseItemTypes { get; }
        List<BaseItem> OneHandedMeleeItemTypes { get; }
        List<BaseItem> TwoHandedMeleeItemTypes { get; }
        List<BaseItem> CreatureBaseItemTypes { get; }
        List<BaseItem> DroidBaseItemTypes { get; }
    }
}
