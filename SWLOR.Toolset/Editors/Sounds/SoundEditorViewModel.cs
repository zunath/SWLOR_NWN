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
    public sealed partial class SoundEditorViewModel : ObservableObject, IDisposable
    {
        private readonly SoundValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Func<string, IReadOnlyList<BehaviorChoice>>? _resolveChoices;
        private readonly IReadOnlyList<string> _audioResources;
        private readonly Services.SoundPreviewService? _preview;
        private readonly IGameCodeIndex? _gameCodeIndex;

        /// <summary>Asks before a switch throws something away. Null in tests, which never lose data.</summary>
        private readonly Services.IEditorPromptService? _prompts;
        private readonly bool _isInstance;
        private bool _disposed;

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

        public string HeaderOwner { get; private set; }

        public void SetHeaderOwner(string value)
        {
            HeaderOwner = value;
            OnPropertyChanged(nameof(HeaderOwner));
        }

        public bool ShowsVariablesTab => Behavior.AllowsVariables;

        public string? Incomplete { get; private set; }

        public bool IsIncomplete => Incomplete != null;

        public event Action? ValueChanged;

        private readonly Workspace.OutputLogService? _log;


        public SoundEditorViewModel(
            JsonGffStruct sound,
            string headerOwner,
            bool isInstance,
            Func<string, Action, bool> runEdit,
            IGameCodeIndex? gameCodeIndex = null,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveChoices = null,
            IReadOnlyList<string>? audioResources = null,
            Services.SoundPreviewService? preview = null,
            Services.IEditorPromptService? prompts = null,
            Workspace.OutputLogService? log = null)
        {
            ArgumentNullException.ThrowIfNull(sound);
            _log = log;

            _store = new SoundValueStore(sound);
            _runEdit = runEdit;
            _gameCodeIndex = gameCodeIndex;
            _resolveChoices = resolveChoices;
            _audioResources = audioResources ?? Array.Empty<string>();
            _preview = preview;
            _prompts = prompts;
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

            _ = ChooseBehaviorGuardedAsync(behavior);
        }

        /// <summary>
        /// Observes the command's fire-and-forget switch. A fault would otherwise vanish as an
        /// unobserved task while the rail stayed highlighting a behavior the document never got, so
        /// it is handled the way a declined prompt is: put the highlight back on what the sound
        /// actually is.
        /// </summary>
        private async Task ChooseBehaviorGuardedAsync(SoundBehavior behavior)
        {
            try
            {
                await ChooseBehaviorAsync(behavior).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                _log?.AppendLine(
                    $"Behavior switch to '{behavior.DisplayName}' failed: {ex.Message}");
                BehaviorListItemViewModel.Select(BehaviorList, Behavior.Id);
            }
        }

        /// <summary>
        /// The switch itself, with the confirmation in front of it when something real is being
        /// discarded — the same flow as the door, trigger and waypoint editors, which asked while
        /// this editor cleared silently.
        /// </summary>
        public async Task ChooseBehaviorAsync(SoundBehavior behavior)
        {
            ArgumentNullException.ThrowIfNull(behavior);

            var previous = Behavior;
            if (behavior.Id == previous.Id)
                return;

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

            // Mirrors SoundValueStore.Clear: a field the incoming preset also owns as an editable
            // slot is kept rather than cleared, so it must not be named as a loss. Leaving Custom
            // keeps nothing.
            var keptByIncoming = new HashSet<string>(
                behavior.Fields.Select(field => field.Name), StringComparer.Ordinal);
            var losses = entersRawEditing
                ? Array.Empty<string>()
                : BehaviorSwitchLosses.Describe(
                    _store,
                    previous.Manages,
                    previous.AllowsVariables
                        ? previous.Fields
                        : previous.Fields.Where(field => !keptByIncoming.Contains(field.Name)),
                    behavior.Manages);

            // Dropped sound entries are as much a loss as cleared fields: a loop switch that
            // truncates the playlist must ask first even when no field is cleared.
            var clauses = new List<string>();
            if (losses.Count > 0)
            {
                clauses.Add(
                    $"clears {Describe(losses)}, which {(losses.Count == 1 ? "is" : "are")} " +
                    $"not part of {behavior.DisplayName}");
            }
            if (droppedSounds.Count > 0)
            {
                clauses.Add(
                    $"drops {Describe(droppedSounds)} because a loop plays a single sound");
            }

            if (clauses.Count > 0 && _prompts != null)
            {
                var confirmed = await _prompts.ConfirmDestructiveAsync(
                    $"Change behavior to {behavior.DisplayName}?",
                    $"This {string.Join(" and ", clauses)}. Undo will put everything back " +
                    "until the sound is saved.",
                    "Change behavior").ConfigureAwait(true);

                if (!confirmed)
                {
                    // Put the rail's highlight back on what the sound actually is.
                    BehaviorListItemViewModel.Select(BehaviorList, previous.Id);
                    return;
                }
            }

            var applied = RunEdit($"Set behavior to {behavior.DisplayName}", () =>
            {
                if (!entersRawEditing)
                    _store.Clear(previous, behavior);

                foreach (var value in behavior.Manages)
                    _store.Apply(value, _isInstance);

                _store.ReplaceSounds(keptSounds);
            });
            if (!applied)
            {
                BehaviorListItemViewModel.Select(BehaviorList, previous.Id);
                return;
            }

            Behavior = behavior;
            BehaviorChangeNotice = droppedSounds.Count == 0
                ? null
                : $"Kept {keptSounds[0]}; dropped {string.Join(", ", droppedSounds)} because a loop uses one sound.";
            RebuildBehaviorSection();
            ReloadRowsFromDocument();
        }

        /// <summary>Names the discarded slots in prose, capped so the prompt stays readable.</summary>
        private static string Describe(IReadOnlyList<string> losses)
        {
            const int shown = 6;
            var named = string.Join(", ", losses.Take(shown));
            return losses.Count <= shown
                ? named
                : $"{named} and {losses.Count - shown} more";
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
                RefreshCompleteness,
                _preview);

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

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            foreach (var row in BasicRows.Concat(BehaviorRows))
                row.Dispose();
        }
    }
}
