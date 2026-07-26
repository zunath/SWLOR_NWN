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
        private readonly Func<JsonGffStruct, RenderModel?>? _resolveModel;
        private readonly ChoicePreviewService? _choicePreviews;
        private readonly bool _isInstance;
        private ModelPreviewControl? _previewView;
        private bool _disposed;

        public ObservableCollection<DoorBehaviorListItemViewModel> BehaviorList { get; } = new();

        public ObservableCollection<DoorRowViewModel> BasicRows { get; } = new();

        public ObservableCollection<DoorRowViewModel> BehaviorRows { get; } = new();

        public DoorAppearanceSectionViewModel Appearance { get; }

        [ObservableProperty]
        private VarTableSectionViewModel? _variables;

        [ObservableProperty]
        private DoorBehavior _behavior = DoorBehaviorCatalog.Custom;

        [ObservableProperty]
        private bool _isDirty;

        public string HeaderName => Behavior.DisplayName;

        public string HeaderKind => _isInstance ? "instance" : "blueprint";

        public string HeaderOwner { get; }

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
            Func<JsonGffStruct, RenderModel?>? resolveModel = null,
            bool isDirty = false,
            ThumbnailService? thumbnails = null,
            ChoicePreviewService? choicePreviews = null)
        {
            ArgumentNullException.ThrowIfNull(door);

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
            Appearance = new DoorAppearanceSectionViewModel(
                _store,
                _appearances,
                thumbnails,
                RunEdit,
                OnAppearanceChanged);

            BuildBehaviorList();
            Behavior = DoorBehaviorCatalog.Classify(door);
            BuildBasicRows();
            RebuildBehaviorSection();
            UpdatePreviewScene();
        }

        [RelayCommand]
        public void ChooseBehavior(DoorBehavior? behavior)
        {
            if (behavior == null || behavior.Id == Behavior.Id)
                return;

            var previous = Behavior;
            if (!RunEdit($"Set behavior to {behavior.DisplayName}", () =>
                {
                    _store.Clear(previous);
                    _store.Apply(behavior, _isInstance);
                }))
            {
                return;
            }

            Behavior = behavior;
            RebuildBehaviorSection();
            ReloadRowsFromDocument();
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

        public void SetDirty(bool value) => IsDirty = value;

        private bool RunEdit(string description, Action mutation)
        {
            var applied = _runEdit(description, mutation);
            if (applied)
                IsDirty = true;
            return applied;
        }

        private void BuildBehaviorList()
        {
            string? group = null;
            foreach (var behavior in DoorBehaviorCatalog.All)
            {
                if (behavior.Group == null && group != null)
                {
                    BehaviorList.Add(DoorBehaviorListItemViewModel.Rule());
                    group = null;
                }
                else if (behavior.Group != null && !string.Equals(behavior.Group, group, StringComparison.Ordinal))
                {
                    BehaviorList.Add(DoorBehaviorListItemViewModel.Header(behavior.Group));
                    group = behavior.Group;
                }

                BehaviorList.Add(DoorBehaviorListItemViewModel.For(behavior));
            }
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

            foreach (var item in BehaviorList)
                item.IsSelected = item.Behavior?.Id == Behavior.Id;

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

            var model = _resolveModel?.Invoke(_store.Door);
            PreviewScene = model == null
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
                            Model = model
                        }
                    },
                    Diagnostics = new AreaSceneDiagnostics()
                };

            OnPropertyChanged(nameof(PreviewScene));
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
