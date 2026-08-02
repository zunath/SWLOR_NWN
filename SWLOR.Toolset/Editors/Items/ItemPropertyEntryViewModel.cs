using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Editors.Items;

namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>
    /// One existing PropertiesList entry of a multi-subtype property - one row of an
    /// <see cref="ItemPropertyEntryListViewModel"/> (or an <see cref="ItemEngineLegacySectionViewModel"/>
    /// row). Value write-back follows <see cref="ItemStatCellViewModel"/>'s loading-guard pattern
    /// exactly, with a capped NumericUpDown instead of free text; only the store call it writes to (a
    /// specific PropertyId+SubtypeId pair rather than one fixed definition) differs.
    /// </summary>
    public sealed partial class ItemPropertyEntryViewModel : ObservableObject
    {
        private readonly int _propertyId;
        private readonly int _costTableId;
        private readonly ItemValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Action? _valueChanged;
        private readonly Action? _removed;
        private bool _loading;

        public int SubtypeId { get; }

        /// <summary>The subtype's resolved display text, or the raw subtype number when it can't be resolved.</summary>
        public string SubtypeDisplay { get; }

        public int Minimum => 0;

        /// <summary>The property's real engine cap, resolved from its CostTableId.</summary>
        public int Maximum { get; }

        /// <summary>
        /// False when itempropdef.2da declares no cost table. Such entries are subtype markers:
        /// their normalized CostValue is zero and only the entry itself can be added or removed.
        /// </summary>
        public bool HasEditableValue { get; }

        [ObservableProperty]
        private decimal? _number;

        public IRelayCommand RemoveCommand { get; }

        public ItemPropertyEntryViewModel(
            int propertyId,
            int subtypeId,
            string subtypeDisplay,
            int costTableId,
            ItemValueStore store,
            Func<string, Action, bool> runEdit,
            Action? valueChanged = null,
            Action? removed = null,
            ItemCostTableRanges? costTables = null)
        {
            _propertyId = propertyId;
            HasEditableValue = costTableId >= 0;
            _costTableId = Math.Max(0, costTableId);
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _runEdit = runEdit ?? throw new ArgumentNullException(nameof(runEdit));
            _valueChanged = valueChanged;
            _removed = removed;

            SubtypeId = subtypeId;
            SubtypeDisplay = subtypeDisplay ?? throw new ArgumentNullException(nameof(subtypeDisplay));
            Maximum = Math.Min(
                costTables?.MaxFor(costTableId) ?? ItemCostTableRanges.DefaultMax,
                ushort.MaxValue);

            RemoveCommand = new RelayCommand(Remove);

            RestoreValue();
        }

        partial void OnNumberChanged(decimal? value)
        {
            if (_loading)
                return;

            if (!HasEditableValue)
            {
                RestoreValue();
                return;
            }

            if (value.HasValue &&
                (decimal.Truncate(value.Value) != value.Value ||
                 value.Value < Minimum ||
                 value.Value > Maximum))
            {
                RestoreValue();
                return;
            }

            Write(value.HasValue ? (int)value.Value : null);
        }

        private void Remove() => Write(null);

        private void Write(int? parsed)
        {
            var applied = _runEdit($"Set {SubtypeDisplay}", () =>
                _store.SetPropertyValue(_propertyId, SubtypeId, _costTableId, parsed));

            if (!applied)
            {
                RestoreValue();
                return;
            }

            if (parsed is null)
            {
                // The entry is gone from the store now - the owning list/section rebuilds its row
                // set rather than this row trying to remove itself from a collection it doesn't own.
                _removed?.Invoke();
                return;
            }

            _valueChanged?.Invoke();
        }

        private void RestoreValue()
        {
            _loading = true;
            try
            {
                Number = _store.GetPropertyValue(_propertyId, SubtypeId);
            }
            finally
            {
                _loading = false;
            }
        }
    }
}
