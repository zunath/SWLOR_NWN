using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Editors.Creatures;

namespace SWLOR.Toolset.Editors.Creatures
{
    public enum CreatureAbilityAudience
    {
        All,
        Npc,
        Player
    }

    public sealed record CreatureAbilityAudienceFilter(CreatureAbilityAudience Value, string Label)
    {
        public override string ToString() => Label;
    }

    public sealed record CreatureAbilitySkillFilter(int? SkillId, string Label)
    {
        public override string ToString() => Label;
    }

    /// <summary>Registered ability picker layered over the UTC FeatList.</summary>
    public sealed partial class CreatureAbilitiesViewModel : ObservableObject
    {
        // The result list contains controls with commands and wrapped descriptions. Publishing the
        // whole catalog makes a single assignment pay to construct every row again. Forty is enough
        // to browse immediately; the same progressive-loading pattern used by the appearance and
        // behavior pickers publishes the remainder as the builder scrolls.
        private const int PageSize = 40;

        private static readonly IReadOnlyList<CreatureAbilityAudienceFilter> SharedAudienceFilters =
        [
            new(CreatureAbilityAudience.All, "All abilities"),
            new(CreatureAbilityAudience.Npc, "NPC-intended"),
            new(CreatureAbilityAudience.Player, "Player-intended")
        ];

        private static readonly Lazy<IReadOnlyList<CreatureAbilityInfo>> SharedCatalog =
            new(CreatureAbilityCatalog.Build);
        private static readonly Lazy<IReadOnlyDictionary<int, CreaturePerkInfo>> SharedPerks =
            new(CreaturePerkCatalog.Build);
        private readonly CreatureValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly IReadOnlyList<CreatureAbilityInfo> _catalog;
        private readonly IReadOnlyDictionary<int, CreaturePerkInfo> _perks;
        private List<CreatureAbilityInfo> _matches = new();

        public ObservableCollection<CreatureAbilityEntryViewModel> Assigned { get; } = new();
        public ObservableCollection<CreatureAbilityInfo> Matching { get; } = new();
        public IReadOnlyList<CreatureAbilityAudienceFilter> AudienceFilters => SharedAudienceFilters;
        public IReadOnlyList<CreatureAbilitySkillFilter> SkillFilters { get; }

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private CreatureAbilityAudienceFilter _selectedAudienceFilter;

        [ObservableProperty]
        private CreatureAbilitySkillFilter _selectedSkillFilter;

        public string SearchSummary
        {
            get
            {
                if (_matches.Count == 0)
                    return "No matching registered abilities";

                var noun = _matches.Count == 1 ? "ability" : "abilities";
                return Matching.Count >= _matches.Count
                    ? $"{_matches.Count} matching {noun}"
                    : $"{Matching.Count} of {_matches.Count} matching {noun}";
            }
        }

        public bool CanLoadMore => Matching.Count < _matches.Count;

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

            SkillFilters =
            [
                new CreatureAbilitySkillFilter(null, "All skills"),
                .. _catalog
                    .GroupBy(info => info.SkillId)
                    .Select(group => new CreatureAbilitySkillFilter(
                        group.Key,
                        group.Key == 0
                            ? "No skill"
                            : group.Select(info => info.SkillName)
                                .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)) ?? "No skill"))
                    .OrderBy(filter => filter.SkillId == 0 ? 1 : 0)
                    .ThenBy(filter => filter.Label, StringComparer.OrdinalIgnoreCase)
            ];
            _selectedAudienceFilter = AudienceFilters[0];
            _selectedSkillFilter = SkillFilters[0];
            Reload();
        }

        [RelayCommand]
        private void Add(CreatureAbilityInfo? info)
        {
            if (info == null || Assigned.Any(entry => entry.FeatId == info.FeatId))
                return;
            if (!_runEdit($"Add {info.Name}", () => _store.AddFeat(info.FeatId)))
                return;

            InsertAssigned(CreateEntry(info));
            RemoveAvailable(info);
        }

        public void Reload()
        {
            Assigned.Clear();
            var featIds = _store.Feats.ToHashSet();
            foreach (var info in _catalog.Where(info => featIds.Contains(info.FeatId)))
                Assigned.Add(CreateEntry(info));

            RebuildMatching();
        }

        private CreatureAbilityEntryViewModel CreateEntry(CreatureAbilityInfo info)
        {
            var maximum = info.EffectivePerkId > 0 && _perks.TryGetValue(info.EffectivePerkId, out var perk)
                ? perk.MaximumLevel
                : 1;
            return new CreatureAbilityEntryViewModel(info, maximum, _store, _runEdit, Remove);
        }

        private void InsertAssigned(CreatureAbilityEntryViewModel entry)
        {
            var index = 0;
            while (index < Assigned.Count && Compare(Assigned[index].Info, entry.Info) < 0)
                index++;
            Assigned.Insert(index, entry);
        }

        private void Remove(CreatureAbilityEntryViewModel entry)
        {
            if (!_runEdit($"Remove {entry.Name}", () =>
                {
                    _store.RemoveFeat(entry.FeatId);
                    if (entry.Info.EffectivePerkId > 0 &&
                        !HasRemainingPerkDependency(entry.Info.EffectivePerkId))
                    {
                        _store.Locals.Remove($"PERK_LEVEL_{entry.Info.EffectivePerkId}");
                    }
                }))
                return;

            Assigned.Remove(entry);
            InsertAvailable(entry.Info);
        }

        private bool HasRemainingPerkDependency(int perkId)
        {
            var remainingFeats = _store.Feats.ToHashSet();
            if (_catalog.Any(info =>
                    info.EffectivePerkId == perkId && remainingFeats.Contains(info.FeatId)))
            {
                return true;
            }

            return _perks.TryGetValue(perkId, out var perk) &&
                   perk.GrantedFeatIds?.Any(remainingFeats.Contains) == true;
        }

        [RelayCommand]
        private void LoadMore() => PublishPage();

        partial void OnSearchTextChanged(string value) => RebuildMatching();
        partial void OnSelectedAudienceFilterChanged(CreatureAbilityAudienceFilter value) => RebuildMatching();
        partial void OnSelectedSkillFilterChanged(CreatureAbilitySkillFilter value) => RebuildMatching();

        private void RebuildMatching()
        {
            var assigned = Assigned.Select(entry => entry.FeatId).ToHashSet();
            var words = SearchText.Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            _matches = _catalog
                .Where(info => !assigned.Contains(info.FeatId) && MatchesFilters(info, words))
                .OrderBy(info => info.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(info => info.FeatId)
                .ToList();

            Matching.Clear();
            PublishPage();
        }

        private bool MatchesFilters(CreatureAbilityInfo info, IReadOnlyList<string> words)
        {
            if (SelectedAudienceFilter.Value == CreatureAbilityAudience.Npc && !info.IsNpcIntended ||
                SelectedAudienceFilter.Value == CreatureAbilityAudience.Player && info.IsNpcIntended)
            {
                return false;
            }

            if (SelectedSkillFilter.SkillId is { } skillId && info.SkillId != skillId)
                return false;

            return words.Count == 0 || words.All(word =>
                info.Name.Contains(word, StringComparison.OrdinalIgnoreCase) ||
                info.Description.Contains(word, StringComparison.OrdinalIgnoreCase) ||
                info.SkillName.Contains(word, StringComparison.OrdinalIgnoreCase) ||
                info.IntendedFor.Contains(word, StringComparison.OrdinalIgnoreCase));
        }

        private void PublishPage()
        {
            var end = Math.Min(Matching.Count + PageSize, _matches.Count);
            for (var index = Matching.Count; index < end; index++)
                Matching.Add(_matches[index]);
            NotifyMatchStateChanged();
        }

        /// <summary>
        /// Removes one selected row without clearing and reconstructing the page around it. If the
        /// row was visible, only that control and the next load-ahead row change.
        /// </summary>
        private void RemoveAvailable(CreatureAbilityInfo info)
        {
            var index = _matches.FindIndex(match => match.FeatId == info.FeatId);
            if (index < 0)
                return;

            _matches.RemoveAt(index);
            if (index < Matching.Count)
            {
                Matching.RemoveAt(index);
                if (Matching.Count < _matches.Count)
                    Matching.Add(_matches[Matching.Count]);
            }

            NotifyMatchStateChanged();
        }

        /// <summary>
        /// Returns one removed assignment to its sorted filtered position. A row outside the current
        /// page does not touch the realized controls at all.
        /// </summary>
        private void InsertAvailable(CreatureAbilityInfo info)
        {
            var words = SearchText.Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!MatchesFilters(info, words))
                return;

            var wasFullyPublished = Matching.Count == _matches.Count;
            var index = _matches.BinarySearch(info, Comparer<CreatureAbilityInfo>.Create(Compare));
            if (index < 0)
                index = ~index;
            _matches.Insert(index, info);

            var targetVisible = wasFullyPublished ? Matching.Count + 1 : Matching.Count;
            if (index < targetVisible)
            {
                Matching.Insert(index, info);
                if (Matching.Count > targetVisible)
                    Matching.RemoveAt(Matching.Count - 1);
            }

            NotifyMatchStateChanged();
        }

        private static int Compare(CreatureAbilityInfo left, CreatureAbilityInfo right)
        {
            var byName = StringComparer.OrdinalIgnoreCase.Compare(left.Name, right.Name);
            return byName != 0 ? byName : left.FeatId.CompareTo(right.FeatId);
        }

        private void NotifyMatchStateChanged()
        {
            OnPropertyChanged(nameof(SearchSummary));
            OnPropertyChanged(nameof(CanLoadMore));
        }
    }
}
