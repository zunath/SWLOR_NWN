using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Editors.Creatures;

namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>Searchable visible-equipment blueprint picker.</summary>
    public sealed partial class CreatureEquipmentPickerViewModel : ObservableObject
    {
        private const int SearchLimit = 100;
        private readonly int _slot;
        private readonly CreatureValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Action _changed;
        private bool _loading;

        public string Label { get; }
        public IReadOnlyList<CreatureEquipmentChoice> Choices { get; }
        public ObservableCollection<CreatureEquipmentChoice> Matching { get; } = new();

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private CreatureEquipmentChoice? _selected;

        public string CurrentDisplay => Selected?.Display ?? "None";

        public CreatureEquipmentPickerViewModel(
            string label,
            int slot,
            CreatureValueStore store,
            Func<string, Action, bool> runEdit,
            IReadOnlyList<CreatureEquipmentChoice> choices,
            Action changed)
        {
            Label = label;
            _slot = slot;
            _store = store;
            _runEdit = runEdit;
            Choices = choices;
            _changed = changed;
            Reload();
        }

        public void Reload()
        {
            _loading = true;
            try
            {
                var resRef = _store.EquippedResRef(_slot);
                Selected = Choices.FirstOrDefault(choice =>
                    string.Equals(choice.ResRef, resRef, StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                _loading = false;
            }
            Rebuild();
            OnPropertyChanged(nameof(CurrentDisplay));
        }

        [RelayCommand]
        private void Choose(CreatureEquipmentChoice? choice)
        {
            if (choice != null)
                Selected = choice;
        }

        [RelayCommand]
        private void Clear()
        {
            if (!_runEdit($"Clear {Label}", () => _store.SetEquippedResRef(_slot, null)))
                return;
            Reload();
            _changed();
        }

        partial void OnSearchTextChanged(string value) => Rebuild();

        partial void OnSelectedChanged(CreatureEquipmentChoice? value)
        {
            OnPropertyChanged(nameof(CurrentDisplay));
            if (_loading || value == null)
                return;
            if (!_runEdit($"Change {Label}", () => _store.SetEquippedResRef(_slot, value.ResRef)))
            {
                Reload();
                return;
            }
            _changed();
        }

        private void Rebuild()
        {
            var query = SearchText.Trim();
            Matching.Clear();
            foreach (var choice in Choices.Where(choice => query.Length == 0 ||
                         choice.Display.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                         choice.ResRef.Contains(query, StringComparison.OrdinalIgnoreCase)).Take(SearchLimit))
                Matching.Add(choice);
        }
    }
}
