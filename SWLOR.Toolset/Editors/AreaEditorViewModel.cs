using System.Collections.ObjectModel;
using System.Numerics;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Editors;
using SWLOR.Toolset.Domain.Editors.Schemas;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors
{
    /// <summary>
    /// The composite area editor, docked as a document tab: the area's static properties (the
    /// .are file, schema-driven like BlueprintEditorViewModel) alongside one expandable section
    /// per placed-instance list in the paired .git file (Creatures/Placeables/Doors/Waypoints/
    /// Stores/Sounds/Triggers).
    /// </summary>
    /// <remarks>
    /// Owns two independent DocumentSessions - one per file - because the .are and .git files
    /// are separate nwn_gff documents with separate undo histories. Undo/Redo is deliberately
    /// split rather than merged into one combined stack: the area-properties group gets its own
    /// small Undo/Redo pair (mirroring BlueprintEditorViewModel), while the toolbar's primary
    /// Undo/Redo acts on the .git session, since instance placement/deletion is the editing this
    /// screen is mostly used for. Save writes whichever session(s) are dirty; the title's dirty
    /// marker reflects either session being dirty.
    /// </remarks>
    public partial class AreaEditorViewModel : Document
    {
        private static readonly (string Title, string ListFieldName, ResourceType BlueprintType)[] InstanceListConfigs =
        {
            ("Creatures", "Creature List", ResourceType.Utc),
            ("Placeables", "Placeable List", ResourceType.Utp),
            ("Doors", "Door List", ResourceType.Utd),
            ("Waypoints", "WaypointList", ResourceType.Utw),
            ("Stores", "StoreList", ResourceType.Utm),
            ("Sounds", "SoundList", ResourceType.Uts),
            ("Triggers", "TriggerList", ResourceType.Utt)
        };

        private readonly DocumentSession _areSession;
        private readonly DocumentSession _gitSession;
        private readonly OutputLogService _log;
        private readonly string _areResRef;
        private readonly TilesetCatalog? _tilesetCatalog;
        private readonly TileModelCache? _tileModelCache;
        private readonly PlaceableAppearanceService? _placeableAppearances;
        private readonly DoorTypeService? _doorTypes;
        private readonly TileWalkmeshCache? _tileWalkmeshCache;
        private bool _sceneBuildRequested;

        public ObservableCollection<EditorGroup> AreaPropertyGroups { get; } = new();

        public ObservableCollection<InstanceListSectionViewModel> Sections { get; } = new();

        public bool IsDirty => _areSession.UndoStack.IsDirty || _gitSession.UndoStack.IsDirty;

        /// <summary>Resource index used by the 3D View tab to resolve mesh/tile textures; null when game data services aren't loaded.</summary>
        public ResourceIndex? ResourceIndex { get; }

        private AreaScene? _areaScene;

        /// <summary>The most recently built 3D scene for this area, or null before the first build. Rebuilt only on tab activation or an explicit <see cref="RebuildSceneCommand"/>.</summary>
        public AreaScene? AreaScene
        {
            get => _areaScene;
            private set
            {
                _areaScene = value;
                OnPropertyChanged(nameof(AreaScene));
            }
        }

        private bool _isBuildingScene;

        public bool IsBuildingScene
        {
            get => _isBuildingScene;
            private set
            {
                _isBuildingScene = value;
                OnPropertyChanged(nameof(IsBuildingScene));
            }
        }

        private string _sceneStatus = "Switch to this tab to build the 3D view.";

        public string SceneStatus
        {
            get => _sceneStatus;
            private set
            {
                _sceneStatus = value;
                OnPropertyChanged(nameof(SceneStatus));
            }
        }

        // ----- WP5.1: 3D-view <-> instance-list selection sync -----

        private bool _syncingSelection;

        private InstanceMarker? _selectedSceneInstance;

        /// <summary>
        /// The instance currently selected (from either the 3D view or an instance-list row) - the
        /// view mirrors this onto <c>GlAreaControl.SelectedInstance</c> for the 3D highlight. Always
        /// an object from the current <see cref="AreaScene"/>'s <c>Instances</c> list (or null);
        /// changes flow through <see cref="ApplySelection"/> only, so it and every section's
        /// SelectedRow never drift out of sync.
        /// </summary>
        public InstanceMarker? SelectedSceneInstance
        {
            get => _selectedSceneInstance;
            private set
            {
                if (ReferenceEquals(_selectedSceneInstance, value))
                    return;

                _selectedSceneInstance = value;
                OnPropertyChanged(nameof(SelectedSceneInstance));
                OnPropertyChanged(nameof(SelectionStatus));
            }
        }

        /// <summary>Human-readable readout of the current selection for the 3D-view status border, or empty when nothing is selected.</summary>
        public string SelectionStatus => SelectedSceneInstance is { } instance
            ? $"Selected: {instance.Kind} \"{instance.Tag}\"" +
              (string.IsNullOrEmpty(instance.TemplateResRef) ? string.Empty : $" ({instance.TemplateResRef})")
            : string.Empty;

        /// <summary>
        /// Maps a 3D-view instance's kind to the blueprint type of the section that lists it, or
        /// null when this editor has no section for that kind (Item and Encounter lists have no
        /// InstanceListSectionViewModel - see InstanceListConfigs above).
        /// </summary>
        private static ResourceType? MapKindToSectionType(InstanceMarkerKind kind) => kind switch
        {
            InstanceMarkerKind.Creature => ResourceType.Utc,
            InstanceMarkerKind.Placeable => ResourceType.Utp,
            InstanceMarkerKind.Door => ResourceType.Utd,
            InstanceMarkerKind.Waypoint => ResourceType.Utw,
            InstanceMarkerKind.Store => ResourceType.Utm,
            InstanceMarkerKind.Sound => ResourceType.Uts,
            InstanceMarkerKind.Trigger => ResourceType.Utt,
            _ => null
        };

        private static InstanceMarkerKind? MapSectionTypeToKind(ResourceType type) => type switch
        {
            ResourceType.Utc => InstanceMarkerKind.Creature,
            ResourceType.Utp => InstanceMarkerKind.Placeable,
            ResourceType.Utd => InstanceMarkerKind.Door,
            ResourceType.Utw => InstanceMarkerKind.Waypoint,
            ResourceType.Utm => InstanceMarkerKind.Store,
            ResourceType.Uts => InstanceMarkerKind.Sound,
            ResourceType.Utt => InstanceMarkerKind.Trigger,
            _ => null
        };

        /// <summary>
        /// Single funnel for every selection change (3D-view pick or instance-list row click):
        /// updates <see cref="SelectedSceneInstance"/> and syncs every section's SelectedRow to
        /// match (clearing the ones that don't correspond to <paramref name="instance"/>). Both
        /// entry points (<see cref="SelectSceneInstance"/> and the sections' own SelectedRow
        /// PropertyChanged) route through here, guarded by <see cref="_syncingSelection"/> so
        /// setting a section's SelectedRow from in here doesn't re-enter and recompute again.
        /// </summary>
        private void ApplySelection(InstanceMarker? instance)
        {
            if (_syncingSelection)
                return;

            _syncingSelection = true;
            try
            {
                SelectedSceneInstance = instance;

                var targetType = instance != null ? MapKindToSectionType(instance.Kind) : null;

                // Index-within-kind mapping (WORKLOG/WP4.4): both the scene's Instances (built by
                // AreaSceneBuilder.BuildInstances, one AddMarkers call per kind, each iterating its
                // .git list in order) and a section's Rows (InstanceListSectionViewModel.RefreshFromDocument,
                // iterating that same .git list field in order) enumerate the identical underlying
                // list in the identical order - so instance N of a given kind in the scene is always
                // list-row N in that kind's section.
                var indexWithinKind = instance != null ? IndexWithinKind(instance) : -1;

                foreach (var section in Sections)
                {
                    section.SelectedRow = targetType != null && section.BlueprintType == targetType
                        && indexWithinKind >= 0 && indexWithinKind < section.Rows.Count
                        ? section.Rows[indexWithinKind]
                        : null;
                }
            }
            finally
            {
                _syncingSelection = false;
            }
        }

        /// <summary>Called by the view when a 3D-view click lands on an instance (or empty space - instance is null).</summary>
        public void SelectSceneInstance(InstanceMarker? instance) => ApplySelection(instance);

        /// <summary>Called (via each section's PropertyChanged subscription set up in the constructor) whenever a section's own SelectedRow changes, e.g. from a user clicking a DataGrid row.</summary>
        private void OnSectionSelectionChanged(InstanceListSectionViewModel section)
        {
            if (_syncingSelection)
                return;

            var row = section.SelectedRow;
            if (row == null)
            {
                ApplySelection(null);
                return;
            }

            var kind = MapSectionTypeToKind(section.BlueprintType);
            var kindInstances = kind != null && AreaScene != null
                ? AreaScene.Instances.Where(i => i.Kind == kind).ToList()
                : new List<InstanceMarker>();

            var instance = row.Index >= 0 && row.Index < kindInstances.Count ? kindInstances[row.Index] : null;
            ApplySelection(instance);
        }

        /// <summary>The current scene's index of <paramref name="instance"/> within its own kind's instance list - the same convention <see cref="ApplySelection"/> and every WP5.2 gizmo/placement path below uses to rebind a selection across a scene rebuild.</summary>
        private int IndexWithinKind(InstanceMarker instance) =>
            AreaScene != null ? AreaScene.Instances.Where(i => i.Kind == instance.Kind).ToList().IndexOf(instance) : -1;

        /// <summary>The instance-list section covering <paramref name="kind"/>, or null when this editor has no section for it (Item/Encounter).</summary>
        private InstanceListSectionViewModel? SectionForKind(InstanceMarkerKind kind)
        {
            var type = MapKindToSectionType(kind);
            return type != null ? Sections.FirstOrDefault(s => s.BlueprintType == type) : null;
        }

        // ----- WP5.2: 3D-view move/rotate gizmo and place-from-palette -----

        private InstanceListSectionViewModel? _placementSection;

        /// <summary>Which instance-list section (blueprint type/palette) the 3D-view "Place..." button pulls from - bound to a picker in the 3D View tab; defaults to the first section the first time "Place..." is used.</summary>
        public InstanceListSectionViewModel? PlacementSection
        {
            get => _placementSection;
            set
            {
                if (ReferenceEquals(_placementSection, value))
                    return;

                _placementSection = value;
                OnPropertyChanged(nameof(PlacementSection));
            }
        }

        private InstanceListSectionViewModel? _pendingPlacementSection;
        private string? _pendingPlacementResRef;

        /// <summary>True from the moment a palette blueprint is chosen for placement until the next viewport click (or Esc/right-click cancel) resolves it - drives GlAreaControl.IsPlacementActive.</summary>
        public bool IsPlacementPending => _pendingPlacementResRef != null;

        /// <summary>3D-view status line while a placement is pending, or empty otherwise.</summary>
        public string PlacementStatus => IsPlacementPending
            ? $"Click to place {_pendingPlacementResRef}... (Esc or right-click to cancel)"
            : string.Empty;

        /// <summary>
        /// Opens <see cref="PlacementSection"/>'s palette browser (falling back to the first
        /// section) - the same InstanceListSectionViewModel.OpenPaletteBrowser flow that section's
        /// own "Add..." button uses. Choosing a blueprint arms placement mode; the next viewport
        /// click (routed here via <see cref="CommitPlacement"/>) creates the instance there.
        /// </summary>
        [RelayCommand]
        private void BeginPlace()
        {
            var section = PlacementSection ?? Sections.FirstOrDefault();
            if (section == null)
            {
                _log.AppendLine("No instance-list section available to place from.");
                return;
            }

            PlacementSection = section;

            section.OpenPaletteBrowser(
                resRef =>
                {
                    _pendingPlacementSection = section;
                    _pendingPlacementResRef = resRef;
                    OnPropertyChanged(nameof(IsPlacementPending));
                    OnPropertyChanged(nameof(PlacementStatus));
                },
                () => { });
        }

        /// <summary>
        /// Called by the view when a viewport click resolves a pending placement
        /// (GlAreaControl.PlacementPointPicked): creates the instance at the clicked ground
        /// position through the pending section's InstanceFieldMap-based Add path (one RunGitEdit
        /// transaction), then rebuilds the scene and selects the new instance.
        /// </summary>
        public void CommitPlacement(Vector3 position)
        {
            var section = _pendingPlacementSection;
            var resRef = _pendingPlacementResRef;
            _pendingPlacementSection = null;
            _pendingPlacementResRef = null;
            OnPropertyChanged(nameof(IsPlacementPending));
            OnPropertyChanged(nameof(PlacementStatus));

            if (section == null || resRef == null)
                return;

            if (!section.AddInstanceAt(resRef, position.X, position.Y, position.Z))
                return;

            var kind = MapSectionTypeToKind(section.BlueprintType);
            var reselect = kind != null ? (kind.Value, section.Rows.Count - 1) : ((InstanceMarkerKind, int)?)null;
            _ = BuildSceneAsync(reselect);
        }

        /// <summary>Called by the view when a pending placement is cancelled (Esc or right-click in the viewport, GlAreaControl.PlacementCancelled).</summary>
        public void CancelPlacement()
        {
            if (_pendingPlacementResRef == null)
                return;

            _pendingPlacementSection = null;
            _pendingPlacementResRef = null;
            OnPropertyChanged(nameof(IsPlacementPending));
            OnPropertyChanged(nameof(PlacementStatus));
        }

        /// <summary>
        /// Called by the view when the 3D-view move gizmo releases (GlAreaControl.InstanceMoved):
        /// commits the final X/Y (Z unchanged) through the matching section's
        /// InstanceFieldMap.SetPosition path as one RunGitEdit transaction, then refreshes the
        /// scene keeping the same instance selected (rebind by kind+index).
        /// </summary>
        public void MoveSelectedInstance(InstanceMarker instance, Vector3 newPosition)
        {
            var section = SectionForKind(instance.Kind);
            var index = IndexWithinKind(instance);
            if (section == null || index < 0)
                return;

            if (!section.SetInstancePosition(index, newPosition.X, newPosition.Y, newPosition.Z,
                    $"Move {instance.Kind} \"{instance.Tag}\""))
                return;

            _ = BuildSceneAsync((instance.Kind, index));
        }

        /// <summary>Called by the view when the 3D-view rotate gizmo releases (GlAreaControl.InstanceRotated): mirrors <see cref="MoveSelectedInstance"/> for heading.</summary>
        public void RotateSelectedInstance(InstanceMarker instance, Vector2 newOrientation)
        {
            var section = SectionForKind(instance.Kind);
            var index = IndexWithinKind(instance);
            if (section == null || index < 0)
                return;

            if (!section.SetInstanceOrientation(index, newOrientation.X, newOrientation.Y,
                    $"Rotate {instance.Kind} \"{instance.Tag}\""))
                return;

            _ = BuildSceneAsync((instance.Kind, index));
        }

        public AreaEditorViewModel(
            string areResRef,
            ModuleWorkspace workspace,
            LookupOptionProvider lookups,
            IGameCodeIndex? gameCodeIndex,
            OutputLogService log,
            TilesetCatalog? tilesetCatalog = null,
            TileModelCache? tileModelCache = null,
            ResourceIndex? resourceIndex = null,
            PlaceableAppearanceService? placeableAppearances = null,
            DoorTypeService? doorTypes = null,
            TileWalkmeshCache? tileWalkmeshCache = null)
        {
            _log = log;
            _areResRef = areResRef;
            _tilesetCatalog = tilesetCatalog;
            _tileModelCache = tileModelCache;
            _placeableAppearances = placeableAppearances;
            _doorTypes = doorTypes;
            _tileWalkmeshCache = tileWalkmeshCache;
            ResourceIndex = resourceIndex;
            Id = $"area-editor:{areResRef}";

            var arePath = workspace.GetResourcePath(ResourceType.Area, areResRef);
            var gitPath = Path.Combine(workspace.ModuleRoot, "git", areResRef + ".git.json");

            _areSession = DocumentSession.Open(arePath);
            _gitSession = DocumentSession.Open(gitPath);

            var areContext = new EditorFieldContext(_areSession.Document, RunAreEdit);
            foreach (var group in AreSchema.Build().Groups)
            {
                var fields = group.Fields.Select(descriptor => CreateFieldViewModel(descriptor, areContext, lookups)).ToList();
                AreaPropertyGroups.Add(new EditorGroup(group.Title, fields));
            }

            foreach (var config in InstanceListConfigs)
            {
                Sections.Add(new InstanceListSectionViewModel(
                    config.Title, config.ListFieldName, config.BlueprintType,
                    _gitSession, workspace, RunGitEdit, gameCodeIndex, log));
            }

            // WP5.1 selection sync: a row click in any section should update the 3D-view highlight
            // (and clear every other section's own selection) via ApplySelection.
            foreach (var section in Sections)
            {
                section.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(InstanceListSectionViewModel.SelectedRow))
                        OnSectionSelectionChanged(section);
                };
            }

            UpdateTitle();
        }

        /// <summary>
        /// Called by the view when the 3D View tab is first activated. Builds the scene once
        /// (lazily) so opening the area editor itself stays fast - subsequent activations are a
        /// no-op until <see cref="RebuildSceneCommand"/> is used explicitly. Safe to call multiple
        /// times or before game-data services are available.
        /// </summary>
        public void EnsureSceneBuilt()
        {
            if (_sceneBuildRequested)
                return;

            _sceneBuildRequested = true;
            _ = BuildSceneAsync();
        }

        /// <summary>Manual refresh for the 3D view after edits - there is no live auto-rebuild (WP5.x territory).</summary>
        [RelayCommand]
        private async Task RebuildScene()
        {
            await BuildSceneAsync();
        }

        /// <summary>
        /// Rebuilds the scene. When <paramref name="reselect"/> is supplied (a WP5.2 gizmo edit,
        /// placement, or an undo/redo of one), the instance at that kind+index in the freshly
        /// built scene is reselected (rebind by kind+index - the fresh scene's InstanceMarker
        /// objects never equal the old ones by reference); otherwise any stale selection is
        /// dropped, matching the pre-WP5.2 behavior.
        /// </summary>
        private async Task BuildSceneAsync((InstanceMarkerKind Kind, int Index)? reselect = null)
        {
            if (_tilesetCatalog == null || _tileModelCache == null)
            {
                SceneStatus = "3D view unavailable (game data services not loaded).";
                return;
            }

            IsBuildingScene = true;
            SceneStatus = "Building scene...";

            var tilesetCatalog = _tilesetCatalog;
            var tileModelCache = _tileModelCache;
            var are = new AreDocument(_areSession.Document);
            var git = new GitDocument(_gitSession.Document);

            try
            {
                var scene = await Task.Run(() => AreaSceneBuilder.Build(
                    are, git, tilesetCatalog, tileModelCache, _placeableAppearances, _doorTypes, _tileWalkmeshCache));

                AreaScene = scene;

                InstanceMarker? toSelect = null;
                if (reselect is { } key)
                {
                    var kindInstances = scene.Instances.Where(i => i.Kind == key.Kind).ToList();
                    if (key.Index >= 0 && key.Index < kindInstances.Count)
                        toSelect = kindInstances[key.Index];
                }

                // Every previous scene's InstanceMarker objects are gone now (Build returns a fresh
                // list each time) - a selection with no reselect key (or whose key no longer
                // resolves) must be dropped rather than left pointing at objects no longer in this
                // scene.
                ApplySelection(toSelect);
                SceneStatus = scene.Diagnostics.MissingModels.Count == 0
                    ? $"{scene.Tiles.Count} tiles, {scene.Instances.Count} instances."
                    : $"{scene.Tiles.Count} tiles, {scene.Instances.Count} instances ({scene.Diagnostics.MissingModels.Count} fallback tiles).";
            }
            catch (Exception ex)
            {
                SceneStatus = $"Failed to build 3D scene: {ex.Message}";
                _log.AppendLine($"Area 3D scene build failed for {_areResRef}: {ex.Message}");
            }
            finally
            {
                IsBuildingScene = false;
            }
        }

        private static FieldViewModel CreateFieldViewModel(
            FieldDescriptor descriptor, EditorFieldContext context, LookupOptionProvider lookups)
        {
            return descriptor.Kind switch
            {
                EditorKind.Integer => new IntegerFieldViewModel(descriptor, context),
                EditorKind.Float => new FloatFieldViewModel(descriptor, context),
                EditorKind.Check => new CheckFieldViewModel(descriptor, context),
                EditorKind.LocString => new LocStringFieldViewModel(descriptor, context),
                EditorKind.TwoDaDropdown => new DropdownFieldViewModel(
                    descriptor, context, lookups.GetOptions(descriptor.LookupKey)),
                EditorKind.ScriptSlot => new ScriptFieldViewModel(descriptor, context),
                _ => new TextFieldViewModel(descriptor, context)
            };
        }

        private bool RunAreEdit(string description, Action mutation) => RunEdit(_areSession, description, mutation);

        private bool RunGitEdit(string description, Action mutation) => RunEdit(_gitSession, description, mutation);

        private bool RunEdit(DocumentSession session, string description, Action mutation)
        {
            try
            {
                using (session.Begin(description))
                    mutation();

                AfterHistoryChange();
                return true;
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Edit failed ({description}): {ex.Message}");
                return false;
            }
        }

        [RelayCommand]
        private void Save()
        {
            SaveSession(_areSession);
            SaveSession(_gitSession);
            AfterHistoryChange();
        }

        private void SaveSession(DocumentSession session)
        {
            if (!session.UndoStack.IsDirty)
                return;

            try
            {
                Services.SaveService.WriteAtomic(session.FilePath, session.Document.ToBytes());
                session.UndoStack.MarkSaved();
                _log.AppendLine($"Saved {session.FilePath}.");
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Save failed for {session.FilePath}: {ex.Message}");
            }
        }

        /// <summary>Undo/redo for the area-properties (.are) group's own small history.</summary>
        [RelayCommand(CanExecute = nameof(CanUndoAre))]
        private void UndoAre()
        {
            _areSession.UndoStack.Undo();
            RefreshAreaPropertyFields();
            AfterHistoryChange();
        }

        public bool CanUndoAre => _areSession.UndoStack.CanUndo;

        [RelayCommand(CanExecute = nameof(CanRedoAre))]
        private void RedoAre()
        {
            _areSession.UndoStack.Redo();
            RefreshAreaPropertyFields();
            AfterHistoryChange();
        }

        public bool CanRedoAre => _areSession.UndoStack.CanRedo;

        /// <summary>Undo/redo for the instance lists (.git) - the toolbar's primary pair, since
        /// placing/moving/removing instances is the bulk of this screen's editing. Also refreshes
        /// the 3D view (WP5.2) when it has ever been built, rebinding the current selection by
        /// kind+index so undoing/redoing a 3D-view gizmo edit is visible without pressing Rebuild.</summary>
        [RelayCommand(CanExecute = nameof(CanUndoInstances))]
        private void UndoInstances()
        {
            var reselect = CaptureReselectKey();

            _gitSession.UndoStack.Undo();
            RefreshInstanceSections();

            if (_sceneBuildRequested)
                _ = BuildSceneAsync(reselect);

            AfterHistoryChange();
        }

        public bool CanUndoInstances => _gitSession.UndoStack.CanUndo;

        [RelayCommand(CanExecute = nameof(CanRedoInstances))]
        private void RedoInstances()
        {
            var reselect = CaptureReselectKey();

            _gitSession.UndoStack.Redo();
            RefreshInstanceSections();

            if (_sceneBuildRequested)
                _ = BuildSceneAsync(reselect);

            AfterHistoryChange();
        }

        /// <summary>The current selection's kind+index, captured before an undo/redo mutates the
        /// document (the selected InstanceMarker itself remains valid to read until the scene is
        /// actually rebuilt) - used to rebind selection across the post-undo/redo scene refresh.</summary>
        private (InstanceMarkerKind Kind, int Index)? CaptureReselectKey()
        {
            if (SelectedSceneInstance is not { } instance)
                return null;

            var index = IndexWithinKind(instance);
            return index >= 0 ? (instance.Kind, index) : null;
        }

        public bool CanRedoInstances => _gitSession.UndoStack.CanRedo;

        /// <summary>Raised when the tab closes so the editor registry can forget this instance.</summary>
        public event Action<AreaEditorViewModel>? Closed;

        public override bool OnClose()
        {
            _areSession.Dispose();
            _gitSession.Dispose();
            Closed?.Invoke(this);
            return base.OnClose();
        }

        private void RefreshAreaPropertyFields()
        {
            foreach (var group in AreaPropertyGroups)
            foreach (var field in group.Fields)
                field.RefreshFromDocument();
        }

        private void RefreshInstanceSections()
        {
            foreach (var section in Sections)
                section.RefreshFromDocument();
        }

        private void AfterHistoryChange()
        {
            UpdateTitle();
            UndoAreCommand.NotifyCanExecuteChanged();
            RedoAreCommand.NotifyCanExecuteChanged();
            UndoInstancesCommand.NotifyCanExecuteChanged();
            RedoInstancesCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(IsDirty));
            OnPropertyChanged(nameof(CanUndoAre));
            OnPropertyChanged(nameof(CanRedoAre));
            OnPropertyChanged(nameof(CanUndoInstances));
            OnPropertyChanged(nameof(CanRedoInstances));
        }

        private void UpdateTitle()
        {
            Title = IsDirty ? $"{_areResRef} *" : _areResRef;
        }
    }
}
