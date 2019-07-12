using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;

using static NWN._;

namespace SWLOR.Game.Server.Placeable.StructureStorage
{
    public class OnDisturbed : IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWPlayer oPC = (_.GetLastDisturbed());
            NWItem item = (_.GetInventoryDisturbItem());
            NWPlaceable container = (NWGameObject.OBJECT_SELF);
            int disturbType = _.GetInventoryDisturbType();
            var structureID = new Guid(container.GetLocalString("PC_BASE_STRUCTURE_ID"));
            var structure = DataService.Single<PCBaseStructure>(x => x.ID == structureID);
            var baseStructure = DataService.Get<BaseStructure>(structure.BaseStructureID);
            int itemLimit = baseStructure.Storage + structure.StructureBonus;

            int itemCount = container.InventoryItems.Count();
            string itemResref = item.Resref;

            if (disturbType == INVENTORY_DISTURB_TYPE_ADDED)
            {
                if (_.GetHasInventory(item) == TRUE)
                {
                    item.SetLocalInt("RETURNING_ITEM", TRUE);
                    ItemService.ReturnItem(oPC, item);
                    oPC.SendMessage(ColorTokenService.Red("Containers cannot currently be stored inside banks."));
                    return;
                }
                
                if (itemCount > itemLimit)
                {
                    ItemService.ReturnItem(oPC, item);
                    oPC.SendMessage(ColorTokenService.Red("No more items can be placed inside."));
                }
                else if (item.BaseItemType == BASE_ITEM_GOLD)
                {
                    ItemService.ReturnItem(oPC, item);
                    oPC.SendMessage(ColorTokenService.Red("Credits cannot be placed inside."));
                }
                else
                {
                    PCBaseStructureItem itemEntity = new PCBaseStructureItem
                    {
                        ItemName = item.Name,
                        ItemResref = itemResref,
                        ItemTag = item.Tag,
                        PCBaseStructureID = structureID,
                        ItemGlobalID = item.GlobalID.ToString(),
                        ItemObject = SerializationService.Serialize(item)
                    };
                    DataService.SubmitDataChange(itemEntity, DatabaseActionType.Insert);
                }
            }
            else if (disturbType == INVENTORY_DISTURB_TYPE_REMOVED)
            {
                if (item.GetLocalInt("RETURNING_ITEM") == TRUE)
                {
                    item.DeleteLocalInt("RETURNING_ITEM");
                }
                else
                {
                    var dbItem = DataService.Single<PCBaseStructureItem>(x => x.ItemGlobalID == item.GlobalID.ToString());
                    DataService.SubmitDataChange(dbItem, DatabaseActionType.Delete);
                }
            }

            oPC.SendMessage(ColorTokenService.White("Item Limit: " + itemCount + " / ") + ColorTokenService.Red(itemLimit.ToString()));
        }
    }
}
