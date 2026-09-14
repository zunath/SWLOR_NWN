using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Editors.Behaviors;

namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>
    /// One <see cref="ItemMultiEntryDefinition"/>'s editor: every PropertiesList entry the item
    /// currently stores for that property, plus a searchable picker for adding another subtype not
    /// already present. Used both by the Stats tab (one list per property, attached to the
    /// <see cref="ItemStatGroupViewModel"/> whose <see cref="ItemStatGroup"/> matches the
    /// definition's Context) and by the Requirements tab (Perk/Race, whose IsRequirement is true).
    /// </summary>
    public sealed partial class ItemPropertyEntryListViewModel : ObservableObject
    {
        private readonly ItemMultiEntryDefinition _definition;
        private readonly ItemValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly IReadOnlyList<BehaviorChoice> _subtypeChoices;
        private readonly Action? _valueChanged;
        private readonly ItemCostTableRanges? _costTables;

        public string Label => _definition.Label;

        /// <summary>Watermark for the add-search box - "Search Perks", "Search Races", or a generic fallback.</summary>
        public string AddWatermark => $"Search {_definition.SearchNoun ?? Label}";

        public ObservableCollection<ItemPropertyEntryViewModel> Entries { get; } = new();

        public ObservableCollection<BehaviorChoiceViewModel> AddChoices { get; } = new();

        [ObservableProperty]
        private string _addSearchText = string.Empty;

        public IRelayCommand<BehaviorChoiceViewModel> AddCommand { get; }

        public ItemPropertyEntryListViewModel(
            ItemMultiEntryDefinition definition,
            ItemValueStore store,
            Func<string, Action, bool> runEdit,
            IReadOnlyList<BehaviorChoice> subtypeChoices,
            Action? valueChanged = null,
            ItemCostTableRanges? costTables = null)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _runEdit = runEdit ?? throw new ArgumentNullException(nameof(runEdit));
            _subtypeChoices = subtypeChoices ?? Array.Empty<BehaviorChoice>();
            _valueChanged = valueChanged;
            _costTables = costTables;

            AddCommand = new RelayCommand<BehaviorChoiceViewModel>(Add);

            Reload();
        }

        /// <summary>Rebuilds <see cref="Entries"/> (and, from the new set, <see cref="AddChoices"/>) from the store.</summary>
        public void Reload()
        {
            Entries.Clear();

            var subtypeIds = _store.Properties
                .Where(property => property.PropertyId == _definition.PropertyId)
                .Select(property => property.SubtypeId)
                .Distinct()
                .OrderBy(subtypeId => subtypeId);

            foreach (var subtypeId in subtypeIds)
                Entries.Add(BuildEntry(subtypeId));

            RefreshAddChoices();
        }

        partial void OnAddSearchTextChanged(string value) => RefreshAddChoices();

        private void Add(BehaviorChoiceViewModel? choice)
        {
            if (choice == null)
                return;

            var subtypeId = (int)choice.Value;
            var storedCostTableId = Math.Max(0, _definition.CostTableId);
            var initialCostValue = _definition.CostTableId < 0 ? 0 : 1;
            var applied = _runEdit($"Add {Label} - {choice.Display}", () =>
                _store.SetPropertyValue(_definition.PropertyId, subtypeId, storedCostTableId, initialCostValue));

            if (!applied)
                return;

            Reload();
            _valueChanged?.Invoke();
        }

        private ItemPropertyEntryViewModel BuildEntry(int subtypeId) =>
            new(_definition.PropertyId, subtypeId, DisplayFor(subtypeId), _definition.CostTableId,
                _store, _runEdit, OnEntryValueChanged, OnEntryRemoved, _costTables);

        private string DisplayFor(int subtypeId)
        {
            var match = _subtypeChoices.FirstOrDefault(choice => choice.Value == subtypeId);
            return match?.Display ?? subtypeId.ToString(CultureInfo.InvariantCulture);
        }

        private void OnEntryValueChanged() => _valueChanged?.Invoke();

        private void OnEntryRemoved()
        {
            // The entry's own PropertiesList row is already gone from the store - Reload() drops it
            // from Entries and puts its subtype back into AddChoices in one pass.
            Reload();
            _valueChanged?.Invoke();
        }

        /// <summary>Every subtype not already present, filtered by the search box, capped at 50 shown.</summary>
        private void RefreshAddChoices()
        {
            AddChoices.Clear();

            var present = new HashSet<int>(Entries.Select(entry => entry.SubtypeId));
            var candidates = _subtypeChoices.Where(choice => !present.Contains((int)choice.Value));

            if (!string.IsNullOrWhiteSpace(AddSearchText))
            {
                candidates = candidates.Where(choice =>
                    choice.Display.Contains(AddSearchText, StringComparison.OrdinalIgnoreCase));
            }

            // Every match, not a first page: the list is virtualized in the template, and a cap
            // here was invisible to the builder - scrolling a 600-perk list simply stopped at 50
            // with nothing to say why or any way to reach the rest.
            foreach (var choice in BehaviorChoiceViewModel.From(candidates.ToList()))
                AddChoices.Add(choice);
        }
    }
}
