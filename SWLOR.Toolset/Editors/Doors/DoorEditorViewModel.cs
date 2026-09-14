using System.Collections.ObjectModel;
using System.Numerics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Doors;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Editors.Triggers;
using SWLOR.Toolset.Editors.Behaviors;
using SWLOR.Toolset.Viewport;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors.Doors
{
    /// <summary>The behavior-shaped door editor shared by blueprints and placements.</summary>
    public sealed partial class DoorEditorViewModel : ObservableObject, IModelPreviewSource, IDisposable
    {
        private readonly DoorValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly IGameCodeIndex? _gameCodeIndex;
        private readonly Func<BehaviorTagScope, string, string?>? _resolveTag;
        private readonly Func<string, IReadOnlyList<BehaviorChoice>>? _resolveChoices;
        private readonly IReadOnlyList<DoorAppearanceChoice> _appearances;
        private readonly Func<JsonGffStruct, BlueprintModelRenderResult>? _resolveModel;
        private readonly ChoicePreviewService? _choicePreviews;
        private readonly bool _isInstance;
        private ModelPreviewControl? _previewView;
        private bool _disposed;

        public ObservableCollection<BehaviorListItemViewModel> BehaviorList { get; } = new();

        public ObservableCollection<DoorRowViewModel> BasicRows { get; } = new();

        public ObservableCollection<DoorRowViewModel> BehaviorRows { get; } = new();

        public Appearance.AppearanceGallerySectionViewModel Appearance { get; }

        [ObservableProperty]
        private VarTableSectionViewModel? _variables;

        [ObservableProperty]
        private DoorBehavior _behavior = DoorBehaviorCatalog.Custom;

        [ObservableProperty]
        private bool _isDirty;

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

        public ResourceIndex? ResourceIndex { get; }

        public AreaScene? PreviewScene { get; private set; }

        public string? PreviewAnimationName => null;

        public bool IsAnimationPlaying => false;

        public string AppearanceDescription => Appearance.CurrentDescription;

        public string DoorTag => _store.GetString(BehaviorFieldStorage.Field, "Tag");

        public string TemplateResRef => _store.GetString(BehaviorFieldStorage.Field, "TemplateResRef");

        public Avalonia.Controls.Control PreviewView
        {
            get
            {
                if (_previewView != null)
                    return _previewView;

                _previewView = new ModelPreviewControl { DataContext = this };
                _previewView.SetHostVisible(true);
                return _previewView;
            }
        }

        private readonly Workspace.OutputLogService? _log;


        public DoorEditorViewModel(
            JsonGffStruct door,
            string headerOwner,
            bool isInstance,
            Func<string, Action, bool> runEdit,
            IGameCodeIndex? gameCodeIndex = null,
            Func<BehaviorTagScope, string, string?>? resolveTag = null,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveChoices = null,
            IReadOnlyList<DoorAppearanceChoice>? appearances = null,
            ResourceIndex? resourceIndex = null,
            Func<JsonGffStruct, BlueprintModelRenderResult>? resolveModel = null,
            bool isDirty = false,
            ThumbnailService? thumbnails = null,
            ChoicePreviewService? choicePreviews = null,
            Services.IEditorPromptService? prompts = null,
            Workspace.OutputLogService? log = null)
        {
            ArgumentNullException.ThrowIfNull(door);
            _log = log;

            _prompts = prompts;
            _store = new DoorValueStore(door);
            _runEdit = runEdit;
            _gameCodeIndex = gameCodeIndex;
            _resolveTag = resolveTag;
            _resolveChoices = resolveChoices;
            _appearances = appearances ?? Array.Empty<DoorAppearanceChoice>();
            _resolveModel = resolveModel;
            _choicePreviews = choicePreviews;
            _isInstance = isInstance;
            HeaderOwner = headerOwner;
            ResourceIndex = resourceIndex;
            IsDirty = isDirty;
            // The same grid the creature editor picks its appearance from. Doors and creatures
            // want exactly the same thing - search a table, look at the pictures, click one - and
            // the two had arrived at it separately.
            Appearance = new Appearance.AppearanceGallerySectionViewModel(
                _appearances
                    .Select(choice => new Appearance.AppearanceOption(
                        AppearanceKey(choice),
                        choice.Display,
                        choice.Model,
                        ModelResRef: choice.Model,
                        IsDoorTransition: choice.IsDoorTransition))
                    .ToList(),
                thumbnails,
                () => _store.GetAppearance(_appearances) is { } current
                    ? AppearanceKey(current)
                    : string.Empty,
                option => ApplyAppearance(option));

            BehaviorListItemViewModel.Build(BehaviorList, DoorBehaviorCatalog.All);
            Behavior = DoorBehaviorCatalog.Classify(door);
            BuildBasicRows();
            RebuildBehaviorSection();
            UpdatePreviewScene();
        }

        [RelayCommand]
        public void ChooseBehavior(IBehaviorDescriptor? descriptor)
        {
            if (descriptor is not DoorBehavior behavior || behavior.Id == Behavior.Id)
                return;

            _ = ChooseBehaviorGuardedAsync(behavior);
        }

        /// <summary>
        /// Observes the command's fire-and-forget switch. A fault would otherwise vanish as an
        /// unobserved task while the rail stayed highlighting a behavior the document never got, so
        /// it is handled the way a declined prompt is: put the highlight back on what the door
        /// actually is.
        /// </summary>
        private async Task ChooseBehaviorGuardedAsync(DoorBehavior behavior)
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

        /// <summary>Asks before a switch throws something away. Null in tests, which never lose data.</summary>
        private readonly Services.IEditorPromptService? _prompts;

        /// <summary>
        /// The switch itself, with the confirmation in front of it when something real is being
        /// discarded.
        /// </summary>
        /// <remarks>
        /// A door is classified Custom whenever it carries locals, and those locals are frequently
        /// unrelated gameplay wiring rather than anything a door behavior owns. Choosing Area
        /// Transition or Locked Door then swept the whole VarTable with nothing said, and the loss
        /// became permanent on the next save.
        /// </remarks>
        public async Task ChooseBehaviorAsync(DoorBehavior behavior)
        {
            ArgumentNullException.ThrowIfNull(behavior);

            var previous = Behavior;
            if (behavior.Id == previous.Id)
                return;

            // Entering Custom clears nothing: Custom is the raw editor for these very fields, and
            // nothing is replacing them.
            //
            // A door is deliberately NOT on that rule. The catalog classifies a door by its locals,
            // so switching a Key Item Door to Custom is precisely how a builder unwires it - the
            // clear is the operation, not a side effect of it. What a door needed was the
            // confirmation below, for the locals the preset does not own.
            var losses = BehaviorSwitchLosses.Describe(
                _store,
                previous.Manages,
                previous.Fields,
                behavior.Manages,
                DoorValueStore.LocalsClearedBySwitchingFrom(_store, previous));

            if (losses.Count > 0 && _prompts != null)
            {
                var confirmed = await _prompts.ConfirmDestructiveAsync(
                    $"Change behavior to {behavior.DisplayName}?",
                    $"This clears {Describe(losses)}, which {(losses.Count == 1 ? "is" : "are")} " +
                    $"not part of {behavior.DisplayName}. Undo will put {(losses.Count == 1 ? "it" : "them")} back " +
                    "until the door is saved.",
                    "Change behavior").ConfigureAwait(true);

                if (!confirmed)
                {
                    // Put the rail's highlight back on what the door actually is.
                    BehaviorListItemViewModel.Select(BehaviorList, previous.Id);
                    return;
                }
            }

            if (!RunEdit($"Set behavior to {behavior.DisplayName}", () =>
                {
                    _store.Clear(previous);
                    _store.Apply(behavior, _isInstance);
                }))
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

        public void ReloadFromDocument()
        {
            var classified = DoorBehaviorCatalog.Classify(_store.Door);
            if (classified.Id != Behavior.Id)
            {
                Behavior = classified;
                RebuildBehaviorSection();
            }

            ReloadRowsFromDocument();
            UpdatePreviewScene();
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

        private void RebuildChoiceRows(ObservableCollection<DoorRowViewModel> rows)
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

        public void SetDirty(bool value) => IsDirty = value;

        private bool RunEdit(string description, Action mutation)
        {
            var applied = _runEdit(description, mutation);
            if (applied)
                IsDirty = true;
            return applied;
        }

        private void BuildBasicRows()
        {
            foreach (var definition in DoorEditorLayout.Basic)
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

            UpdateConditionalRows();
            OnPropertyChanged(nameof(HeaderName));
            OnPropertyChanged(nameof(ShowsVariablesTab));
            RefreshCompleteness();
        }

        private DoorRowViewModel CreateRow(DoorFieldDefinition definition)
        {
            return new DoorRowViewModel(
                definition,
                _store,
                RunEdit,
                _resolveTag,
                ApplyDerivedMutation,
                OnRowChanged,
                ResolveChoices(definition),
                _gameCodeIndex?.KeyItems,
                _choicePreviews);
        }

        private IReadOnlyList<BehaviorChoice> ResolveChoices(DoorFieldDefinition definition)
        {
            if (definition.ChoicesKey == null)
                return definition.Choices;

            return _resolveChoices?.Invoke(definition.ChoicesKey) ?? Array.Empty<BehaviorChoice>();
        }

        private void ApplyDerivedMutation(DoorFieldDefinition definition)
        {
            if (definition.Name == "Locked" &&
                _store.GetInteger(BehaviorFieldStorage.Field, "Locked") != 1)
            {
                _store.ClearConditionalLockFields(Behavior.Fields);
            }

            if (definition.Name == "KeyName")
                _store.UpdateKeyRequired();
        }

        private void OnRowChanged(DoorRowViewModel row)
        {
            if (row.Definition.Name == "Locked")
            {
                UpdateConditionalRows();
                foreach (var conditional in BehaviorRows.Where(candidate =>
                             candidate.Definition.VisibleWhenField == "Locked"))
                {
                    conditional.Reload();
                }
            }

            foreach (var candidate in BasicRows.Concat(BehaviorRows))
                candidate.RefreshStatus();

            OnPropertyChanged(nameof(AppearanceDescription));
            OnPropertyChanged(nameof(DoorTag));
            OnPropertyChanged(nameof(TemplateResRef));
            RefreshCompleteness();
        }

        private void UpdateConditionalRows()
        {
            foreach (var row in BehaviorRows)
            {
                row.IsVisible = row.Definition.VisibleWhenField == null ||
                    _store.GetInteger(BehaviorFieldStorage.Field, row.Definition.VisibleWhenField) ==
                    row.Definition.VisibleWhenValue;
            }
        }

        private void ReloadRowsFromDocument()
        {
            foreach (var row in BasicRows.Concat(BehaviorRows))
                row.Reload();

            Appearance.ReloadFromDocument();
            UpdateConditionalRows();
            Variables?.RefreshFromDocument();
            foreach (var row in BasicRows.Concat(BehaviorRows))
                row.RefreshStatus();

            OnPropertyChanged(nameof(AppearanceDescription));
            OnPropertyChanged(nameof(DoorTag));
            OnPropertyChanged(nameof(TemplateResRef));
            RefreshCompleteness();
        }

        /// <summary>
        /// Identity for an appearance row. The kind has to be part of it: generic row 12 and
        /// specific row 12 are different doors, and they are stored in different fields.
        /// </summary>
        private static string AppearanceKey(DoorAppearanceChoice choice) =>
            $"{choice.Kind}:{choice.Id}";

        private bool ApplyAppearance(Appearance.AppearanceOption option)
        {
            var choice = _appearances.FirstOrDefault(
                candidate => AppearanceKey(candidate) == option.Key);
            if (choice == null)
                return false;

            if (!RunEdit($"Change appearance to {choice.Display}", () => _store.SetAppearance(choice)))
                return false;

            OnAppearanceChanged();
            return true;
        }

        private void OnAppearanceChanged()
        {
            UpdatePreviewScene();
            OnPropertyChanged(nameof(AppearanceDescription));
        }

        private void RefreshCompleteness()
        {
            var missing = BehaviorRows
                .Where(row => row.IsVisible && row.IsRequired && !row.HasValue)
                .Select(row => row.Label)
                .ToList();

            Incomplete = missing.Count == 0
                ? null
                : $"{Behavior.DisplayName} still needs {string.Join(", ", missing)}.";
            OnPropertyChanged(nameof(Incomplete));
            OnPropertyChanged(nameof(IsIncomplete));
        }

        private void UpdatePreviewScene()
        {
            if (_disposed)
                return;

            var preview = _resolveModel?.Invoke(_store.Door) ?? default;
            PreviewScene = preview.Model == null && !preview.IsDoorTransition
                ? null
                : new AreaScene
                {
                    Tileset = string.Empty,
                    Width = 1,
                    Height = 1,
                    Tiles = Array.Empty<TilePlacement>(),
                    Instances = new[]
                    {
                        new InstanceMarker
                        {
                            Kind = InstanceMarkerKind.Door,
                            TemplateResRef = TemplateResRef,
                            Tag = DoorTag,
                            Position = new Vector3(
                                AreaSceneBuilder.TileSize / 2f,
                                AreaSceneBuilder.TileSize / 2f,
                                0f),
                            Orientation = new Vector2(1f, 0f),
                            Model = preview.Model,
                            IsDoorTransition = preview.IsDoorTransition
                        }
                    },
                    Diagnostics = new AreaSceneDiagnostics()
                };

            OnPropertyChanged(nameof(PreviewScene));
        }

        public void ReloadGameResources()
        {
            Appearance.ReloadPreviews();
            UpdatePreviewScene();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            foreach (var row in BasicRows.Concat(BehaviorRows))
                row.Dispose();
            Appearance.Dispose();
            _previewView?.Dispose();
            _previewView = null;
            PreviewScene = null;
        }
    }
}
