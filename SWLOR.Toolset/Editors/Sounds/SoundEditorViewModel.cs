using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Sounds;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Editors.Behaviors;

namespace SWLOR.Toolset.Editors.Sounds
{
    /// <summary>The behavior editor shared by ambient-sound blueprints and area placements.</summary>
    public sealed partial class SoundEditorViewModel : ObservableObject
    {
        private readonly SoundValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Func<string, IReadOnlyList<BehaviorChoice>>? _resolveChoices;
        private readonly IReadOnlyList<string> _audioResources;
        private readonly IGameCodeIndex? _gameCodeIndex;
        private readonly bool _isInstance;

        public ObservableCollection<BehaviorListItemViewModel> BehaviorList { get; } = new();

        public ObservableCollection<SoundRowViewModel> BasicRows { get; } = new();

        public ObservableCollection<SoundRowViewModel> BehaviorRows { get; } = new();

        [ObservableProperty]
        private VarTableSectionViewModel? _variables;

        [ObservableProperty]
        private SoundBehavior _behavior = SoundBehaviorCatalog.Custom;

        [ObservableProperty]
        private string? _behaviorChangeNotice;

        public string HeaderName => Behavior.DisplayName;

        public string HeaderKind => _isInstance ? "instance" : "blueprint";

        public string HeaderOwner { get; }

        public bool ShowsVariablesTab => Behavior.AllowsVariables;

        public string? Incomplete { get; private set; }

        public bool IsIncomplete => Incomplete != null;

        public event Action? ValueChanged;

        public SoundEditorViewModel(
            JsonGffStruct sound,
            string headerOwner,
            bool isInstance,
            Func<string, Action, bool> runEdit,
            IGameCodeIndex? gameCodeIndex = null,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveChoices = null,
            IReadOnlyList<string>? audioResources = null)
        {
            ArgumentNullException.ThrowIfNull(sound);

            _store = new SoundValueStore(sound);
            _runEdit = runEdit;
            _gameCodeIndex = gameCodeIndex;
            _resolveChoices = resolveChoices;
            _audioResources = audioResources ?? Array.Empty<string>();
            _isInstance = isInstance;
            HeaderOwner = headerOwner;

            BehaviorListItemViewModel.Build(BehaviorList, SoundBehaviorCatalog.All);
            Behavior = SoundBehaviorCatalog.Classify(sound);
            BuildBasicRows();
            RebuildBehaviorSection();
        }

        [RelayCommand]
        public void ChooseBehavior(IBehaviorDescriptor? descriptor)
        {
            if (descriptor is not SoundBehavior behavior || behavior.Id == Behavior.Id)
                return;

            var previous = Behavior;
            var sounds = _store.GetSounds().ToList();
            var keptSounds = behavior.IsLoop ? sounds.Take(1).ToList() : sounds;
            var droppedSounds = behavior.IsLoop ? sounds.Skip(1).ToList() : new List<string>();

            // Entering Custom clears nothing. Custom is the raw editor for these very fields, so
            // wiping them on the way in leaves the panel that exists to expose the configuration
            // opening with the configuration erased - a Map Note switched to Custom lost its text,
            // HasMapNote, MapNoteEnabled and appearance, and a Point Ambience lost its Volume,
            // Interval, PitchVariation, MaxDistance, Elevation and Times. Nothing is replacing any
            // of it either, which is what makes the clear pure loss rather than a swap.
            var entersRawEditing = behavior.AllowsVariables;

            var applied = RunEdit($"Set behavior to {behavior.DisplayName}", () =>
            {
                if (!entersRawEditing)
                    _store.Clear(previous);

                foreach (var value in behavior.Manages)
                    _store.Apply(value, _isInstance);

                _store.ReplaceSounds(keptSounds);
            });
            if (!applied)
                return;

            Behavior = behavior;
            BehaviorChangeNotice = droppedSounds.Count == 0
                ? null
                : $"Kept {keptSounds[0]}; dropped {string.Join(", ", droppedSounds)} because a loop uses one sound.";
            RebuildBehaviorSection();
            ReloadRowsFromDocument();
        }

        public void ReloadFromDocument()
        {
            BehaviorChangeNotice = null;
            var classified = SoundBehaviorCatalog.Classify(_store.Sound);
            if (classified.Id != Behavior.Id)
            {
                Behavior = classified;
                RebuildBehaviorSection();
            }

            ReloadRowsFromDocument();
        }

        /// <summary>Rebuilds the category row after its module ITP changes.</summary>
        public void RefreshPaletteChoices()
        {
            var index = BasicRows
                .Select((row, rowIndex) => (row, rowIndex))
                .Where(item => item.row.Definition.Name == "PaletteID")
                .Select(item => item.rowIndex)
                .DefaultIfEmpty(-1)
                .Single();
            if (index < 0)
                return;

            var definition = BasicRows[index].Definition;
            BasicRows[index].Dispose();
            BasicRows[index] = CreateRow(definition);
            RefreshCompleteness();
        }

        private bool RunEdit(string description, Action mutation)
        {
            var applied = _runEdit(description, mutation);
            if (applied)
                ValueChanged?.Invoke();
            return applied;
        }

        private void BuildBasicRows()
        {
            foreach (var definition in SoundEditorLayout.Basic)
                BasicRows.Add(CreateRow(definition));
        }

        private void RebuildBehaviorSection()
        {
            foreach (var row in BehaviorRows)
                row.Dispose();

            BehaviorRows.Clear();
            foreach (var definition in Behavior.Fields)
                BehaviorRows.Add(CreateRow(definition));

            Variables = Behavior.AllowsVariables
                ? new VarTableSectionViewModel(RunEdit, _store.Locals, _gameCodeIndex)
                : null;

            BehaviorListItemViewModel.Select(BehaviorList, Behavior.Id);

            OnPropertyChanged(nameof(HeaderName));
            OnPropertyChanged(nameof(ShowsVariablesTab));
            RefreshCompleteness();
        }

        private SoundRowViewModel CreateRow(BehaviorFieldDefinition definition) =>
            new(
                definition,
                _store,
                RunEdit,
                ResolveChoices(definition),
                _audioResources,
                RefreshCompleteness);

        private IReadOnlyList<BehaviorChoice> ResolveChoices(BehaviorFieldDefinition definition)
        {
            if (definition.ChoicesKey == null)
                return definition.Choices;

            return _resolveChoices?.Invoke(definition.ChoicesKey) ?? Array.Empty<BehaviorChoice>();
        }

        private void ReloadRowsFromDocument()
        {
            foreach (var row in BasicRows.Concat(BehaviorRows))
                row.Reload();

            Variables?.RefreshFromDocument();
            RefreshCompleteness();
        }

        private void RefreshCompleteness()
        {
            var missing = BehaviorRows
                .Where(row => row.IsRequired && !row.HasValue)
                .Select(row => row.Label)
                .ToList();

            Incomplete = missing.Count == 0
                ? null
                : $"{Behavior.DisplayName} still needs {string.Join(", ", missing)}.";
            OnPropertyChanged(nameof(Incomplete));
            OnPropertyChanged(nameof(IsIncomplete));
        }
    }
}
