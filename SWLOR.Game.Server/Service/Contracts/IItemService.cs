using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using NWN;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IItemService
    {
        string GetNameByResref(string resref);
        CustomItemType GetCustomItemTypeByResref(string resref);
        void OnModuleActivatedItem();
        void OnModuleUnequipItem();
        void OnModuleEquipItem();
        string OnModuleExamine(string existingDescription, NWPlayer examiner, NWObject examinedObject);
        void ReturnItem(NWObject target, NWItem item);
        void StripAllItemProperties(NWItem item);
        ItemProperty GetCustomItemPropertyByItemTag(string tag);
        void FinishActionItem(IActionItem actionItem, NWPlayer user, NWItem item, NWObject target, Location targetLocation, Vector userStartPosition, CustomData customData);
        SkillType GetSkillTypeForItem(NWItem item);
    }
}
