using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Editors.Items;

namespace SWLOR.Toolset.Editors.Merchants
{
    /// <summary>Resolved catalog information for one item blueprint.</summary>
    public sealed record MerchantItemDefinition(
        string ResRef,
        string Name,
        long BaseCost,
        int StorePanel = (int)global::SWLOR.Toolset.Domain.Editors.Merchants.MerchantInventoryCategory.Miscellaneous,
        IReadOnlyList<ItemStatSummaryGroup>? StatGroups = null,
        bool HasKnownStorePanel = false)
    {
        public IReadOnlyList<ItemStatSummaryGroup> Stats =>
            StatGroups ?? Array.Empty<ItemStatSummaryGroup>();
        public ItemStatCompactSummary CompactStats { get; } =
            ItemStatSummary.CompactParts(StatGroups);
        public bool HasStats => Stats.Count > 0;
        public string PrimaryStatSummary => CompactStats.Primary;
        public string AdditionalStatSummary => CompactStats.Overflow;
        public bool HasAdditionalStats => CompactStats.HasOverflow;
        public string StatSummary => CompactStats.Text;
        public string Display => string.IsNullOrWhiteSpace(Name) ||
                                 string.Equals(Name, ResRef, StringComparison.OrdinalIgnoreCase)
            ? ResRef
            : $"{Name}  ·  {ResRef}";
    }

    /// <summary>One progressively published item in the merchant's searchable add-item list.</summary>
    public sealed partial class MerchantItemCandidateViewModel : ObservableObject
    {
        public MerchantItemDefinition Definition { get; }
        public string ResRef => Definition.ResRef;
        public string DisplayName => string.IsNullOrWhiteSpace(Definition.Name)
            ? Definition.ResRef
            : Definition.Name;
        public bool HasStats => Definition.HasStats;
        public string PrimaryStatSummary => Definition.PrimaryStatSummary;
        public string AdditionalStatSummary => Definition.AdditionalStatSummary;
        public bool HasAdditionalStats => Definition.HasAdditionalStats;
        public string StatSummary => Definition.StatSummary;
        public string Glyph => string.IsNullOrWhiteSpace(DisplayName)
            ? "?"
            : DisplayName.Trim()[..1].ToUpperInvariant();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPreview))]
        private Bitmap? _preview;

        public bool HasPreview => Preview != null;
        public bool PreviewRequested { get; set; }

        public MerchantItemCandidateViewModel(MerchantItemDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public override string ToString() => Definition.Display;
    }

    public sealed partial class MerchantInventoryCategoryViewModel : ObservableObject
    {
        public int Index { get; }
        public string Name { get; }

        [ObservableProperty]
        private int _count;

        public string Display => $"{Name}  ({Count})";

        public MerchantInventoryCategoryViewModel(int index, string name)
        {
            Index = index;
            Name = name;
        }

        partial void OnCountChanged(int value) => OnPropertyChanged(nameof(Display));
    }

    public sealed partial class MerchantInventoryItemViewModel : ObservableObject
    {
        private readonly Action<bool> _setInfinite;
        private readonly Action<bool> _setChecked;

        public int PaneIndex { get; }
        public int ItemIndex { get; }
        public string ResRef { get; }
        public string Name { get; }
        public long BaseCost { get; }
        public long StoreSellsFor { get; }
        public long StoreBuysFor { get; }
        public IReadOnlyList<ItemStatSummaryGroup> StatGroups { get; }
        public ItemStatCompactSummary CompactStats { get; }
        public bool HasStats => StatGroups.Count > 0;
        public string DisplayName => string.IsNullOrWhiteSpace(Name) ? ResRef : Name;
        public string SellPrice => $"{StoreSellsFor:N0} cr";
        public string BuyPrice => $"{StoreBuysFor:N0} cr";
        public string PrimaryStatSummary => CompactStats.Primary;
        public string AdditionalStatSummary => CompactStats.Overflow;
        public bool HasAdditionalStats => CompactStats.HasOverflow;
        public string StatSummary => CompactStats.Text;

        /// <summary>The same rendered UTI inventory artwork used by the item editor and palette.</summary>
        [ObservableProperty]
        private Bitmap? _preview;

        [ObservableProperty]
        private bool _isInfinite;

        [ObservableProperty]
        private bool _isChecked;

        private bool _loading;

        public MerchantInventoryItemViewModel(
            int paneIndex,
            int itemIndex,
            MerchantItemDefinition item,
            bool isInfinite,
            bool isChecked,
            int markUp,
            int markDown,
            Action<bool> setInfinite,
            Action<bool> setChecked)
        {
            PaneIndex = paneIndex;
            ItemIndex = itemIndex;
            ResRef = item.ResRef;
            Name = item.Name;
            BaseCost = item.BaseCost;
            StatGroups = item.Stats;
            CompactStats = item.CompactStats;
            StoreSellsFor = Math.Max(0, item.BaseCost * markUp / 100);
            StoreBuysFor = Math.Max(0, item.BaseCost * markDown / 100);
            _setInfinite = setInfinite ?? throw new ArgumentNullException(nameof(setInfinite));
            _setChecked = setChecked ?? throw new ArgumentNullException(nameof(setChecked));

            _loading = true;
            IsInfinite = isInfinite;
            IsChecked = isChecked;
            _loading = false;
        }

        public void SetCheckedWithoutWriting(bool isChecked)
        {
            _loading = true;
            IsChecked = isChecked;
            _loading = false;
        }

        partial void OnIsInfiniteChanged(bool value)
        {
            if (!_loading)
                _setInfinite(value);
        }

        partial void OnIsCheckedChanged(bool value)
        {
            if (!_loading)
                _setChecked(value);
        }
    }

    public sealed partial class MerchantBuyingRuleViewModel : ObservableObject
    {
        private readonly Action<bool> _setSelected;

        public int BaseItem { get; }
        public string Name { get; }

        [ObservableProperty]
        private bool _isSelected;

        private bool _loading;

        public MerchantBuyingRuleViewModel(
            int baseItem,
            string name,
            bool isSelected,
            Action<bool> setSelected)
        {
            BaseItem = baseItem;
            Name = name;
            _setSelected = setSelected ?? throw new ArgumentNullException(nameof(setSelected));
            SetSelectedWithoutWriting(isSelected);
        }

        public void SetSelectedWithoutWriting(bool selected)
        {
            _loading = true;
            IsSelected = selected;
            _loading = false;
        }

        partial void OnIsSelectedChanged(bool value)
        {
            if (!_loading)
                _setSelected(value);
        }
    }
}
