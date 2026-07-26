using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Triggers;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.Gff;

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
    public sealed partial class TriggerEditorViewModel : ObservableObject
    {
        private readonly TriggerValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Func<string, string?>? _resolveTag;
        private readonly Func<string, IReadOnlyList<TriggerChoice>>? _resolveChoices;
        private readonly IGameCodeIndex? _gameCodeIndex;
        private readonly bool _isInstance;

        public ObservableCollection<TriggerBehaviorListItemViewModel> BehaviorList { get; } = new();

        public ObservableCollection<TriggerRowViewModel> BasicRows { get; } = new();

        public ObservableCollection<TriggerRowViewModel> BehaviorRows { get; } = new();

        public ObservableCollection<TriggerRowViewModel> AdvancedRows { get; } = new();

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
        public string HeaderOwner { get; }

        public bool ShowsVariablesTab => Behavior.AllowsVariables;

        /// <summary>Everything the behavior needs but has not been given, for the footer warning.</summary>
        public string? Incomplete { get; private set; }

        public bool IsIncomplete => Incomplete != null;

        public TriggerEditorViewModel(
            JsonGffStruct trigger,
            string headerOwner,
            bool isInstance,
            Func<string, Action, bool> runEdit,
            IGameCodeIndex? gameCodeIndex = null,
            Func<string, string?>? resolveTag = null,
            Func<string, IReadOnlyList<TriggerChoice>>? resolveChoices = null)
        {
            ArgumentNullException.ThrowIfNull(trigger);

            _store = new TriggerValueStore(trigger);
            _runEdit = runEdit;
            _gameCodeIndex = gameCodeIndex;
            _resolveTag = resolveTag;
            _resolveChoices = resolveChoices;
            _isInstance = isInstance;
            HeaderOwner = headerOwner;

            BuildBehaviorList();
            Behavior = TriggerBehaviorCatalog.Classify(trigger);
            BuildFixedRows();
            RebuildBehaviorSection();
        }

        /// <summary>
        /// Switches behavior: clear what the old one owned, then write what the new one manages, as
        /// one undo step so a mis-click is one Ctrl+Z rather than several.
        /// </summary>
        [RelayCommand]
        public void ChooseBehavior(TriggerBehavior? behavior)
        {
            if (behavior == null || behavior.Id == Behavior.Id)
                return;

            var previous = Behavior;
            var applied = _runEdit($"Set behavior to {behavior.DisplayName}", () =>
            {
                _store.Clear(previous);
                foreach (var value in behavior.Manages)
                    _store.Apply(value);
            });

            if (!applied)
                return;

            Behavior = behavior;
            RebuildBehaviorSection();
            ReloadFromDocument();
        }

        /// <summary>Re-reads every row, after an undo/redo or an external reload.</summary>
        public void ReloadFromDocument()
        {
            foreach (var row in BasicRows.Concat(BehaviorRows).Concat(AdvancedRows))
                row.Reload();

            Variables?.RefreshFromDocument();
            RefreshCompleteness();
        }

        private void BuildBehaviorList()
        {
            // An ungrouped behavior ends the run it follows rather than joining it. Custom has no
            // group, and without this it rendered under whichever heading happened to come last -
            // which is how it ended up filed as a hazard.
            string? group = null;
            foreach (var behavior in TriggerBehaviorCatalog.All)
            {
                if (behavior.Group == null && group != null)
                {
                    BehaviorList.Add(TriggerBehaviorListItemViewModel.Rule());
                    group = null;
                }
                else if (behavior.Group != null && behavior.Group != group)
                {
                    BehaviorList.Add(TriggerBehaviorListItemViewModel.Header(behavior.Group));
                    group = behavior.Group;
                }

                BehaviorList.Add(TriggerBehaviorListItemViewModel.For(behavior));
            }
        }

        private void BuildFixedRows()
        {
            foreach (var definition in TriggerEditorLayout.Basic)
                BasicRows.Add(CreateRow(definition));

            foreach (var definition in TriggerEditorLayout.Advanced)
                AdvancedRows.Add(CreateRow(definition));
        }

        private TriggerRowViewModel CreateRow(TriggerFieldDefinition definition) =>
            new(definition, _store, _runEdit, _resolveTag, ResolveChoices(definition));

        /// <summary>
        /// A row's choices, from game data when it names a key. An unresolvable key yields an empty
        /// list, which the row shows as an empty picker rather than as invented values.
        /// </summary>
        private IReadOnlyList<TriggerChoice> ResolveChoices(TriggerFieldDefinition definition)
        {
            if (definition.ChoicesKey == null)
                return definition.Choices;

            return _resolveChoices?.Invoke(definition.ChoicesKey) ?? Array.Empty<TriggerChoice>();
        }

        private void RebuildBehaviorSection()
        {
            BehaviorRows.Clear();
            foreach (var definition in Behavior.Fields)
                BehaviorRows.Add(CreateRow(definition));

            Variables = Behavior.AllowsVariables
                ? new VarTableSectionViewModel(_runEdit, _store.Locals, _gameCodeIndex)
                : null;

            foreach (var item in BehaviorList)
                item.IsSelected = item.Behavior?.Id == Behavior.Id;

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
                .Where(row => row.IsRequired && string.IsNullOrWhiteSpace(row.Text))
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
