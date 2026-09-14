using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>
    /// One plain integer field on the Appearance tab's armor grid: a body-part number or a dye
    /// channel. Shaped after <see cref="ItemStatCellViewModel"/> - a loading guard around
    /// <see cref="Reload"/>, and a capped NumericUpDown makes garbage input impossible in the UI -
    /// but a value that reaches this class out of range (or absent, for a field that must always
    /// carry one on a real blueprint) is still refused and put back to what is stored, the same
    /// defense-in-depth the control's own Minimum/Maximum give it. This cell writes a bare GFF field
    /// instead of a PropertiesList entry, so the caller supplies its own read/write closures instead
    /// of a PropertyId/SubtypeId pair. That is what lets the same cell serve a lone field
    /// (ArmorPart_Torso) and a mirrored pair's left side (which also has to write its sibling and both
    /// their "x" twins) without this class knowing which case it is in.
    /// </summary>
    public sealed partial class ItemFieldCellViewModel : ObservableObject
    {
        private readonly Func<int?> _read;
        private readonly Func<int, bool> _write;
        private readonly Func<int, (byte R, byte G, byte B)?>? _sampleColor;
        private bool _loading;

        public string Label { get; }

        public int Minimum { get; }

        public int Maximum { get; }

        [ObservableProperty]
        private decimal? _number;

        /// <summary>
        /// True for a mirrored pair's right cell while mirroring is on: the value still reflects the
        /// left side (see <see cref="Reload"/> callers), but the control must refuse direct edits.
        /// </summary>
        [ObservableProperty]
        private bool _isReadOnly;

        /// <summary>What the template's NumericUpDown binds its own IsEnabled to.</summary>
        public bool IsEnabled => !IsReadOnly;

        /// <summary>
        /// The real dye color at the stored index, for a Colors-panel cell that was built with a
        /// <c>sampleColor</c> function; null for every other cell (no swatch renders) and for a dye
        /// cell whose palette artwork can't be resolved (a neutral chip renders instead).
        /// </summary>
        [ObservableProperty]
        private IBrush? _swatchBrush;

        /// <summary>
        /// The values that actually exist for this field, ascending; empty when the caller has no
        /// such list and the cell stays a plain number box.
        /// </summary>
        /// <remarks>
        /// A body part's numbering is sparse, so a spinner walks through numbers with no model
        /// behind them - and a part with no model just vanishes off the body, which reads as the
        /// armor not covering it rather than as a bad value.
        /// </remarks>
        private IReadOnlyList<int> _options = Array.Empty<int>();

        public IReadOnlyList<int> Options
        {
            get => _options;
            private set => SetProperty(ref _options, value);
        }

        /// <summary>Whether to render this cell as a list of real values rather than a number box.</summary>
        public bool HasOptions => Options.Count > 0;

        /// <summary>
        /// <see cref="Number"/> as a list selection. Null when the stored value is not one of the
        /// real variants, which leaves the list blank rather than silently moving the item onto a
        /// neighbouring part.
        /// </summary>
        public int? SelectedOption
        {
            get
            {
                if (Number is not { } number)
                    return null;
                var value = (int)number;
                return Options.Contains(value) ? value : null;
            }
            set
            {
                if (value is { } chosen)
                    Number = chosen;
            }
        }

        public ItemFieldCellViewModel(
            string label,
            Func<int?> read,
            Func<int, bool> write,
            int min,
            int max,
            Func<int, (byte R, byte G, byte B)?>? sampleColor = null,
            IReadOnlyList<int>? options = null)
        {
            Label = label ?? throw new ArgumentNullException(nameof(label));
            _read = read ?? throw new ArgumentNullException(nameof(read));
            _write = write ?? throw new ArgumentNullException(nameof(write));
            _sampleColor = sampleColor;
            Minimum = min;
            Maximum = max;
            Options = options ?? Array.Empty<int>();

            Reload();
        }

        /// <summary>Re-reads the field, suppressing write-back.</summary>
        public void Reload()
        {
            _loading = true;
            try
            {
                Number = _read();
                RefreshSwatch();
            }
            finally
            {
                _loading = false;
            }
        }

        /// <summary>
        /// Replaces the real model choices when a paired armor field changes between independent
        /// left/right editing and mirrored editing.
        /// </summary>
        public void SetOptions(IReadOnlyList<int>? options)
        {
            Options = options ?? Array.Empty<int>();
            OnPropertyChanged(nameof(HasOptions));
            OnPropertyChanged(nameof(SelectedOption));
        }

        partial void OnNumberChanged(decimal? value)
        {
            OnPropertyChanged(nameof(SelectedOption));

            if (_loading)
                return;

            // A part number or dye channel is always present on a real blueprint, so an empty box
            // is refused; anything the control let through outside Minimum/Maximum is refused too.
            // Both restore what is actually stored.
            if (!value.HasValue ||
                decimal.Truncate(value.Value) != value.Value ||
                value.Value < Minimum ||
                value.Value > Maximum ||
                (HasOptions && !Options.Contains((int)value.Value)))
            {
                Reload();
                return;
            }

            if (!_write((int)value.Value))
            {
                Reload();
                return;
            }

            RefreshSwatch();
        }

        partial void OnIsReadOnlyChanged(bool value) => OnPropertyChanged(nameof(IsEnabled));

        private void RefreshSwatch()
        {
            if (_sampleColor == null || !Number.HasValue)
            {
                SwatchBrush = null;
                return;
            }

            var sampled = _sampleColor((int)Number.Value);
            SwatchBrush = sampled.HasValue
                ? new SolidColorBrush(Color.FromRgb(sampled.Value.R, sampled.Value.G, sampled.Value.B))
                : null;
        }
    }
}
