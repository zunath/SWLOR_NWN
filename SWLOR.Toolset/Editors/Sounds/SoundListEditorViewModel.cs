using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Editors.Sounds;

namespace SWLOR.Toolset.Editors.Sounds
{
    /// <summary>Adds, removes and reorders the Sounds GFF list.</summary>
    public sealed partial class SoundListEditorViewModel : ObservableObject
    {
        private readonly SoundValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Action _changed;
        private readonly int _maxItems;

        public ObservableCollection<SoundListEntryViewModel> Rows { get; } = new();

        public IReadOnlyList<string> AvailableSounds { get; }

        public bool HasAudioCatalog => AvailableSounds.Count > 0;

        public bool HasRoom => _maxItems == 0 || Rows.Count < _maxItems;

        public bool HasValidCount => Rows.Count > 0 && (_maxItems == 0 || Rows.Count <= _maxItems);

        public bool HasSelection => SelectedEntry != null;

        [ObservableProperty]
        private SoundListEntryViewModel? _selectedEntry;

        [ObservableProperty]
        private string _candidate = string.Empty;

        [ObservableProperty]
        private string? _status;

        public SoundListEditorViewModel(
            SoundValueStore store,
            Func<string, Action, bool> runEdit,
            IReadOnlyList<string> availableSounds,
            int maxItems,
            Action changed)
        {
            _store = store;
            _runEdit = runEdit;
            AvailableSounds = availableSounds;
            _maxItems = maxItems;
            _changed = changed;
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
            var candidate = Candidate.Trim();
            return HasRoom
                   && candidate.Length > 0
                   && AvailableSounds.Contains(candidate, StringComparer.OrdinalIgnoreCase);
        }

        [RelayCommand(CanExecute = nameof(CanAdd))]
        private void Add()
        {
            var candidate = Candidate.Trim();
            if (!_runEdit("Add sound", () => _store.AddSound(candidate)))
                return;

            Candidate = string.Empty;
            Status = null;
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

        partial void OnCandidateChanged(string value)
        {
            Status = value.Length == 0 || AvailableSounds.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase)
                ? null
                : "Choose an audio resource from the index.";
            AddCommand.NotifyCanExecuteChanged();
        }

        partial void OnSelectedEntryChanged(SoundListEntryViewModel? value) => NotifyState();

        private void NotifyState()
        {
            OnPropertyChanged(nameof(HasRoom));
            OnPropertyChanged(nameof(HasValidCount));
            OnPropertyChanged(nameof(HasSelection));
            AddCommand.NotifyCanExecuteChanged();
            RemoveCommand.NotifyCanExecuteChanged();
            MoveUpCommand.NotifyCanExecuteChanged();
            MoveDownCommand.NotifyCanExecuteChanged();
        }
    }
}
