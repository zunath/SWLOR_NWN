using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PlayerMarketService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class MarketBuyViewModel: GuiViewModelBase<MarketBuyViewModel>
    {
        private const int ListingsPerPage = 30;

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

        private int SelectedWeaponCategoryIndex { get; set; }

        private int SelectedArmorCategoryIndex { get; set; }

        private int SelectedOtherCategoryIndex { get; set; }


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

        public GuiBindingList<bool> WeaponCategoryToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }
        public GuiBindingList<bool> ArmorCategoryToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }
        public GuiBindingList<bool> OtherCategoryToggles
        {
            get => Get<GuiBindingList<bool>>();
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
            var weaponCategoryToggles = new GuiBindingList<bool>();
            var armorCategoryToggles = new GuiBindingList<bool>();
            var otherCategoryToggles = new GuiBindingList<bool>();

            foreach (var unused in _weaponCategories)
            {
                weaponCategoryToggles.Add(false);
            }

            foreach (var unused in _armorCategories)
            {
                armorCategoryToggles.Add(false);
            }

            foreach (var unused in _otherCategories)
            {
                otherCategoryToggles.Add(false);
            }

            WeaponCategoryNames = _weaponCategories;
            ArmorCategoryNames = _armorCategories;
            OtherCategoryNames = _otherCategories;

            WeaponCategoryToggles = weaponCategoryToggles;
            ArmorCategoryToggles = armorCategoryToggles;
            OtherCategoryToggles = otherCategoryToggles;

        }

        public Action OnLoadWindow() => () =>
        {
            SearchText = string.Empty;
            LoadData();
            ResetCategorySelections();
            Search();

            WatchOnClient(model => model.SearchText);
        };

        private void Search()
        {
            var selectedCategoryId = MarketCategoryType.Invalid;

            if (SelectedWeaponCategoryIndex > -1)
                selectedCategoryId = _weaponCategoryTypes[SelectedWeaponCategoryIndex];
            else if (SelectedArmorCategoryIndex > -1)
                selectedCategoryId = _armorCategoryTypes[SelectedArmorCategoryIndex];
            else if (SelectedOtherCategoryIndex > -1)
                selectedCategoryId = _otherCategoryTypes[SelectedOtherCategoryIndex];

            var query = new DBQuery<MarketItem>()
                .AddFieldSearch(nameof(MarketItem.IsListed), true);

            if (!string.IsNullOrWhiteSpace(SearchText))
                query.AddFieldSearch(nameof(MarketItem.Name), SearchText, true);

            if (selectedCategoryId != MarketCategoryType.Invalid)
                query.AddFieldSearch(nameof(MarketItem.Category), (int)selectedCategoryId);

            query.AddPaging(ListingsPerPage, ListingsPerPage * SelectedPage);
            var results = DB.Search(query);

            _itemPrices.Clear();
            var itemIconResrefs = new GuiBindingList<string>();
            var itemMarkets = new GuiBindingList<string>();
            var itemNames = new GuiBindingList<string>();
            var itemPriceNames = new GuiBindingList<string>();
            var itemSellerNames = new GuiBindingList<string>();

            foreach (var record in results)
            {
                _itemPrices.Add(record.Price);
                itemIconResrefs.Add(record.IconResref);
                itemMarkets.Add(record.MarketName);
                itemNames.Add(record.Name);
                itemPriceNames.Add($"{record.Price} cr");
                itemSellerNames.Add(record.SellerName);
            }

            ItemIconResrefs = itemIconResrefs;
            ItemMarkets = itemMarkets;
            ItemNames = itemNames;
            ItemPriceNames = itemPriceNames;
            ItemSellerNames = itemSellerNames;
        }

        public Action OnClickSearch() => () =>
        {
            Search();
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

        private void ResetCategorySelections()
        {
            if (SelectedWeaponCategoryIndex > -1)
                WeaponCategoryToggles[SelectedWeaponCategoryIndex] = false;
            if (SelectedArmorCategoryIndex > -1)
                ArmorCategoryToggles[SelectedArmorCategoryIndex] = false;
            if (SelectedOtherCategoryIndex > -1)
                OtherCategoryToggles[SelectedOtherCategoryIndex] = false;

            SelectedWeaponCategoryIndex = -1;
            SelectedArmorCategoryIndex = -1;
            SelectedOtherCategoryIndex = -1;
        }

        public Action OnClickWeaponCategory() => () =>
        {
            var index = NuiGetEventArrayIndex();
            ResetCategorySelections();
            WeaponCategoryToggles[index] = !WeaponCategoryToggles[index];

            if (WeaponCategoryToggles[index])
                SelectedWeaponCategoryIndex = index;
        };

        public Action OnClickArmorCategory() => () =>
        {
            var index = NuiGetEventArrayIndex();
            ResetCategorySelections();
            ArmorCategoryToggles[index] = !ArmorCategoryToggles[index];

            if (ArmorCategoryToggles[index])
                SelectedArmorCategoryIndex = index;
        };

        public Action OnClickOtherCategory() => () =>
        {
            var index = NuiGetEventArrayIndex();
            ResetCategorySelections();
            OtherCategoryToggles[index] = !OtherCategoryToggles[index];

            if (OtherCategoryToggles[index])
                SelectedOtherCategoryIndex = index;
        };
    }
}
