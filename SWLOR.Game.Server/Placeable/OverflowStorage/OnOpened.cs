using System.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Placeable.OverflowStorage
{
    public class OnOpened: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IDataContext _db;
        private readonly ISerializationService _serialization;

        public OnOpened(INWScript script,
            IDataContext db,
            ISerializationService serialization)
        {
            _ = script;
            _db = db;
            _serialization = serialization;
        }

        public bool Run(params object[] args)
        {
            NWPlaceable container = NWPlaceable.Wrap(Object.OBJECT_SELF);
            NWPlayer oPC = NWPlayer.Wrap(_.GetLastOpenedBy());
            var items = _db.PCOverflowItems.Where(x => x.PlayerID == oPC.GlobalID);
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
