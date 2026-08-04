using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Merchants;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Editors.Behaviors;
using SWLOR.Toolset.Editors.Items;

namespace SWLOR.Toolset.Editors.Merchants
{
    /// <summary>The SWLOR-focused merchant editor over one UTM blueprint.</summary>
    public sealed partial class MerchantEditorViewModel : ObservableObject, IDisposable
    {
        public const string OnOpenStoreScript = "on_open_store";
        public const string OnStoreClosedScript = "on_close_store";
        private const int ItemCandidatePageSize = 48;
        private static readonly TimeSpan ItemSearchDebounce = TimeSpan.FromMilliseconds(250);

        private readonly MerchantValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Func<string, IReadOnlyList<BehaviorChoice>>? _resolveChoices;
        private readonly Func<string, MerchantItemDefinition?>? _loadItem;
        private readonly Func<string, int, int, int, CancellationToken,
            Task<IReadOnlyList<MerchantItemDefinition>>>? _searchItems;
        private readonly Action<string, Action<Bitmap>>? _requestItemPreview;
        private readonly Action<string>? _openItem;
        private readonly Action<string, MerchantInstancePlacement>? _goToInstance;
        private readonly IReadOnlyList<BehaviorChoice> _baseItems;
        private readonly MerchantInstanceService? _instances;
        private readonly List<MerchantBuyingRuleViewModel> _allBuyingRules = new();
        private readonly HashSet<(int PaneIndex, int ItemIndex)> _checkedInventoryItems = new();
        private int _instanceRefreshGeneration;
        private string? _loadedPlacementResRef;
        private int _inventoryRefreshGeneration;
        private int _itemCandidateRefreshGeneration;
        private int _itemCandidateOffset;
        private bool _itemCandidatesExhausted;
        private CancellationTokenSource? _itemSearchDebounce;
        private CancellationTokenSource? _itemCandidateRequest;
        private bool _isUpdatingInventoryChecks;
        private bool _loading;
        private bool _disposed;

        public ObservableCollection<BehaviorRowViewModel> DetailRows { get; } = new();
        public ObservableCollection<BehaviorRowViewModel> PricingRows { get; } = new();
        public ObservableCollection<MerchantInventoryCategoryViewModel> InventoryCategories { get; } = new();
        public ObservableCollection<MerchantInventoryItemViewModel> InventoryItems { get; } = new();
        public ObservableCollection<MerchantItemCandidateViewModel> ItemCandidates { get; } = new();
        public ObservableCollection<MerchantBuyingRuleViewModel> BuyingRules { get; } = new();
        public ObservableCollection<MerchantInstancePlacement> PlacedInstances { get; } = new();

        public string HeaderName => _store.GetLocalizedText("LocName");
        public string HeaderKind => "merchant";
        public string HeaderOwner { get; private set; }

        public void SetHeaderOwner(string value)
        {
            HeaderOwner = value;
            OnPropertyChanged(nameof(HeaderOwner));
        }
        public string ResRef => _store.GetString(BehaviorFieldStorage.Field, "ResRef");

        public bool HasInventoryItems => InventoryItems.Count > 0;
        public bool HasSelectedInventoryItem => SelectedInventoryItem != null;
        public int CheckedInventoryItemCount => _checkedInventoryItems.Count;
        public string CheckedInventorySummary =>
            $"{CheckedInventoryItemCount} item{(CheckedInventoryItemCount == 1 ? string.Empty : "s")} selected";
        public string RemoveCheckedInventoryLabel =>
            $"Remove selected ({CheckedInventoryItemCount})";
        public bool? ShownInventorySelectionState
        {
            get
            {
                if (InventoryItems.Count == 0 || InventoryItems.All(item => !item.IsChecked))
                    return false;
                return InventoryItems.All(item => item.IsChecked) ? true : null;
            }
        }
        public bool CanToggleShownInventoryItems => InventoryItems.Count > 0;
        public bool HasItemCandidates => ItemCandidates.Count > 0;
        public bool CanLoadMoreItemCandidates =>
            !IsLoadingItemCandidates && !_itemCandidatesExhausted && SelectedInventoryCategory != null;
        public bool IsInstanceOperationBusy => IsLoadingInstances || IsUpdatingInstances;
        public string InstanceOperationStatus => IsUpdatingInstances
            ? "Updating placed merchant instances..."
            : "Scanning placed merchant instances...";
        public bool HasPlacedInstances => PlacedInstances.Count > 0;
        public bool HasOutOfDateInstances => PlacedInstances.Any(instance => !instance.IsCurrent);
        public int OutOfDateMerchantRecords =>
            PlacedInstances.Sum(instance => instance.OutOfDateMerchantRecords);
        public int OutOfDateItemRecords =>
            PlacedInstances.Sum(instance => instance.OutOfDateItemRecords);
        public string InventorySummary =>
            $"{InventoryItems.Count} item{(InventoryItems.Count == 1 ? string.Empty : "s")} shown";
        public string CandidateSummary => ItemCandidateError != null
            ? $"Item search failed: {ItemCandidateError}"
            : IsLoadingItemCandidates && ItemCandidates.Count == 0
            ? "Loading items..."
            : ItemCandidates.Count == 0
                ? "No matching items"
                : IsLoadingItemCandidates
                    ? $"{ItemCandidates.Count} items loaded · loading more..."
                    : CanLoadMoreItemCandidates
                        ? $"{ItemCandidates.Count} items loaded · scroll for more"
                        : $"{ItemCandidates.Count} item{(ItemCandidates.Count == 1 ? string.Empty : "s")}";
        public string BuyingRuleSummary =>
            $"{BuyingRules.Count} of {_allBuyingRules.Count} base item types";
        public string InstanceSummary
        {
            get
            {
                if (IsLoadingInstances)
                    return "Scanning the module for placed instances...";
                if (!ArePlacedInstancesLoaded)
                {
                    return PlacedInstancesNeedRefresh
                        ? "Merchant data changed. Refresh to rescan placed instances."
                        : "Instance status loads only when this tab is opened.";
                }
                if (PlacedInstances.Count == 0)
                {
                    return "0 merchant records and 0 item records out of date. " +
                           "This merchant has no placed instances.";
                }
                if (HasOutOfDateInstances)
                {
                    return $"{RecordCount(OutOfDateMerchantRecords, "merchant")} and " +
                           $"{RecordCount(OutOfDateItemRecords, "item")} out of date across " +
                           $"{PlacedInstances.Count(instance => !instance.IsCurrent)} of " +
                           $"{PlacedInstances.Count} placed instance" +
                           $"{(PlacedInstances.Count == 1 ? string.Empty : "s")}.";
                }

                return $"0 merchant records and 0 item records out of date. All " +
                       $"{PlacedInstances.Count} placed instance" +
                       $"{(PlacedInstances.Count == 1 ? string.Empty : "s")} up to date.";
            }
        }
        public string SelectedItemName => SelectedInventoryItem?.DisplayName ?? "No item selected";
        public string SelectedItemResRef => SelectedInventoryItem?.ResRef ?? string.Empty;
        public Bitmap? SelectedItemPreview => SelectedInventoryItem?.Preview;
        public string SelectedItemSellPrice => SelectedInventoryItem?.SellPrice ?? "—";
        public string SelectedItemBuyPrice => SelectedInventoryItem?.BuyPrice ?? "—";
        public IReadOnlyList<ItemStatSummaryGroup> SelectedItemStatGroups =>
            SelectedInventoryItem?.StatGroups ?? Array.Empty<ItemStatSummaryGroup>();
        public bool HasSelectedItemStats => SelectedItemStatGroups.Count > 0;
        public bool ShowsSelectedItemStatsStatus =>
            HasSelectedInventoryItem && !HasSelectedItemStats;
        public string SelectedItemStatsStatus => ShowsSelectedItemStatsStatus
            ? "This item has no gameplay stats."
            : string.Empty;
        public bool SelectedItemInfinite
        {
            get => SelectedInventoryItem?.IsInfinite == true;
            set
            {
                if (SelectedInventoryItem != null)
                    SelectedInventoryItem.IsInfinite = value;
            }
        }

        public bool NeedsSaveNormalization =>
            _store.Owner.GetStringOrNull("Comment") != string.Empty ||
            _store.Owner.GetIntOrNull("IdentifyPrice") != 0 ||
            _store.Owner.GetIntOrNull("BlackMarket") != 1 ||
            _store.Owner.GetIntOrNull("MaxBuyPrice") != -1 ||
            _store.Owner.GetIntOrNull("StoreGold") != -1 ||
            !string.Equals(
                _store.Owner.GetStringOrNull("OnOpenStore"),
                OnOpenStoreScript,
                StringComparison.Ordinal) ||
            !string.Equals(
                _store.Owner.GetStringOrNull("OnStoreClosed"),
                OnStoreClosedScript,
                StringComparison.Ordinal) ||
            _store.Owner.GetListOrEmpty("StoreList").Count != MerchantValueStore.InventoryPaneCount ||
            _store.Owner.GetOrNull(MerchantValueStore.WillNotBuyField)?.Type != GffFieldType.List ||
            _store.Owner.GetOrNull(MerchantValueStore.WillOnlyBuyField)?.Type != GffFieldType.List ||
            (_loadItem != null && !_store.InventoryMatchesCategories(ResolveStorePanel));

        [ObservableProperty]
        private MerchantInventoryCategoryViewModel? _selectedInventoryCategory;

        [ObservableProperty]
        private MerchantInventoryItemViewModel? _selectedInventoryItem;

        [ObservableProperty]
        private string _inventorySearchText = string.Empty;

        [ObservableProperty]
        private string _itemSearchText = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddInventoryItemCommand))]
        private MerchantItemCandidateViewModel? _selectedItemCandidate;

        [ObservableProperty]
        private bool _buyOnlySelected;

        public bool BuyAllExceptSelected
        {
            get => !BuyOnlySelected;
            set
            {
                if (value)
                    BuyOnlySelected = false;
            }
        }

        [ObservableProperty]
        private string _buyingRuleSearchText = string.Empty;

        [ObservableProperty]
        private string? _buyingRuleError;

        [ObservableProperty]
        private bool _isLoadingInstances;

        [ObservableProperty]
        private bool _isUpdatingInstances;

        [ObservableProperty]
        private string? _instanceError;

        [ObservableProperty]
        private bool _isLoadingItemCandidates;

        [ObservableProperty]
        private string? _itemCandidateError;

        [ObservableProperty]
        private int _selectedTabIndex;

        [ObservableProperty]
        private bool _arePlacedInstancesLoaded;

        [ObservableProperty]
        private bool _placedInstancesNeedRefresh;

        public MerchantEditorViewModel(
            JsonGffStruct merchant,
            string headerOwner,
            Func<string, Action, bool> runEdit,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveChoices = null,
            IReadOnlyList<BehaviorChoice>? baseItems = null,
            Func<string, MerchantItemDefinition?>? loadItem = null,
            Func<string, int, int, int, CancellationToken,
                Task<IReadOnlyList<MerchantItemDefinition>>>? searchItems = null,
            MerchantInstanceService? instances = null,
            Action<string, Action<Bitmap>>? requestItemPreview = null,
            Action<string>? openItem = null,
            Action<string, MerchantInstancePlacement>? goToInstance = null)
        {
            ArgumentNullException.ThrowIfNull(merchant);
            _store = new MerchantValueStore(merchant);
            _runEdit = runEdit ?? throw new ArgumentNullException(nameof(runEdit));
            _resolveChoices = resolveChoices;
            _baseItems = baseItems ?? Array.Empty<BehaviorChoice>();
            _loadItem = loadItem;
            _searchItems = searchItems;
            _instances = instances;
            _requestItemPreview = requestItemPreview;
            _openItem = openItem;
            _goToInstance = goToInstance;
            HeaderOwner = headerOwner;

            BuildRows();
            BuildInventoryCategories();
            BuildBuyingRules();

            _loading = true;
            BuyOnlySelected = _store.UsesBuyOnlyRules;
            _loading = false;
            OnPropertyChanged(nameof(BuyAllExceptSelected));
            RefreshBuyingRuleSelections();

            SelectedInventoryCategory = InventoryCategories[0];
        }

        [RelayCommand(CanExecute = nameof(CanGoToInstance))]
        private void GoToInstance(MerchantInstancePlacement? placement)
        {
            if (placement != null)
                _goToInstance?.Invoke(_loadedPlacementResRef ?? HeaderOwner, placement);
        }

        private bool CanGoToInstance(MerchantInstancePlacement? placement) =>
            _goToInstance != null && placement != null;

        public void ReloadFromDocument()
        {
            ClearCheckedInventoryItemsCore();
            BuyingRuleError = null;
            foreach (var row in DetailRows.Concat(PricingRows))
                row.Reload();

            _loading = true;
            BuyOnlySelected = _store.UsesBuyOnlyRules;
            _loading = false;
            OnPropertyChanged(nameof(BuyAllExceptSelected));

            RefreshInventory();
            RefreshBuyingRuleSelections();
            OnPropertyChanged(nameof(HeaderName));
            OnPropertyChanged(nameof(ResRef));
        }

        public void RefreshPaletteChoices()
        {
            var index = DetailRows
                .Select((row, rowIndex) => (row, rowIndex))
                .Where(entry => entry.row.Definition.Name == "ID")
                .Select(entry => entry.rowIndex)
                .DefaultIfEmpty(-1)
                .Single();
            if (index < 0)
                return;

            var definition = DetailRows[index].Definition;
            DetailRows[index].Dispose();
            DetailRows[index] = CreateRow(definition);
        }

        /// <summary>Refreshes inventory names/prices and the add-item picker after a UTI save.</summary>
        public void RefreshItemCatalog()
        {
            RefreshInventory();
            RefreshItemCandidates();
        }

        public bool PrepareForSave()
        {
            if (!NeedsSaveNormalization)
                return true;

            var applied = _runEdit("Apply SWLOR merchant defaults", () =>
            {
                _store.Owner.SetString("Comment", GffFieldType.CExoString, string.Empty);
                _store.Owner.SetInt("IdentifyPrice", GffFieldType.Int, 0);
                _store.Owner.SetInt("BlackMarket", GffFieldType.Byte, 1);
                _store.Owner.SetInt("MaxBuyPrice", GffFieldType.Int, -1);
                _store.Owner.SetInt("StoreGold", GffFieldType.Int, -1);
                _store.Owner.SetString("OnOpenStore", GffFieldType.ResRef, OnOpenStoreScript);
                _store.Owner.SetString("OnStoreClosed", GffFieldType.ResRef, OnStoreClosedScript);
                _store.EnsureInventoryPanes();
                _store.NormalizeInventoryPanes(ResolveStorePanel);
                _store.EnsureBuyingRuleLists();
            });

            if (applied)
                ReloadFromDocument();
            return applied;
        }

        [RelayCommand(CanExecute = nameof(CanAddInventoryItem))]
        private void AddInventoryItem()
        {
            if (SelectedInventoryCategory == null || SelectedItemCandidate == null)
                return;

            var candidate = _loadItem?.Invoke(SelectedItemCandidate.ResRef) ??
                            SelectedItemCandidate.Definition;
            var storePanel = NormalizeStorePanel(candidate.StorePanel);
            var category = InventoryCategories.Single(entry => entry.Index == storePanel);
            if (_runEdit(
                    $"Add {candidate.Name} to {category.Name}",
                    () => _store.AddInventoryItem(storePanel, candidate.ResRef)))
            {
                SelectedInventoryCategory = category;
                RefreshInventory();
            }
        }

        private bool CanAddInventoryItem() =>
            SelectedInventoryCategory != null && SelectedItemCandidate != null;

        [RelayCommand(CanExecute = nameof(CanOpenItemDetails))]
        private void OpenItemDetails(string? resRef)
        {
            if (!string.IsNullOrWhiteSpace(resRef))
                _openItem?.Invoke(resRef);
        }

        private bool CanOpenItemDetails(string? resRef) =>
            _openItem != null && !string.IsNullOrWhiteSpace(resRef);

        [RelayCommand]
        private void RemoveInventoryItem(MerchantInventoryItemViewModel? item)
        {
            if (item == null)
                return;

            if (_runEdit(
                    $"Remove {item.DisplayName} from merchant inventory",
                    () => _store.RemoveInventoryItem(item.PaneIndex, item.ItemIndex)))
            {
                ClearCheckedInventoryItemsCore();
                RefreshInventory();
            }
        }

        [RelayCommand]
        private void ToggleShownInventorySelection()
        {
            var shouldSelect = InventoryItems.Any(item => !item.IsChecked);
            _isUpdatingInventoryChecks = true;
            try
            {
                foreach (var item in InventoryItems)
                    item.IsChecked = shouldSelect;
            }
            finally
            {
                _isUpdatingInventoryChecks = false;
            }

            NotifyInventorySelectionChanged();
        }

        [RelayCommand(CanExecute = nameof(CanRemoveCheckedInventoryItems))]
        private void RemoveCheckedInventoryItems()
        {
            var removals = _checkedInventoryItems.ToList();
            if (removals.Count == 0)
                return;

            var itemLabel = removals.Count == 1 ? "item" : "items";
            if (_runEdit(
                    $"Remove {removals.Count} {itemLabel} from merchant inventory",
                    () => _store.RemoveInventoryItems(removals)))
            {
                ClearCheckedInventoryItemsCore();
                RefreshInventory();
            }
        }

        private bool CanRemoveCheckedInventoryItems() => _checkedInventoryItems.Count > 0;

        partial void OnSelectedInventoryCategoryChanged(MerchantInventoryCategoryViewModel? value)
        {
            AddInventoryItemCommand.NotifyCanExecuteChanged();
            RefreshInventory();
            RefreshItemCandidates();
        }

        partial void OnSelectedInventoryItemChanged(MerchantInventoryItemViewModel? value)
        {
            OnPropertyChanged(nameof(HasSelectedInventoryItem));
            OnPropertyChanged(nameof(SelectedItemName));
            OnPropertyChanged(nameof(SelectedItemResRef));
            OnPropertyChanged(nameof(SelectedItemPreview));
            OnPropertyChanged(nameof(SelectedItemSellPrice));
            OnPropertyChanged(nameof(SelectedItemBuyPrice));
            OnPropertyChanged(nameof(SelectedItemStatGroups));
            OnPropertyChanged(nameof(HasSelectedItemStats));
            OnPropertyChanged(nameof(ShowsSelectedItemStatsStatus));
            OnPropertyChanged(nameof(SelectedItemStatsStatus));
            OnPropertyChanged(nameof(SelectedItemInfinite));
        }

        partial void OnInventorySearchTextChanged(string value) => RefreshInventory();

        partial void OnItemSearchTextChanged(string value)
        {
            _itemSearchDebounce?.Cancel();
            _itemSearchDebounce?.Dispose();
            _itemSearchDebounce = null;

            if (string.IsNullOrWhiteSpace(value))
            {
                RefreshItemCandidates();
                return;
            }

            var pending = new CancellationTokenSource();
            _itemSearchDebounce = pending;
            Task.Delay(ItemSearchDebounce, pending.Token).ContinueWith(
                task =>
                {
                    if (!task.IsCanceled)
                        Dispatcher.UIThread.Post(RefreshItemCandidates);
                },
                TaskScheduler.Default);
        }

        partial void OnBuyOnlySelectedChanged(bool value)
        {
            OnPropertyChanged(nameof(BuyAllExceptSelected));
            if (_loading)
                return;

            BuyingRuleError = null;
            if (value && _store.BuyingRuleIds(buyOnlySelected: false).Count == 0)
            {
                BuyingRuleError = "Select at least one base item type before choosing 'buy only'.";
                _loading = true;
                BuyOnlySelected = false;
                _loading = false;
                OnPropertyChanged(nameof(BuyAllExceptSelected));
                return;
            }

            if (_runEdit(
                    value
                        ? "Only buy selected base item types"
                        : "Buy all except selected base item types",
                    () => _store.SwitchBuyingRuleMode(value)))
            {
                RefreshBuyingRuleSelections();
            }
            else
            {
                _loading = true;
                BuyOnlySelected = _store.UsesBuyOnlyRules;
                _loading = false;
                OnPropertyChanged(nameof(BuyAllExceptSelected));
            }
        }

        partial void OnBuyingRuleSearchTextChanged(string value) => FilterBuyingRules();

        partial void OnItemCandidateErrorChanged(string? value) =>
            OnPropertyChanged(nameof(CandidateSummary));

        partial void OnSelectedTabIndexChanged(int value)
        {
            if (value == 3 && !ArePlacedInstancesLoaded)
                _ = RefreshPlacedInstancesAsync();
        }

        [RelayCommand(CanExecute = nameof(CanRefreshPlacedInstances))]
        public async Task RefreshPlacedInstancesAsync()
        {
            if (_instances == null || _disposed)
                return;

            var generation = ++_instanceRefreshGeneration;
            IsLoadingInstances = true;
            InstanceError = null;
            try
            {
                var sourceResRef = ResRef;
                var found = await _instances.FindAsync(sourceResRef).ConfigureAwait(true);
                if (_disposed || generation != _instanceRefreshGeneration)
                    return;

                _loadedPlacementResRef = sourceResRef;
                PlacedInstances.Clear();
                foreach (var placement in found)
                    PlacedInstances.Add(placement);
                ArePlacedInstancesLoaded = true;
                PlacedInstancesNeedRefresh = false;
                NotifyInstanceShapeChanged();
            }
            catch (Exception ex)
            {
                if (generation == _instanceRefreshGeneration)
                    InstanceError = ex.Message;
            }
            finally
            {
                if (generation == _instanceRefreshGeneration)
                    IsLoadingInstances = false;
            }
        }

        private bool CanRefreshPlacedInstances() =>
            _instances != null && !IsInstanceOperationBusy;

        /// <summary>
        /// Drops status derived from the saved merchant without starting another module scan. The
        /// next visit to Placed Instances, or an explicit Refresh, resolves it on demand.
        /// </summary>
        public void InvalidatePlacedInstances()
        {
            _instanceRefreshGeneration++;
            IsLoadingInstances = false;
            ArePlacedInstancesLoaded = false;
            PlacedInstancesNeedRefresh = true;
            InstanceError = null;
            _loadedPlacementResRef = null;
            PlacedInstances.Clear();
            NotifyInstanceShapeChanged();
        }

        [RelayCommand(CanExecute = nameof(CanUpdateOutOfDateInstances))]
        private async Task UpdateOutOfDateInstances()
        {
            if (_instances == null)
                return;

            IsUpdatingInstances = true;
            InstanceError = null;
            try
            {
                // Updating invalidates the module placement index, which tells every open Source tab
                // to discard its derived rows. This command deliberately operates on the displayed
                // snapshot, so retain it across that notification and republish it as current below.
                var displayedPlacements = PlacedInstances.ToList();
                var targetAreas = displayedPlacements
                    .Where(placement => !placement.IsCurrent)
                    .Select(placement => placement.AreaResRef)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                await _instances.UpdateOutOfDateAsync(ResRef, targetAreas).ConfigureAwait(true);

                // The service rebuilds every matching merchant in each target area from the saved
                // blueprint. Those displayed records are therefore current without a second full-
                // module discovery scan. Refresh remains available to discover new placements.
                PlacedInstances.Clear();
                foreach (var placement in displayedPlacements)
                {
                    PlacedInstances.Add(
                        targetAreas.Contains(placement.AreaResRef, StringComparer.OrdinalIgnoreCase)
                            ? placement with
                            {
                                OutOfDateMerchantRecords = 0,
                                OutOfDateItemRecords = 0
                            }
                            : placement);
                }

                ArePlacedInstancesLoaded = true;
                PlacedInstancesNeedRefresh = false;
                NotifyInstanceShapeChanged();
            }
            catch (Exception ex)
            {
                InstanceError = ex.Message;
            }
            finally
            {
                IsUpdatingInstances = false;
                UpdateOutOfDateInstancesCommand.NotifyCanExecuteChanged();
            }
        }

        private bool CanUpdateOutOfDateInstances() =>
            _instances != null && ArePlacedInstancesLoaded && HasOutOfDateInstances &&
            !IsInstanceOperationBusy;

        partial void OnIsLoadingInstancesChanged(bool value) =>
            NotifyInstanceOperationStateChanged();

        partial void OnIsUpdatingInstancesChanged(bool value) =>
            NotifyInstanceOperationStateChanged();

        private void NotifyInstanceOperationStateChanged()
        {
            OnPropertyChanged(nameof(IsInstanceOperationBusy));
            OnPropertyChanged(nameof(InstanceOperationStatus));
            OnPropertyChanged(nameof(InstanceSummary));
            RefreshPlacedInstancesCommand.NotifyCanExecuteChanged();
            UpdateOutOfDateInstancesCommand.NotifyCanExecuteChanged();
        }

        private void BuildRows()
        {
            foreach (var definition in MerchantEditorLayout.Details)
                DetailRows.Add(CreateRow(definition));
            foreach (var definition in MerchantEditorLayout.Pricing)
                PricingRows.Add(CreateRow(definition));
        }

        private BehaviorRowViewModel CreateRow(BehaviorFieldDefinition definition)
        {
            var row = new BehaviorRowViewModel(
                definition,
                _store,
                _runEdit,
                definition.ChoicesKey == null
                    ? definition.Choices
                    : _resolveChoices?.Invoke(definition.ChoicesKey) ?? Array.Empty<BehaviorChoice>(),
                () => OnRowChanged(definition));
            row.Reload();
            return row;
        }

        private void OnRowChanged(BehaviorFieldDefinition definition)
        {
            if (definition.Name is "MarkUp" or "MarkDown")
                RefreshInventory();
            if (definition.Name == "LocName")
                OnPropertyChanged(nameof(HeaderName));
        }

        private void BuildInventoryCategories()
        {
            InventoryCategories.Add(new MerchantInventoryCategoryViewModel(
                (int)MerchantInventoryCategory.Armor, "Armor"));
            InventoryCategories.Add(new MerchantInventoryCategoryViewModel(
                (int)MerchantInventoryCategory.Weapons, "Weapons"));
            InventoryCategories.Add(new MerchantInventoryCategoryViewModel(
                (int)MerchantInventoryCategory.PotionsScrolls, "Potions/Scrolls"));
            InventoryCategories.Add(new MerchantInventoryCategoryViewModel(
                (int)MerchantInventoryCategory.RingsAmulets, "Rings/Amulets"));
            InventoryCategories.Add(new MerchantInventoryCategoryViewModel(
                (int)MerchantInventoryCategory.Miscellaneous, "Miscellaneous"));
            RefreshCategoryCounts();
        }

        private void RefreshInventory()
        {
            var refreshGeneration = ++_inventoryRefreshGeneration;
            InventoryItems.Clear();
            SelectedInventoryItem = null;
            var inventory = InventorySnapshot();
            RefreshCategoryCounts(inventory);

            var category = SelectedInventoryCategory;
            if (category == null)
            {
                NotifyInventoryShapeChanged();
                return;
            }

            var query = InventorySearchText.Trim();
            var markUp = _store.Owner.GetIntOrNull("MarkUp") ?? 100;
            var markDown = _store.Owner.GetIntOrNull("MarkDown") ?? 0;
            foreach (var entry in inventory.Where(entry =>
                         NormalizeStorePanel(entry.Definition.StorePanel) == category.Index))
            {
                var resRef = entry.Definition.ResRef;
                var definition = entry.Definition;
                if (query.Length > 0 &&
                    !definition.Name.Contains(query, StringComparison.OrdinalIgnoreCase) &&
                    !definition.ResRef.Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var capturedPane = entry.PaneIndex;
                var capturedIndex = entry.ItemIndex;
                var inventoryKey = (capturedPane, capturedIndex);
                var inventoryItem = new MerchantInventoryItemViewModel(
                    capturedPane,
                    capturedIndex,
                    definition,
                    entry.Slot.GetIntOrNull("Infinite") != 0,
                    _checkedInventoryItems.Contains(inventoryKey),
                    markUp,
                    markDown,
                    infinite =>
                    {
                        if (!_runEdit(
                                $"Set {definition.Name} inventory quantity",
                                () => _store.SetInventoryInfinite(
                                    capturedPane, capturedIndex, infinite)))
                        {
                            RefreshInventory();
                        }
                    },
                    isChecked =>
                    {
                        if (isChecked)
                            _checkedInventoryItems.Add(inventoryKey);
                        else
                            _checkedInventoryItems.Remove(inventoryKey);
                        if (!_isUpdatingInventoryChecks)
                            NotifyInventorySelectionChanged();
                    });
                InventoryItems.Add(inventoryItem);
                _requestItemPreview?.Invoke(resRef, preview =>
                {
                    if (!_disposed && refreshGeneration == _inventoryRefreshGeneration)
                    {
                        inventoryItem.Preview = preview;
                        if (ReferenceEquals(SelectedInventoryItem, inventoryItem))
                            OnPropertyChanged(nameof(SelectedItemPreview));
                    }
                });
            }

            SelectedInventoryItem = InventoryItems.FirstOrDefault();
            NotifyInventoryShapeChanged();
        }

        private void RefreshCategoryCounts() => RefreshCategoryCounts(InventorySnapshot());

        private void RefreshCategoryCounts(
            IReadOnlyList<(int PaneIndex, int ItemIndex, JsonGffStruct Slot,
                MerchantItemDefinition Definition)> inventory)
        {
            foreach (var category in InventoryCategories)
            {
                category.Count = inventory.Count(entry =>
                    NormalizeStorePanel(entry.Definition.StorePanel) == category.Index);
            }
        }

        private void RefreshItemCandidates()
        {
            var generation = ++_itemCandidateRefreshGeneration;
            _itemCandidateRequest?.Cancel();
            _itemCandidateRequest?.Dispose();
            _itemCandidateRequest = new CancellationTokenSource();
            ItemCandidates.Clear();
            SelectedItemCandidate = null;
            ItemCandidateError = null;
            _itemCandidateOffset = 0;
            _itemCandidatesExhausted = SelectedInventoryCategory == null || _searchItems == null;
            IsLoadingItemCandidates = false;
            NotifyItemCandidateShapeChanged();
            if (!_itemCandidatesExhausted)
                _ = LoadItemCandidatePageAsync(generation, _itemCandidateRequest.Token);
        }

        [RelayCommand(CanExecute = nameof(CanLoadMoreItemCandidates))]
        private async Task LoadMoreItemCandidates()
        {
            if (!CanLoadMoreItemCandidates || _itemCandidateRequest == null)
                return;

            await LoadItemCandidatePageAsync(
                _itemCandidateRefreshGeneration,
                _itemCandidateRequest.Token).ConfigureAwait(true);
        }

        private async Task LoadItemCandidatePageAsync(
            int generation,
            CancellationToken cancellationToken)
        {
            var category = SelectedInventoryCategory;
            var searchItems = _searchItems;
            if (category == null || searchItems == null || _disposed ||
                generation != _itemCandidateRefreshGeneration)
            {
                return;
            }

            IsLoadingItemCandidates = true;
            // A stale failure must not outlive the retry that fixes it - the summary reports the
            // error ahead of the results, so a successful reload would otherwise still read as failed.
            ItemCandidateError = null;
            NotifyItemCandidateShapeChanged();
            try
            {
                var page = await searchItems(
                    ItemSearchText.Trim(),
                    category.Index,
                    _itemCandidateOffset,
                    ItemCandidatePageSize + 1,
                    cancellationToken).ConfigureAwait(true);
                if (_disposed || cancellationToken.IsCancellationRequested ||
                    generation != _itemCandidateRefreshGeneration ||
                    !ReferenceEquals(category, SelectedInventoryCategory))
                {
                    return;
                }

                foreach (var candidate in page.Take(ItemCandidatePageSize))
                {
                    if (NormalizeStorePanel(candidate.StorePanel) != category.Index)
                        continue;

                    ItemCandidates.Add(new MerchantItemCandidateViewModel(candidate));
                }

                // The search index's skip counts rows it served, before the panel re-check above,
                // so the offset must advance by what was consumed from the page rather than by what
                // survived: advancing only by the published rows re-serves the filtered tail as
                // duplicates. The extra row beyond the page size is an unconsumed probe — its
                // presence alone proves the index has more.
                _itemCandidateOffset += Math.Min(page.Count, ItemCandidatePageSize);
                _itemCandidatesExhausted = page.Count <= ItemCandidatePageSize;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                if (generation == _itemCandidateRefreshGeneration)
                    ItemCandidateError = ex.Message;
            }
            finally
            {
                if (generation == _itemCandidateRefreshGeneration)
                {
                    IsLoadingItemCandidates = false;
                    NotifyItemCandidateShapeChanged();
                }
            }
        }

        /// <summary>Requests a candidate picture only after the virtualized list realizes its row.</summary>
        public void EnsureItemCandidatePreview(MerchantItemCandidateViewModel? candidate)
        {
            if (candidate == null || candidate.PreviewRequested || _requestItemPreview == null)
                return;

            candidate.PreviewRequested = true;
            var refreshGeneration = _itemCandidateRefreshGeneration;
            _requestItemPreview(candidate.ResRef, preview =>
            {
                if (!_disposed &&
                    refreshGeneration == _itemCandidateRefreshGeneration &&
                    ItemCandidates.Contains(candidate))
                {
                    candidate.Preview = preview;
                }
            });
        }

        private void NotifyItemCandidateShapeChanged()
        {
            OnPropertyChanged(nameof(HasItemCandidates));
            OnPropertyChanged(nameof(CandidateSummary));
            OnPropertyChanged(nameof(CanLoadMoreItemCandidates));
            LoadMoreItemCandidatesCommand.NotifyCanExecuteChanged();
        }

        private IReadOnlyList<(int PaneIndex, int ItemIndex, JsonGffStruct Slot,
            MerchantItemDefinition Definition)> InventorySnapshot()
        {
            var inventory = new List<(int, int, JsonGffStruct, MerchantItemDefinition)>();
            var panes = _store.Owner.GetListOrEmpty("StoreList");
            for (var paneIndex = 0; paneIndex < panes.Count; paneIndex++)
            {
                var slots = panes[paneIndex].GetListOrEmpty("ItemList");
                for (var itemIndex = 0; itemIndex < slots.Count; itemIndex++)
                {
                    var slot = slots[itemIndex];
                    var resRef = slot.GetStringOrNull("InventoryRes") ?? string.Empty;
                    var definition = _loadItem?.Invoke(resRef)
                                     ?? new MerchantItemDefinition(
                                         resRef,
                                         resRef,
                                         0,
                                         NormalizeStorePanel(paneIndex));
                    inventory.Add((paneIndex, itemIndex, slot, definition));
                }
            }

            return inventory;
        }

        private int? ResolveStorePanel(string resRef) =>
            _loadItem?.Invoke(resRef) is { } item
                ? NormalizeStorePanel(item.StorePanel)
                : null;

        private static int NormalizeStorePanel(int storePanel) =>
            storePanel is >= 0 and < MerchantValueStore.InventoryPaneCount
                ? storePanel
                : (int)MerchantInventoryCategory.Miscellaneous;

        private static string RecordCount(int count, string kind) =>
            $"{count} {kind} record{(count == 1 ? string.Empty : "s")}";

        private void BuildBuyingRules()
        {
            _allBuyingRules.Clear();
            foreach (var choice in _baseItems.OrderBy(
                         choice => choice.Display,
                         StringComparer.OrdinalIgnoreCase))
            {
                var baseItem = checked((int)choice.Value);
                _allBuyingRules.Add(new MerchantBuyingRuleViewModel(
                    baseItem,
                    choice.Display,
                    false,
                    selected =>
                    {
                        BuyingRuleError = null;
                        if (BuyOnlySelected &&
                            !selected &&
                            _store.BuyingRuleIds(buyOnlySelected: true).Count == 1 &&
                            _store.BuyingRuleIds(buyOnlySelected: true).Contains(baseItem))
                        {
                            BuyingRuleError =
                                "A 'buy only' merchant must keep at least one base item type selected.";
                            RefreshBuyingRuleSelections();
                            return;
                        }

                        if (!_runEdit(
                                $"Change buying rule for {choice.Display}",
                                () => _store.SetBuyingRule(BuyOnlySelected, baseItem, selected)))
                        {
                            RefreshBuyingRuleSelections();
                        }
                    }));
            }

            RefreshBuyingRuleSelections();
        }

        private void RefreshBuyingRuleSelections()
        {
            var selected = _store.BuyingRuleIds(BuyOnlySelected);
            foreach (var rule in _allBuyingRules)
                rule.SetSelectedWithoutWriting(selected.Contains(rule.BaseItem));
            FilterBuyingRules();
        }

        private void FilterBuyingRules()
        {
            var query = BuyingRuleSearchText.Trim();
            BuyingRules.Clear();
            foreach (var rule in _allBuyingRules)
            {
                if (query.Length > 0 &&
                    !rule.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                BuyingRules.Add(rule);
            }

            OnPropertyChanged(nameof(BuyingRuleSummary));
        }

        private void NotifyInventoryShapeChanged()
        {
            OnPropertyChanged(nameof(HasInventoryItems));
            OnPropertyChanged(nameof(InventorySummary));
            OnPropertyChanged(nameof(ShownInventorySelectionState));
            OnPropertyChanged(nameof(CanToggleShownInventoryItems));
        }

        private void ClearCheckedInventoryItemsCore()
        {
            if (_checkedInventoryItems.Count == 0 && InventoryItems.All(item => !item.IsChecked))
                return;

            _checkedInventoryItems.Clear();
            foreach (var item in InventoryItems)
                item.SetCheckedWithoutWriting(false);
            NotifyInventorySelectionChanged();
        }

        private void NotifyInventorySelectionChanged()
        {
            OnPropertyChanged(nameof(CheckedInventoryItemCount));
            OnPropertyChanged(nameof(CheckedInventorySummary));
            OnPropertyChanged(nameof(RemoveCheckedInventoryLabel));
            OnPropertyChanged(nameof(ShownInventorySelectionState));
            RemoveCheckedInventoryItemsCommand.NotifyCanExecuteChanged();
        }

        private void NotifyInstanceShapeChanged()
        {
            OnPropertyChanged(nameof(HasPlacedInstances));
            OnPropertyChanged(nameof(HasOutOfDateInstances));
            OnPropertyChanged(nameof(OutOfDateMerchantRecords));
            OnPropertyChanged(nameof(OutOfDateItemRecords));
            OnPropertyChanged(nameof(InstanceSummary));
            UpdateOutOfDateInstancesCommand.NotifyCanExecuteChanged();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _instanceRefreshGeneration++;
            _itemCandidateRefreshGeneration++;
            _itemCandidateRequest?.Cancel();
            _itemCandidateRequest?.Dispose();
            _itemCandidateRequest = null;
            _itemSearchDebounce?.Cancel();
            _itemSearchDebounce?.Dispose();
            _itemSearchDebounce = null;
            foreach (var row in DetailRows.Concat(PricingRows))
                row.Dispose();
        }
    }
}
