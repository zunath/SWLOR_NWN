using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Editors.Items;

namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>
    /// One stat's value box: a single itemprop.2da property (and subtype) shown as a capped
    /// NumericUpDown. A null <see cref="Number"/> means the property is absent from PropertiesList;
    /// any other value is its CostValue. Garbage input is impossible - the control itself only
    /// accepts a number within <see cref="Minimum"/>/<see cref="Maximum"/>.
    /// </summary>
    public sealed partial class ItemStatCellViewModel : ObservableObject
    {
        private readonly ItemStatDefinition _definition;
        private readonly ItemValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Action? _valueChanged;
        private bool _loading;

        public string Label => _definition.Label;

        public int Minimum => 0;

        /// <summary>The stat's real engine cap, resolved from its CostTableId; 255 when unresolved.</summary>
        public int Maximum { get; }

        [ObservableProperty]
        private decimal? _number;

        public ItemStatCellViewModel(
            ItemStatDefinition definition,
            ItemValueStore store,
            Func<string, Action, bool> runEdit,
            Action? valueChanged = null,
            Func<int, int?>? costTableMax = null)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _runEdit = runEdit ?? throw new ArgumentNullException(nameof(runEdit));
            _valueChanged = valueChanged;
            Maximum = costTableMax?.Invoke(definition.CostTableId) ?? ItemCostTableRanges.DefaultMax;
            Reload();
        }

        /// <summary>Re-reads the stat's CostValue out of the document, suppressing write-back.</summary>
        public void Reload()
        {
            _loading = true;
            try
            {
                Number = _store.GetPropertyValue(_definition.PropertyId, _definition.SubtypeId);
            }
            finally
            {
                _loading = false;
            }
        }

        partial void OnNumberChanged(decimal? value)
        {
            if (_loading)
                return;

            Write(value.HasValue ? (int)value.Value : null);
        }

        private void Write(int? parsed)
        {
            var applied = _runEdit($"Set {Label}", () => _store.SetPropertyValue(
                _definition.PropertyId, _definition.SubtypeId, _definition.CostTableId, parsed));

            if (!applied)
            {
                Reload();
                return;
            }

            _valueChanged?.Invoke();
        }
    }
}
