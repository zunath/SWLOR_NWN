using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PlayerMarketService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class MarketBuyViewModel: GuiViewModelBase<MarketBuyViewModel>
    {
        private static readonly List<MarketCategoryType> _weaponCategoryTypes = new();
        private static readonly List<MarketCategoryType> _armorCategoryTypes = new();
        private static readonly List<MarketCategoryType> _otherCategoryTypes = new();

        private static readonly GuiBindingList<string> _weaponCategories = new();
        private static readonly GuiBindingList<string> _armorCategories = new();
        private static readonly GuiBindingList<string> _otherCategories = new();

        /// <summary>
        /// When the module loads, set up the category lists so they don't need
        /// to be initialized for every player.
        /// </summary>
        [NWNEventHandler("mod_load")]
        public static void LoadCategories()
        {
            foreach (var (type, category) in PlayerMarket.GetCategoriesByGroup(MarketGroupType.Weapon))
            {
                _weaponCategoryTypes.Add(type);
                _weaponCategories.Add(category.Name);
            }
            foreach (var (type, category) in PlayerMarket.GetCategoriesByGroup(MarketGroupType.Armor))
            {
                _armorCategoryTypes.Add(type);
                _armorCategories.Add(category.Name);
            }
            foreach (var (type, category) in PlayerMarket.GetCategoriesByGroup(MarketGroupType.Other))
            {
                _otherCategoryTypes.Add(type);
                _otherCategories.Add(category.Name);
            }
        }

        public string SearchText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<GuiComboEntry> PageNumbers
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public int SelectedPage
        {
            get => Get<int>();
            set => Set(value);
        }

        public int SelectedWeaponCategoryId
        {
            get => Get<int>();
            set => Set(value);
        }

        public int SelectedArmorCategoryId
        {
            get => Get<int>();
            set => Set(value);
        }

        public int SelectedCraftingCategoryId
        {
            get => Get<int>();
            set => Set(value);
        }

        public int SelectedOtherCategoryId
        {
            get => Get<int>();
            set => Set(value);
        }


        public GuiBindingList<string> WeaponCategoryNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> ArmorCategoryNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> OtherCategoryNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

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

        public GuiBindingList<string> ItemSellerNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        private void LoadData()
        {
            var itemIconResrefs = new GuiBindingList<string>();
            var itemMarkets = new GuiBindingList<string>();
            var itemNames = new GuiBindingList<string>();
            var itemPriceNames = new GuiBindingList<string>();
            _itemPrices.Clear();
            var itemSellerNames = new GuiBindingList<string>();


            WeaponCategoryNames = _weaponCategories;
            ArmorCategoryNames = _armorCategories;
            OtherCategoryNames = _otherCategories;

            ItemIconResrefs = itemIconResrefs;
            ItemMarkets = itemMarkets;
            ItemNames = itemNames;
            ItemPriceNames = itemPriceNames;
            ItemSellerNames = itemSellerNames;
        }

        public Action OnLoadWindow() => () =>
        {
            LoadData();
        };

        public Action OnClickSearch() => () =>
        {

        };

        public Action OnClickClearSearch() => () =>
        {

        };

        public Action OnClickPreviousPage() => () =>
        {

        };

        public Action OnClickNextPage() => () =>
        {

        };

        public Action OnClickExamine() => () =>
        {
            var index = NuiGetEventArrayIndex();

        };

        public Action OnClickBuy() => () =>
        {
            var index = NuiGetEventArrayIndex();

        };
    }
}
