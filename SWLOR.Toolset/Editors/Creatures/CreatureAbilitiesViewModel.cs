using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Editors.Creatures;

namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>Registered ability picker layered over the UTC FeatList.</summary>
    public sealed partial class CreatureAbilitiesViewModel : ObservableObject
    {
        private const int SearchLimit = 200;
        private static readonly Lazy<IReadOnlyList<CreatureAbilityInfo>> SharedCatalog =
            new(CreatureAbilityCatalog.Build);
        private static readonly Lazy<IReadOnlyDictionary<int, CreaturePerkInfo>> SharedPerks =
            new(CreaturePerkCatalog.Build);
        private readonly CreatureValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly IReadOnlyList<CreatureAbilityInfo> _catalog;
        private readonly IReadOnlyDictionary<int, CreaturePerkInfo> _perks;

        public ObservableCollection<CreatureAbilityEntryViewModel> Assigned { get; } = new();
        public ObservableCollection<CreatureAbilityInfo> Matching { get; } = new();

        [ObservableProperty]
        private string _searchText = string.Empty;

        public string SearchSummary => Matching.Count == 0
            ? "No matching registered abilities"
            : $"{Matching.Count} registered abilit{(Matching.Count == 1 ? "y" : "ies")}";

        public string PreservedSummary { get; private set; } = string.Empty;
        public bool HasPreservedFeats => PreservedSummary.Length > 0;

        public CreatureAbilitiesViewModel(
            CreatureValueStore store,
            Func<string, Action, bool> runEdit,
            IReadOnlyList<CreatureAbilityInfo>? catalog = null,
            IReadOnlyDictionary<int, CreaturePerkInfo>? perks = null)
        {
            _store = store;
            _runEdit = runEdit;
            _catalog = catalog ?? SharedCatalog.Value;
            _perks = perks ?? SharedPerks.Value;
            Reload();
        }

        [RelayCommand]
        private void Add(CreatureAbilityInfo? info)
        {
            if (info == null || Assigned.Any(entry => entry.FeatId == info.FeatId))
                return;
            if (!_runEdit($"Add {info.Name}", () => _store.AddFeat(info.FeatId)))
                return;
            Reload();
        }

        public void Reload()
        {
            Assigned.Clear();
            var featIds = _store.Feats.ToHashSet();
            foreach (var info in _catalog.Where(info => featIds.Contains(info.FeatId)))
            {
                var maximum = info.EffectivePerkId > 0 && _perks.TryGetValue(info.EffectivePerkId, out var perk)
                    ? perk.MaximumLevel
                    : 1;
                Assigned.Add(new CreatureAbilityEntryViewModel(
                    info, maximum, _store, _runEdit, Remove));
            }

            var registered = _catalog.Select(info => info.FeatId).ToHashSet();
            var preserved = featIds.Where(id => !registered.Contains(id)).Order().ToList();
            PreservedSummary = preserved.Count == 0
                ? string.Empty
                : $"{preserved.Count} engine feat{(preserved.Count == 1 ? " is" : "s are")} preserved but not editable here.";
            OnPropertyChanged(nameof(PreservedSummary));
            OnPropertyChanged(nameof(HasPreservedFeats));
            RebuildMatching();
        }

        private void Remove(CreatureAbilityEntryViewModel entry)
        {
            if (!_runEdit($"Remove {entry.Name}", () => _store.RemoveFeat(entry.FeatId)))
                return;
            Reload();
        }

        partial void OnSearchTextChanged(string value) => RebuildMatching();

        private void RebuildMatching()
        {
            var assigned = Assigned.Select(entry => entry.FeatId).ToHashSet();
            var query = SearchText.Trim();
            Matching.Clear();
            foreach (var info in _catalog.Where(info => !assigned.Contains(info.FeatId) &&
                         (query.Length == 0 ||
                          info.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                          info.Description.Contains(query, StringComparison.OrdinalIgnoreCase)))
                     .Take(SearchLimit))
            {
                Matching.Add(info);
            }
            OnPropertyChanged(nameof(SearchSummary));
        }
    }
}
