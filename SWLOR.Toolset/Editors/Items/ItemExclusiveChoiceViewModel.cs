using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Editors.Behaviors;

namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>
    /// An <see cref="ItemMultiEntryDefinition"/> whose <see cref="ItemMultiEntryDefinition.IsExclusive"/>
    /// is set (WeaponDamageType, 134 today): the item carries at most one entry of this property, so
    /// it is a single "pick one, or none" choice rather than the add/remove
    /// <see cref="ItemPropertyEntryListViewModel"/> the other multi-subtype properties use.
    /// </summary>
    public sealed partial class ItemExclusiveChoiceViewModel : ObservableObject
    {
        private readonly ItemMultiEntryDefinition _definition;
        private readonly ItemValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Action? _valueChanged;
        private bool _loading;

        public string Label => _definition.Label;

        /// <summary>A leading "none" option, then every real subtype choice.</summary>
        public IReadOnlyList<BehaviorChoiceViewModel> Options { get; }

        [ObservableProperty]
        private BehaviorChoiceViewModel? _selected;

        public ItemExclusiveChoiceViewModel(
            ItemMultiEntryDefinition definition,
            ItemValueStore store,
            Func<string, Action, bool> runEdit,
            IReadOnlyList<BehaviorChoice> subtypeChoices,
            Action? valueChanged = null)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _runEdit = runEdit ?? throw new ArgumentNullException(nameof(runEdit));
            _valueChanged = valueChanged;

            var none = new BehaviorChoiceViewModel(new BehaviorChoice(-1, "— none"));
            var real = BehaviorChoiceViewModel.From(subtypeChoices ?? Array.Empty<BehaviorChoice>());
            Options = new List<BehaviorChoiceViewModel> { none }.Concat(real).ToList();

            Reload();
        }

        /// <summary>Re-reads whichever single entry (if any) the store carries for this property.</summary>
        public void Reload()
        {
            _loading = true;
            try
            {
                var match = _store.Properties.FirstOrDefault(property => property.PropertyId == _definition.PropertyId);
                var hasEntry = _store.Properties.Any(property => property.PropertyId == _definition.PropertyId);
                Selected = hasEntry
                    ? Options.FirstOrDefault(option => option.Value == match.SubtypeId) ?? Options[0]
                    : Options[0];
            }
            finally
            {
                _loading = false;
            }
        }

        partial void OnSelectedChanged(BehaviorChoiceViewModel? value)
        {
            if (_loading || value == null)
                return;

            var applied = value.Value < 0
                ? _runEdit($"Clear {Label}", () => _store.ClearProperty(_definition.PropertyId))
                : _runEdit($"Set {Label}", () => _store.SetExclusiveProperty(
                    _definition.PropertyId, (int)value.Value, _definition.CostTableId));

            if (!applied)
            {
                Reload();
                return;
            }

            _valueChanged?.Invoke();
        }
    }
}
