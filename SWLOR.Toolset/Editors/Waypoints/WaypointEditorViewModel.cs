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
    public sealed partial class WaypointEditorViewModel : ObservableObject
    {
        private readonly BehaviorValueStore _store;
        private readonly WaypointBehaviorCatalog _catalog;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Func<string, IReadOnlyList<BehaviorChoice>>? _resolveChoices;
        private readonly IGameCodeIndex? _gameCodeIndex;
        private readonly bool _isInstance;

        public ObservableCollection<BehaviorListItemViewModel> BehaviorList { get; } = new();
        public ObservableCollection<WaypointRowViewModel> BasicRows { get; } = new();
        public ObservableCollection<WaypointRowViewModel> BehaviorRows { get; } = new();

        [ObservableProperty]
        private VarTableSectionViewModel? _variables;

        [ObservableProperty]
        private WaypointBehavior _behavior;

        public string HeaderName => Behavior.DisplayName;
        public string HeaderKind => _isInstance ? "instance" : "blueprint";
        public string HeaderOwner { get; }
        public bool ShowsVariablesTab => Behavior.AllowsVariables;
        public bool NeedsSaveNormalization =>
            Behavior.Id == WaypointBehaviorCatalog.MapNoteId &&
            _store.GetInteger(BehaviorFieldStorage.Field, "MapNoteEnabled") != 1;
        public string? Incomplete { get; private set; }
        public bool IsIncomplete => Incomplete != null;

        public WaypointEditorViewModel(
            JsonGffStruct waypoint,
            string headerOwner,
            bool isInstance,
            Func<string, Action, bool> runEdit,
            WaypointBehaviorCatalog catalog,
            IGameCodeIndex? gameCodeIndex = null,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveChoices = null)
        {
            ArgumentNullException.ThrowIfNull(waypoint);
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _store = new BehaviorValueStore(waypoint);
            _runEdit = runEdit;
            _gameCodeIndex = gameCodeIndex;
            _resolveChoices = resolveChoices;
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

            var previous = Behavior;

            // Entering Custom clears nothing. Custom is the raw editor for these very fields, so
            // wiping them on the way in leaves the panel that exists to expose the configuration
            // opening with the configuration erased - a Map Note switched to Custom lost its text,
            // HasMapNote, MapNoteEnabled and appearance, and a Point Ambience lost its Volume,
            // Interval, PitchVariation, MaxDistance, Elevation and Times. Nothing is replacing any
            // of it either, which is what makes the clear pure loss rather than a swap.
            var entersRawEditing = behavior.AllowsVariables;

            var applied = _runEdit($"Set behavior to {behavior.DisplayName}", () =>
            {
                if (!entersRawEditing)
                    _store.Clear(previous.Manages, previous.Fields);

                foreach (var value in behavior.Manages)
                    _store.Apply(value, _isInstance);
            });

            if (!applied)
                return;

            Behavior = behavior;
            RebuildBehaviorSection();
            ReloadRowsFromDocument();
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

        public bool PrepareForSave()
        {
            if (!NeedsSaveNormalization)
                return true;

            var applied = _runEdit(
                "Enable map note on the area map",
                () => _store.SetInteger(
                    BehaviorFieldStorage.Field,
                    "MapNoteEnabled",
                    GffFieldType.Byte,
                    1));
            if (applied)
                ReloadRowsFromDocument();

            return applied;
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
            new(definition, _store, _runEdit, ResolveChoices(definition), RefreshCompleteness);

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

            Incomplete = missing.Count == 0
                ? null
                : $"{Behavior.DisplayName} still needs {string.Join(", ", missing)}.";

            OnPropertyChanged(nameof(Incomplete));
            OnPropertyChanged(nameof(IsIncomplete));
        }
    }
}
