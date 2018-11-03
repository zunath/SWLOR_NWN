using System.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Placeable.OverflowStorage
{
    public class OnDisturbed: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IDataService _data;

        public OnDisturbed(INWScript script,
            IDataService data)
        {
            _ = script;
            _data = data;
        }

        public bool Run(params object[] args)
        {
            NWPlaceable container = (Object.OBJECT_SELF);
            NWPlayer oPC = (_.GetLastDisturbed());
            NWItem oItem = (_.GetInventoryDisturbItem());
            int type = _.GetInventoryDisturbType();

            if (type == NWScript.INVENTORY_DISTURB_TYPE_ADDED)
            {
                container.AssignCommand(() => _.ActionGiveItem(oItem.Object, oPC.Object));
                return true;
            }
            
            int overflowItemID = oItem.GetLocalInt("TEMP_OVERFLOW_ITEM_ID");
            PCOverflowItem overflowItem = _data.PCOverflowItems.Single(x => x.PCOverflowItemID == overflowItemID);
            _data.PCOverflowItems.Remove(overflowItem);
            _data.SaveChanges();

            oItem.DeleteLocalInt("TEMP_OVERFLOW_ITEM_ID");

            if (!container.InventoryItems.Any())
            {
                container.Destroy();
            }
            return true;
        }
    }
}
