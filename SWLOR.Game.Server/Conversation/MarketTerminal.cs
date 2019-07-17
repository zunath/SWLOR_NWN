using System;
using System.Collections.Generic;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject.Dialog;
using static NWN._;

namespace SWLOR.Game.Server.Conversation
{
    public class MarketTerminal: ConversationBase
    {

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            // Entry point into conversation for selecting Buy or Sell
            DialogPage mainPage = new DialogPage(
                ColorTokenService.Green("Galactic Trade Network"),
                "Buy",
                "Sell",
                "Manage Market Listings");

            // Page for selecting browse method - either by category or by seller
            DialogPage buyPage = new DialogPage(
                ColorTokenService.Green("Galactic Trade Network - Buy"),
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
            
            // Page for selling an item.
            DialogPage sellItemPage = new DialogPage(
                ColorTokenService.Green("Galactic Trade Network - Sell Item"),
                ColorTokenService.Green("Refresh"),
                "Pick an Item",
                "Change Price",
                "Change Seller Note",
                "Remove Seller Note",
                "Change Listing Length",
                ColorTokenService.Green("List the Item"));

            // Page for setting a price on an item.
            DialogPage changePricePage = new DialogPage(
                "<SET LATER>",
                "Increase by 100,000 credits",
                "Increase by 10,000 credits",
                "Increase by 1,000 credits",
                "Increase by 100 credits",
                "Increase by 10 credits",
                "Increase by 1 credit",
                "Decrease by 100,000 credits",
                "Decrease by 10,000 credits",
                "Decrease by 1,000 credits",
                "Decrease by 100 credits",
                "Decrease by 10 credits",
                "Decrease by 1 credit");

            // Page for changing the listing length on an item sale.
            DialogPage changeListingLengthPage = new DialogPage("<SET LATER>",
                "Set to Max (30 days, 3% fee)",
                "Set to Default (7 days, 0.7% fee)",
                "Increase by 7 days (+0.7% fee)",
                "Increase by 1 day (+0.1% fee)",
                "Decrease by 7 days (-0.7% fee)",
                "Decrease by 1 day (-0.1% fee)");

            // Page for viewing items currently being sold by the player.
            DialogPage marketListingsPage = new DialogPage(
                ColorTokenService.Green("Galactic Trade Market - Manage Market Listings"));

            // Page for viewing detailed information about a market listing.
            DialogPage marketListingDetailsPage = new DialogPage(
                "<SET LATER>",
                ColorTokenService.Green("Refresh"),
                ColorTokenService.Red("Remove Listing"));

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("BuyPage", buyPage);
            dialog.AddPage("BrowseByCategoryPage", browseByCategoryPage);
            dialog.AddPage("BrowseBySellerPage", browseBySellerPage);
            dialog.AddPage("ItemListPage", itemListPage);
            dialog.AddPage("ItemDetailsPage", itemDetailsPage);
            dialog.AddPage("SellItemPage", sellItemPage);
            dialog.AddPage("ChangePricePage", changePricePage);
            dialog.AddPage("ChangeListingLengthPage", changeListingLengthPage);
            dialog.AddPage("MarketListingsPage", marketListingsPage);
            dialog.AddPage("MarketListingDetailsPage", marketListingDetailsPage);
            return dialog;
        }

        public override void Initialize()
        {
            var player = GetPC();
            var model = MarketService.GetPlayerMarketData(player);

            // Player is returning from an item preview.
            // Send them to the Item Details page.
            if (model.IsReturningFromItemPreview)
            {
                NavigationStack = model.TemporaryDialogNavigationStack;

                if (model.BrowseMode == MarketBrowseMode.ByCategory)
                    LoadBrowseByCategoryPage();
                else
                    LoadBrowseBySellerPage();

                LoadItemListPage();
                LoadItemDetailsPage();
                ChangePage("ItemDetailsPage", false);
            }
            // Player is returning from selecting an item to sell.
            // Send them to the Sell Item page.
            else if (model.IsReturningFromItemPicking)
            {
                NavigationStack = model.TemporaryDialogNavigationStack;

                LoadSellItemPage();
                ChangePage("SellItemPage", false);
            }
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
                case "SellItemPage":
                    SellItemPageResponses(responseID);
                    break;
                case "ChangePricePage":
                    ChangePricePageResponses(responseID);
                    break;
                case "ChangeListingLengthPage":
                    ChangeListingLengthPageResponses(responseID);
                    break;
                case "MarketListingsPage":
                    ManageMarketListingsReponses(responseID);
                    break;
                case "MarketListingDetailsPage":
                    ManageMarketListingDetailsResponses(responseID);
                    break;
            }
        }

        private void MainPageResponses(int responseID)
        {
            var player = GetPC();
            var model = MarketService.GetPlayerMarketData(player);

            switch (responseID)
            {
                case 1: // Buy
                    model.IsSellingItem = false;
                    ChangePage("BuyPage");
                    break;
                case 2: // Sell
                    model.IsSellingItem = true;
                    LoadSellItemPage();
                    ChangePage("SellItemPage");
                    break;
                case 3: // Manage Market Listings
                    model.IsSellingItem = false;
                    LoadManageMarketListingsPage();
                    ChangePage("MarketListingsPage");
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
            string header = ColorTokenService.Green("Galactic Trade Network - Browse by Category") + "\n\n";
            header += "Please select a category.";
            SetPageHeader("BrowseByCategoryPage", header);

            NWPlaceable terminal = NWGameObject.OBJECT_SELF;
            int marketRegionID = MarketService.GetMarketRegionID(terminal);
            IEnumerable<PCMarketListing> listings = DataService.PCMarketListing
                .GetAllByMarketRegionID(marketRegionID)
                .Where(x => x.DateExpires > DateTime.UtcNow &&
                            x.DateSold == null &&
                            x.DateRemoved == null);

            IEnumerable<int> categoryIDs = listings.Select(s => s.MarketCategoryID).Distinct();
            IEnumerable<MarketCategory> categories = DataService.MarketCategory.GetAllByIDs(categoryIDs)
                .OrderBy(o => o.Name);

            ClearPageResponses("BrowseByCategoryPage");
            AddResponseToPage("BrowseByCategoryPage", ColorTokenService.Green("Refresh"), true, -1);
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

            var model = MarketService.GetPlayerMarketData(GetPC());
            model.BrowseMode = MarketBrowseMode.ByCategory;
            model.BrowseCategoryID = categoryID;
            LoadItemListPage();
            ChangePage("ItemListPage");
        }

        private void LoadBrowseBySellerPage()
        {
            string header = ColorTokenService.Green("Galactic Trade Network - Browse by Seller") + "\n\n";
            header += "Please select a seller.";
            SetPageHeader("BrowseBySellerPage", header);

            NWPlaceable terminal = NWGameObject.OBJECT_SELF;
            int marketRegionID = MarketService.GetMarketRegionID(terminal);
            IEnumerable<PCMarketListing> listings = DataService.PCMarketListing
                .GetAllByMarketRegionID(marketRegionID)
                .Where(x => x.DateExpires > DateTime.UtcNow &&
                            x.DateSold == null &&
                            x.DateRemoved == null);
            IEnumerable<Guid> playerIDs = listings.Select(s => s.SellerPlayerID).Distinct();
            IEnumerable<Player> players = DataService.Player.GetAllByIDs(playerIDs)
                .OrderBy(o => o.CharacterName);

            ClearPageResponses("BrowseBySellerPage");
            AddResponseToPage("BrowseBySellerPage", ColorTokenService.Green("Refresh"), true, Guid.Empty);
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

            var model = MarketService.GetPlayerMarketData(GetPC());
            model.BrowseMode = MarketBrowseMode.BySeller;
            model.BrowsePlayerID = playerID;
            LoadItemListPage();
            ChangePage("ItemListPage");
        }

        private void LoadItemListPage()
        {
            var model = MarketService.GetPlayerMarketData(GetPC());
            IEnumerable<PCMarketListing> listings;
            DateTime now = DateTime.UtcNow;
            int marketRegionID = MarketService.GetMarketRegionID(NWGameObject.OBJECT_SELF);
            
            // Pull items by category
            if (model.BrowseMode == MarketBrowseMode.ByCategory)
            {
                listings = DataService.PCMarketListing
                    .GetAllByMarketRegionID(marketRegionID)
                    .Where(x => x.DateExpires > now &&
                                x.MarketCategoryID == model.BrowseCategoryID &&
                                x.DateSold == null &&
                                x.DateRemoved == null);
            }
            // Pull items being sold by a specific player
            else
            {
                listings = DataService.PCMarketListing
                    .GetAllByMarketRegionID(marketRegionID)
                    .Where(x => x.DateExpires > now &&
                                x.SellerPlayerID == model.BrowsePlayerID &&
                                x.DateSold == null &&
                                x.DateRemoved == null);
            }

            // Build the response list.
            ClearPageResponses("ItemListPage");
            AddResponseToPage("ItemListPage", ColorTokenService.Green("Refresh"), true, Guid.Empty);
            foreach (var listing in listings)
            {
                string listingName = BuildItemName(listing);
                AddResponseToPage("ItemListPage", listingName, true, listing.ID);
            }
        }

        private string BuildItemName(PCMarketListing listing)
        {
            // Build the item name. Example:
            // 1x Sword of Doom (5000 Credits) [RL: 50]
            string listingName = listing.ItemStackSize + "x " + listing.ItemName + " (" + listing.Price + " credits)";
            if (listing.ItemRecommendedLevel > 0)
                listingName += " [RL: " + listing.ItemRecommendedLevel + "]";

            return listingName;
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
            var listing = DataService.PCMarketListing.GetByIDOrDefault(listingID);
            if (listing == null || listing.DateSold != null || listing.DateExpires <= DateTime.UtcNow)
            {
                LoadItemListPage();
                player.FloatingText("Unfortunately, that item is no longer available.");
                return;
            }

            var model = MarketService.GetPlayerMarketData(player);
            model.BrowseListingID = listingID;
            LoadItemDetailsPage();
            ChangePage("ItemDetailsPage");
        }

        private void LoadItemDetailsPage()
        {
            var player = GetPC();
            var model = MarketService.GetPlayerMarketData(player);
            var listing = DataService.PCMarketListing.GetByID(model.BrowseListingID);
            string sellerNote = listing.Note;
            if (string.IsNullOrWhiteSpace(listing.Note))
                sellerNote = "[UNSPECIFIED]";

            string header = ColorTokenService.Green("Galactic Trade Network") + "\n\n";
            header += ColorTokenService.Green("Name: ") + listing.ItemStackSize + "x " + listing.ItemName + "\n";

            if(listing.ItemRecommendedLevel > 0)
                header += ColorTokenService.Green("Recommended Level: ") + listing.ItemRecommendedLevel + "\n";

            header += ColorTokenService.Green("Price: ") + listing.Price + " credits\n";
            header += ColorTokenService.Green("Seller Note: ") + sellerNote + "\n";

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
            var model = MarketService.GetPlayerMarketData(buyer);
            var listing = DataService.PCMarketListing.GetByIDOrDefault(model.BrowseListingID);
            NWPlaceable terminal = GetDialogTarget().Object;
            
            // Item was removed, sold, or expired.
            if (listing == null || listing.DateSold != null || listing.DateExpires <= DateTime.UtcNow)
            {
                LoadItemListPage();
                ChangePage("ItemListPage", false);
                buyer.FloatingText("Unfortunately, that item is no longer available.");
                return;
            }

            switch (responseID)
            {
                case 1: // Examine Item
                    _.CreateItemOnObject("exit_preview", terminal);
                    NWItem item = SerializationService.DeserializeItem(listing.ItemObject, terminal);
                    item.IsCursed = true;
                    OpenTerminalInventory();
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
                        MarketService.GiveMarketGoldToPlayer(listing.SellerPlayerID, listing.Price);

                        // Give the item to the buyer.
                        SerializationService.DeserializeItem(listing.ItemObject, buyer);

                        // Mark the listing as sold.
                        listing.DateSold = DateTime.UtcNow;
                        listing.BuyerPlayerID = buyer.GlobalID;
                        DataService.SubmitDataChange(listing, DatabaseActionType.Update);
                        
                        model.IsConfirming = false;
                        SetResponseText("ItemDetailsPage", 2, "Buy Item");
                        LoadItemListPage();
                        NavigationStack.Pop(); // Assume the previous page was the ItemListPage, which we're about to send the player back to now. Remove it from the stack.
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
        
        private void LoadSellItemPage()
        {
            var player = GetPC();
            var model = MarketService.GetPlayerMarketData(player);
            string header;

            // A null or empty item object signifies that an item hasn't been selected for selling yet.
            // Hide all options except for "Pick Item"
            if (string.IsNullOrWhiteSpace(model.ItemObject))
            {
                int numberItemsSelling = DataService
                    .PCMarketListing
                    .GetAllBySellerPlayerID(player.GlobalID)
                    .Count(x => x.DateRemoved == null &&
                                x.DateSold == null);
                bool canSellAnotherItem = numberItemsSelling < MarketService.NumberOfItemsAllowedToBeSoldAtATime;
                header = ColorTokenService.Green("Galactic Trade Network - Sell Item") + "\n\n";
                header += ColorTokenService.Green("Items Selling: ") + numberItemsSelling + " / " + MarketService.NumberOfItemsAllowedToBeSoldAtATime + "\n\n";

                if (canSellAnotherItem)
                {
                    header += "Please select an item to sell.";
                }
                else
                {
                    header += "You cannot sell any more items at this time.";
                }

                SetResponseVisible("SellItemPage", 1, false); // Refresh
                SetResponseVisible("SellItemPage", 2, canSellAnotherItem);  // Pick Item
                SetResponseVisible("SellItemPage", 3, false); // Change Price
                SetResponseVisible("SellItemPage", 4, false); // Change Seller Note
                SetResponseVisible("SellItemPage", 5, false); // Remove Seller Note
                SetResponseVisible("SellItemPage", 6, false); // Change Listing Length
                SetResponseVisible("SellItemPage", 7, false); // List the Item
            }
            // Otherwise an item has already been picked.
            else
            {
                MarketCategory category = DataService.MarketCategory.GetByID(model.ItemMarketCategoryID);
                float feeRate = MarketService.CalculateFeePercentage(model.LengthDays);
                int fees = (int)(model.SellPrice * feeRate);
                if (fees < 1) fees = 1;
                bool canListItem = model.SellPrice > 0;
                string sellerNote = model.SellerNote;
                if (string.IsNullOrWhiteSpace(sellerNote))
                    sellerNote = "[NOT SPECIFIED]";
                
                header = ColorTokenService.Green("Galactic Trade Network - Sell Item") + "\n\n";
                header += ColorTokenService.Green("Item: ") + model.ItemStackSize + "x " + model.ItemName + "\n";
                header += ColorTokenService.Green("Category: ") + category.Name + "\n";

                if(model.ItemRecommendedLevel > 0)
                    header += ColorTokenService.Green("Recommended Level: ") + model.ItemRecommendedLevel + "\n";

                header += ColorTokenService.Green("Sell Price: ") + model.SellPrice + " credits\n";
                header += ColorTokenService.Green("Fees: ") + fees + " credits\n";
                header += ColorTokenService.Green("Listing Length: ") + model.LengthDays + " days\n";
                header += ColorTokenService.Green("Seller Note: ") + sellerNote + "\n\n";

                if (canListItem)
                {
                    header += ColorTokenService.Green("This item can be listed now.");
                }
                else
                {
                    header += ColorTokenService.Red("This item cannot be listed yet. Please confirm all details - such as pricing - have been set.");
                }

                SetResponseVisible("SellItemPage", 1, true);  // Refresh
                SetResponseVisible("SellItemPage", 2, false); // Pick Item
                SetResponseVisible("SellItemPage", 3, true);  // Change Price
                SetResponseVisible("SellItemPage", 4, true);  // Change Seller Note
                SetResponseVisible("SellItemPage", 5, true);  // Remove Seller Note
                SetResponseVisible("SellItemPage", 6, true);  // Change Listing Length
                SetResponseVisible("SellItemPage", 7, canListItem);    // List the Item
                
            }

            SetPageHeader("SellItemPage", header);
        }

        private void SellItemPageResponses(int responseID)
        {
            var player = GetPC();
            var model = MarketService.GetPlayerMarketData(player);

            switch (responseID)
            {
                case 1: // Refresh
                    LoadSellItemPage();
                    break;
                case 2: // Pick Item
                    OpenTerminalInventory();
                    break;
                case 3: // Change Price
                    LoadChangePricePage();
                    ChangePage("ChangePricePage");
                    break;
                case 4: // Change Seller Note
                    model.IsSettingSellerNote = true;
                    player.FloatingText("Please enter text and then select the 'Refresh' option to see your changes.");
                    break;
                case 5: // Remove Seller Note
                    model.SellerNote = string.Empty;
                    LoadSellItemPage();
                    break;
                case 6: // Change Listing Length
                    LoadChangeListingLengthPage();
                    ChangePage("ChangeListingLengthPage");
                    break;
                case 7: // List the Item
                    ListItem();
                    break;
            }
        }

        private void LoadChangePricePage()
        {
            var player = GetPC();
            var model = MarketService.GetPlayerMarketData(player);
            string header = ColorTokenService.Green("Galactic Trade Network - Change Sell Price") + "\n\n";
            header += ColorTokenService.Green("Current Price: ") + model.SellPrice;

            SetPageHeader("ChangePricePage", header);
        }

        private void ChangePricePageResponses(int responseID)
        {
            var player = GetPC();
            var model = MarketService.GetPlayerMarketData(player);

            switch (responseID)
            {
                case 1: // Increase by 100,000 credits
                    model.SellPrice += 100000;
                    break;
                case 2: // Increase by 10,000 credits
                    model.SellPrice += 10000;
                    break;
                case 3: // Increase by 1,000 credits
                    model.SellPrice += 1000;
                    break;
                case 4: // Increase by 100 credits
                    model.SellPrice += 100;
                    break;
                case 5: // Increase by 10 credits
                    model.SellPrice += 10;
                    break;
                case 6: // Increase by 1 credit
                    model.SellPrice += 1;
                    break;
                case 7: // Decrease by 100,000 credits
                    model.SellPrice -= 100000;
                    break;
                case 8: // Decrease by 10,000 credits
                    model.SellPrice -= 10000;
                    break;
                case 9: // Decrease by 1,000 credits
                    model.SellPrice -= 1000;
                    break;
                case 10: // Decrease by 100 credits
                    model.SellPrice -= 100;
                    break;
                case 11: // Decrease by 10 credits
                    model.SellPrice -= 10;
                    break;
                case 12: // Decrease by 1 credit
                    model.SellPrice -= 1;
                    break;
            }

            if (model.SellPrice < 0)
                model.SellPrice = 0;
            else if (model.SellPrice > 999999)
                model.SellPrice = 999999;

            LoadChangePricePage();
        }

        private void LoadChangeListingLengthPage()
        {
            var player = GetPC();
            var model = MarketService.GetPlayerMarketData(player);
            float feePercentage = MarketService.CalculateFeePercentage(model.LengthDays) * 100;
            
            string header = ColorTokenService.Green("Galactic Trade Network - Change Listing Length") + "\n\n";
            header += "Items will be listed, by default, for 7 days (real world time). You may increase or decrease this length as you see fit. A fee of 0.1% per day will be applied to the listing. (7 days = 0.7% fee).\n\n";
            header += ColorTokenService.Green("Days to List: ") + model.LengthDays + "\n";
            header += ColorTokenService.Green("Fees: ") + feePercentage.ToString("0.0") + "%";

            SetPageHeader("ChangeListingLengthPage", header);
        }

        private void ChangeListingLengthPageResponses(int responseID)
        {
            var player = GetPC();
            var model = MarketService.GetPlayerMarketData(player);

            switch (responseID)
            {
                case 1: // Set to Max (30 days, 3% fee)
                    model.LengthDays = 30;
                    break;
                case 2: // Set to Default (7 days, 0.7% fee)
                    model.LengthDays = 7;
                    break;
                case 3: // Increase by 7 days (+0.7% fee)
                    model.LengthDays += 7;
                    break;
                case 4: // Increase by 1 day (+0.1% fee)
                    model.LengthDays += 1;
                    break;
                case 5: // Decrease by 7 days (-0.7% fee)
                    model.LengthDays -= 7;
                    break;
                case 6: // Decrease by 1 day (-0.1% fee)
                    model.LengthDays -= 1;
                    break;
            }

            if (model.LengthDays < 1)
                model.LengthDays = 1;
            else if (model.LengthDays > 30)
                model.LengthDays = 30;

            LoadChangeListingLengthPage();
        }

        private void ListItem()
        {
            var player = GetPC();
            var terminal = NWGameObject.OBJECT_SELF;
            var model = MarketService.GetPlayerMarketData(player);
            var marketRegionID = MarketService.GetMarketRegionID(terminal);
            var feeRate = MarketService.CalculateFeePercentage(model.LengthDays);
            int fees = (int)(model.SellPrice * feeRate);
            if (fees < 1) fees = 1;

            if (player.Gold < fees)
            {
                player.FloatingText("You do not have enough credits to pay the listing fees.");
                return;
            }

            _.TakeGoldFromCreature(fees, player, TRUE);

            PCMarketListing listing = new PCMarketListing
            {
                SellerPlayerID = player.GlobalID,
                Note = model.SellerNote ?? string.Empty,
                Price = model.SellPrice,
                MarketRegionID = marketRegionID,
                MarketCategoryID = model.ItemMarketCategoryID,
                DatePosted = DateTime.UtcNow,
                DateExpires = DateTime.UtcNow.AddDays(model.LengthDays),
                DateSold = null,
                BuyerPlayerID = null,
                ItemID = model.ItemID.ToString(),
                ItemName = model.ItemName,
                ItemTag = model.ItemTag,
                ItemResref = model.ItemResref,
                ItemObject = model.ItemObject,
                ItemRecommendedLevel = model.ItemRecommendedLevel,
                ItemStackSize = model.ItemStackSize
            };

            DataService.SubmitDataChange(listing, DatabaseActionType.Insert);
            player.FloatingText("Item listed for sale!");
            ClearNavigationStack();
            ClearModelData();
            ChangePage("MainPage", false);
        }

        private void LoadManageMarketListingsPage()
        {
            string header = ColorTokenService.Green("Galactic Trade Market - Manage Market Listings") + "\n\n";
            header += "The following is a list of items you are currently selling. Pick one to edit or remove the listing.";
            SetPageHeader("MarketListingsPage", header);

            var player = GetPC();
            var regionID = MarketService.GetMarketRegionID(NWGameObject.OBJECT_SELF);
            var listings = DataService.PCMarketListing.GetAllBySellerPlayerID(player.GlobalID)
                .Where(x => x.DateSold == null &&
                    x.DateRemoved == null &&
                    x.MarketRegionID == regionID);
            
            ClearPageResponses("MarketListingsPage");
            foreach (var listing in listings)
            {
                string itemName = BuildItemName(listing);

                // Display an EXPIRED tag on the item name if the listing has expired.
                if (listing.DateExpires < DateTime.UtcNow)
                    itemName += ColorTokenService.Red(" [EXPIRED]");

                AddResponseToPage("MarketListingsPage", itemName, true, listing.ID);
            }
        }

        private void ManageMarketListingsReponses(int responseID)
        {
            var response = GetResponseByID("MarketListingsPage", responseID);
            var player = GetPC();
            var model = MarketService.GetPlayerMarketData(player);
            model.ManageListingID = (Guid)response.CustomData;

            // Populate the temporary data model for use on other pages.
            var listing = DataService.PCMarketListing.GetByID(model.ManageListingID);
            model.ItemID = new Guid(listing.ItemID);
            model.ItemName = listing.ItemName;
            model.ItemTag = listing.ItemTag;
            model.ItemResref = listing.ItemResref;
            model.ItemObject = listing.ItemObject;
            model.ItemRecommendedLevel = listing.ItemRecommendedLevel;
            model.ItemStackSize = listing.ItemStackSize;
            model.ItemMarketCategoryID = listing.MarketCategoryID;
            model.SellPrice = listing.Price;
            model.SellerNote = listing.Note;
            model.ListingExpirationDate = listing.DateExpires;

            // Switch to details page.
            LoadManageMarketListingDetailsPage();
            ChangePage("MarketListingDetailsPage");
        }

        private void LoadManageMarketListingDetailsPage()
        {
            var player = GetPC();
            var model = MarketService.GetPlayerMarketData(player);
            var category = DataService.MarketCategory.GetByID(model.ItemMarketCategoryID);

            // Build the header
            string header = ColorTokenService.Green("Galactic Trade Market - Manage Market Listing") + "\n\n";
            header += ColorTokenService.Green("Item: ") + model.ItemStackSize + "x " + model.ItemName + "\n";
            header += ColorTokenService.Green("Category: ") + category.Name + "\n";

            if (model.ItemRecommendedLevel > 0)
                header += ColorTokenService.Green("Recommended Level: ") + model.ItemRecommendedLevel + "\n";

            header += ColorTokenService.Green("Sell Price: ") + model.SellPrice + " credits\n";
            header += ColorTokenService.Green("Seller Note: ") + model.SellerNote + "\n\n";

            if (model.IsListingExpired)
            {
                header += ColorTokenService.Red("This listing has expired.");
            }
            else
            {
                DateTime now = DateTime.UtcNow;
                var delta = model.ListingExpirationDate - now;
                string expirationTime = TimeService.GetTimeLongIntervals(delta.Days, delta.Hours, delta.Minutes, delta.Seconds, false);
                header += ColorTokenService.Green("This listing expires in " + expirationTime + ".");
            }

            SetPageHeader("MarketListingDetailsPage", header);

        }

        private void ManageMarketListingDetailsResponses(int responseID)
        {
            var player = GetPC();
            var model = MarketService.GetPlayerMarketData(player);

            switch (responseID)
            {
                case 1: // Refresh
                    LoadManageMarketListingDetailsPage();
                    break;
                case 2: // Remove Listing

                    var listing = DataService.PCMarketListing.GetByID(model.ManageListingID);

                    // Start by verifying the item is still in a valid state.
                    if (listing.DateRemoved != null ||
                        listing.DateSold != null)
                    {
                        player.FloatingText("That item is no longer available. It may have been sold or removed from the listing.");
                        ClearModelData();
                        LoadManageMarketListingsPage();
                        NavigationStack.Pop(); // Remove the duplicate "Back" page, since we're redirecting already.
                        ChangePage("MarketListingsPage", false);
                        return;
                    }

                    if (model.IsConfirming)
                    {
                        SetResponseText("MarketListingDetailsPage", 2, ColorTokenService.Red("Remove Listing"));
                        model.IsConfirming = false;
                        
                        SerializationService.DeserializeItem(listing.ItemObject, player);
                        listing.DateRemoved = DateTime.UtcNow;
                        DataService.SubmitDataChange(listing, DatabaseActionType.Update);

                        ClearModelData();
                        LoadManageMarketListingsPage();
                        NavigationStack.Pop(); // Remove the duplicate "Back" page, since we're redirecting already.
                        ChangePage("MarketListingsPage", false);
                    }
                    else
                    {
                        SetResponseText("MarketListingDetailsPage", 2, ColorTokenService.Red("CONFIRM REMOVE LISTING"));
                        model.IsConfirming = true;
                    }

                    break; 
            }
        }
        
        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
            var model = MarketService.GetPlayerMarketData(player);

            // Leaving the Item Details page.
            if (beforeMovePage == "ItemDetailsPage")
            {
                model.IsConfirming = false;
                SetResponseText("ItemDetailsPage", 2, "Buy Item");
            }
            // Leaving the Sell Item page.
            else if (beforeMovePage == "SellItemPage")
            {
                ReturnSellingItem();
            }
            // Leaving the Market Listing Details page.
            else if (beforeMovePage == "MarketListingDetailsPage")
            {
                ClearModelData();
                SetResponseText("MarketListingDetailsPage", 2, ColorTokenService.Red("Remove Listing"));
            }
            // Returning to the Sell Item page.
            else if (afterMovePage == "SellItemPage")
            {
                LoadSellItemPage();
            }
            // Returning to the Browse By Category page
            else if (afterMovePage == "BrowseByCategoryPage")
            {
                LoadBrowseByCategoryPage();
            }
            // Returning to the Browse By Seller page
            else if (afterMovePage == "BrowseBySellerPage")
            {
                LoadBrowseBySellerPage();
            }
            // Returning to the Manage Market Listings page
            else if (afterMovePage == "MarketListingsPage")
            {
                LoadManageMarketListingsPage();
            }

        }

        private void ReturnSellingItem()
        {
            var player = GetPC();
            var model = MarketService.GetPlayerMarketData(player);

            if (!model.IsSellingItem) return; // Prevent the "Manage Listings" page from firing this.

            if (!string.IsNullOrWhiteSpace(model.ItemObject))
            {
                SerializationService.DeserializeItem(model.ItemObject, player);
            }

            ClearModelData();
        }

        private void ClearModelData()
        {
            var player = GetPC();
            var model = MarketService.GetPlayerMarketData(player);

            model.ItemID = Guid.Empty;
            model.ItemName = string.Empty;
            model.ItemTag = string.Empty;
            model.ItemResref = string.Empty;
            model.ItemObject = string.Empty;
            model.ItemRecommendedLevel = 0;
            model.ItemStackSize = 0;
            model.LengthDays = 7;
            model.SellerNote = string.Empty;
            model.IsSettingSellerNote = false;
            model.TemporaryDialogNavigationStack = null;

        }

        public override void EndDialog()
        {
            var pc = GetPC();
            var model = MarketService.GetPlayerMarketData(pc);

            // We'll only wipe the data if the player isn't accessing the inventory or otherwise
            // changing contexts.
            if (!model.IsAccessingInventory)
            {
                ReturnSellingItem();
                MarketService.ClearPlayerMarketData(pc);
            }
        }

        private void OpenTerminalInventory()
        {
            var model = MarketService.GetPlayerMarketData(GetPC());
            NWPlaceable terminal = GetDialogTarget().Object;
            terminal.IsLocked = false;
            model.IsAccessingInventory = true;
            model.TemporaryDialogNavigationStack = NavigationStack;
            model.IsConfirming = false;

            _.SetEventScript(terminal, EVENT_SCRIPT_PLACEABLE_ON_USED, string.Empty);
            _.SetEventScript(terminal, EVENT_SCRIPT_PLACEABLE_ON_OPEN, "script_2");
            _.SetEventScript(terminal, EVENT_SCRIPT_PLACEABLE_ON_CLOSED, "script_3");
            _.SetEventScript(terminal, EVENT_SCRIPT_PLACEABLE_ON_INVENTORYDISTURBED, "script_4");

            terminal.SetLocalString("SCRIPT_2", "Placeable.MarketTerminal.OnOpened");
            terminal.SetLocalString("SCRIPT_3", "Placeable.MarketTerminal.OnClosed");
            terminal.SetLocalString("SCRIPT_4", "Placeable.MarketTerminal.OnDisturbed");

            GetPC().AssignCommand(() => _.ActionInteractObject(terminal));
            EndConversation();
        }
    }
}
