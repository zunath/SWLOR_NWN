using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Editors.Sounds;
using SWLOR.Toolset.Services;

namespace SWLOR.Toolset.Editors.Sounds
{
    /// <summary>Adds, removes and reorders the Sounds GFF list.</summary>
    public sealed partial class SoundListEditorViewModel : ObservableObject
    {
        private readonly SoundValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Action _changed;
        private readonly SoundPreviewService? _preview;
        private readonly int _maxItems;
        private int _matchCount;

        /// <summary>
        /// Cap on published matches, as everywhere else a picker filters a resource set: enough that a
        /// builder can browse without a query, few enough that the list stays cheap to realize.
        /// </summary>
        private const int MaxSearchResults = 200;

        public ObservableCollection<SoundListEntryViewModel> Rows { get; } = new();

        public IReadOnlyList<string> AvailableSounds { get; }

        /// <summary>The slice of <see cref="AvailableSounds"/> the picker is showing.</summary>
        public ObservableCollection<string> FilteredSounds { get; } = new();

        public bool HasAudioCatalog => AvailableSounds.Count > 0;

        /// <summary>
        /// How much of the catalog the filter is showing, for the picker's count line. Counts matches
        /// rather than published rows, which can also carry a pick the filter excluded.
        /// </summary>
        public string SearchSummary =>
            AvailableSounds.Count == 0
                ? "No sounds"
                : _matchCount == AvailableSounds.Count
                    ? $"{AvailableSounds.Count} sound{(AvailableSounds.Count == 1 ? string.Empty : "s")}"
                    : _matchCount == 0
                        ? "No matching sounds"
                        : $"{_matchCount} of {AvailableSounds.Count} sounds";

        public bool HasRoom => _maxItems == 0 || Rows.Count < _maxItems;

        public bool HasValidCount => Rows.Count > 0 && (_maxItems == 0 || Rows.Count <= _maxItems);

        public bool HasSelection => SelectedEntry != null;

        /// <summary>Whether this build can play anything, which decides if Play and Stop appear.</summary>
        public bool CanPreview => _preview?.IsAvailable == true;

        /// <summary>
        /// What Play would play: the entry picked out of the object's own list, or - when nothing
        /// there is picked - whatever is highlighted in the catalog, so a builder can listen to a
        /// sound before adding it rather than after.
        /// </summary>
        public string? PreviewTarget => SelectedEntry?.ResRef ?? Candidate;

        [ObservableProperty]
        private SoundListEntryViewModel? _selectedEntry;

        /// <summary>The sound picked out of the catalog, which Add appends to the list.</summary>
        [ObservableProperty]
        private string? _candidate;

        [ObservableProperty]
        private string _search = string.Empty;

        [ObservableProperty]
        private string? _status;

        public SoundListEditorViewModel(
            SoundValueStore store,
            Func<string, Action, bool> runEdit,
            IReadOnlyList<string> availableSounds,
            int maxItems,
            Action changed,
            SoundPreviewService? preview = null)
        {
            _store = store;
            _runEdit = runEdit;
            AvailableSounds = availableSounds;
            _maxItems = maxItems;
            _changed = changed;
            _preview = preview;
            RebuildFilteredSounds();
            Reload();
        }

        public void Reload()
        {
            var selectedIndex = SelectedEntry?.Index;
            Rows.Clear();
            var sounds = _store.GetSounds();
            for (var index = 0; index < sounds.Count; index++)
                Rows.Add(new SoundListEntryViewModel(index, sounds[index]));

            SelectedEntry = selectedIndex is { } value && value >= 0 && value < Rows.Count
                ? Rows[value]
                : null;
            NotifyState();
        }

        private bool CanAdd()
        {
            var candidate = Candidate?.Trim() ?? string.Empty;
            return HasRoom
                   && candidate.Length > 0
                   && AvailableSounds.Contains(candidate, StringComparer.OrdinalIgnoreCase);
        }

        [RelayCommand(CanExecute = nameof(CanAdd))]
        private void Add()
        {
            var candidate = Candidate?.Trim() ?? string.Empty;
            if (candidate.Length == 0)
                return;

            if (!_runEdit("Add sound", () => _store.AddSound(candidate)))
                return;

            // Clearing the pick leaves it published if the filter had excluded it, which reads as a
            // sound still waiting to be added.
            Candidate = null;
            RebuildFilteredSounds();
            Reload();
            SelectedEntry = Rows.LastOrDefault();
            _changed();
        }

        [RelayCommand(CanExecute = nameof(HasSelection))]
        private void Remove()
        {
            if (SelectedEntry is not { } selected)
                return;

            if (!_runEdit($"Remove sound {selected.ResRef}", () => _store.RemoveSound(selected.Index)))
                return;

            var nextIndex = Math.Min(selected.Index, Rows.Count - 2);
            Reload();
            SelectedEntry = nextIndex >= 0 && nextIndex < Rows.Count ? Rows[nextIndex] : null;
            _changed();
        }

        private bool CanMoveUp() => SelectedEntry is { Index: > 0 };

        [RelayCommand(CanExecute = nameof(CanMoveUp))]
        private void MoveUp()
        {
            if (SelectedEntry is not { } selected)
                return;

            var target = selected.Index - 1;
            if (!_runEdit($"Move sound {selected.ResRef} up", () => _store.MoveSound(selected.Index, target)))
                return;

            Reload();
            SelectedEntry = Rows[target];
            _changed();
        }

        private bool CanMoveDown() =>
            SelectedEntry is { } selected && selected.Index < Rows.Count - 1;

        [RelayCommand(CanExecute = nameof(CanMoveDown))]
        private void MoveDown()
        {
            if (SelectedEntry is not { } selected)
                return;

            var target = selected.Index + 1;
            if (!_runEdit($"Move sound {selected.ResRef} down", () => _store.MoveSound(selected.Index, target)))
                return;

            Reload();
            SelectedEntry = Rows[target];
            _changed();
        }

        private bool CanPlay() => CanPreview && !string.IsNullOrWhiteSpace(PreviewTarget);

        /// <summary>Plays what is highlighted, the way the original toolset's Play does.</summary>
        [RelayCommand(CanExecute = nameof(CanPlay))]
        private void Play()
        {
            if (_preview == null)
                return;

            Status = _preview.Play(PreviewTarget);
        }

        [RelayCommand]
        private void StopPlayback() => _preview?.Stop();

        partial void OnCandidateChanged(string? value)
        {
            AddCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(PreviewTarget));
            PlayCommand.NotifyCanExecuteChanged();
        }

        partial void OnSearchChanged(string value) => RebuildFilteredSounds();

        /// <summary>
        /// Publishes the catalog entries matching the filter, capped. A picked sound the filter or the
        /// cap would drop is put back at the top and re-selected: emptying the list clears the picker's
        /// selection, and a pick a builder cannot see is one Add appears to refuse for no reason.
        /// </summary>
        private void RebuildFilteredSounds()
        {
            var picked = Candidate;
            var query = Search.Trim();
            FilteredSounds.Clear();

            _matchCount = 0;
            foreach (var sound in AvailableSounds)
            {
                if (query.Length > 0 && !sound.Contains(query, StringComparison.OrdinalIgnoreCase))
                    continue;

                FilteredSounds.Add(sound);
                if (++_matchCount >= MaxSearchResults)
                    break;
            }

            if (picked is { Length: > 0 } &&
                !FilteredSounds.Contains(picked, StringComparer.OrdinalIgnoreCase))
            {
                FilteredSounds.Insert(0, picked);
            }

            Candidate = picked;
            OnPropertyChanged(nameof(SearchSummary));
        }

        partial void OnSelectedEntryChanged(SoundListEntryViewModel? value) => NotifyState();

        private void NotifyState()
        {
            // The only reason a picked sound cannot be added: say so, rather than leaving Add greyed
            // out with nothing to read.
            Status = HasRoom
                ? null
                : $"This behavior plays {_maxItems} sound{(_maxItems == 1 ? string.Empty : "s")}. " +
                  "Remove one to choose another.";

            OnPropertyChanged(nameof(HasRoom));
            OnPropertyChanged(nameof(HasValidCount));
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(PreviewTarget));
            AddCommand.NotifyCanExecuteChanged();
            RemoveCommand.NotifyCanExecuteChanged();
            MoveUpCommand.NotifyCanExecuteChanged();
            MoveDownCommand.NotifyCanExecuteChanged();
            PlayCommand.NotifyCanExecuteChanged();
        }
    }
}
