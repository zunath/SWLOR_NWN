using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SWLOR.Toolset.Editors.Merchants
{
    /// <summary>Resolved catalog information for one item blueprint.</summary>
    public sealed record MerchantItemDefinition(
        string ResRef,
        string Name,
        long BaseCost,
        int StorePanel = (int)global::SWLOR.Toolset.Domain.Editors.Merchants.MerchantInventoryCategory.Miscellaneous)
    {
        public string Display => string.IsNullOrWhiteSpace(Name) ||
                                 string.Equals(Name, ResRef, StringComparison.OrdinalIgnoreCase)
            ? ResRef
            : $"{Name}  ·  {ResRef}";
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

        public int PaneIndex { get; }
        public int ItemIndex { get; }
        public string ResRef { get; }
        public string Name { get; }
        public long BaseCost { get; }
        public long StoreSellsFor { get; }
        public long StoreBuysFor { get; }
        public string DisplayName => string.IsNullOrWhiteSpace(Name) ? ResRef : Name;
        public string SellPrice => $"{StoreSellsFor:N0} cr";
        public string BuyPrice => $"{StoreBuysFor:N0} cr";

        /// <summary>The same rendered UTI inventory artwork used by the item editor and palette.</summary>
        [ObservableProperty]
        private Bitmap? _preview;

        [ObservableProperty]
        private bool _isInfinite;

        private bool _loading;

        public MerchantInventoryItemViewModel(
            int paneIndex,
            int itemIndex,
            MerchantItemDefinition item,
            bool isInfinite,
            int markUp,
            int markDown,
            Action<bool> setInfinite)
        {
            PaneIndex = paneIndex;
            ItemIndex = itemIndex;
            ResRef = item.ResRef;
            Name = item.Name;
            BaseCost = item.BaseCost;
            StoreSellsFor = Math.Max(0, item.BaseCost * markUp / 100);
            StoreBuysFor = Math.Max(0, item.BaseCost * markDown / 100);
            _setInfinite = setInfinite ?? throw new ArgumentNullException(nameof(setInfinite));

            _loading = true;
            IsInfinite = isInfinite;
            _loading = false;
        }

        partial void OnIsInfiniteChanged(bool value)
        {
            if (!_loading)
                _setInfinite(value);
        }
    }

    public sealed partial class MerchantBuyingRuleViewModel : ObservableObject
    {
        private readonly Action<bool> _setSelected;

        public int BaseItem { get; }
        public string Name { get; }
        public string IdDisplay => BaseItem.ToString(System.Globalization.CultureInfo.InvariantCulture);

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
