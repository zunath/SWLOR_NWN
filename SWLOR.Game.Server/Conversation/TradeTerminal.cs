using System;
using System.Collections.Generic;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;
using static NWN.NWScript;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Conversation
{
    public class TradeTerminal: ConversationBase
    {
        private readonly IColorTokenService _color;
        private readonly IMarketService _market;
        private readonly IDataService _data;
        private readonly ISerializationService _serialization;

        public TradeTerminal(
            INWScript script, 
            IDialogService dialog,
            IColorTokenService color,
            IMarketService market,
            IDataService data,
            ISerializationService serialization) 
            : base(script, dialog)
        {
            _color = color;
            _market = market;
            _data = data;
            _serialization = serialization;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            // Entry point into conversation for selecting Buy or Sell
            DialogPage mainPage = new DialogPage(
                _color.Green("Galactic Trade Network"),
                "Buy",
                "Sell");

            // Page for selecting browse method - either by category or by seller
            DialogPage buyPage = new DialogPage(
                _color.Green("Galactic Trade Network - Buy"),
                "Browse by Category",
                "Browse by Seller");

            // Page for selecting which item category to browse
            DialogPage browseByCategoryPage = new DialogPage(); // Dynamically built

            // Page for selecting which seller's items to browse
            DialogPage browseBySellerPage = new DialogPage(); // Dynamically built

            // Page for selecting an item to buy.
            // Populated based on option selected in the "Browse by Category" and "Browse by Seller" pages.
            DialogPage itemListPage = new DialogPage(); // Dynamically built

            // Page for viewing item details and buying the item.
            DialogPage itemDetailsPage = new DialogPage(
                "<SET LATER>",
                "Examine Item", 
                "Buy Item");

            // Page for selling a new item or looking at existing items the player is selling.
            DialogPage sellPage = new DialogPage(
                _color.Green("Galactic Trade Network - Sell"),
                "Sell an Item",
                "View Market Listings");

            // Page for selling an item.
            DialogPage sellItemPage = new DialogPage(
                _color.Green("Galactic Trade Network - Sell an Item"));

            // Page for viewing items currently being sold by the player.
            DialogPage marketListingsPage = new DialogPage(
                _color.Green("Galactic Trade Network - Market Listings")); // Responses dynamically built

            // Page for viewing detailed information about a market listing.
            DialogPage marketListingDetailsPage = new DialogPage(); // Dynamically built

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("BuyPage", buyPage);
            dialog.AddPage("BrowseByCategoryPage", browseByCategoryPage);
            dialog.AddPage("BrowseBySellerPage", browseBySellerPage);
            dialog.AddPage("ItemListPage", itemListPage);
            dialog.AddPage("ItemDetailsPage", itemDetailsPage);
            dialog.AddPage("SellPage", sellPage);
            dialog.AddPage("SellItemPage", sellItemPage);
            dialog.AddPage("MarketListingsPage", marketListingsPage);
            dialog.AddPage("MarketListingDetailsPage", marketListingDetailsPage);
            return dialog;
        }

        public override void Initialize()
        {
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                    MainPageResponses(responseID);
                    break;
                case "BuyPage":
                    BuyPageResponses(responseID);
                    break;
                case "BrowseByCategoryPage":
                    BrowseByCategoryResponses(responseID);
                    break;
                case "BrowseBySellerPage":
                    BrowseBySellerResponses(responseID);
                    break;
                case "ItemListPage":
                    ItemListPageResponses(responseID);
                    break;
                case "ItemDetailsPage":
                    ItemDetailsPageResponses(responseID);
                    break;
            }
        }

        private void MainPageResponses(int responseID)
        {
            switch (responseID)
            {
                case 1: // Buy
                    ChangePage("BuyPage");
                    break;
                case 2: // Sell
                    ChangePage("SellPage");
                    break;
            }
        }

        private void BuyPageResponses(int responseID)
        {
            switch (responseID)
            {
                case 1: // Browse by Category
                    LoadBrowseByCategoryPage();
                    ChangePage("BrowseByCategoryPage");
                    break;
                case 2: // Browse by Seller
                    LoadBrowseBySellerPage();
                    ChangePage("BrowseBySellerPage");
                    break;
            }
        }

        private void LoadBrowseByCategoryPage()
        {
            NWPlaceable terminal = Object.OBJECT_SELF;
            int marketRegionID = _market.GetMarketRegionID(terminal);
            IEnumerable<PCMarketListing> listings = _data.Where<PCMarketListing>(x => x.DateExpires > DateTime.UtcNow &&
                                                                                      x.MarketRegionID == marketRegionID &&
                                                                                      x.DateSold == null);
            IEnumerable<int> categoryIDs = listings.Select(s => s.MarketCategoryID).Distinct();
            IEnumerable<MarketCategory> categories = _data.Where<MarketCategory>(x => categoryIDs.Contains(x.ID))
                .OrderBy(o => o.Name);

            ClearPageResponses("BrowseByCategoryPage");
            AddResponseToPage("BrowseBySellerPage", _color.Green("Refresh"), true, -1);
            foreach (var category in categories)
            {
                AddResponseToPage("BrowseByCategoryPage", category.Name, true, category.ID);
            }
        }

        private void BrowseByCategoryResponses(int responseID)
        {
            DialogResponse response = GetResponseByID("BrowseByCategoryPage", responseID);
            int categoryID = (int)response.CustomData;

            // Refresh listing
            if (categoryID == -1)
            {
                LoadBrowseByCategoryPage();
                return;
            }

            var model = _market.GetPlayerMarketData(GetPC());
            model.BrowseMode = MarketBrowseMode.ByCategory;
            model.BrowseCategoryID = categoryID;
            LoadItemListPage();
            ChangePage("ItemListPage");
        }

        private void LoadBrowseBySellerPage()
        {
            NWPlaceable terminal = Object.OBJECT_SELF;
            int marketRegionID = _market.GetMarketRegionID(terminal);
            IEnumerable<PCMarketListing> listings = _data.Where<PCMarketListing>(x => x.DateExpires > DateTime.UtcNow &&
                                                                                      x.MarketRegionID == marketRegionID &&
                                                                                      x.DateSold == null);
            IEnumerable<Guid> playerIDs = listings.Select(s => s.PlayerID).Distinct();
            IEnumerable<Player> players = _data.Where<Player>(x => playerIDs.Contains(x.ID))
                .OrderBy(o => o.CharacterName);

            ClearPageResponses("BrowseBySellerPage");
            AddResponseToPage("BrowseBySellerPage", _color.Green("Refresh"), true, Guid.Empty);
            foreach (var player in players)
            {
                AddResponseToPage("BrowseBySellerPage", player.CharacterName, true, player.ID);
            }
        }

        private void BrowseBySellerResponses(int responseID)
        {
            DialogResponse response = GetResponseByID("BrowseBySellerPage", responseID);
            Guid playerID = (Guid)response.CustomData;

            // Refresh listing
            if (playerID == Guid.Empty)
            {
                LoadBrowseBySellerPage();
                return;
            }

            var model = _market.GetPlayerMarketData(GetPC());
            model.BrowseMode = MarketBrowseMode.BySeller;
            model.BrowsePlayerID = playerID;
            LoadItemListPage();
            ChangePage("ItemListPage");
        }

        private void LoadItemListPage()
        {
            var model = _market.GetPlayerMarketData(GetPC());
            IEnumerable<PCMarketListing> listings;
            DateTime now = DateTime.UtcNow;
            int marketRegionID = _market.GetMarketRegionID(Object.OBJECT_SELF);
            
            // Pull items by category
            if (model.BrowseMode == MarketBrowseMode.ByCategory)
            {
                listings = _data.Where<PCMarketListing>(x => x.DateExpires > now &&
                                                             x.MarketRegionID == marketRegionID &&
                                                             x.MarketCategoryID == model.BrowseCategoryID &&
                                                             x.DateSold == null);
            }
            // Pull items being sold by a specific player
            else
            {
                listings = _data.Where<PCMarketListing>(x => x.DateExpires > now &&
                                                             x.MarketRegionID == marketRegionID &&
                                                             x.PlayerID == model.BrowsePlayerID &&
                                                             x.DateSold == null);
            }

            // Build the response list.
            ClearPageResponses("ItemListPage");
            AddResponseToPage("ItemListPage", _color.Green("Refresh"), true, Guid.Empty);
            foreach (var listing in listings)
            {
                // Build the item name. Example:
                // Sword of Doom 1x (5000 Credits) [RL: 50]
                string listingName = listing.ItemName + " " + listing.ItemStackSize + "x" + " (" + listing.Price + " credits)";
                if (listing.ItemRecommendedLevel > 0)
                    listingName += " [RL: " + listing.ItemRecommendedLevel + "]";

                AddResponseToPage("ItemListPage", listingName, true, listing.ID);
            }
        }

        private void ItemListPageResponses(int responseID)
        {
            var player = GetPC();
            var response = GetResponseByID("ItemListPage", responseID);
            Guid listingID = (Guid)response.CustomData;

            // Refresh listing
            if (listingID == Guid.Empty)
            {
                LoadItemListPage();
                return;
            }

            // If the item no longer exists on the market (expired, bought, or listing removed)
            // notify the player and refresh the item list page.
            var listing = _data.SingleOrDefault<PCMarketListing>(x => x.ID == listingID && 
                                                                      x.DateSold == null);
            if (listing == null || listing.DateExpires > DateTime.UtcNow)
            {
                LoadItemListPage();
                player.FloatingText("Unfortunately, that item is no longer available.");
                return;
            }

            var model = _market.GetPlayerMarketData(player);
            model.BrowseListingID = listingID;
            LoadItemDetailsPage();
            ChangePage("ItemDetailsPage");
        }

        private void LoadItemDetailsPage()
        {
            var player = GetPC();
            var model = _market.GetPlayerMarketData(player);
            var listing = _data.Single<PCMarketListing>(x => x.ID == model.BrowseListingID);
            string header = _color.Green("Galactic Trade Network") + "\n\n";
            header += _color.Green("Name: ") + listing.ItemName + " " + listing.ItemStackSize + "x" + "\n";

            if(listing.ItemRecommendedLevel > 0)
                header += _color.Green("Recommended Level: ") + listing.ItemRecommendedLevel + "\n";

            header += _color.Green("Price: ") + listing.Price + "\n";
            header += _color.Green("Seller Note: ") + listing.Note + "\n";

            SetPageHeader("ItemDetailsPage", header);

            // Show or hide the "Buy Item" option depending on whether the player has enough gold.
            if (player.Gold < listing.Price)
            {
                SetResponseVisible("ItemDetailsPage", 2, false);
            }
            else
            {
                SetResponseVisible("ItemDetailsPage", 2, true);
            }

        }

        private void ItemDetailsPageResponses(int responseID)
        {
            var buyer = GetPC();
            var model = _market.GetPlayerMarketData(buyer);
            var listing = _data.SingleOrDefault<PCMarketListing>(x => x.ID == model.BrowseListingID && 
                                                                      x.DateSold == null);

            // Item was removed, sold, or expired.
            if (listing == null || listing.DateExpires > DateTime.UtcNow)
            {
                LoadItemListPage();
                ChangePage("ItemListPage", false);
                buyer.FloatingText("Unfortunately, that item is no longer available.");
                return;
            }

            switch (responseID)
            {
                case 1: // Examine Item
                    break;
                case 2: // Buy Item / Confirm Buy Item

                    if (model.IsConfirming)
                    {
                        // Player no longer has enough gold to buy the item.
                        // Set back to no longer confirming and hide the "Buy Item" option. Notify them.
                        if (buyer.Gold < listing.Price)
                        {
                            model.IsConfirming = false;
                            SetResponseText("ItemDetailsPage", 2, "Buy Item");
                            SetResponseVisible("ItemDetailsPage", 2, false);
                            buyer.FloatingText("You do not have enough credits to purchase that item.");
                            return;
                        }

                        // Take gold from buyer.
                        _.TakeGoldFromCreature(listing.Price, buyer, TRUE);

                        // Give gold to seller.
                        _market.GiveMarketGoldToPlayer(listing.PlayerID, listing.Price);

                        // Give the item to the buyer.
                        _serialization.DeserializeItem(listing.ItemObject, buyer);

                        // Mark the listing as sold.
                        listing.DateSold = DateTime.UtcNow;
                        _data.SubmitDataChange(listing, DatabaseActionType.Update);

                        model.IsConfirming = false;
                        SetResponseText("ItemDetailsPage", 2, "Buy Item");
                        ChangePage("ItemListPage", false);
                    }
                    else
                    {
                        model.IsConfirming = true;
                        SetResponseText("ItemDetailsPage", 2, "CONFIRM BUY ITEM");
                    }

                    break;
            }
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
            var model = _market.GetPlayerMarketData(player);
            if (beforeMovePage == "ItemDetailsPage")
                model.IsConfirming = false;

        }

        public override void EndDialog()
        {
            var pc = GetPC();
            _market.ClearPlayerMarketData(pc);
        }
    }
}
