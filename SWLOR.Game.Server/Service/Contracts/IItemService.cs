using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.NWScript;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IItemService
    {
        int[] ArmorBaseItemTypes { get; }
        int[] WeaponBaseItemTypes { get; }

        string GetNameByResref(string resref);
        void OnModuleActivatedItem();
        void OnModuleEquipItem();
        string OnModuleExamine(string existingDescription, NWPlayer examiner, NWObject examinedObject);
        void OnModuleHeartbeat();
        void ReturnItem(NWObject target, NWItem item);
        void StripAllItemProperties(NWItem item);
        CustomItemType GetCustomItemType(NWItem item);
        ItemProperty GetCustomItemPropertyByItemTag(string tag);
    }
}
