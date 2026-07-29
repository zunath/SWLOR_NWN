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

        /// <summary>
        /// The rows this stat's cost table actually offers, when it is a set of coded choices rather
        /// than a range of numbers; empty when a number box is right.
        /// </summary>
        /// <remarks>
        /// Delay is the clearest case: iprp_delay's rows start at 11 and row 11 is labelled "110",
        /// so a spinner both offered rows that do not exist and displayed a row index as if it were
        /// the delay.
        /// </remarks>
        public IReadOnlyList<ItemCostTableOption> Options { get; }

        public bool HasOptions => Options.Count > 0;

        /// <summary>The stored CostValue as a row of <see cref="Options"/>; null when it matches none.</summary>
        public ItemCostTableOption? SelectedOption
        {
            get
            {
                if (Number is not { } number)
                    return null;
                var value = (int)number;
                foreach (var option in Options)
                {
                    if (option.Value == value)
                        return option;
                }

                return null;
            }
            set
            {
                if (value is { } chosen)
                    Number = chosen.Value;
            }
        }

        public ItemStatCellViewModel(
            ItemStatDefinition definition,
            ItemValueStore store,
            Func<string, Action, bool> runEdit,
            Action? valueChanged = null,
            ItemCostTableRanges? costTables = null)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _runEdit = runEdit ?? throw new ArgumentNullException(nameof(runEdit));
            _valueChanged = valueChanged;
            Maximum = costTables?.MaxFor(definition.CostTableId) ?? ItemCostTableRanges.DefaultMax;
            Options = costTables?.OptionsFor(definition.CostTableId) ?? Array.Empty<ItemCostTableOption>();
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
            OnPropertyChanged(nameof(SelectedOption));

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
