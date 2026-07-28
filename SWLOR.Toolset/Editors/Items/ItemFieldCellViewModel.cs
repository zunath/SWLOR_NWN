using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>
    /// One plain integer field on the Appearance tab's armor grid: a body-part number or a dye
    /// channel. Shaped after <see cref="ItemStatCellViewModel"/> - a loading guard around
    /// <see cref="Reload"/>, and a value that is blank, unparsable, or out of range is refused rather
    /// than written, restoring what was actually stored - but this cell writes a bare GFF field
    /// instead of a PropertiesList entry, so the caller supplies its own read/write closures instead
    /// of a PropertyId/SubtypeId pair. That is what lets the same cell serve a lone field
    /// (ArmorPart_Torso) and a mirrored pair's left side (which also has to write its sibling and both
    /// their "x" twins) without this class knowing which case it is in.
    /// </summary>
    public sealed partial class ItemFieldCellViewModel : ObservableObject
    {
        private readonly Func<int?> _read;
        private readonly Func<int, bool> _write;
        private readonly int _min;
        private readonly int _max;
        private bool _loading;

        public string Label { get; }

        [ObservableProperty]
        private string _value = string.Empty;

        public ItemFieldCellViewModel(string label, Func<int?> read, Func<int, bool> write, int min, int max)
        {
            Label = label ?? throw new ArgumentNullException(nameof(label));
            _read = read ?? throw new ArgumentNullException(nameof(read));
            _write = write ?? throw new ArgumentNullException(nameof(write));
            _min = min;
            _max = max;

            Reload();
        }

        /// <summary>Re-reads the field, suppressing write-back.</summary>
        public void Reload()
        {
            _loading = true;
            try
            {
                Value = _read()?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            }
            finally
            {
                _loading = false;
            }
        }

        partial void OnValueChanged(string value)
        {
            if (_loading)
                return;

            // A part number or dye channel is always present on a real blueprint - a blank or
            // non-numeric box is a typo, and so is anything outside the field's real range. All three
            // are refused rather than written, and the box is put back to what is actually stored.
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ||
                parsed < _min || parsed > _max)
            {
                Reload();
                return;
            }

            if (!_write(parsed))
                Reload();
        }
    }
}
