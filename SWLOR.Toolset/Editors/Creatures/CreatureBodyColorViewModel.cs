using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
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
        private static readonly Color NeutralColor = Color.FromRgb(128, 128, 128);

        private readonly IReadOnlyList<TintMapColorRowViewModel> _tintRows;
        private readonly Func<Color, bool> _writeCustom;
        private readonly Func<bool> _resetCustom;
        private bool _loading;

        public ItemDyeCellViewModel Palette { get; }
        public string Label => Palette.Label;
        public bool AllowsNumericFallback => Palette.AllowsNumericFallback;
        public bool HasNumericFallback => Palette.HasNumericFallback;
        public bool HasCustomTint => _tintRows.Count > 0;

        [ObservableProperty]
        private Color _customColor = NeutralColor;

        public IBrush CustomBrush => new SolidColorBrush(CustomColor);

        [ObservableProperty]
        private bool _hasOverride;

        [ObservableProperty]
        private bool _hasMixedOverrides;

        public string TintStatus => !HasCustomTint
            ? "Custom tint unavailable"
            : HasMixedOverrides
                ? "Mixed custom tints"
                : HasOverride
                    ? $"Custom #{CustomColor.R:X2}{CustomColor.G:X2}{CustomColor.B:X2}"
                    : "Using preset";

        public CreatureBodyColorViewModel(
            ItemDyeCellViewModel palette,
            IReadOnlyList<TintMapColorRowViewModel> tintRows,
            Func<Color, bool> writeCustom,
            Func<bool> resetCustom)
        {
            Palette = palette ?? throw new ArgumentNullException(nameof(palette));
            _tintRows = tintRows ?? throw new ArgumentNullException(nameof(tintRows));
            _writeCustom = writeCustom ?? throw new ArgumentNullException(nameof(writeCustom));
            _resetCustom = resetCustom ?? throw new ArgumentNullException(nameof(resetCustom));
            Reload();
        }

        partial void OnCustomColorChanged(Color value)
        {
            OnPropertyChanged(nameof(CustomBrush));
            if (_loading || !HasCustomTint)
                return;

            if (!_writeCustom(value))
            {
                Reload();
                return;
            }

            Reload();
        }

        [RelayCommand]
        private void ResetCustom()
        {
            if (!HasOverride || !_resetCustom())
                return;

            Reload();
        }

        public void Reload()
        {
            Palette.Reload();
            foreach (var row in _tintRows)
                row.Reload();

            _loading = true;
            try
            {
                var customRows = _tintRows.Where(row => row.IsCustom).ToList();
                HasOverride = _tintRows.Any(row => row.HasOverride);
                HasMixedOverrides = customRows.Select(row => row.Color).Distinct().Skip(1).Any() ||
                                    customRows.Count != 0 && customRows.Count != _tintRows.Count;

                if (customRows.Count > 0)
                {
                    CustomColor = customRows[0].Color;
                }
                else if (Palette.SelectedBrush is SolidColorBrush selected)
                {
                    CustomColor = selected.Color;
                }
                else
                {
                    CustomColor = NeutralColor;
                }
            }
            finally
            {
                _loading = false;
            }

            OnPropertyChanged(nameof(TintStatus));
        }
    }
}
