using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Creatures;
using SWLOR.Toolset.Editors.Behaviors;

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

    public sealed record CreatureAbilityCatalogData(
        IReadOnlyList<CreatureAbilityInfo> Abilities,
        IReadOnlyDictionary<int, CreaturePerkInfo> Perks);

    /// <summary>Registered ability picker layered over the UTC FeatList.</summary>
    public sealed partial class CreatureAbilitiesViewModel : ObservableObject, IDisposable
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

        // Reflection over every ability and perk definition is a one-time application catalog
        // build, not part of opening a UTC. Keep one shared background task so opening the first
        // creature does not block the UI thread and subsequent editors reuse the same result.
        private static readonly object SharedCatalogLock = new();
        private static Lazy<Task<CreatureAbilityCatalogData>> _sharedCatalog =
            CreateSharedCatalog();
        private readonly CreatureValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Func<Task<CreatureAbilityCatalogData>>? _catalogLoader;
        private readonly ChoicePreviewService? _choicePreviews;
        private readonly Func<int, string?>? _iconResolver;
        private readonly ConcurrentDictionary<int, Task<string?>> _iconResRefs = new();
        private IReadOnlyList<CreatureAbilityInfo> _catalog = Array.Empty<CreatureAbilityInfo>();
        private IReadOnlyDictionary<int, CreaturePerkInfo> _perks =
            new Dictionary<int, CreaturePerkInfo>();
        private List<CreatureAbilityInfo> _matches = new();
        private Task? _loadTask;
        private bool _loaded;
        private bool _disposed;

        public ObservableCollection<CreatureAbilityEntryViewModel> Assigned { get; } = new();
        public ObservableCollection<CreatureAbilityChoiceViewModel> Matching { get; } = new();
        public IReadOnlyList<CreatureAbilityAudienceFilter> AudienceFilters => SharedAudienceFilters;
        public ObservableCollection<CreatureAbilitySkillFilter> SkillFilters { get; } = new();

        public bool IsLoaded => _loaded;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _loadError = string.Empty;

        public bool HasLoadError => LoadError.Length > 0;

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
            IReadOnlyDictionary<int, CreaturePerkInfo>? perks = null,
            Func<Task<CreatureAbilityCatalogData>>? catalogLoader = null,
            ChoicePreviewService? choicePreviews = null,
            Func<int, string?>? iconResolver = null)
        {
            _store = store;
            _runEdit = runEdit;
            _choicePreviews = choicePreviews;
            _iconResolver = iconResolver;
            SkillFilters.Add(new CreatureAbilitySkillFilter(null, "All skills"));
            _selectedAudienceFilter = AudienceFilters.Single(
                filter => filter.Value == CreatureAbilityAudience.Npc);
            _selectedSkillFilter = SkillFilters[0];

            if (catalog != null || perks != null)
            {
                ApplyCatalog(new CreatureAbilityCatalogData(
                    catalog ?? Array.Empty<CreatureAbilityInfo>(),
                    perks ?? new Dictionary<int, CreaturePerkInfo>()));
            }
            else
            {
                _catalogLoader = catalogLoader ?? GetSharedCatalogAsync;
            }
        }

        [RelayCommand(CanExecute = nameof(CanEdit))]
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
            if (!_loaded)
                return;

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

        partial void OnSearchTextChanged(string value)
        {
            if (_loaded)
                RebuildMatching();
        }

        partial void OnSelectedAudienceFilterChanged(CreatureAbilityAudienceFilter value)
        {
            if (_loaded)
                RebuildMatching();
        }

        partial void OnSelectedSkillFilterChanged(CreatureAbilitySkillFilter value)
        {
            if (_loaded)
                RebuildMatching();
        }

        /// <summary>
        /// Builds the registered definition catalogs only after the Abilities tab is selected.
        /// The reflection work stays off the UI thread and every later creature reuses its task.
        /// </summary>
        public Task EnsureLoadedAsync()
        {
            if (_loaded || _disposed)
                return Task.CompletedTask;
            if (_loadTask != null)
                return _loadTask;

            IsLoading = true;
            var task = LoadAsync();

            // A loader that completes synchronously (e.g. an already-faulted or already-resolved
            // Task, as tests use) runs LoadAsync's own finally - which clears _loadTask - before
            // this assignment executes, so this would otherwise stomp that null right back to a
            // completed task and make every later EnsureLoadedAsync call believe a load is still
            // in flight, permanently blocking retry after a failure.
            _loadTask = task.IsCompleted ? null : task;
            return task;
        }

        private async Task LoadAsync()
        {
            try
            {
                var data = await _catalogLoader!().ConfigureAwait(true);
                if (_disposed)
                    return;

                ApplyCatalog(data);
            }
            catch (Exception ex)
            {
                if (_disposed)
                    return;

                // Left retryable, the same way CreatureEditorViewModel.LoadAppearanceCatalogAsync
                // leaves _appearanceCatalogLoaded false: a transient failure must not permanently
                // disable this tab for the life of the editor instance.
                LoadError = $"Registered abilities could not be loaded: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                _loadTask = null;
                AddCommand.NotifyCanExecuteChanged();
            }
        }

        private void ApplyCatalog(CreatureAbilityCatalogData data)
        {
            _catalog = data.Abilities;
            _perks = data.Perks;
            _loaded = true;
            LoadError = string.Empty;

            // Keep the existing All skills row in place. Clearing the ItemsSource causes Avalonia's
            // ComboBox to publish a transient null SelectedItem, which used to refilter immediately
            // and dereference that null selection while the Abilities tab was opening.
            while (SkillFilters.Count > 1)
                SkillFilters.RemoveAt(SkillFilters.Count - 1);

            foreach (var filter in _catalog
                         .GroupBy(info => info.SkillId)
                         .Select(group => new CreatureAbilitySkillFilter(
                             group.Key,
                             group.Key == 0
                                 ? "No skill"
                                 : group.Select(info => info.SkillName)
                                     .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)) ?? "No skill"))
                         .OrderBy(filter => filter.SkillId == 0 ? 1 : 0)
                         .ThenBy(filter => filter.Label, StringComparer.OrdinalIgnoreCase))
            {
                SkillFilters.Add(filter);
            }

            SelectedSkillFilter = SkillFilters[0];
            OnPropertyChanged(nameof(IsLoaded));
            OnPropertyChanged(nameof(HasLoadError));
            Reload();
            AddCommand.NotifyCanExecuteChanged();
        }

        private bool CanEdit() => _loaded && !IsLoading && !HasLoadError;

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
            // Two-way ComboBox bindings may temporarily publish null while their controls are being
            // attached or their item sources change. Retain the builder-facing defaults during that
            // transition instead of allowing a tab switch to fail.
            var audience = SelectedAudienceFilter?.Value ?? CreatureAbilityAudience.Npc;
            if (audience == CreatureAbilityAudience.Npc && !info.IsNpcIntended ||
                audience == CreatureAbilityAudience.Player && info.IsNpcIntended)
            {
                return false;
            }

            if (SelectedSkillFilter?.SkillId is { } skillId && info.SkillId != skillId)
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
                Matching.Add(new CreatureAbilityChoiceViewModel(_matches[index]));
            NotifyMatchStateChanged();
        }

        public Task EnsureIconAsync(CreatureAbilityChoiceViewModel row)
        {
            if (!row.TryBeginIconRequest())
                return Task.CompletedTask;

            return LoadIconAsync(row.FeatId, icon => row.Icon = icon);
        }

        public Task EnsureIconAsync(CreatureAbilityEntryViewModel row)
        {
            if (!row.TryBeginIconRequest())
                return Task.CompletedTask;

            return LoadIconAsync(row.FeatId, icon => row.Icon = icon);
        }

        private async Task LoadIconAsync(int featId, Action<Bitmap> apply)
        {
            if (_choicePreviews == null || _iconResolver == null || _disposed)
                return;

            try
            {
                // The first feat.2da parse can be noticeable. Resolve it off the UI thread and let
                // the shared preview service bound image decoding to its existing worker pool.
                var resource = await _iconResRefs.GetOrAdd(
                    featId,
                    id => Task.Run(() => _iconResolver(id))).ConfigureAwait(true);
                if (_disposed || string.IsNullOrWhiteSpace(resource))
                    return;

                var choice = new BehaviorChoice(
                    featId,
                    string.Empty,
                    resource);
                if (_choicePreviews.Cached(choice, 48, cropTransparentCanvas: true) is { } cached)
                {
                    apply(cached);
                    return;
                }

                await _choicePreviews.RequestAsync(
                    choice,
                    48,
                    icon =>
                    {
                        if (!_disposed)
                            apply(icon);
                    },
                    cropTransparentCanvas: true).ConfigureAwait(true);
            }
            catch
            {
                // A missing or malformed icon must not prevent the ability from being edited.
            }
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
                    Matching.Add(new CreatureAbilityChoiceViewModel(_matches[Matching.Count]));
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
                Matching.Insert(index, new CreatureAbilityChoiceViewModel(info));
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

        private static CreatureAbilityCatalogData BuildSharedCatalog()
        {
            var perks = CreaturePerkCatalog.Build();
            return new CreatureAbilityCatalogData(CreatureAbilityCatalog.Build(perks), perks);
        }

        private static Lazy<Task<CreatureAbilityCatalogData>> CreateSharedCatalog() =>
            new(() => Task.Run(BuildSharedCatalog));

        /// <summary>
        /// Returns the shared catalog task, rebuilding it if the previous attempt faulted. Without
        /// this, one transient failure (e.g. a definition assembly locked mid-build) would poison
        /// the process-wide <see cref="Lazy{T}"/> and permanently disable the Abilities tab for
        /// every creature editor opened for the rest of the session.
        /// </summary>
        private static Task<CreatureAbilityCatalogData> GetSharedCatalogAsync()
        {
            var current = _sharedCatalog;
            if (current.IsValueCreated && current.Value.IsFaulted)
            {
                lock (SharedCatalogLock)
                {
                    if (ReferenceEquals(_sharedCatalog, current))
                        _sharedCatalog = CreateSharedCatalog();
                }

                current = _sharedCatalog;
            }

            return current.Value;
        }

        private void NotifyMatchStateChanged()
        {
            OnPropertyChanged(nameof(SearchSummary));
            OnPropertyChanged(nameof(CanLoadMore));
        }

        partial void OnLoadErrorChanged(string value) => OnPropertyChanged(nameof(HasLoadError));

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
