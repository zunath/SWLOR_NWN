using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripts.Placeable.StructureStorage
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
            var structure = DataService.PCBaseStructure.GetByID(structureID);
            var baseStructure = DataService.BaseStructure.GetByID(structure.BaseStructureID);
            int itemLimit = baseStructure.Storage + structure.StructureBonus;

            int itemCount = container.InventoryItems.Count();
            string itemResref = item.Resref;

            if (disturbType == _.INVENTORY_DISTURB_TYPE_ADDED)
            {
                if (_.GetHasInventory(item) == _.TRUE)
                {
                    item.SetLocalInt("RETURNING_ITEM", _.TRUE);
                    ItemService.ReturnItem(oPC, item);
                    oPC.SendMessage(ColorTokenService.Red("Containers cannot currently be stored inside banks."));
                    return;
                }
                
                if (itemCount > itemLimit)
                {
                    ItemService.ReturnItem(oPC, item);
                    oPC.SendMessage(ColorTokenService.Red("No more items can be placed inside."));
                }
                else if (item.BaseItemType == _.BASE_ITEM_GOLD)
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
                    MessageHub.Instance.Publish(new OnStoreStructureItem(oPC, itemEntity));
                }
            }
            else if (disturbType == _.INVENTORY_DISTURB_TYPE_REMOVED)
            {
                if (item.GetLocalInt("RETURNING_ITEM") == _.TRUE)
                {
                    item.DeleteLocalInt("RETURNING_ITEM");
                }
                else
                {
                    var dbItem = DataService.PCBaseStructureItem.GetByItemGlobalID(item.GlobalID.ToString());
                    DataService.SubmitDataChange(dbItem, DatabaseActionType.Delete);
                    MessageHub.Instance.Publish(new OnRemoveStructureItem(oPC, dbItem));
                }
            }

            oPC.SendMessage(ColorTokenService.White("Item Limit: " + itemCount + " / ") + ColorTokenService.Red(itemLimit.ToString()));
        }
    }
}
