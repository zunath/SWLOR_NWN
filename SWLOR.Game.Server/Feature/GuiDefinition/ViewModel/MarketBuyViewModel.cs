using System;
using System.Collections.Generic;

using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PlayerMarketService;
using SWLOR.NWN.API.NWNX;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Event;
using SWLOR.Shared.Core.Log;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Core.Service;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class MarketBuyViewModel: GuiViewModelBase<MarketBuyViewModel, MarketPayload>
    {
        private ILogger _logger = ServiceContainer.GetService<ILogger>();
        private static readonly IDatabaseService _db = ServiceContainer.GetService<IDatabaseService>();
        
        private const int ListingsPerPage = 20;

        private static readonly List<MarketCategoryType> _categoryTypes = new();
        private static readonly GuiBindingList<string> _categories = new();

        private bool _skipPaginationSearch;
        private readonly List<int> _activeCategoryIdFilters = new();
        private MarketRegionType _regionType;
        private bool _sortByPriceAscending;

        /// <summary>
        /// When the module loads, set up the category lists so they don't need
        /// to be initialized for every player.
        /// </summary>
        [ScriptHandler(ScriptName.OnModuleLoad)]
        public static void LoadCategories()
        {
            foreach (var (type, category) in PlayerMarket.GetActiveCategories())
            {
                _categoryTypes.Add(type);
                _categories.Add(category.Name);
            }
        }

        public string WindowTitle
        {
            get => Get<string>();
            set => Set(value);
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

        public int SelectedPageIndex
        {
            get => Get<int>();
            set
            {
                Set(value);

                if(!_skipPaginationSearch)
                    Search();
            }
        }

        private readonly List<string> _itemIds = new();
        private readonly List<int> _itemPrices = new();

        public GuiBindingList<string> CategoryNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> CategoryToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

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

        public GuiBindingList<bool> ItemBuyEnabled
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public string SortByPriceText
        {
            get => Get<string>();
            set => Set(value);
        }

        private void LoadData()
        {
            var categoryToggles = new GuiBindingList<bool>();

            foreach (var unused in _categories)
            {
                categoryToggles.Add(false);
            }

            CategoryNames = _categories;
            CategoryToggles = categoryToggles;
        }

        protected override void Initialize(MarketPayload initialPayload)
        {
            _regionType = initialPayload.RegionType;
            var regionDetail = PlayerMarket.GetMarketRegion(_regionType);
            _skipPaginationSearch = true;
            _activeCategoryIdFilters.Clear();
            SelectedPageIndex = 0;
            SearchText = string.Empty;
            WindowTitle = $"{regionDetail.Name} Market";
            
            // Always default to lowest price first
            _sortByPriceAscending = true;
            SortByPriceText = "Price: Low-High";
            
            LoadData();
            Search();

            WatchOnClient(model => model.SearchText);
            WatchOnClient(model => model.SelectedPageIndex);
            WatchOnClient(model => model.CategoryToggles);
            _skipPaginationSearch = false;
        }

        private void Search()
        {
            var marketDetail = PlayerMarket.GetMarketRegion(_regionType);
            var query = new DBQuery<MarketItem>()
                .AddFieldSearch(nameof(MarketItem.IsListed), true)
                .AddFieldSearch(nameof(MarketItem.MarketId), marketDetail.MarketId, false);

            if (!string.IsNullOrWhiteSpace(SearchText))
                query.AddFieldSearch(nameof(MarketItem.Name), SearchText, true);

            if (_activeCategoryIdFilters.Count > 0)
            {
                query.AddFieldSearch(nameof(MarketItem.Category), _activeCategoryIdFilters);
            }

            // Add sorting by price
            query.OrderBy(nameof(MarketItem.Price), _sortByPriceAscending);

            query.AddPaging(ListingsPerPage, ListingsPerPage * SelectedPageIndex);

            var totalRecordCount = _db.SearchCount(query);
            UpdatePagination(totalRecordCount);

            var credits = GetGold(Player);
            var results = _db.Search(query);

            _itemIds.Clear();
            _itemPrices.Clear();
            var itemIconResrefs = new GuiBindingList<string>();
            var itemNames = new GuiBindingList<string>();
            var itemPriceNames = new GuiBindingList<string>();
            var itemSellerNames = new GuiBindingList<string>();
            var itemBuyEnabled = new GuiBindingList<bool>();

            foreach (var record in results)
            {
                _itemIds.Add(record.Id);
                _itemPrices.Add(record.Price);
                itemIconResrefs.Add(record.IconResref);
                itemNames.Add($"{record.Quantity}x {record.Name}");
                itemPriceNames.Add($"{record.Price} cr");
                itemSellerNames.Add(record.SellerName);
                itemBuyEnabled.Add(credits >= record.Price);
            }

            ItemIconResrefs = itemIconResrefs;
            ItemNames = itemNames;
            ItemPriceNames = itemPriceNames;
            ItemSellerNames = itemSellerNames;
            ItemBuyEnabled = itemBuyEnabled;
        }

        private void UpdatePagination(long totalRecordCount)
        {
            _skipPaginationSearch = true;
            var pageNumbers = new GuiBindingList<GuiComboEntry>();
            var pages = (int)(totalRecordCount / ListingsPerPage + (totalRecordCount % ListingsPerPage == 0 ? 0 : 1));

            // Always add page 1. In the event no items are for sale,
            // it still needs to be displayed.
            pageNumbers.Add(new GuiComboEntry($"Page 1", 0));
            for (var x = 2; x <= pages; x++)
            {
                pageNumbers.Add(new GuiComboEntry($"Page {x}", x-1));
            }

            PageNumbers = pageNumbers;

            // In the event no results are found, default the index to zero
            if (pages <= 0)
                SelectedPageIndex = 0;
            // Otherwise, if current page is outside the new page bounds,
            // set it to the last page in the list.
            else if (SelectedPageIndex > pages - 1)
                SelectedPageIndex = pages - 1;

            _skipPaginationSearch = false;
        }

        public Action OnClickSearch() => Search;

        public Action OnClickClearSearch() => () =>
        {
            SearchText = string.Empty;
            Search();
        };

        public Action OnClickPreviousPage() => () =>
        {
            _skipPaginationSearch = true;
            var newPage = SelectedPageIndex - 1;
            if (newPage < 0)
                newPage = 0;

            SelectedPageIndex = newPage;
            _skipPaginationSearch = false;
            Search();
        };

        public Action OnClickNextPage() => () =>
        {
            _skipPaginationSearch = true;
            var newPage = SelectedPageIndex + 1;
            if (newPage > PageNumbers.Count - 1)
                newPage = PageNumbers.Count - 1;

            SelectedPageIndex = newPage;
            _skipPaginationSearch = false;
            Search();
        };

        public Action OnClickExamine() => () =>
        {
            var index = NuiGetEventArrayIndex();
            var itemId = _itemIds[index];
            var dbItem = _db.Get<MarketItem>(itemId);

            var item = ObjectPlugin.Deserialize(dbItem.Data);
            var payload = new ExamineItemPayload(GetName(item), GetDescription(item), Item.BuildItemPropertyString(item));
            Gui.TogglePlayerWindow(Player, GuiWindowType.ExamineItem, payload);
            DestroyObject(item);
        };

        public Action OnClickBuy() => () =>
        {
            var index = NuiGetEventArrayIndex();
            var itemId = _itemIds[index];
            var itemName = ItemNames[index];
            var price = _itemPrices[index];

            ShowModal($"Are you sure you want to buy '{itemName}' for {price} credits?", () =>
            {
                var dbItem = _db.Get<MarketItem>(itemId);

                // If another player buys the item or the item gets removed from the market,
                // prevent the player from purchasing it.
                if (dbItem == null)
                {
                    FloatingTextStringOnCreature("This item is no longer available. It may have been sold or removed from the market.", Player, false);
                    return;
                }

                // If the player no longer has enough money to purchase the item, prevent them from purchasing it.
                // This can happen if the player opens the modal, drops their money and clicks yes.
                // Another potential scenario is the seller adjusts the price on the item while they're mid-purchase.
                if (GetGold(Player) < price)
                {
                    FloatingTextStringOnCreature("You do not have enough credits to purchase this item.", Player, false);
                    return;
                }

                // Item's price has been changed since the player's search.
                // Notify them and refresh the search.
                if (dbItem.Price != _itemPrices[index])
                {
                    FloatingTextStringOnCreature("The price of this item has been changed by the seller. Please try again.", Player, false);
                    Search();
                    return;
                }

                // Take money and give the item to the buyer.
                AssignCommand(Player, () =>
                {
                    TakeGoldFromCreature(price, Player, true);
                });
                var item = ObjectPlugin.Deserialize(dbItem.Data);
                _logger.Write<PlayerMarketLogGroup>($"{GetName(Player)} [{GetObjectUUID(Player)}] bought {GetItemStackSize(item)}x {GetName(item)} from {dbItem.SellerName} for {price} credits.");
                ObjectPlugin.AcquireItem(Player, item);

                // Remove this item from the client's search results.
                _itemIds.RemoveAt(index);
                _itemPrices.RemoveAt(index);
                ItemIconResrefs.RemoveAt(index);
                ItemNames.RemoveAt(index);
                ItemPriceNames.RemoveAt(index);
                ItemSellerNames.RemoveAt(index);
                ItemBuyEnabled.RemoveAt(index);

                // Remove the item from the database.
                _db.Delete<MarketItem>(itemId);

                // Give the money to the seller.
                var market = PlayerMarket.GetMarketRegion(_regionType);
                var sellerPlayerId = dbItem.PlayerId;
                var dbSeller = _db.Get<Player>(sellerPlayerId);
                var proceeds = (int)(price - (price * market.TaxRate));
                dbSeller.MarketTill += proceeds;
                _db.Set(dbSeller);
            });
        };

        public Action OnClickClearFilters() => () =>
        {
            LoadData();
            _activeCategoryIdFilters.Clear();
            Search();
        };

        public Action OnClickCategory() => () =>
        {
            var index = NuiGetEventArrayIndex();
            var categoryType = (int)_categoryTypes[index];

            if(CategoryToggles[index] && !_activeCategoryIdFilters.Contains(categoryType))
                _activeCategoryIdFilters.Add(categoryType);
            else if (!CategoryToggles[index] && _activeCategoryIdFilters.Contains(categoryType))
                _activeCategoryIdFilters.Remove(categoryType);

            Search();
        };

        public Action OnClickSortByPrice() => () =>
        {
            _sortByPriceAscending = !_sortByPriceAscending;
            SortByPriceText = _sortByPriceAscending ? "Price: Low-High" : "Price: High-Low";
            
            _skipPaginationSearch = true;
            SelectedPageIndex = 0;
            _skipPaginationSearch = false;
            Search();
        };

    }
}
