using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Editors.Items;

namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>
    /// One stat's value selector: a single itemprop.2da property (and subtype) shown as the real
    /// populated rows of its cost table. When game data is unavailable, the stored number remains
    /// visible but read-only so a constrained CostValue never turns into arbitrary numeric input.
    /// A null <see cref="Number"/> means the property is absent from PropertiesList; any other value
    /// is its CostValue.
    /// </summary>
    public sealed partial class ItemStatCellViewModel : ObservableObject
    {
        private static readonly ItemCostTableOption NoneOption = new(-1, "— none");

        private readonly ItemStatDefinition _definition;
        private readonly ItemValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Action? _valueChanged;
        private bool _loading;

        public string Label => _definition.Label;

        public int Minimum => 0;

        /// <summary>The stat's real engine cap, resolved from its CostTableId.</summary>
        public int Maximum { get; }

        [ObservableProperty]
        private decimal? _number;

        /// <summary>
        /// The populated rows this stat's cost table actually offers; empty only when the table
        /// cannot be resolved and the stored value must remain read-only.
        /// </summary>
        /// <remarks>
        /// Delay is the clearest case: iprp_delay's rows start at 11 and row 11 is labelled "110",
        /// so a spinner both offered rows that do not exist and displayed a row index as if it were
        /// the delay.
        /// </remarks>
        public IReadOnlyList<ItemCostTableOption> Options { get; }

        public bool HasOptions => Options.Count > 0;

        public string LookupUnavailableMessage =>
            "2DA cost-table metadata unavailable. The stored value is shown read-only.";

        /// <summary>The clear choice followed by every real row in the cost table.</summary>
        public IReadOnlyList<ItemCostTableOption> SelectableOptions { get; }

        /// <summary>The stored CostValue as a selectable row, or the clear choice when absent.</summary>
        public ItemCostTableOption? SelectedOption
        {
            get
            {
                if (Number is not { } number)
                    return NoneOption;
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
                    Number = chosen.Value < 0 ? null : chosen.Value;
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
            Maximum = Math.Min(
                costTables?.MaxFor(definition.CostTableId) ?? ItemCostTableRanges.DefaultMax,
                ushort.MaxValue);
            Options = costTables?.OptionsFor(definition.CostTableId) ?? Array.Empty<ItemCostTableOption>();
            SelectableOptions = new[] { NoneOption }.Concat(Options).ToList();
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

            if (!HasOptions)
            {
                Reload();
                return;
            }

            if (value.HasValue &&
                (decimal.Truncate(value.Value) != value.Value ||
                 value.Value < Minimum ||
                 value.Value > Maximum ||
                 (HasOptions && !Options.Any(option => option.Value == (int)value.Value))))
            {
                Reload();
                return;
            }

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
