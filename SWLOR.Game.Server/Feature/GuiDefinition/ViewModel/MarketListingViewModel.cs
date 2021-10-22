using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class MarketListingViewModel: GuiViewModelBase<MarketListingViewModel>
    {
        private static readonly GuiColor _red = new GuiColor(255, 0, 0);
        private static readonly GuiColor _green = new GuiColor(0, 255, 0);

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
        private readonly List<string> _itemIds = new List<string>();

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

        public GuiBindingList<string> ItemPrices
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> ItemListDelistNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<GuiColor> ItemListDelistColors
        {
            get => Get<GuiBindingList<GuiColor>>();
            set => Set(value);
        }

        private void LoadData()
        {
            var itemMarkets = new GuiBindingList<string>();
            var itemNames = new GuiBindingList<string>();
            var itemPrices = new GuiBindingList<string>();
            var itemListDelistNames = new GuiBindingList<string>();
            var itemListDelistColors = new GuiBindingList<GuiColor>();

            _itemIds.Clear();
            var playerId = GetObjectUUID(Player);
            var records = DB.Search<MarketItem>(nameof(MarketItem.PlayerId), playerId);
            var count = 0;

            foreach (var record in records)
            {
                count++;

                if (!string.IsNullOrWhiteSpace(SearchText) &&
                    !record.Name.ToLower().Contains(SearchText.ToLower()))
                    continue;

                _itemIds.Add(record.ItemId);
                itemMarkets.Add(record.MarketName);
                itemNames.Add($"{record.Quantity}x {record.Name}");
                itemPrices.Add(record.Price.ToString());

                if (record.IsListed)
                {
                    itemListDelistNames.Add("Delist");
                    itemListDelistColors.Add(_red);
                }
                else
                {
                    itemListDelistNames.Add("List");
                    itemListDelistColors.Add(_green);
                }
            }

            _itemCount = count;
            UpdateItemCount();
            IsAddItemEnabled = _itemIds.Count < PlayerMarket.MaxListingCount;
            ItemMarkets = itemMarkets;
            ItemNames = itemNames;
            ItemPrices = itemPrices;
            ItemListDelistNames = itemListDelistNames;
            ItemListDelistColors = itemListDelistColors;
        }

        private void UpdateItemCount()
        {
            ListCount = $"{_itemCount} / {PlayerMarket.MaxListingCount} Items Listed";
        }

        public Action OnLoadWindow() => () =>
        {
            SearchText = string.Empty;
            LoadData();

            WatchOnClient(model => model.SearchText);
        };

        public Action OnClickAddItem() => () =>
        {
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
                Quantity = GetItemStackSize(item)
            };

            DB.Set(listing.ItemId, listing);
            DestroyObject(item);

            _itemIds.Add(listing.ItemId);
            ItemMarkets.Add(listing.MarketName);
            ItemNames.Add($"{listing.Quantity}x {listing.Name}");
            ItemPrices.Add(listing.Price.ToString());
            ItemListDelistNames.Add("List");
            ItemListDelistColors.Add(_green);

            _itemCount++;
            UpdateItemCount();
        }

        public Action OnClickListDelist() => () =>
        {
            var index = NuiGetEventArrayIndex();
            var itemId = _itemIds[index];
            var dbListing = DB.Get<MarketItem>(itemId);

            dbListing.IsListed = !dbListing.IsListed;

            if (dbListing.IsListed)
            {
                ItemListDelistNames[index] = "Delist";
                ItemListDelistColors[index] = _red;
            }
            else
            {
                ItemListDelistNames[index] = "List";
                ItemListDelistColors[index] = _green;
            }
        };

        public Action OnClickRemove() => () =>
        {
            var index = NuiGetEventArrayIndex();
            var itemId = _itemIds[index];

            ShowModal("Are you sure you want to remove this item's listing? It will return to your inventory.", () =>
            {
                var dbListing = DB.Get<MarketItem>(itemId);

                var deserialized = ObjectPlugin.Deserialize(dbListing.Data);
                ObjectPlugin.AcquireItem(Player, deserialized);
                DB.Delete<MarketItem>(itemId);

                _itemIds.RemoveAt(index);
                ItemMarkets.RemoveAt(index);
                ItemNames.RemoveAt(index);
                ItemPrices.RemoveAt(index);
                ItemListDelistNames.RemoveAt(index);
                ItemListDelistColors.RemoveAt(index);

                _itemCount--;
                UpdateItemCount();
            });
        };

        public Action OnClickSearch() => LoadData;
    }
}
