using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>
    /// One dye channel on the Appearance tab's Colors panel, picked as a color rather than typed as
    /// an index: the current color shows as a chip, and the picker offers the material's whole
    /// palette drawn from its real artwork.
    /// </summary>
    /// <remarks>
    /// A dye value is an index into a palette texture, not a color anyone can name - the number is
    /// meaningless to a builder, which is why nothing here asks them to know it (it survives only
    /// in each swatch's tooltip, so a specific dye can still be found or reported by number).
    /// When the palette artwork cannot be resolved - a session with no base-game data - there are
    /// no colors to show, and <see cref="HasPalette"/> is the template's cue to use the caller's
    /// chosen unavailable-palette behavior. Legacy item editing permits index entry; creature
    /// colors deliberately do not because a raw palette number is not a meaningful color control.
    /// </remarks>
    public sealed partial class ItemDyeCellViewModel : ObservableObject
    {
        private readonly Func<int?> _read;
        private readonly Func<int, bool> _write;
        private readonly Func<Color?>? _readCustom;
        private readonly Func<Color, bool>? _writeCustom;
        private readonly Func<bool>? _hasExternalOverride;

        public string Label { get; }

        public ObservableCollection<ItemDyeSwatchViewModel> Swatches { get; } = new();

        /// <summary>False when the palette artwork is unavailable, leaving only index entry.</summary>
        public bool HasPalette => Swatches.Count > 0;

        /// <summary>Whether a missing palette may fall back to raw index entry.</summary>
        public bool AllowsNumericFallback { get; }

        /// <summary>True only for legacy callers that explicitly allow raw index entry.</summary>
        public bool HasNumericFallback => !HasPalette && AllowsNumericFallback;

        /// <summary>True when this row must remain a color picker but its palette could not load.</summary>
        public bool IsPaletteUnavailable => !HasPalette && !AllowsNumericFallback && !HasCustomOption;

        /// <summary>True when this palette selector includes the always-visible custom RGB editor.</summary>
        public bool HasCustomOption => _readCustom != null && _writeCustom != null;

        /// <summary>The combined selector is useful when either presets or Custom are available.</summary>
        public bool HasColorPicker => HasPalette || HasCustomOption;

        /// <summary>
        /// Without palette artwork there are no swatches to clear a custom tint. The stored NWN
        /// palette index still exists, so expose an explicit way to restore it while Custom is active.
        /// </summary>
        public bool CanRestorePreset => HasCustomOption && !HasPalette && IsUsingCustomColor;

        public int Minimum => 0;

        /// <summary>The palette's own last index, or NWN's dye range when there is no palette to measure.</summary>
        public int Maximum => Swatches.Count > 0 ? Swatches.Count - 1 : 175;

        [ObservableProperty]
        private decimal? _number;

        /// <summary>The chip the row shows: the currently stored color, or null when unresolvable.</summary>
        [ObservableProperty]
        private IBrush? _selectedBrush;

        [ObservableProperty]
        private Color _customColor = Color.FromRgb(128, 128, 128);

        public IBrush CustomBrush => new SolidColorBrush(CustomColor);
        public IBrush? DisplayBrush => IsUsingCustomColor ? CustomBrush : SelectedBrush;

        [ObservableProperty]
        private bool _isUsingCustomColor;

        [ObservableProperty]
        private bool _isPickerOpen;

        private bool _loading;

        public ItemDyeCellViewModel(
            string label,
            Func<int?> read,
            Func<int, bool> write,
            IReadOnlyList<(byte R, byte G, byte B)> paletteColors,
            bool allowsNumericFallback = true,
            Func<Color?>? readCustom = null,
            Func<Color, bool>? writeCustom = null,
            Func<bool>? hasExternalOverride = null)
        {
            Label = label ?? throw new ArgumentNullException(nameof(label));
            _read = read ?? throw new ArgumentNullException(nameof(read));
            _write = write ?? throw new ArgumentNullException(nameof(write));
            _readCustom = readCustom;
            _writeCustom = writeCustom;
            _hasExternalOverride = hasExternalOverride;
            AllowsNumericFallback = allowsNumericFallback;

            for (var index = 0; index < paletteColors.Count; index++)
                Swatches.Add(new ItemDyeSwatchViewModel(index, paletteColors[index]));

            Reload();
        }

        /// <summary>Re-reads the stored index after an undo, redo, or external reload.</summary>
        public void Reload()
        {
            _loading = true;
            try
            {
                Number = _read();
            }
            finally
            {
                _loading = false;
            }

            SyncSelection();

            _loading = true;
            try
            {
                var custom = _readCustom?.Invoke();
                IsUsingCustomColor = custom.HasValue;
                if (custom.HasValue)
                    CustomColor = custom.Value;
                else if (SelectedBrush is SolidColorBrush selected)
                    CustomColor = selected.Color;
            }
            finally
            {
                _loading = false;
            }

            OnPropertyChanged(nameof(CustomBrush));
            OnPropertyChanged(nameof(DisplayBrush));
        }

        [RelayCommand]
        private void Pick(ItemDyeSwatchViewModel? swatch)
        {
            if (swatch == null ||
                swatch.Index == (int?)Number &&
                !IsUsingCustomColor &&
                !(_hasExternalOverride?.Invoke() ?? false))
                return;

            if (_write(swatch.Index))
                Reload();
        }

        [RelayCommand]
        private void RestorePreset()
        {
            if (!CanRestorePreset)
                return;

            var index = Number.HasValue
                ? (int)Math.Clamp(Number.Value, Minimum, Maximum)
                : Minimum;
            if (_write(index))
                Reload();
        }

        partial void OnNumberChanged(decimal? value)
        {
            if (_loading)
                return;

            // The numeric fallback path (no palette artwork). A dye index is a whole number, so
            // "12.9" is refused outright rather than silently stored as 12 - the same rule the
            // shared integer rows follow. Reload puts the box back to what the document holds,
            // whether the write was refused here or by the store.
            if (value.HasValue && decimal.Truncate(value.Value) != value.Value)
            {
                Reload();
                return;
            }

            var index = value.HasValue
                ? (int)Math.Clamp(value.Value, Minimum, Maximum)
                : Minimum;
            _write(index);
            Reload();
        }

        partial void OnCustomColorChanged(Color value)
        {
            OnPropertyChanged(nameof(CustomBrush));
            OnPropertyChanged(nameof(DisplayBrush));
            if (_loading || _writeCustom == null)
                return;

            if (!_writeCustom(value))
            {
                Reload();
                return;
            }

            Reload();
            IsPickerOpen = true;
        }

        partial void OnIsUsingCustomColorChanged(bool value)
        {
            OnPropertyChanged(nameof(DisplayBrush));
            OnPropertyChanged(nameof(CanRestorePreset));
        }

        private void SyncSelection()
        {
            var current = (int?)Number;
            SelectedBrush = null;

            foreach (var swatch in Swatches)
            {
                swatch.IsSelected = swatch.Index == current;
                if (swatch.IsSelected)
                    SelectedBrush = swatch.Brush;
            }
        }
    }
}
