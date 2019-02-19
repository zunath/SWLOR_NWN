using System;
using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.MarketTerminal
{
    public class OnDisturbed: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IMarketService _market;
        private readonly IItemService _item;
        private readonly IDialogService _dialog;
        private readonly ISerializationService _serialization;

        public OnDisturbed(
            INWScript script, 
            IMarketService market,
            IItemService item,
            IDialogService dialog,
            ISerializationService serialization)
        {
            _ = script;
            _market = market;
            _item = item;
            _dialog = dialog;
            _serialization = serialization;
        }

        public bool Run(params object[] args)
        {
            int type = _.GetInventoryDisturbType();

            if (type == INVENTORY_DISTURB_TYPE_REMOVED)
            {
                HandleRemoveItem();
            }
            else if (type == INVENTORY_DISTURB_TYPE_ADDED)
            {
                HandleAddItem();
            }
            return true;
        }

        private void HandleAddItem()
        {
            NWPlayer player = _.GetLastDisturbed();
            NWItem item = _.GetInventoryDisturbItem();
            NWPlaceable device = Object.OBJECT_SELF;
            var model = _market.GetPlayerMarketData(player);

            // If selling an item, serialize it and store its information in the player's temporary data.
            if (model.IsSellingItem)
            {
                // Check the item's category. If one cannot be determined, the player cannot put it on the market.
                int marketCategoryID = _market.DetermineMarketCategory(item);
                if (marketCategoryID <= 0)
                {
                    _item.ReturnItem(player, item);
                    player.FloatingText("This item cannot be placed on the market.");
                    return;
                }

                model.ItemID = item.GlobalID;
                model.ItemName = item.Name;
                model.ItemRecommendedLevel = item.RecommendedLevel;
                model.ItemStackSize = item.StackSize;
                model.ItemTag = item.Tag;
                model.ItemResref = item.Resref;
                model.ItemMarketCategoryID = marketCategoryID;
                model.ItemObject = _serialization.Serialize(item);
                model.SellPrice = 0;
                
                item.Destroy();

                device.DestroyAllInventoryItems();
                device.IsLocked = false;
                model.IsReturningFromItemPicking = true;
                model.IsAccessingInventory = false;
                _dialog.StartConversation(player, device, "MarketTerminal");
            }
            else
            {
                _item.ReturnItem(player, item);
            }
        }

        private void HandleRemoveItem()
        {
            NWPlayer player = _.GetLastDisturbed();
            NWItem item = _.GetInventoryDisturbItem();
            NWPlaceable device = Object.OBJECT_SELF;
            var model = _market.GetPlayerMarketData(player);

            // Done previewing an item. Return to menu.
            if (item.Resref == "exit_preview")
            {
                item.Destroy();
                device.DestroyAllInventoryItems();
                device.IsLocked = false;
                model.IsAccessingInventory = false;
                model.IsReturningFromItemPreview = true;
                _dialog.StartConversation(player, device, "MarketTerminal");
            }
        }

    }
}
