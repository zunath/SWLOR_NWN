using System;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Scripts.Placeable.Bank
{
    public class OnDisturbed : IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWPlaceable terminal = OBJECT_SELF;
            var bankID = terminal.GetLocalInt("BANK_ID");
            if (bankID <= 0) return;

            NWPlayer player = GetLastDisturbed();
            NWItem item = GetInventoryDisturbItem();
            var disturbType = GetInventoryDisturbType();
            var itemCount = terminal.InventoryItems.Count();
            var itemLimit = terminal.GetLocalInt("BANK_LIMIT");
            if (itemLimit <= 0) itemLimit = 20;

            if (disturbType == DisturbType.Added)
            {
                if (GetHasInventory(item) == true)
                {
                    SetLocalBool(item, "RETURNING_ITEM", true);
                    ItemService.ReturnItem(player, item);
                    player.SendMessage(ColorTokenService.Red("Containers cannot currently be stored inside banks."));
                    return;
                }

                if (itemCount > itemLimit)
                {
                    ItemService.ReturnItem(player, item);
                    player.SendMessage(ColorTokenService.Red("No more items can be placed inside."));
                }
                else
                {
                    var itemEntity = new BankItem
                    {
                        ItemName = item.Name,
                        ItemTag = item.Tag,
                        ItemResref = item.Resref,
                        ItemID = item.GlobalID.ToString(),
                        ItemObject = SerializationService.Serialize(item),
                        BankID = bankID,
                        PlayerID = player.GlobalID,
                        DateStored = DateTime.UtcNow
                    };

                    DataService.SubmitDataChange(itemEntity, DatabaseActionType.Insert);
                    MessageHub.Instance.Publish(new OnStoreBankItem(player, itemEntity));
                }
            }
            else if (disturbType == DisturbType.Removed)
            {
                if (GetLocalBool(item, "RETURNING_ITEM") == true)
                {
                    item.DeleteLocalInt("RETURNING_ITEM");
                }
                else
                {
                    var record = DataService.BankItem.GetByItemID(item.GlobalID.ToString());
                    DataService.SubmitDataChange(record, DatabaseActionType.Delete);
                    MessageHub.Instance.Publish(new OnRemoveBankItem(player, record));
                }
            }

            player.SendMessage(ColorTokenService.White("Item Limit: " + (itemCount > itemLimit ? itemLimit : itemCount) + " / ") + ColorTokenService.Red("" + itemLimit));
        }
    }
}
