using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Items;

namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>
    /// The item editor's engine-legacy surface: one editable, removable row per
    /// <see cref="ItemEngineLegacyCatalog"/> property actually present on the item. Unlike
    /// <see cref="ItemPropertyEntryListViewModel"/>, this is a single flat sweep across every legacy
    /// property rather than one list per property - these rows predate SWLOR's own stat groups and
    /// are not something a builder is expected to add, so there is no add affordance, only
    /// edit/remove for whatever the corpus already carries.
    /// </summary>
    public sealed class ItemEngineLegacySectionViewModel : ObservableObject
    {
        private const string SubtypeKeyPrefix = "item.subtypes:";

        private readonly ItemValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Func<string, IReadOnlyList<BehaviorChoice>>? _resolveSubtypeChoices;
        private readonly ItemCostTableRanges? _costTables;
        private readonly Action? _valueChanged;

        private IReadOnlyList<ItemPropertyEntryViewModel> _entries = Array.Empty<ItemPropertyEntryViewModel>();

        public IReadOnlyList<ItemPropertyEntryViewModel> Entries
        {
            get => _entries;
            private set => SetProperty(ref _entries, value);
        }

        public bool HasEntries => Entries.Count > 0;

        public ItemEngineLegacySectionViewModel(
            ItemValueStore store,
            Func<string, Action, bool> runEdit,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveSubtypeChoices = null,
            ItemCostTableRanges? costTables = null,
            Action? valueChanged = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _runEdit = runEdit ?? throw new ArgumentNullException(nameof(runEdit));
            _resolveSubtypeChoices = resolveSubtypeChoices;
            _costTables = costTables;
            _valueChanged = valueChanged;

            Rebuild();
        }

        /// <summary>Re-sweeps the store's PropertiesList for every legacy property still present.</summary>
        public void Rebuild()
        {
            var rows = new List<ItemPropertyEntryViewModel>();

            foreach (var property in _store.Properties.OrderBy(p => p.PropertyId).ThenBy(p => p.SubtypeId))
            {
                var definition = ItemEngineLegacyCatalog.All
                    .FirstOrDefault(candidate => candidate.PropertyId == property.PropertyId);
                if (definition == null)
                    continue;

                rows.Add(BuildRow(property.PropertyId, property.SubtypeId, definition));
            }

            Entries = rows;
            OnPropertyChanged(nameof(HasEntries));
        }

        private ItemPropertyEntryViewModel BuildRow(
            int propertyId, int subtypeId, ItemEngineLegacyDefinition definition) =>
            new(propertyId, subtypeId, DisplayFor(definition, subtypeId), definition.CostTableId,
                _store, _runEdit, valueChanged: _valueChanged, removed: OnEntryRemoved, costTables: _costTables);

        private void OnEntryRemoved()
        {
            Rebuild();
            _valueChanged?.Invoke();
        }

        /// <summary>
        /// "Label" alone when the property has no subtype table; "Label (resolved subtype)" when it
        /// does and the subtype resolves; "Label (raw number)" when it does but nothing resolves it.
        /// </summary>
        private string DisplayFor(ItemEngineLegacyDefinition definition, int subtypeId)
        {
            if (string.IsNullOrWhiteSpace(definition.SubtypeTableResRef))
                return definition.Label;

            var choices = _resolveSubtypeChoices?.Invoke($"{SubtypeKeyPrefix}{definition.SubtypeTableResRef}")
                ?? Array.Empty<BehaviorChoice>();
            var match = choices.FirstOrDefault(choice => choice.Value == subtypeId);

            return $"{definition.Label} ({match?.Display ?? subtypeId.ToString()})";
        }
    }
}
