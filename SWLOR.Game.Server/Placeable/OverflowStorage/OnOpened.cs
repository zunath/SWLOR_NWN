using System.Linq;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Placeable.OverflowStorage
{
    public class OnOpened: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlaceable container = (Object.OBJECT_SELF);
            NWPlayer oPC = (_.GetLastOpenedBy());
            var items = DataService.Where<PCOverflowItem>(x => x.PlayerID == oPC.GlobalID).ToList();
            foreach (PCOverflowItem item in items)
            {
                NWItem oItem = SerializationService.DeserializeItem(item.ItemObject, container);
                oItem.SetLocalString("TEMP_OVERFLOW_ITEM_ID", item.ID.ToString());
            }

            container.IsUseable = false;
            return true;
        }
    }
}
