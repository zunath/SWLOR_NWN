using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class MarketListingViewModel: GuiViewModelBase<MarketListingViewModel>, IGuiAcceptsPriceChange
    {
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

        private int _itemCount;

        private readonly List<string> _itemIds = new();

        public GuiBindingList<string> ItemIconResrefs
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> ItemMarkets
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

        private void LoadData()
        {
            var itemIconResrefs = new GuiBindingList<string>();
            var itemMarkets = new GuiBindingList<string>();
            var itemNames = new GuiBindingList<string>();
            var itemPriceNames = new GuiBindingList<string>();
            var itemListed = new GuiBindingList<bool>();
            var listingCheckboxEnabled = new GuiBindingList<bool>();

            _itemIds.Clear();
            _itemPrices.Clear();
            var playerId = GetObjectUUID(Player);
            var query = new DBQuery<MarketItem>()
                .AddFieldSearch(nameof(MarketItem.PlayerId), playerId, false)
                .OrderBy(nameof(MarketItem.Name));
            var records = DB.Search(query);
            var count = 0;

            foreach (var record in records)
            {
                count++;

                if (!string.IsNullOrWhiteSpace(SearchText) &&
                    !record.Name.ToLower().Contains(SearchText.ToLower()))
                    continue;

                _itemIds.Add(record.ItemId);
                itemIconResrefs.Add(record.IconResref);
                itemMarkets.Add(record.MarketName);
                itemNames.Add($"{record.Quantity}x {record.Name}");
                _itemPrices.Add(record.Price);
                itemPriceNames.Add($"{record.Price} cr");
                itemListed.Add(record.IsListed && record.Price > 0);
                listingCheckboxEnabled.Add(record.Price > 0);
            }

            _itemCount = count;
            UpdateItemCount();

            ItemIconResrefs = itemIconResrefs;
            ItemMarkets = itemMarkets;
            ItemNames = itemNames;
            ItemPriceNames = itemPriceNames;
            ItemListed = itemListed;
            ListingCheckboxEnabled = listingCheckboxEnabled;
        }

        private void UpdateItemCount()
        {
            ListCount = $"  {_itemCount} / {PlayerMarket.MaxListingCount} Items Listed";
            IsAddItemEnabled = _itemIds.Count < PlayerMarket.MaxListingCount;
        }

        public Action OnLoadWindow() => () =>
        {
            SearchText = string.Empty;
            LoadData();

            WatchOnClient(model => model.SearchText);
            WatchOnClient(model => model.ItemListed);
        };

        public Action OnClickAddItem() => () =>
        {
            ClosePriceWindow();
            EnterTargetingMode(Player, ObjectType.Item);
        };

        [NWNEventHandler("mod_p_target")]
        public static void SelectItem()
        {
            var player = GetLastPlayerToSelectTarget();
            var target = GetTargetingModeSelectedObject();
            var window = Gui.GetPlayerWindow(player, GuiWindowType.MarketListing);
            var vm = (MarketListingViewModel)window.ViewModel;
            
            vm.AddItem(target);
        }

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

            if (_itemIds.Count >= PlayerMarket.MaxListingCount)
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

            var listing = new MarketItem
            {
                MarketId = "TestMarket", // todo: change
                ItemId = GetObjectUUID(item),
                MarketName = "Test Market", // todo: change
                PlayerId = GetObjectUUID(Player),
                SellerName = GetName(Player),
                Price = 0,
                IsListed = false,
                Name = GetName(item),
                Tag = GetTag(item),
                Resref = GetResRef(item),
                Data = ObjectPlugin.Serialize(item),
                Quantity = GetItemStackSize(item),
                IconResref = PlayerMarket.GetIconResref(item),
                Category = PlayerMarket.GetItemMarketCategory(item)
            };

            DB.Set(listing.ItemId, listing);
            DestroyObject(item);

            _itemIds.Add(listing.ItemId);
            ItemIconResrefs.Add(listing.IconResref);
            ItemMarkets.Add(listing.MarketName);
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

                var deserialized = ObjectPlugin.Deserialize(dbListing.Data);
                ObjectPlugin.AcquireItem(Player, deserialized);
                DB.Delete<MarketItem>(itemId);

                _itemIds.RemoveAt(index);
                ItemIconResrefs.RemoveAt(index);
                ItemMarkets.RemoveAt(index);
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
                DB.Set(id, dbListing);
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
            if (!Gui.IsWindowOpen(Player, GuiWindowType.PriceSelection))
            {
                var pricePickerWindow = Gui.GetPlayerWindow(Player, GuiWindowType.PriceSelection);
                var vm = (PriceSelectionViewModel)pricePickerWindow.ViewModel;
                var index = NuiGetEventArrayIndex();
                var recordId = _itemIds[index];
                var currentPrice = _itemPrices[index];
                var itemName = ItemNames[index];

                vm.SpecifyTargetWindow(GuiWindowType.MarketListing, recordId, currentPrice, itemName);
            }
            
            Gui.TogglePlayerWindow(Player, GuiWindowType.PriceSelection);
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
    }
}
