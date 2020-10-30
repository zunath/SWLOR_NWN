using System;
using System.Linq;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Core.NWScript.Enum;
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
            NWPlaceable container = (NWScript.OBJECT_SELF);
            NWPlayer oPC = (NWScript.GetLastDisturbed());
            NWItem oItem = (NWScript.GetInventoryDisturbItem());
            var type = NWScript.GetInventoryDisturbType();

            if (type == DisturbType.Added)
            {
                container.AssignCommand(() => NWScript.ActionGiveItem(oItem.Object, oPC.Object));
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
