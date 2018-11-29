using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
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
        private readonly IDataService _data;
        private readonly IColorTokenService _color;
        private readonly ISerializationService _serialization;

        public OnDisturbed(
            INWScript script,
            IItemService item,
            IDataService data,
            IColorTokenService color,
            ISerializationService serialization)
        {
            _ = script;
            _item = item;
            _data = data;
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
            
            if (disturbType == INVENTORY_DISTURB_TYPE_ADDED)
            {
                if (_.GetHasInventory(item) == TRUE)
                {
                    item.SetLocalInt("RETURNING_ITEM", TRUE);
                    _item.ReturnItem(player, item);
                    player.SendMessage(_color.Red("Containers cannot currently be stored inside banks."));
                    return false;
                }

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
                        ItemID = item.GlobalID.ToString(),
                        ItemObject = _serialization.Serialize(item),
                        BankID = bankID,
                        PlayerID = player.GlobalID,
                        DateStored = DateTime.UtcNow
                    };
                    
                    _data.SubmitDataChange(itemEntity, DatabaseActionType.Insert);
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
                    var record = _data.Single<BankItem>(x => x.ItemID == item.GlobalID.ToString());
                    _data.SubmitDataChange(record, DatabaseActionType.Delete);
                }
            }

            player.SendMessage(_color.White("Item Limit: " + (itemCount > itemLimit ? itemLimit : itemCount) + " / ") + _color.Red("" + itemLimit));
            return true;
        }
    }
}
