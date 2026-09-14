using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Waypoints;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Editors.Behaviors;

namespace SWLOR.Toolset.Editors.Waypoints
{
    public sealed partial class WaypointEditorViewModel : ObservableObject, IDisposable
    {
        private readonly BehaviorValueStore _store;
        private WaypointBehaviorCatalog _catalog;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Func<string, IReadOnlyList<BehaviorChoice>>? _resolveChoices;
        private readonly IGameCodeIndex? _gameCodeIndex;
        private readonly ChoicePreviewService? _previews;
        private readonly Services.IEditorPromptService? _prompts;
        private readonly Func<string, bool>? _singletonTagInUse;
        private readonly bool _isInstance;
        private bool _disposed;

        public ObservableCollection<BehaviorListItemViewModel> BehaviorList { get; } = new();
        public ObservableCollection<WaypointRowViewModel> BasicRows { get; } = new();
        public ObservableCollection<WaypointRowViewModel> BehaviorRows { get; } = new();

        [ObservableProperty]
        private VarTableSectionViewModel? _variables;

        [ObservableProperty]
        private WaypointBehavior _behavior;

        public string HeaderName => Behavior.DisplayName;
        public string HeaderKind => _isInstance ? "instance" : "blueprint";
        public string HeaderOwner { get; private set; }

        public void SetHeaderOwner(string value)
        {
            HeaderOwner = value;
            OnPropertyChanged(nameof(HeaderOwner));
        }
        public bool ShowsVariablesTab => Behavior.AllowsVariables;
        public bool NeedsSaveNormalization =>
            Behavior.Manages.Any(value => !_store.Matches(value, _isInstance)) ||
            !HasExpectedPersistedBehavior();
        public string? Incomplete { get; private set; }
        public bool IsIncomplete => Incomplete != null;

        private readonly Workspace.OutputLogService? _log;


        public WaypointEditorViewModel(
            JsonGffStruct waypoint,
            string headerOwner,
            bool isInstance,
            Func<string, Action, bool> runEdit,
            WaypointBehaviorCatalog catalog,
            IGameCodeIndex? gameCodeIndex = null,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveChoices = null,
            ChoicePreviewService? previews = null,
            Services.IEditorPromptService? prompts = null,
            Func<string, bool>? singletonTagInUse = null,
            Workspace.OutputLogService? log = null)
        {
            ArgumentNullException.ThrowIfNull(waypoint);
            _log = log;
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _store = new BehaviorValueStore(waypoint);
            _runEdit = runEdit;
            _gameCodeIndex = gameCodeIndex;
            _resolveChoices = resolveChoices;
            _previews = previews;
            _prompts = prompts;
            _singletonTagInUse = singletonTagInUse;
            _isInstance = isInstance;
            HeaderOwner = headerOwner;
            _behavior = _catalog.Classify(waypoint);

            BehaviorListItemViewModel.Build(BehaviorList, _catalog.All);
            BuildBasicRows();
            RebuildBehaviorSection();
        }

        [RelayCommand]
        public void ChooseBehavior(IBehaviorDescriptor? descriptor)
        {
            if (descriptor is not WaypointBehavior behavior || behavior.Id == Behavior.Id)
                return;

            _ = ChooseBehaviorGuardedAsync(behavior);
        }

        /// <summary>
        /// Observes the command's fire-and-forget switch. A fault would otherwise vanish as an
        /// unobserved task while the rail stayed highlighting a behavior the document never got, so
        /// it is handled the way a declined prompt is: put the highlight back on what the waypoint
        /// actually is.
        /// </summary>
        private async Task ChooseBehaviorGuardedAsync(WaypointBehavior behavior)
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
        /// Switches behavior after confirming any custom fields the incoming preset would clear.
        /// </summary>
        public async Task ChooseBehaviorAsync(WaypointBehavior behavior)
        {
            ArgumentNullException.ThrowIfNull(behavior);

            var previous = Behavior;
            if (behavior.Id == previous.Id)
                return;

            // Entering Custom clears nothing. Custom is the raw editor for these very fields, so
            // wiping them on the way in leaves the panel that exists to expose the configuration
            // opening with the configuration erased - a Map Note switched to Custom lost its text,
            // HasMapNote, MapNoteEnabled and appearance, and a Point Ambience lost its Volume,
            // Interval, PitchVariation, MaxDistance, Elevation and Times. Nothing is replacing any
            // of it either, which is what makes the clear pure loss rather than a swap.
            var entersRawEditing = behavior.AllowsVariables;

            var losses = entersRawEditing
                ? Array.Empty<string>()
                : BehaviorSwitchLosses.Describe(
                    _store, previous.Manages, previous.Fields, behavior.Manages);

            if (losses.Count > 0 && _prompts != null)
            {
                var confirmed = await _prompts.ConfirmDestructiveAsync(
                    $"Change behavior to {behavior.DisplayName}?",
                    $"This clears {Describe(losses)}, which {(losses.Count == 1 ? "is" : "are")} " +
                    $"not part of {behavior.DisplayName}. Undo will put {(losses.Count == 1 ? "it" : "them")} back " +
                    "until the waypoint is saved.",
                    "Change behavior").ConfigureAwait(true);

                if (!confirmed)
                {
                    BehaviorListItemViewModel.Select(BehaviorList, previous.Id);
                    return;
                }
            }

            var applied = _runEdit($"Set behavior to {behavior.DisplayName}", () =>
            {
                if (!entersRawEditing)
                    _store.Clear(previous.Manages, previous.Fields);

                foreach (var value in behavior.Manages)
                    _store.Apply(value, _isInstance);

                PersistBehavior(behavior);
            });

            if (!applied)
            {
                BehaviorListItemViewModel.Select(BehaviorList, previous.Id);
                return;
            }

            Behavior = behavior;
            RebuildBehaviorSection();
            ReloadRowsFromDocument();
        }

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
            var classified = _catalog.Classify(_store.ValueStruct);
            if (classified.Id != Behavior.Id)
            {
                Behavior = classified;
                RebuildBehaviorSection();
            }

            ReloadRowsFromDocument();
        }

        /// <summary>
        /// Replaces the module-derived transition classifier and immediately reclassifies the live
        /// waypoint without editing it. The transition index changes when a door or trigger link is
        /// saved externally or from another open tab, so retaining the catalog captured at open time
        /// can leave an inbound-only destination displayed as Transition Destination after its last
        /// link has been removed.
        /// </summary>
        public void RefreshCatalog(WaypointBehaviorCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            BehaviorListItemViewModel.Build(BehaviorList, _catalog.All);
            Behavior = _catalog.Classify(_store.ValueStruct);
            RebuildBehaviorSection();
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

        /// <summary>Rebuilds every materialized choice row after TLK-backed labels change.</summary>
        public void RefreshTlkLabels()
        {
            RebuildChoiceRows(BasicRows);
            RebuildChoiceRows(BehaviorRows);
            RefreshCompleteness();
        }

        private void RebuildChoiceRows(ObservableCollection<WaypointRowViewModel> rows)
        {
            for (var index = 0; index < rows.Count; index++)
            {
                if (rows[index].Definition.ChoicesKey == null)
                    continue;

                var definition = rows[index].Definition;
                rows[index].Dispose();
                rows[index] = CreateRow(definition);
            }
        }

        public bool PrepareForSave()
        {
            RefreshCompleteness();
            if (HasSingletonTagConflict())
                return false;

            if (!NeedsSaveNormalization)
                return true;

            var applied = _runEdit(
                $"Normalize {Behavior.DisplayName} waypoint behavior",
                () =>
                {
                    foreach (var value in Behavior.Manages)
                        _store.Apply(value, _isInstance);

                    PersistBehavior(Behavior);
                });
            if (applied)
                ReloadRowsFromDocument();

            return applied;
        }

        private bool HasExpectedPersistedBehavior()
        {
            var persisted = _store.Locals.GetString(WaypointBehaviorCatalog.PersistedBehaviorLocal);
            return Behavior.Id == WaypointBehaviorCatalog.TransitionDestinationId
                ? string.Equals(
                    persisted,
                    WaypointBehaviorCatalog.TransitionDestinationId,
                    StringComparison.Ordinal)
                : persisted == null;
        }

        private void PersistBehavior(WaypointBehavior behavior)
        {
            if (behavior.Id == WaypointBehaviorCatalog.TransitionDestinationId)
            {
                _store.Locals.SetString(
                    WaypointBehaviorCatalog.PersistedBehaviorLocal,
                    WaypointBehaviorCatalog.TransitionDestinationId);
            }
            else
            {
                _store.Locals.Remove(WaypointBehaviorCatalog.PersistedBehaviorLocal);
            }
        }

        private void ReloadRowsFromDocument()
        {
            foreach (var row in BasicRows.Concat(BehaviorRows))
                row.Reload();

            Variables?.RefreshFromDocument();
            RefreshCompleteness();
        }

        private void BuildBasicRows()
        {
            foreach (var definition in WaypointEditorLayout.Basic)
                BasicRows.Add(CreateRow(definition));
        }

        private WaypointRowViewModel CreateRow(BehaviorFieldDefinition definition) =>
            new(definition, _store, _runEdit, ResolveChoices(definition), RefreshCompleteness,
                _previews);

        private IReadOnlyList<BehaviorChoice> ResolveChoices(BehaviorFieldDefinition definition)
        {
            if (definition.ChoicesKey == null)
                return definition.Choices;

            return _resolveChoices?.Invoke(definition.ChoicesKey) ?? Array.Empty<BehaviorChoice>();
        }

        private void RebuildBehaviorSection()
        {
            foreach (var row in BehaviorRows)
                row.Dispose();

            BehaviorRows.Clear();
            foreach (var definition in Behavior.Fields)
                BehaviorRows.Add(CreateRow(definition));

            Variables = Behavior.AllowsVariables
                ? new VarTableSectionViewModel(_runEdit, _store.Locals, _gameCodeIndex)
                : null;

            BehaviorListItemViewModel.Select(BehaviorList, Behavior.Id);

            OnPropertyChanged(nameof(HeaderName));
            OnPropertyChanged(nameof(ShowsVariablesTab));
            RefreshCompleteness();
        }

        private void RefreshCompleteness()
        {
            var missing = BehaviorRows
                .Where(row => row.IsRequired && row.IsEmpty)
                .Select(row => row.Label)
                .ToList();

            Incomplete = HasSingletonTagConflict()
                ? "This destination tag is already used by another placed waypoint. Choose a unique destination."
                : missing.Count == 0
                    ? null
                    : $"{Behavior.DisplayName} still needs {string.Join(", ", missing)}.";

            OnPropertyChanged(nameof(Incomplete));
            OnPropertyChanged(nameof(IsIncomplete));
        }

        private bool HasSingletonTagConflict()
        {
            var tag = _store.GetString(BehaviorFieldStorage.Field, "Tag");
            return _catalog.IsSingletonDestinationTag(tag) &&
                   _singletonTagInUse?.Invoke(tag) == true;
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
