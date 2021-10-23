using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class MarketListingViewModel: GuiViewModelBase<MarketListingViewModel>
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
        private readonly List<string> _itemIds = new List<string>();

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

        public GuiBindingList<int> ItemPrices
        {
            get => Get<GuiBindingList<int>>();
            set => Set(value);
        }

        public GuiBindingList<bool> ItemListed
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        private void LoadData()
        {
            var itemIconResrefs = new GuiBindingList<string>();
            var itemMarkets = new GuiBindingList<string>();
            var itemNames = new GuiBindingList<string>();
            var itemPrices = new GuiBindingList<int>();
            var itemListed = new GuiBindingList<bool>();

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
                itemIconResrefs.Add(record.IconResref);
                itemMarkets.Add(record.MarketName);
                itemNames.Add($"{record.Quantity}x {record.Name}");
                itemPrices.Add(record.Price);
                itemListed.Add(false);
            }

            _itemCount = count;
            UpdateItemCount();
            IsAddItemEnabled = _itemIds.Count < PlayerMarket.MaxListingCount;

            ItemIconResrefs = itemIconResrefs;
            ItemMarkets = itemMarkets;
            ItemNames = itemNames;
            ItemPrices = itemPrices;
            ItemListed = itemListed;
        }

        private void UpdateItemCount()
        {
            ListCount = $"  {_itemCount} / {PlayerMarket.MaxListingCount} Items Listed";
        }

        public Action OnLoadWindow() => () =>
        {
            SearchText = string.Empty;
            LoadData();

            WatchOnClient(model => model.SearchText);
            WatchOnClient(model => model.ItemPrices);
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
                Quantity = GetItemStackSize(item),
                IconResref = GetIconResref(item, GetBaseItemType(item))
            };

            DB.Set(listing.ItemId, listing);
            DestroyObject(item);

            _itemIds.Add(listing.ItemId);
            ItemIconResrefs.Add(listing.IconResref);
            ItemMarkets.Add(listing.MarketName);
            ItemNames.Add($"{listing.Quantity}x {listing.Name}");
            ItemPrices.Add(listing.Price);
            ItemListed.Add(listing.IsListed);

            _itemCount++;
            UpdateItemCount();
        }

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
                ItemListed.RemoveAt(index);

                _itemCount--;
                UpdateItemCount();
            });
        };

        public Action OnClickSearch() => LoadData;

        public Action OnClickClear() => () =>
        {
            SearchText = string.Empty;
            LoadData();
        };

        public Action OnClickSaveChanges() => () =>
        {

        };

        private string GetIconResref(uint item, BaseItem baseItem)
        {
            if (baseItem == BaseItem.Cloak) // Cloaks use PLTs so their default icon doesn't really work
                return "iit_cloak";
            else if (baseItem == BaseItem.SpellScroll || baseItem == BaseItem.EnchantedScroll)
            {// Scrolls get their icon from the cast spell property
                if (GetItemHasItemProperty(item, ItemPropertyType.CastSpell))
                {
                    for(var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                    {
                        if (GetItemPropertyType(ip) == ItemPropertyType.CastSpell)
                            return Get2DAString("iprp_spells", "Icon", GetItemPropertySubType(ip));
                    }
                }
            }
            else if (Get2DAString("baseitems", "ModelType", (int)baseItem) == "0")
            {// Create the icon resref for simple modeltype items
                var sSimpleModelId  = GetItemAppearance(item, ItemAppearanceType.SimpleModel, 0).ToString();
                while (GetStringLength(sSimpleModelId) < 3)
                {
                    sSimpleModelId = "0" + sSimpleModelId;
                }

                var sDefaultIcon = Get2DAString("baseitems", "DefaultIcon", (int)baseItem);
                switch (baseItem)
                {
                    case BaseItem.MiscSmall:
                    case BaseItem.CraftMaterialSmall:
                        sDefaultIcon = "iit_smlmisc_" + sSimpleModelId;
                        break;
                    case BaseItem.MiscMedium:
                    case BaseItem.CraftMaterialMedium:
                    case BaseItem.CraftBase:
                        sDefaultIcon = "iit_midmisc_" + sSimpleModelId;
                        break;
                    case BaseItem.MiscLarge:
                        sDefaultIcon = "iit_talmisc_" + sSimpleModelId;
                        break;
                    case BaseItem.MiscThin:
                        sDefaultIcon = "iit_thnmisc_" + sSimpleModelId;
                        break;
                }

                var nLength = GetStringLength(sDefaultIcon);
                if (GetSubString(sDefaultIcon, nLength - 4, 1) == "_")// Some items have a default icon of xx_yyy_001, we strip the last 4 symbols if that is the case
                    sDefaultIcon = GetStringLeft(sDefaultIcon, nLength - 4);
                var sIcon = sDefaultIcon + "_" + sSimpleModelId;
                if (ResManGetAliasFor(sIcon, ResType.TGA) != "")// Check if the icon actually exists, if not, we'll fall through and return the default icon
                    return sIcon;
            }

            // For everything else use the item's default icon
            return Get2DAString("baseitems", "DefaultIcon", (int)baseItem);
        }

    }
}
