﻿using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;
using _ = SWLOR.Game.Server.NWScript._;

namespace SWLOR.Game.Server.Scripting.Placeable.MarketTerminal
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
            var type = _.GetInventoryDisturbType();

            if (type == InventoryDisturbType.Removed)
            {
                HandleRemoveItem();
            }
            else if (type == InventoryDisturbType.Added)
            {
                HandleAddItem();
            }
        }

        private void HandleAddItem()
        {
            NWPlayer player = _.GetLastDisturbed();
            NWItem item = _.GetInventoryDisturbItem();
            NWPlaceable device = NWGameObject.OBJECT_SELF;
            var model = MarketService.GetPlayerMarketData(player);

            // Serializing containers can be tricky so for the time being we'll leave them disabled.
            if (_.GetHasInventory(item) == true)
            {
                ItemService.ReturnItem(player, item);
                player.SendMessage(ColorTokenService.Red("Containers cannot be sold on the market."));
                return;
            }
            
            // If selling an item, serialize it and store its information in the player's temporary data.
            if (model.IsSellingItem)
            {
                // Check the item's category. If one cannot be determined, the player cannot put it on the market.
                var marketCategoryID = MarketService.DetermineMarketCategory(item);
                if (marketCategoryID <= 0)
                {
                    ItemService.ReturnItem(player, item);
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
                model.ItemObject = SerializationService.Serialize(item);
                model.SellPrice = 0;
                model.LengthDays = 7;
                
                item.Destroy();

                device.DestroyAllInventoryItems();
                device.IsLocked = false;
                model.IsReturningFromItemPicking = true;
                model.IsAccessingInventory = false;
                DialogService.StartConversation(player, device, "MarketTerminal");
            }
            else
            {
                ItemService.ReturnItem(player, item);
            }
        }

        private void HandleRemoveItem()
        {
            NWPlayer player = _.GetLastDisturbed();
            NWItem item = _.GetInventoryDisturbItem();
            NWPlaceable device = NWGameObject.OBJECT_SELF;
            var model = MarketService.GetPlayerMarketData(player);

            // Done previewing an item. Return to menu.
            if (item.Resref == "exit_preview")
            {
                item.Destroy();
                device.DestroyAllInventoryItems();
                device.IsLocked = false;
                model.IsAccessingInventory = false;
                model.IsReturningFromItemPreview = true;
                DialogService.StartConversation(player, device, "MarketTerminal");
            }
        }

    }
}
