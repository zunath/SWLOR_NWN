using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Merchants;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Editors.Behaviors;

namespace SWLOR.Toolset.Editors.Merchants
{
    /// <summary>The SWLOR-focused merchant editor over one UTM blueprint.</summary>
    public sealed partial class MerchantEditorViewModel : ObservableObject, IDisposable
    {
        public const string OnOpenStoreScript = "on_open_store";
        public const string OnStoreClosedScript = "on_close_store";

        private readonly MerchantValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Func<string, IReadOnlyList<BehaviorChoice>>? _resolveChoices;
        private readonly Func<string, MerchantItemDefinition?>? _loadItem;
        private readonly Func<string, int, IReadOnlyList<MerchantItemDefinition>>? _searchItems;
        private readonly Action<string, Action<Bitmap>>? _requestItemPreview;
        private readonly IReadOnlyList<BehaviorChoice> _baseItems;
        private readonly MerchantInstanceService? _instances;
        private readonly List<MerchantBuyingRuleViewModel> _allBuyingRules = new();
        private int _instanceRefreshGeneration;
        private int _inventoryRefreshGeneration;
        private bool _loading;
        private bool _disposed;

        public ObservableCollection<BehaviorRowViewModel> DetailRows { get; } = new();
        public ObservableCollection<BehaviorRowViewModel> PricingRows { get; } = new();
        public ObservableCollection<MerchantInventoryCategoryViewModel> InventoryCategories { get; } = new();
        public ObservableCollection<MerchantInventoryItemViewModel> InventoryItems { get; } = new();
        public ObservableCollection<MerchantItemDefinition> ItemCandidates { get; } = new();
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
        public bool HasItemCandidates => ItemCandidates.Count > 0;
        public bool HasPlacedInstances => PlacedInstances.Count > 0;
        public bool HasOutOfDateInstances => PlacedInstances.Any(instance => !instance.IsCurrent);
        public int OutOfDateMerchantRecords =>
            PlacedInstances.Sum(instance => instance.OutOfDateMerchantRecords);
        public int OutOfDateItemRecords =>
            PlacedInstances.Sum(instance => instance.OutOfDateItemRecords);
        public string InventorySummary =>
            $"{InventoryItems.Count} item{(InventoryItems.Count == 1 ? string.Empty : "s")} shown";
        public string CandidateSummary =>
            $"{ItemCandidates.Count} item{(ItemCandidates.Count == 1 ? string.Empty : "s")} found";
        public string BuyingRuleSummary =>
            $"{BuyingRules.Count} of {_allBuyingRules.Count} base item types";
        public string InstanceSummary =>
            PlacedInstances.Count == 0
                ? "0 merchant records and 0 item records out of date. This merchant has no placed instances."
                : HasOutOfDateInstances
                    ? $"{RecordCount(OutOfDateMerchantRecords, "merchant")} and " +
                      $"{RecordCount(OutOfDateItemRecords, "item")} out of date across " +
                      $"{PlacedInstances.Count(instance => !instance.IsCurrent)} of {PlacedInstances.Count} placed " +
                      $"instance{(PlacedInstances.Count == 1 ? string.Empty : "s")}."
                    : $"0 merchant records and 0 item records out of date. All {PlacedInstances.Count} placed " +
                      $"instance{(PlacedInstances.Count == 1 ? string.Empty : "s")} up to date.";
        public string SelectedItemName => SelectedInventoryItem?.DisplayName ?? "No item selected";
        public string SelectedItemResRef => SelectedInventoryItem?.ResRef ?? string.Empty;
        public string SelectedItemSellPrice => SelectedInventoryItem?.SellPrice ?? "—";
        public string SelectedItemBuyPrice => SelectedInventoryItem?.BuyPrice ?? "—";
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
        private MerchantItemDefinition? _selectedItemCandidate;

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

        public MerchantEditorViewModel(
            JsonGffStruct merchant,
            string headerOwner,
            Func<string, Action, bool> runEdit,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveChoices = null,
            IReadOnlyList<BehaviorChoice>? baseItems = null,
            Func<string, MerchantItemDefinition?>? loadItem = null,
            Func<string, int, IReadOnlyList<MerchantItemDefinition>>? searchItems = null,
            MerchantInstanceService? instances = null,
            Action<string, Action<Bitmap>>? requestItemPreview = null)
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
            _ = RefreshPlacedInstancesAsync();
        }

        public void ReloadFromDocument()
        {
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

            var candidate = _loadItem?.Invoke(SelectedItemCandidate.ResRef) ?? SelectedItemCandidate;
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

        [RelayCommand]
        private void RemoveInventoryItem(MerchantInventoryItemViewModel? item)
        {
            if (item == null)
                return;

            if (_runEdit(
                    $"Remove {item.DisplayName} from merchant inventory",
                    () => _store.RemoveInventoryItem(item.PaneIndex, item.ItemIndex)))
            {
                RefreshInventory();
            }
        }

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
            OnPropertyChanged(nameof(SelectedItemSellPrice));
            OnPropertyChanged(nameof(SelectedItemBuyPrice));
            OnPropertyChanged(nameof(SelectedItemInfinite));
        }

        partial void OnInventorySearchTextChanged(string value) => RefreshInventory();

        partial void OnItemSearchTextChanged(string value) => RefreshItemCandidates();

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

        [RelayCommand]
        public async Task RefreshPlacedInstancesAsync()
        {
            if (_instances == null || _disposed)
                return;

            var generation = ++_instanceRefreshGeneration;
            IsLoadingInstances = true;
            InstanceError = null;
            try
            {
                var found = await _instances.FindAsync(ResRef).ConfigureAwait(true);
                if (_disposed || generation != _instanceRefreshGeneration)
                    return;

                PlacedInstances.Clear();
                foreach (var placement in found)
                    PlacedInstances.Add(placement);
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

        [RelayCommand(CanExecute = nameof(CanUpdateOutOfDateInstances))]
        private async Task UpdateOutOfDateInstances()
        {
            if (_instances == null)
                return;

            IsUpdatingInstances = true;
            InstanceError = null;
            try
            {
                await _instances.UpdateOutOfDateAsync(ResRef).ConfigureAwait(true);
                await RefreshPlacedInstancesAsync().ConfigureAwait(true);
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
            _instances != null && HasOutOfDateInstances && !IsUpdatingInstances;

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
                var inventoryItem = new MerchantInventoryItemViewModel(
                    capturedPane,
                    capturedIndex,
                    definition,
                    entry.Slot.GetIntOrNull("Infinite") != 0,
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
                    });
                InventoryItems.Add(inventoryItem);
                _requestItemPreview?.Invoke(resRef, preview =>
                {
                    if (!_disposed && refreshGeneration == _inventoryRefreshGeneration)
                        inventoryItem.Preview = preview;
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
            ItemCandidates.Clear();
            SelectedItemCandidate = null;
            var category = SelectedInventoryCategory;
            if (category == null)
            {
                OnPropertyChanged(nameof(HasItemCandidates));
                OnPropertyChanged(nameof(CandidateSummary));
                return;
            }

            foreach (var candidate in _searchItems?.Invoke(ItemSearchText.Trim(), category.Index)
                         ?? Array.Empty<MerchantItemDefinition>())
            {
                if (NormalizeStorePanel(candidate.StorePanel) == category.Index)
                    ItemCandidates.Add(candidate);
            }

            SelectedItemCandidate = ItemCandidates.FirstOrDefault();
            OnPropertyChanged(nameof(HasItemCandidates));
            OnPropertyChanged(nameof(CandidateSummary));
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
                    !rule.Name.Contains(query, StringComparison.OrdinalIgnoreCase) &&
                    !rule.IdDisplay.Contains(query, StringComparison.OrdinalIgnoreCase))
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
            foreach (var row in DetailRows.Concat(PricingRows))
                row.Dispose();
        }
    }
}
