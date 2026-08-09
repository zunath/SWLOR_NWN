using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Editors.Items;
using SWLOR.Toolset.Editors.TintMaps;

namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>
    /// One semantic creature color channel. The stock palette and unrestricted RGB tint are two
    /// ways to edit the same channel, so they deliberately share one row in the Body editor.
    /// </summary>
    public sealed partial class CreatureBodyColorViewModel : ObservableObject
    {
        private readonly IReadOnlyList<TintMapColorRowViewModel> _tintRows;

        public ItemDyeCellViewModel Palette { get; }
        public string Label => Palette.Label;
        public bool AllowsNumericFallback => Palette.AllowsNumericFallback;
        public bool HasNumericFallback => Palette.HasNumericFallback;
        public bool HasCustomTint => _tintRows.Count > 0;
        public Avalonia.Media.Color CustomColor
        {
            get => Palette.CustomColor;
            set => Palette.CustomColor = value;
        }
        public bool HasOverride => _tintRows.Any(row => row.HasOverride);

        public CreatureBodyColorViewModel(
            ItemDyeCellViewModel palette,
            IReadOnlyList<TintMapColorRowViewModel> tintRows)
        {
            Palette = palette ?? throw new ArgumentNullException(nameof(palette));
            _tintRows = tintRows ?? throw new ArgumentNullException(nameof(tintRows));
            Reload();
        }

        public void Reload()
        {
            foreach (var row in _tintRows)
                row.Reload();
            Palette.Reload();
            OnPropertyChanged(nameof(CustomColor));
            OnPropertyChanged(nameof(HasOverride));
        }
    }
}
