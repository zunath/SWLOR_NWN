using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using NWN;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IItemService
    {
        int[] ArmorBaseItemTypes { get; }
        int[] WeaponBaseItemTypes { get; }

        string GetNameByResref(string resref);
        CustomItemType GetCustomItemTypeByResref(string resref);
        void OnModuleActivatedItem();
        void OnModuleEquipItem();
        string OnModuleExamine(string existingDescription, NWPlayer examiner, NWObject examinedObject);
        void OnModuleHeartbeat();
        void ReturnItem(NWObject target, NWItem item);
        void StripAllItemProperties(NWItem item);
        CustomItemType GetCustomItemType(NWItem item);
        ItemProperty GetCustomItemPropertyByItemTag(string tag);
        void OnModuleItemAcquired();
        void OnModuleItemUnacquired();
    }
}
