using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PlayerMarketService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class MarketListingViewModel: GuiViewModelBase<MarketListingViewModel, MarketPayload>, IGuiAcceptsPriceChange
    {
        private MarketRegionType _regionType;

        public string SearchText
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsAddItemEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string ListCount
        {
            get => Get<string>();
            set => Set(value);
        }

        public string WindowTitle
        {
            get => Get<string>();
            set => Set(value);
        }

        private int _itemCount;

        private readonly List<string> _itemIds = new();

        public GuiBindingList<string> ItemIconResrefs
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> ItemNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        private readonly List<int> _itemPrices = new();
        public GuiBindingList<string> ItemPriceNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> ItemListed
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<bool> ListingCheckboxEnabled
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public string ShopTill
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsShopTillEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        private void LoadData()
        {
            var itemIconResrefs = new GuiBindingList<string>();
            var itemNames = new GuiBindingList<string>();
            var itemPriceNames = new GuiBindingList<string>();
            var itemListed = new GuiBindingList<bool>();
            var listingCheckboxEnabled = new GuiBindingList<bool>();

            _itemIds.Clear();
            _itemPrices.Clear();
            var playerId = GetObjectUUID(Player);
            var market = PlayerMarket.GetMarketRegion(_regionType);
            var query = new DBQuery<MarketItem>()
                .AddFieldSearch(nameof(MarketItem.PlayerId), playerId, false)
                .AddFieldSearch(nameof(MarketItem.MarketId), market.MarketId, false)
                .OrderBy(nameof(MarketItem.Name));
            var records = DB.Search(query);
            var count = 0;

            foreach (var record in records)
            {
                count++;

                if (!string.IsNullOrWhiteSpace(SearchText) &&
                    !record.Name.ToLower().Contains(SearchText.ToLower()))
                    continue;

                _itemIds.Add(record.Id);
                itemIconResrefs.Add(record.IconResref);
                itemNames.Add($"{record.Quantity}x {record.Name}");
                _itemPrices.Add(record.Price);
                itemPriceNames.Add($"{record.Price} cr");
                itemListed.Add(record.IsListed && record.Price > 0);
                listingCheckboxEnabled.Add(record.Price > 0);
            }

            _itemCount = count;
            UpdateItemCount();

            ItemIconResrefs = itemIconResrefs;
            ItemNames = itemNames;
            ItemPriceNames = itemPriceNames;
            ItemListed = itemListed;
            ListingCheckboxEnabled = listingCheckboxEnabled;

            var dbPlayer = DB.Get<Player>(playerId);
            ShopTill = $"Till: {dbPlayer.MarketTill} cr";
            IsShopTillEnabled = dbPlayer.MarketTill > 0;

        }

        private void UpdateItemCount()
        {
            ListCount = $"  {_itemCount} / {PlayerMarket.MaxListingsPerMarket} Items Listed";
            IsAddItemEnabled = _itemIds.Count < PlayerMarket.MaxListingsPerMarket;
        }

        protected override void Initialize(MarketPayload initialPayload)
        {
            _regionType = initialPayload.RegionType;
            var regionDetail = PlayerMarket.GetMarketRegion(_regionType);
            var taxRate = regionDetail.TaxRate * 100;
            WindowTitle = $"  {regionDetail.Name} Market [Tax Rate {taxRate:0.#}%]";
            SearchText = string.Empty;
            LoadData();

            WatchOnClient(model => model.SearchText);
            WatchOnClient(model => model.ItemListed);
        }

        public Action OnClickAddItem() => () =>
        {
            ClosePriceWindow();

            Targeting.EnterTargetingMode(Player, ObjectType.Item, "Please click on an item within your inventory.", AddItem);
            EnterTargetingMode(Player, ObjectType.Item);
            SetLocalBool(Player, "MARKET_LISTING_TARGETING_MODE", true);
        };

        public void AddItem(uint item)
        {
            if (GetItemPossessor(item) != Player)
            {
                FloatingTextStringOnCreature("Item must be in your inventory.", Player, false);
                return;
            }

            if (GetHasInventory(item))
            {
                FloatingTextStringOnCreature("Containers cannot be listed.", Player, false);
                return;
            }

            if (_itemIds.Count >= PlayerMarket.MaxListingsPerMarket)
            {
                FloatingTextStringOnCreature("You cannot list any more items.", Player, false);
                return;
            }

            if (GetItemCursedFlag(item) ||
                GetPlotFlag(item))
            {
                FloatingTextStringOnCreature("This item cannot be sold on the market.", Player, false);
                return;
            }

            if (Item.IsLegacyItem(item))
            {
                FloatingTextStringOnCreature($"Legacy items cannot be sold on the market.", Player, false);
                return;
            }

            var marketDetail = PlayerMarket.GetMarketRegion(_regionType);
            var listing = new MarketItem
            {
                MarketId = marketDetail.MarketId,
                Id = GetObjectUUID(item),
                MarketName = marketDetail.Name,
                PlayerId = GetObjectUUID(Player),
                SellerName = GetName(Player),
                Price = 0,
                IsListed = false,
                Name = GetName(item),
                Tag = GetTag(item),
                Resref = GetResRef(item),
                Data = ObjectPlugin.Serialize(item),
                Quantity = GetItemStackSize(item),
                IconResref = Item.GetIconResref(item),
                Category = PlayerMarket.GetItemMarketCategory(item)
            };

            DB.Set(listing);
            DestroyObject(item);

            _itemIds.Add(listing.Id);
            ItemIconResrefs.Add(listing.IconResref);
            ItemNames.Add($"{listing.Quantity}x {listing.Name}");
            _itemPrices.Add(listing.Price);
            ItemPriceNames.Add($"{listing.Price} cr");
            ItemListed.Add(listing.IsListed && listing.Price > 0);
            ListingCheckboxEnabled.Add(listing.Price > 0);

            _itemCount++;
            UpdateItemCount();
        }

        public Action OnClickRemove() => () =>
        {
            ClosePriceWindow();
            var index = NuiGetEventArrayIndex();
            var itemId = _itemIds[index];

            ShowModal("Are you sure you want to remove this item's listing? It will return to your inventory.", () =>
            {
                var dbListing = DB.Get<MarketItem>(itemId);

                // The item was either bought or removed already. 
                // Remove it from the client's view, but don't take any action on the server.
                if (dbListing != null)
                {
                    var deserialized = ObjectPlugin.Deserialize(dbListing.Data);
                    ObjectPlugin.AcquireItem(Player, deserialized);
                    DB.Delete<MarketItem>(itemId);
                }
                else
                {
                    FloatingTextStringOnCreature("Your listing for '' has been removed or sold already.", Player, false);
                }

                _itemIds.RemoveAt(index);
                ItemIconResrefs.RemoveAt(index);
                ItemNames.RemoveAt(index);
                _itemPrices.RemoveAt(index);
                ItemPriceNames.RemoveAt(index);
                ItemListed.RemoveAt(index);
                ListingCheckboxEnabled.RemoveAt(index);

                _itemCount--;
                UpdateItemCount();
            });
        };

        public Action OnClickSearch()
        {
            ClosePriceWindow();
            return LoadData;
        }

        public Action OnClickClear() => () =>
        {
            ClosePriceWindow();
            SearchText = string.Empty;
            LoadData();
        };

        public Action OnClickSaveChanges() => () =>
        {
            ClosePriceWindow();

            for(var index = 0; index < _itemIds.Count; index++)
            {
                var id = _itemIds[index];
                var dbListing = DB.Get<MarketItem>(id);

                // It's possible the item was sold already, in which case there won't be a DB record.
                // Skip this update.
                if (dbListing == null)
                    continue;

                // Only do updates if either the price or listing status has changed.
                if (dbListing.Price == _itemPrices[index] && 
                    dbListing.IsListed == ItemListed[index]) 
                    continue;

                // Do the update for this record.
                dbListing.Price = _itemPrices[index];
                dbListing.IsListed = ItemListed[index];

                if(dbListing.IsListed)
                    dbListing.DateListed = DateTime.UtcNow;

                DB.Set(dbListing);
            }
            
            LoadData();
        };

        private void ClosePriceWindow()
        {
            if (Gui.IsWindowOpen(Player, GuiWindowType.PriceSelection))
            {
                Gui.TogglePlayerWindow(Player, GuiWindowType.PriceSelection);
            }
        }

        public Action OnClickChangePrice() => () =>
        {
            // There is a defect with NUI which prevents text boxes from working within lists.
            // As a workaround, we use a button to display a price change window. 
            // The price is entered by the user and saved, which then updates this window.
            // If/when the defect gets fixed, this can be replaced in favor of a simple text edit control.
            var index = NuiGetEventArrayIndex();
            var recordId = _itemIds[index];
            var currentPrice = _itemPrices[index];
            var itemName = ItemNames[index];
            var payload = new PriceSelectionPayload(GuiWindowType.MarketListing, recordId, currentPrice, itemName, "Price For:");
            Gui.TogglePlayerWindow(Player, GuiWindowType.PriceSelection, payload);
        };

        public void ChangePrice(string recordId, int price)
        {
            var index = _itemIds.IndexOf(recordId);

            // Couldn't find the record.
            if (index <= -1)
                return;

            _itemPrices[index] = price;
            ItemPriceNames[index] = $"{price} cr";

            if (price <= 0)
            {
                ListingCheckboxEnabled[index] = false;
                ItemListed[index] = false;
            }
            else
            {
                ListingCheckboxEnabled[index] = true;
            }
        }

        public Action OnClickShopTill() => () =>
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var credits = dbPlayer.MarketTill;

            if (credits <= 0)
                return;

            GiveGoldToCreature(Player, credits);
            dbPlayer.MarketTill = 0;
            DB.Set(dbPlayer);

            IsShopTillEnabled = false;
            ShopTill = "Shop Till: 0 cr";
        };

    }
}
