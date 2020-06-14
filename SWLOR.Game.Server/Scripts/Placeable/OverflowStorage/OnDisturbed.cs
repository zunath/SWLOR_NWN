using System;
using System.Linq;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Enum;
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
            NWPlaceable container = (_.OBJECT_SELF);
            NWPlayer oPC = (_.GetLastDisturbed());
            NWItem oItem = (_.GetInventoryDisturbItem());
            var type = _.GetInventoryDisturbType();

            if (type == DisturbType.Added)
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
