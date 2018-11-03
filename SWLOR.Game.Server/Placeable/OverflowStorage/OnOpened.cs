using System.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Placeable.OverflowStorage
{
    public class OnOpened: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IDataService _data;
        private readonly ISerializationService _serialization;

        public OnOpened(INWScript script,
            IDataService data,
            ISerializationService serialization)
        {
            _ = script;
            _data = data;
            _serialization = serialization;
        }

        public bool Run(params object[] args)
        {
            NWPlaceable container = (Object.OBJECT_SELF);
            NWPlayer oPC = (_.GetLastOpenedBy());
            var items = _data.Where<PCOverflowItem>(x => x.PlayerID == oPC.GlobalID).ToList();
            foreach (PCOverflowItem item in items)
            {
                NWItem oItem = _serialization.DeserializeItem(item.ItemObject, container);
                oItem.SetLocalInt("TEMP_OVERFLOW_ITEM_ID", (int)item.PCOverflowItemID);
            }

            container.IsUseable = false;
            return true;
        }
    }
}
