using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Editors.Items;

namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>
    /// One stat's value box: a single itemprop.2da property (and subtype) shown as a plain integer
    /// text field. An empty box means the property is absent from PropertiesList; any other value is
    /// its CostValue.
    /// </summary>
    public sealed partial class ItemStatCellViewModel : ObservableObject
    {
        private readonly ItemStatDefinition _definition;
        private readonly ItemValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Action? _valueChanged;
        private bool _loading;

        public string Label => _definition.Label;

        [ObservableProperty]
        private string _value = string.Empty;

        public ItemStatCellViewModel(
            ItemStatDefinition definition,
            ItemValueStore store,
            Func<string, Action, bool> runEdit,
            Action? valueChanged = null)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _runEdit = runEdit ?? throw new ArgumentNullException(nameof(runEdit));
            _valueChanged = valueChanged;
            Reload();
        }

        /// <summary>Re-reads the stat's CostValue out of the document, suppressing write-back.</summary>
        public void Reload()
        {
            _loading = true;
            try
            {
                var stored = _store.GetPropertyValue(_definition.PropertyId, _definition.SubtypeId);
                Value = stored?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
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

            // A blank box is a deliberate clear; a non-numeric one is a typo. Only the former reaches
            // the store - the latter refuses the write outright and puts the stored value back, so a
            // stray keystroke can never overwrite or erase a real number.
            if (string.IsNullOrWhiteSpace(value))
            {
                Write(null);
                return;
            }

            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                Reload();
                return;
            }

            Write(parsed);
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
