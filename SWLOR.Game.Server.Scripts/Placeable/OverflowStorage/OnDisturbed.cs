using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripts.Placeable.OverflowStorage
{
    public class OnDisturbed: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWPlaceable container = (NWGameObject.OBJECT_SELF);
            NWPlayer oPC = (_.GetLastDisturbed());
            NWItem oItem = (_.GetInventoryDisturbItem());
            int type = _.GetInventoryDisturbType();

            if (type == _.INVENTORY_DISTURB_TYPE_ADDED)
            {
                container.AssignCommand(() => _.ActionGiveItem(oItem.Object, oPC.Object));
                return;
            }
            
            Guid overflowItemID = new Guid(oItem.GetLocalString("TEMP_OVERFLOW_ITEM_ID"));
            PCOverflowItem overflowItem = DataService.PCOverflowItem.GetByID(overflowItemID);
            DataService.SubmitDataChange(overflowItem, DatabaseActionType.Delete);
            oItem.DeleteLocalInt("TEMP_OVERFLOW_ITEM_ID");

            if (!container.InventoryItems.Any())
            {
                container.Destroy();
            }
        }
    }
}
