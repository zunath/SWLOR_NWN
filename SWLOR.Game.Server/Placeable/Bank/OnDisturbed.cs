using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.Contracts;
using static NWN._;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.Bank
{
    public class OnDisturbed : IRegisteredEvent
    {
        
        
        
        
        private readonly ISerializationService _serialization;

        public OnDisturbed(
            
            
            
            
            ISerializationService serialization)
        {
            
            
            
            
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
                    ItemService.ReturnItem(player, item);
                    player.SendMessage(ColorTokenService.Red("Containers cannot currently be stored inside banks."));
                    return false;
                }

                if (itemCount > itemLimit)
                {
                    ItemService.ReturnItem(player, item);
                    player.SendMessage(ColorTokenService.Red("No more items can be placed inside."));
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
                    
                    DataService.SubmitDataChange(itemEntity, DatabaseActionType.Insert);
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
                    var record = DataService.Single<BankItem>(x => x.ItemID == item.GlobalID.ToString());
                    DataService.SubmitDataChange(record, DatabaseActionType.Delete);
                }
            }

            player.SendMessage(ColorTokenService.White("Item Limit: " + (itemCount > itemLimit ? itemLimit : itemCount) + " / ") + ColorTokenService.Red("" + itemLimit));
            return true;
        }
    }
}
