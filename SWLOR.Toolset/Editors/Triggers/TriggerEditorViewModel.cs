using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Triggers;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Editors.Behaviors;

namespace SWLOR.Toolset.Editors.Triggers
{
    /// <summary>
    /// The trigger editor, shared by blueprints and placements. Both are a
    /// <see cref="JsonGffStruct"/> carrying the same fields, so the only differences are the header
    /// and which rows are marked as belonging to one placement.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Behavior tab is the centre of it: pick what the trigger is for and its own fields appear,
    /// alongside the raw values it writes on your behalf. Swapping behavior clears what the previous
    /// one owned before applying the new one, so a trigger never keeps a script it no longer runs.
    /// </para>
    /// <para>
    /// Local variables are reachable under <b>Custom</b> alone. Every other behavior exposes the
    /// locals it needs as named fields — a message, a quest id — so there is never a second place to
    /// set the same value, and no way to desynchronise the two.
    /// </para>
    /// </remarks>
    public sealed partial class TriggerEditorViewModel : ObservableObject, IDisposable
    {
        private readonly BehaviorValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Func<BehaviorTagScope, string, string?>? _resolveTag;
        private readonly Func<string, IReadOnlyList<BehaviorChoice>>? _resolveChoices;
        private readonly ChoicePreviewService? _previews;
        private readonly IGameCodeIndex? _gameCodeIndex;
        private readonly bool _isInstance;
        private bool _disposed;

        public ObservableCollection<BehaviorListItemViewModel> BehaviorList { get; } = new();

        public ObservableCollection<TriggerRowViewModel> BasicRows { get; } = new();

        public ObservableCollection<TriggerRowViewModel> BehaviorRows { get; } = new();

        /// <summary>The raw local-variable grid. Present only while the behavior is Custom.</summary>
        [ObservableProperty]
        private VarTableSectionViewModel? _variables;

        [ObservableProperty]
        private TriggerBehavior _behavior = TriggerBehaviorCatalog.Custom;

        /// <summary>Header: the behavior's name, which is what the trigger actually is.</summary>
        public string HeaderName => Behavior.DisplayName;

        /// <summary>Header: "blueprint" or "instance".</summary>
        public string HeaderKind => _isInstance ? "instance" : "blueprint";

        /// <summary>Header: the file this trigger lives in — its own resref, or its area's.</summary>
        public string HeaderOwner { get; private set; }

        public void SetHeaderOwner(string value)
        {
            HeaderOwner = value;
            OnPropertyChanged(nameof(HeaderOwner));
        }

        public bool ShowsVariablesTab => Behavior.AllowsVariables;

        /// <summary>Everything the behavior needs but has not been given, for the footer warning.</summary>
        public string? Incomplete { get; private set; }

        public bool IsIncomplete => Incomplete != null;

        private readonly Workspace.OutputLogService? _log;


        public TriggerEditorViewModel(
            JsonGffStruct trigger,
            string headerOwner,
            bool isInstance,
            Func<string, Action, bool> runEdit,
            IGameCodeIndex? gameCodeIndex = null,
            Func<BehaviorTagScope, string, string?>? resolveTag = null,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveChoices = null,
            ChoicePreviewService? previews = null,
            Services.IEditorPromptService? prompts = null,
            Workspace.OutputLogService? log = null)
        {
            ArgumentNullException.ThrowIfNull(trigger);
            _log = log;

            _prompts = prompts;
            _store = new BehaviorValueStore(trigger);
            _runEdit = runEdit;
            _gameCodeIndex = gameCodeIndex;
            _resolveTag = resolveTag;
            _resolveChoices = resolveChoices;
            _previews = previews;
            _isInstance = isInstance;
            HeaderOwner = headerOwner;

            BehaviorListItemViewModel.Build(BehaviorList, TriggerBehaviorCatalog.All);
            Behavior = TriggerBehaviorCatalog.Classify(trigger);
            BuildBasicRows();
            RebuildBehaviorSection();
        }

        /// <summary>Asks before a switch throws something away. Null in tests, which never lose data.</summary>
        private readonly Services.IEditorPromptService? _prompts;

        /// <summary>
        /// Switches behavior: clear what the old one owned, then write what the new one manages, as
        /// one undo step so a mis-click is one Ctrl+Z rather than several.
        /// </summary>
        [RelayCommand]
        public void ChooseBehavior(IBehaviorDescriptor? descriptor)
        {
            if (descriptor is not TriggerBehavior behavior || behavior.Id == Behavior.Id)
                return;

            _ = ChooseBehaviorGuardedAsync(behavior);
        }

        /// <summary>
        /// Observes the command's fire-and-forget switch. A fault would otherwise vanish as an
        /// unobserved task while the rail stayed highlighting a behavior the document never got, so
        /// it is handled the way a declined prompt is: put the highlight back on what the trigger
        /// actually is.
        /// </summary>
        private async Task ChooseBehaviorGuardedAsync(TriggerBehavior behavior)
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
        /// discarded.
        /// </summary>
        /// <remarks>
        /// Custom is the case that needed this. Its <c>Fields</c> are every raw script slot the
        /// trigger has — heartbeat, user-defined, an enter handler nobody recognises — and the clear
        /// below removes all of them before the preset writes its two. Nothing warned, and the loss
        /// only became visible after the document was saved.
        /// </remarks>
        public async Task ChooseBehaviorAsync(TriggerBehavior behavior)
        {
            ArgumentNullException.ThrowIfNull(behavior);

            var previous = Behavior;
            if (behavior.Id == previous.Id)
                return;

            // Entering Custom clears nothing: Custom is the raw editor for these very fields, and
            // nothing is replacing them. See the waypoint and sound editors for the same rule.
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
                    "until the trigger is saved.",
                    "Change behavior").ConfigureAwait(true);

                if (!confirmed)
                {
                    // Put the rail's highlight back on what the trigger actually is.
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

        /// <summary>Names the discarded slots in prose, capped so the prompt stays readable.</summary>
        private static string Describe(IReadOnlyList<string> losses)
        {
            const int shown = 6;
            var named = string.Join(", ", losses.Take(shown));
            return losses.Count <= shown
                ? named
                : $"{named} and {losses.Count - shown} more";
        }

        /// <summary>
        /// Re-reads everything from the document, after a revert, an undo/redo or an external
        /// reload — including which behavior the document now describes. Reverting a behavior swap
        /// puts the fields back, so the editor has to follow them; otherwise Revert left the old
        /// behavior's form on screen over the restored document, claiming a behavior the trigger no
        /// longer had.
        /// </summary>
        public void ReloadFromDocument()
        {
            var classified = TriggerBehaviorCatalog.Classify(_store.ValueStruct);
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

        private void ReloadRowsFromDocument()
        {
            foreach (var row in BasicRows.Concat(BehaviorRows))
                row.Reload();

            Variables?.RefreshFromDocument();
            RefreshCompleteness();
        }

        private void BuildBasicRows()
        {
            foreach (var definition in TriggerEditorLayout.Basic)
                BasicRows.Add(CreateRow(definition));
        }

        private TriggerRowViewModel CreateRow(BehaviorFieldDefinition definition) =>
            new(definition, _store, _runEdit, _resolveTag, ResolveChoices(definition), _previews,
                RefreshAfterValueChange);

        private void RefreshAfterValueChange()
        {
            RefreshCompleteness();
            foreach (var row in BehaviorRows)
                row.RefreshStatus();
        }

        /// <summary>
        /// A row's choices, from game data when it names a key. An unresolvable key yields an empty
        /// list, which the row shows as an empty picker rather than as invented values.
        /// </summary>
        private IReadOnlyList<BehaviorChoice> ResolveChoices(BehaviorFieldDefinition definition)
        {
            if (definition.ChoicesKey == null)
                return definition.Choices;

            return _resolveChoices?.Invoke(definition.ChoicesKey) ?? Array.Empty<BehaviorChoice>();
        }

        private void RebuildBehaviorSection()
        {
            // A row can be holding a pending gallery search; dropping it without saying so leaves
            // that timer to fire against a form nobody is looking at.
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

        /// <summary>
        /// Names what the behavior still needs. Stated rather than blocked: a half-configured trigger
        /// is a normal step on the way to a finished one, and refusing to save it helps nobody.
        /// </summary>
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
