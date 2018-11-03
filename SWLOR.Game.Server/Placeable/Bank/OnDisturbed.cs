using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.Bank
{
    public class OnDisturbed : IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IItemService _item;
        private readonly IDataContext _db;
        private readonly IColorTokenService _color;
        private readonly ISerializationService _serialization;

        public OnDisturbed(
            INWScript script,
            IItemService item,
            IDataContext db,
            IColorTokenService color,
            ISerializationService serialization)
        {
            _ = script;
            _item = item;
            _db = db;
            _color = color;
            _serialization = serialization;
        }

        public bool Run(params object[] args)
        {
            NWPlaceable terminal = Object.OBJECT_SELF;
            int bankID = terminal.GetLocalInt("BANK_ID");
            if (bankID <= 0) return false;

            NWPlayer player = _.GetLastDisturbed();
            NWItem item = _.GetInventoryDisturbItem();
            int disturbType = _.GetInventoryDisturbType();
            int itemCount = terminal.InventoryItems.Count();
            int itemLimit = terminal.GetLocalInt("BANK_LIMIT");
            if (itemLimit <= 0) itemLimit = 20;

            Data.Entity.Bank entity = _db.Banks.Single(x => x.BankID == bankID);

            if (disturbType == INVENTORY_DISTURB_TYPE_ADDED)
            {
                if (itemCount > itemLimit)
                {
                    _item.ReturnItem(player, item);
                    player.SendMessage(_color.Red("No more items can be placed inside."));
                }
                else
                {
                    BankItem itemEntity = new BankItem
                    {
                        ItemName = item.Name,
                        ItemTag = item.Tag,
                        ItemResref = item.Resref,
                        ItemID = item.GlobalID,
                        ItemObject = _serialization.Serialize(item),
                        BankID = entity.BankID,
                        PlayerID = player.GlobalID,
                        DateStored = DateTime.UtcNow
                    };

                    entity.BankItems.Add(itemEntity);
                    _db.SaveChanges();
                }
            }
            else if (disturbType == INVENTORY_DISTURB_TYPE_REMOVED)
            {
                var record = _db.BankItems.Single(x => x.ItemID == item.GlobalID);
                _db.BankItems.Remove(record);
                _db.SaveChanges();
            }

            player.SendMessage(_color.White("Item Limit: " + (itemCount > itemLimit ? itemLimit : itemCount) + " / ") + _color.Red("" + itemLimit));
            return true;
        }
    }
}
