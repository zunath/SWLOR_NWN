using Nwn.Preview.Areas.Clipboard;
using Nwn.Authoring.Areas.Placement;
using Nwn.Authoring.Areas.Editing;
using Nwn.Toolset.Avalonia.Areas;
using System.Collections.ObjectModel;
using System.Numerics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.Documents;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Editing;
using SWLOR.Toolset.Domain.Editors;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Schemas;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.Tilesets;
using Nwn.Formats.Tilesets;
using Nwn.Authoring.Areas.Tiles;
using Nwn.Authoring.Documents.NimGff;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Services;
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
    /// Owns three DocumentSessions - one per file - because the .are, .git and .gic files
    /// are separate nwn_gff documents. GIC structural edits are captured in the paired GIT
    /// transaction so one undo keeps their parallel lists aligned. ARE/GIT Undo/Redo is deliberately
    /// split rather than merged into one combined stack: the area-properties group gets its own
    /// small Undo/Redo pair (mirroring BlueprintEditorViewModel), while the toolbar's primary
    /// Undo/Redo acts on the .git session, since instance placement/deletion is the editing this
    /// screen is mostly used for. Save writes whichever session(s) are dirty; the title's dirty
    /// marker reflects any session being dirty.
    /// </remarks>
    public partial class AreaEditorViewModel
        : Document, IEditorDocument, IDocumentStatusSource, Shell.Panels.IAreaPlacementTarget
    {
        private static readonly (string Title, string ListFieldName, ResourceType BlueprintType)[] InstanceListConfigs =
        {
            ("Creatures", "Creature List", ResourceType.Utc),
            ("Placeables", "Placeable List", ResourceType.Utp),
            ("Doors", "Door List", ResourceType.Utd),
            ("Waypoints", "WaypointList", ResourceType.Utw),
            ("Stores", "StoreList", ResourceType.Utm),
            ("Sounds", "SoundList", ResourceType.Uts),
            ("Triggers", "TriggerList", ResourceType.Utt),
            // Loose items on the ground. The GIT calls this one just "List".
            ("Items", "List", ResourceType.Uti)
        };

        private readonly DocumentSession _areSession;
        private readonly DocumentSession _gitSession;
        private readonly DocumentSession _gicSession;
        private readonly AreaDocumentEditSession _documentEdits;
        private readonly AreaInstanceEditor _instanceEditor;
        private readonly AreaSceneInstanceEditor _sceneInstanceEditor;
        private AreaTileEditor? _tileEditor;
        private byte[] _savedGicBytes = Array.Empty<byte>();
        private bool _gicDirty;
        private readonly OutputLogService _log;
        private readonly LookupOptionProvider _lookups;
        private readonly ModuleWorkspace _workspace;
        private readonly string _areResRef;
        private readonly TilesetCatalog? _tilesetCatalog;
        private readonly TileModelCache? _tileModelCache;
        private readonly PlaceableAppearanceService? _placeableAppearances;
        private readonly DoorTypeService? _doorTypes;
        private readonly WaypointAppearanceService? _waypointAppearances;

        /// <summary>Builds an armed blueprint's geometry and fallback metadata for the placement ghost.</summary>
        private readonly Func<ResourceType, string, bool, BlueprintModelRenderResult>? _resolveBlueprintModel;
        private readonly Func<JsonGffStruct, RenderModel?>? _resolvePlacedCreatureModel;
        private readonly TileWalkmeshCache? _tileWalkmeshCache;
        private readonly IEditorPromptService _prompts;
        private readonly IScriptSlotHost? _scriptSlotHost;

        /// <summary>Resolves a blueprint's display name from the catalog, so the selection bar can lead with it.</summary>
        private readonly Func<ResourceType?, string?, string?>? _resolveBlueprintName;

        /// <summary>Opens a blueprint in its own editor tab - the selection bar's one action.</summary>
        private readonly Action<ResourceType, string>? _openBlueprint;

        /// <summary>Creates and opens an independent custom copy of a selected instance's blueprint.</summary>
        private readonly Func<ResourceType, string, string?>? _editCopyBlueprint;

        /// <summary>The shared pack/build/validation lock governing module writes.</summary>
        private readonly ModuleMutationLock? _mutationLock;

        /// <summary>Shared by open area tabs so copied placements can cross area boundaries.</summary>
        private readonly AreaInstanceClipboard _instanceClipboard;

        private bool _sceneBuildRequested;
        private long _sceneBuildGeneration;
        private long _sceneInputRevision;
        private long _builtSceneInputRevision = -1;
        private long _buildingSceneInputRevision = -1;
        private bool _closeApproved;
        private bool _closePromptOpen;
        private bool _disposed;

        public ObservableCollection<EditorGroup> AreaPropertyGroups { get; } = new();

        public ObservableCollection<InstanceListSectionViewModel> Sections { get; } = new();

        /// <summary>
        /// The front page of this area document (0 = Scene, 1 = Properties). This belongs to the
        /// document rather than its transient view so changing document tabs returns the builder to
        /// the page they were using.
        /// </summary>
        [ObservableProperty]
        private int _selectedRootTabIndex;

        /// <summary>Whether the top-level Area Properties card is expanded in this open document.</summary>
        [ObservableProperty]
        private bool _areaPropertiesExpanded;

        /// <summary>The Properties page's retained scroll position, stored without an Avalonia dependency.</summary>
        public Vector2 PropertiesScrollOffset { get; set; }

        /// <summary>The last camera owned by this open area tab, restored when its view is recreated.</summary>
        public AreaViewportState? ViewportState { get; set; }

        public bool IsDirty =>
            _areSession.UndoStack.IsDirty ||
            _gitSession.UndoStack.IsDirty ||
            _gicDirty;

        /// <summary>Resource index used by the 3D View tab to resolve mesh/tile textures; null when game data services aren't loaded.</summary>
        public ResourceIndex? ResourceIndex { get; }

        private AreaScene? _areaScene;

        /// <summary>The most recently built 3D scene for this area, or null before the first build. Built when the editor opens and refreshed automatically after every edit.</summary>
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
                OnPropertyChanged(nameof(HasViewportHud));
            }
        }

        private string _sceneStatus = string.Empty;

        /// <summary>
        /// A transient message about the map: build progress, a build failure, or a placement that would
        /// not fit. Empty the rest of the time - which is most of the time.
        /// </summary>
        public string SceneStatus
        {
            get => _sceneStatus;
            private set
            {
                _sceneStatus = value;
                OnPropertyChanged(nameof(SceneStatus));
                OnPropertyChanged(nameof(HasSceneStatus));
                OnPropertyChanged(nameof(HasViewportHud));
            }
        }

        public bool HasSceneStatus => !string.IsNullOrEmpty(SceneStatus);

        /// <summary>
        /// Whether the overlay in the corner of the map has anything to say. It is hidden outright when it
        /// does not, rather than sitting there empty: an overlay covers the map it is drawn on, so it has
        /// to earn the space every time it appears.
        /// </summary>
        /// <remarks>
        /// Suppressed while a build is running, because that state has its own centred notice and two
        /// overlays saying "Building scene..." at once is one too many.
        /// </remarks>
        public bool HasViewportHud =>
            !IsBuildingScene &&
            (HasSceneStatus || HasSceneSelection || HasTileSelection ||
             !string.IsNullOrEmpty(PlacementStatus));

        // ----- 3D-view <-> instance-list selection sync -----

        private bool _syncingSelection;

        private (ResourceType Type, int Index)? _pendingSectionSelection;

        /// <summary>
        /// A Source-tab Go To can run before either the area view or the Area Contents panel has
        /// attached to this document. Keep both requests on the document until their respective
        /// views consume them, otherwise opening a cold area selects the row but loses the visible
        /// camera/list navigation that made the action useful.
        /// </summary>
        private Vector3? _pendingCameraFocusRequest;
        private (ResourceType Type, int Index)? _pendingAreaContentsReveal;

        private InstanceMarker? _selectedSceneInstance;

        /// <summary>
        /// The instance currently selected (from either the 3D view or an instance-list row) - the
        /// view mirrors this onto <c>AreaViewportControl.SelectedInstance</c> for the 3D highlight. Always
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
                OnPropertyChanged(nameof(HasSceneSelection));
                OnPropertyChanged(nameof(HasViewportHud));
                OnPropertyChanged(nameof(SelectionName));
                OnPropertyChanged(nameof(SelectionResRef));
                OnPropertyChanged(nameof(SelectionKindLabel));
                OnPropertyChanged(nameof(SelectionGlyph));
                OnPropertyChanged(nameof(SelectionCoordinates));
                OnPropertyChanged(nameof(IDocumentStatusSource.StatusDetail));
                EditSelectedBlueprintCommand.NotifyCanExecuteChanged();
                EditCopySelectedBlueprintCommand.NotifyCanExecuteChanged();
                OpenSelectedInstancePropertiesCommand.NotifyCanExecuteChanged();
            }
        }

        /// <summary>Human-readable readout of the current selection for the 3D-view status border, or empty when nothing is selected.</summary>
        public string SelectionStatus => SelectedSceneInstance is { } instance
            ? $"Selected: {instance.Kind} \"{instance.Tag}\"" +
              (string.IsNullOrEmpty(instance.TemplateResRef) ? string.Empty : $" ({instance.TemplateResRef})")
            : string.Empty;

        // ----- selection bar -----
        //
        // The bar under the map answers one question - what have I got selected - so these are the
        // structured pieces of that answer rather than one concatenated status string. Position is not
        // among them: it belongs to the gizmo while you drag and to the status bar the rest of the time,
        // because a coordinate box in the chrome is never wide enough to label.

        public bool HasSceneSelection => SelectedSceneInstance != null;

        /// <summary>
        /// The selected instance's blueprint name, resolved through the catalog so the bar shows
        /// "Work Station, Droid Repair" rather than "_mdrn_pl_conso08". Falls back to the tag, then the
        /// resref, so the bar is never blank while something is selected.
        /// </summary>
        public string SelectionName
        {
            get
            {
                if (SelectedSceneInstance is not { } instance)
                    return string.Empty;

                var resolved = _resolveBlueprintName?.Invoke(
                    MapKindToSectionType(instance.Kind), instance.TemplateResRef);

                if (!string.IsNullOrWhiteSpace(resolved))
                    return resolved;

                if (!string.IsNullOrWhiteSpace(instance.Tag))
                    return instance.Tag;

                return instance.TemplateResRef ?? instance.Kind.ToString();
            }
        }

        public string SelectionResRef => SelectedSceneInstance?.TemplateResRef ?? string.Empty;

        /// <summary>The friendly singular kind ("Placeable"), not the enum name.</summary>
        public string SelectionKindLabel => SelectedSceneInstance is { } instance
            ? MapKindToSectionType(instance.Kind)?.SingularDisplayName() ?? instance.Kind.ToString()
            : string.Empty;

        /// <summary>Where the selection stands, for the status bar - Aurora put coordinates there too.</summary>
        public string SelectionCoordinates => SelectedSceneInstance is { } instance
            ? $"x {instance.Position.X:0.00}  y {instance.Position.Y:0.00}  z {instance.Position.Z:0.00}"
            : string.Empty;

        /// <summary>A one-letter stand-in for the selection's icon until blueprint thumbnails exist.</summary>
        public string SelectionGlyph
        {
            get
            {
                var label = SelectionKindLabel;
                return label.Length == 0 ? "?" : label[..1];
            }
        }

        /// <summary>Feeds the shell status bar, which shows where the selection stands.</summary>
        string IDocumentStatusSource.StatusDetail => SelectionCoordinates;

        /// <summary>
        /// Opens the blueprint the selection was placed from. The door out of the map and into the
        /// thing itself, which is the only edit the bar offers.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanEditSelectedBlueprint))]
        private void EditSelectedBlueprint()
        {
            if (SelectedSceneInstance is not { } instance ||
                string.IsNullOrWhiteSpace(instance.TemplateResRef) ||
                MapKindToSectionType(instance.Kind) is not { } type)
                return;

            _openBlueprint?.Invoke(type, instance.TemplateResRef);
        }

        /// <summary>
        /// True only when "Edit object..." would actually open something.
        /// </summary>
        /// <remarks>
        /// A non-null <see cref="InstanceMarker.TemplateResRef"/> was not enough. An instance may
        /// reference a blueprint that lives only in the Standard palette, a hak, or the base game, and
        /// <c>EditorService.TryOpenEditor</c> needs a module-local file - so the command lit up for
        /// perfectly valid instances and then answered with "File not found". A blank resref enabled it
        /// too, for a command whose own body refuses blanks. This asks the same question the open path
        /// asks, so the button is enabled exactly when it works.
        /// </remarks>
        private bool CanEditSelectedBlueprint() =>
            _openBlueprint != null &&
            SelectedSceneInstance is { } instance &&
            !string.IsNullOrWhiteSpace(instance.TemplateResRef) &&
            MapKindToSectionType(instance.Kind) is { } type &&
            File.Exists(_workspace.GetResourcePath(type, instance.TemplateResRef));

        /// <summary>
        /// Creates a new Custom blueprint from the selected instance's source and opens the new copy.
        /// The placed instance remains linked to its original blueprint.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanEditCopySelectedBlueprint))]
        private void EditCopySelectedBlueprint()
        {
            if (SelectedSceneInstance is not { } instance ||
                string.IsNullOrWhiteSpace(instance.TemplateResRef) ||
                MapKindToSectionType(instance.Kind) is not { } type ||
                _editCopyBlueprint == null)
                return;

            var copyResRef = _editCopyBlueprint(type, instance.TemplateResRef);
            SceneStatus = copyResRef == null
                ? $"Could not copy {SelectionName} - see Output."
                : $"Created and opened blueprint copy {copyResRef}.";
        }

        private bool CanEditCopySelectedBlueprint() =>
            _editCopyBlueprint != null &&
            _mutationLock?.IsLocked != true &&
            SelectedSceneInstance is { } instance &&
            !string.IsNullOrWhiteSpace(instance.TemplateResRef) &&
            MapKindToSectionType(instance.Kind) is { } type &&
            CanResolveBlueprint(type, instance.TemplateResRef);

        private bool CanResolveBlueprint(ResourceType type, string resRef)
        {
            if (File.Exists(_workspace.GetResourcePath(type, resRef)))
                return true;

            if (ResourceIndex == null)
                return false;

            var identity = new ResourceIdentity(
                resRef,
                ResourceIdentity.TypeFromExtension(type.Extension()));
            return ResourceIndex.Contains(identity);
        }

        private void OnMutationLockChanged() =>
            EditCopySelectedBlueprintCommand.NotifyCanExecuteChanged();

        /// <summary>
        /// Maps a 3D-view instance's kind to the blueprint type of the section that lists it, or
        /// null when this editor has no section for that kind.
        /// </summary>
        private static ResourceType? MapKindToSectionType(InstanceMarkerKind kind) => kind switch
        {
            InstanceMarkerKind.Creature => ResourceType.Utc,
            InstanceMarkerKind.Item => ResourceType.Uti,
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
            ResourceType.Uti => InstanceMarkerKind.Item,
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

            _pendingSectionSelection = null;
            _syncingSelection = true;
            try
            {
                SelectedSceneInstance = instance;
                OnPropertyChanged(nameof(CanRotateSelection));

                // An object and a tile are never selected together - raise/lower would have no way to
                // say which one it meant. Cleared here rather than only in the viewport so a
                // list-driven selection drops the tile highlight too.
                if (instance != null)
                    SelectedTile = null;

                var targetType = instance != null ? MapKindToSectionType(instance.Kind) : null;

                // Index-within-kind mapping: both the scene's Instances (built by
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

            // The 3D scene is built lazily when the user first opens that tab. Keep the list row
            // and its detail/actions selected until a scene exists, then bind it to the matching
            // marker at the end of the first successful build.
            if (AreaScene == null)
            {
                _pendingSectionSelection = (section.BlueprintType, row.Index);

                _syncingSelection = true;
                try
                {
                    SelectedSceneInstance = null;
                    foreach (var otherSection in Sections.Where(candidate => !ReferenceEquals(candidate, section)))
                        otherSection.SelectedRow = null;
                }
                finally
                {
                    _syncingSelection = false;
                }

                return;
            }

            var kind = MapSectionTypeToKind(section.BlueprintType);
            var kindInstances = kind != null
                ? AreaScene.Instances.Where(i => i.Kind == kind).ToList()
                : new List<InstanceMarker>();

            var instance = row.Index >= 0 && row.Index < kindInstances.Count ? kindInstances[row.Index] : null;
            ApplySelection(instance);
        }

        /// <summary>The current scene's index of <paramref name="instance"/> within its own kind's instance list - the same convention <see cref="ApplySelection"/> and every gizmo/placement path below uses to rebind a selection across a scene rebuild.</summary>
        private int IndexWithinKind(InstanceMarker instance) =>
            AreaScene != null ? AreaScene.Instances.Where(i => i.Kind == instance.Kind).ToList().IndexOf(instance) : -1;

        /// <summary>The instance-list section covering <paramref name="kind"/>, or null when this editor has no section for it.</summary>
        private InstanceListSectionViewModel? SectionForKind(InstanceMarkerKind kind)
        {
            var type = MapKindToSectionType(kind);
            return type != null ? Sections.FirstOrDefault(s => s.BlueprintType == type) : null;
        }

        // ----- 3D-view move/rotate gizmo and place-from-palette -----

        private InstanceListSectionViewModel? _pendingPlacementSection;
        private string? _pendingPlacementResRef;
        private bool _pendingPlacementUsesIndexedBlueprint;
        private AreaInstanceClipboardEntry? _pendingPlacementCopy;

        /// <summary>True from the moment a palette blueprint is chosen for placement until the next viewport click (or Esc/right-click cancel) resolves it - drives AreaViewportControl.IsPlacementActive.</summary>
        public bool IsPlacementPending => _pendingPlacementSection != null;

        /// <summary>
        /// 3D-view status line while a placement is pending, or empty otherwise. A door names the
        /// doorway rule, since it is the one placement a click on open floor will not resolve.
        /// </summary>
        public string PlacementStatus =>
            IsPlacementPending
                ? _pendingPlacementSection?.BlueprintType == ResourceType.Utd
                    ? $"Click an empty doorway to hang {_pendingPlacementResRef}... (Esc or right-click to cancel)"
                    : $"Click to place {_pendingPlacementResRef}... (Esc or right-click to cancel)"
            : _pendingTile is { } tile
                ? tile.Terrain != null
                    ? $"Click to paint {tile.Label}... (Esc or right-click to stop)"
                : tile.Crosser != null
                    ? $"Click a grid edge to paint {tile.Label}... (Esc or right-click to stop)"
                : CanRotatePendingTile
                    ? $"Click a cell to place {tile.Label} facing {PendingTileFacing}... " +
                      "(R to rotate, Esc or right-click to cancel)"
                    : $"Click a cell to place {tile.Label}... (Esc or right-click to cancel)"
            : string.Empty;

        /// <summary>This area's tileset resref, which is what the Tiles palette lists tiles from.</summary>
        public string? TilesetResRef => new AreDocument(_areSession.Document).Tileset;

        private TilePaletteEntry? _pendingTile;

        /// <summary>True while a tile or group is armed - drives AreaViewportControl.IsTilePlacementActive.</summary>
        public bool IsTilePlacementPending => _pendingTile != null;

        /// <summary>
        /// Raised when a paint click was declined by the solver, so the viewport can answer it where
        /// the builder is looking. Not raised for a paint that legitimately changes nothing (a
        /// repaint of what is already there), which is a success with no work to do.
        /// </summary>
        public event Action? PaintRejected;

        /// <summary>The armed stamp's footprint in cells, for the viewport's cell highlight.</summary>
        public (int Columns, int Rows) TilePlacementFootprint =>
            _pendingTile is { } entry ? (entry.Columns, entry.Rows) : (1, 1);

        /// <summary>
        /// True while the armed palette entry is a terrain - which paints grid VERTICES, the way the
        /// reference toolset does, rather than stamping cells. Drives
        /// AreaViewportControl.TilePlacementTargetsVertex: the viewport then snaps its cursor to the
        /// nearest vertex, draws the red vertex-centred paint square, and reports vertex
        /// coordinates through the pick event.
        /// </summary>
        public bool TilePlacementTargetsVertex => _pendingTile?.Terrain is { Length: > 0 };

        /// <summary>
        /// True while the armed palette entry is a crosser brush (road, bridge, wall - or the
        /// eraser, whose crosser is the empty string). Crossers paint grid EDGES: the viewport
        /// snaps to the nearest edge, draws the red edge-centred paint square, and reports edge
        /// coordinates through AreaViewportControl.TileEdgePicked.
        /// </summary>
        public bool TilePlacementTargetsEdge => _pendingTile?.Crosser != null;

        private IReadOnlyList<RenderModel?> _tilePlacementModels = Array.Empty<RenderModel?>();

        /// <summary>
        /// The armed stamp's tile models, row-major over its footprint - what the viewport draws under
        /// the cursor. A null slot is a hole in the group's rectangle, or a tile whose model is missing.
        /// </summary>
        /// <remarks>
        /// A tile is the one thing in this palette whose shape is the whole point of choosing it - a
        /// doorway, a stair, a corner - and an outlined cell shows none of that. The models are resolved
        /// once when the stamp is armed rather than per frame: the cache has them parsed already, but the
        /// tileset lookup is not free and the answer does not change while the stamp follows the cursor.
        /// </remarks>
        public IReadOnlyList<RenderModel?> TilePlacementModels => _tilePlacementModels;

        private TilesetDefinition? ResolveTilesetForEditing(string resRef) =>
            _tilesetCatalog != null && _tilesetCatalog.TryGetTileset(resRef, out var tileset)
                ? tileset
                : null;

        private IReadOnlyList<RenderModel?> ResolveTileModels(TilePaletteEntry entry)
        {
            if (_tileModelCache == null || _tilesetCatalog == null ||
                TilesetResRef is not { Length: > 0 } tilesetResRef ||
                !_tilesetCatalog.TryGetTileset(tilesetResRef, out var tileset))
                return Array.Empty<RenderModel?>();

            var models = new RenderModel?[entry.TileIds.Count];
            for (var i = 0; i < entry.TileIds.Count; i++)
            {
                var tileId = entry.TileIds[i];
                if (tileId < 0 || tileId >= tileset.Tiles.Count)
                    continue;

                var modelResRef = tileset.Tiles[tileId].Model;
                if (!string.IsNullOrWhiteSpace(modelResRef))
                    models[i] = _tileModelCache.GetOrBuild(modelResRef);
            }

            return models;
        }

        /// <summary>Quarter turns the armed tile will be stamped with, 0-3.</summary>
        private int _pendingTileOrientation;

        /// <summary>
        /// Whether the armed stamp can be turned. A single tile can; a multi-cell group cannot,
        /// because its cells are authored as a fixed arrangement and turning it would mean
        /// re-deriving which tile belongs in which cell, not just setting an angle. A terrain brush
        /// cannot either - it does not stamp a tile, it solves for one, and the solver chooses the
        /// facing that matches the neighbours.
        /// </summary>
        public bool CanRotatePendingTile => IsRotatable(_pendingTile);

        /// <summary>
        /// Whether <paramref name="entry"/> is a tile a builder may turn: a single cell, and not a
        /// terrain or crosser brush (each picks its own tiles and so has no one orientation to set).
        /// </summary>
        /// <remarks>
        /// Takes the entry rather than reading <see cref="_pendingTile"/>, because the commit path has
        /// already cleared that field by the time it needs the answer - which is how every rotated tile
        /// came to be written at orientation 0 while the HUD reported 90, 180 or 270.
        /// </remarks>
        private static bool IsRotatable(TilePaletteEntry? entry) =>
            entry is { Columns: 1, Rows: 1, Terrain: null, Crosser: null };

        /// <summary>The armed tile's facing, as the compass label the status line shows.</summary>
        public string PendingTileFacing => _pendingTileOrientation switch
        {
            1 => "90 degrees",
            2 => "180 degrees",
            3 => "270 degrees",
            _ => "0 degrees"
        };

        /// <summary>
        /// Turns the armed tile a quarter turn. NWN stores tile facing as Tile_Orientation 0-3, so
        /// this is the whole range - asymmetric pieces (doors, wall ends, transitions) need it, and
        /// without it every stamp went in facing the tileset's default direction.
        /// </summary>
        public void RotatePendingTile()
        {
            if (!CanRotatePendingTile || _tileEditor?.Rotate() != true)
                return;

            _pendingTileOrientation = _tileEditor.ArmedOrientation;
            OnPropertyChanged(nameof(PendingTileFacing));
            OnPropertyChanged(nameof(PlacementStatus));
        }

        /// <summary>
        /// Arms a tile stamp chosen in the Palette panel. The next map click writes it into the grid.
        /// </summary>
        public bool ArmTilePlacement(TilePaletteEntry entry)
        {
            if (entry == null || _tileEditor?.Arm(entry) != true)
                return false;

            // Arming a tile cancels any armed object and vice versa: two ghosts following one cursor
            // would leave the builder guessing which one a click resolves.
            CancelPlacement();

            _pendingTile = entry;
            _pendingTileOrientation = 0;
            InvalidateTilePlacementValidity();
            _tilePlacementModels = ResolveTileModels(entry);
            OnPropertyChanged(nameof(IsTilePlacementPending));
            OnPropertyChanged(nameof(TilePlacementTargetsVertex));
            OnPropertyChanged(nameof(TilePlacementTargetsEdge));
            OnPropertyChanged(nameof(TilePlacementFootprint));
            OnPropertyChanged(nameof(TilePlacementModels));
            OnPropertyChanged(nameof(CanRotatePendingTile));
            OnPropertyChanged(nameof(PendingTileFacing));
            OnPropertyChanged(nameof(PlacementStatus));
            OnPropertyChanged(nameof(HasViewportHud));
            return true;
        }

        /// <summary>Called by the view when a tile placement is cancelled from inside the viewport.</summary>
        public void CancelTilePlacement()
        {
            if (_pendingTile == null)
                return;

            _tileEditor?.Cancel();
            _pendingTile = null;
            _tilePlacementModels = Array.Empty<RenderModel?>();
            InvalidateTilePlacementValidity();
            OnPropertyChanged(nameof(IsTilePlacementPending));
            OnPropertyChanged(nameof(TilePlacementTargetsVertex));
            OnPropertyChanged(nameof(TilePlacementTargetsEdge));
            OnPropertyChanged(nameof(TilePlacementModels));
            OnPropertyChanged(nameof(PlacementStatus));
            OnPropertyChanged(nameof(HasViewportHud));
        }

        /// <summary>Commits the shared tile editor's armed stamp or terrain brush.</summary>
        public void CommitTilePlacement(int anchorColumn, int anchorRow)
        {
            var entry = _pendingTile;
            if (entry == null || entry.Crosser != null || _tileEditor == null)
                return;

            if (entry.Terrain == null)
                _pendingTile = null;
            var label = entry.Terrain is { Length: > 0 }
                ? $"Paint {entry.Label} at vertex ({anchorColumn},{anchorRow})"
                : $"Place {entry.Label} at ({anchorColumn},{anchorRow})";
            var outcome = _tileEditor.CommitAt(anchorColumn, anchorRow, label);
            if (entry.Terrain == null)
                _tileEditor.Cancel();
            if (outcome == AreaTileEditOutcome.OutOfBounds)
                SceneStatus = $"'{entry.Label}' does not fit at ({anchorColumn},{anchorRow}).";
            else if (outcome == AreaTileEditOutcome.MissingTileset)
                SceneStatus = "Terrain painting is unavailable (this area's tileset could not be read).";
            if (entry.Terrain == null)
                CompleteTilePlacementUi();
            if (outcome == AreaTileEditOutcome.Changed)
            {
                InvalidateTilePlacementValidity();
                Interlocked.Increment(ref _sceneInputRevision);
                RequestSceneRefresh(immediate: true);
                AfterHistoryChange();
            }
        }

        private void CompleteTilePlacementUi()
        {
            _pendingTile = null;
            _pendingTileOrientation = 0;
            _tilePlacementModels = Array.Empty<RenderModel?>();
            OnPropertyChanged(nameof(IsTilePlacementPending));
            OnPropertyChanged(nameof(TilePlacementTargetsVertex));
            OnPropertyChanged(nameof(TilePlacementTargetsEdge));
            OnPropertyChanged(nameof(TilePlacementModels));
            OnPropertyChanged(nameof(CanRotatePendingTile));
            OnPropertyChanged(nameof(PendingTileFacing));
            OnPropertyChanged(nameof(PlacementStatus));
            OnPropertyChanged(nameof(HasViewportHud));
        }
        public bool CanPlaceArmedTileAt(int column, int row) =>
            _tileEditor?.CanPlaceAt(column, row) == true;

        private (int Column, int Row)? _selectedTile;

        /// <summary>
        /// The grid cell the builder has selected in the 3D view, or null when none is. The view
        /// mirrors it onto <c>AreaViewportControl.SelectedTileCell</c> for the highlight, and the raise and
        /// lower commands act on it.
        /// </summary>
        /// <remarks>
        /// Selecting a tile is what a click on open ground means, so it and
        /// <see cref="SelectedSceneInstance"/> are mutually exclusive - one click cannot leave both a
        /// tile and an object looking selected, because the next command would be ambiguous.
        /// </remarks>
        public (int Column, int Row)? SelectedTile
        {
            get => _selectedTile;
            private set
            {
                if (_selectedTile == value)
                    return;

                _selectedTile = value;
                _tileEditor?.SelectCell(value);
                OnPropertyChanged(nameof(SelectedTile));
                OnPropertyChanged(nameof(HasTileSelection));
                OnPropertyChanged(nameof(TileSelectionStatus));
                OnPropertyChanged(nameof(HasViewportHud));
                RaiseTileCommand.NotifyCanExecuteChanged();
                LowerTileCommand.NotifyCanExecuteChanged();
            }
        }

        public bool HasTileSelection => _selectedTile != null;

        /// <summary>The selected cell and its height level, for the 3D-view overlay; empty when no tile is selected.</summary>
        public string TileSelectionStatus
        {
            get
            {
                if (_selectedTile is not { } cell)
                    return string.Empty;

                var state = AreaTiles.StateAt(new AreDocument(_areSession.Document), cell.Column, cell.Row);
                return state == null
                    ? $"Tile ({cell.Column},{cell.Row})"
                    : $"Tile ({cell.Column},{cell.Row}) - height {state.Value.HeightLevel}";
            }
        }

        /// <summary>
        /// Called by the view when a click in the 3D view resolves to a grid cell (or to none).
        /// </summary>
        public void SelectTile((int Column, int Row)? cell) => SelectedTile = cell;

        [RelayCommand(CanExecute = nameof(HasTileSelection))]
        private void RaiseTile() => AdjustSelectedTileElevation(1);

        [RelayCommand(CanExecute = nameof(HasTileSelection))]
        private void LowerTile() => AdjustSelectedTileElevation(-1);

        /// <summary>
        /// Moves the selected tile one height level, immediately. Aurora's model: the tile you can see
        /// highlighted is the tile that moves, so pressing again steps it another level - rather than
        /// arming a mode that the next map click resolves, which showed nothing about which cell was
        /// going to change and cost a click per level.
        /// </summary>
        private void AdjustSelectedTileElevation(int delta)
        {
            if (_selectedTile is not { } cell || _tileEditor == null)
                return;

            var area = new AreDocument(_areSession.Document);
            var current = AreaTiles.StateAt(area, cell.Column, cell.Row);
            if (current == null)
                return;
            var direction = Math.Sign(delta);
            if (current.Value.HeightLevel + direction < AreaTiles.MinimumHeightLevel)
            {
                SceneStatus = $"Tile ({cell.Column},{cell.Row}) is already at the minimum height " +
                              $"{AreaTiles.MinimumHeightLevel}.";
                return;
            }

            var verb = direction > 0 ? "Raise" : "Lower";
            if (_tileEditor.AdjustSelectedHeight(
                    direction, $"{verb} tile at ({cell.Column},{cell.Row})") != AreaTileEditOutcome.Changed)
                return;

            InvalidateTilePlacementValidity();
            Interlocked.Increment(ref _sceneInputRevision);
            RequestSceneRefresh(immediate: true);
            OnPropertyChanged(nameof(TileSelectionStatus));
            AfterHistoryChange();
        }
        /// <summary>Drops the memoised answers - the grid they were computed against has changed.</summary>
        private void InvalidateTilePlacementValidity() => _tileEditor?.InvalidatePlacementValidity();

        public bool CanPlaceArmedCrosserAt(int edgeColumn, int edgeRow, bool verticalEdge) =>
            _tileEditor?.CanPaintCrosserAt(edgeColumn, edgeRow, verticalEdge) == true;
        /// <summary>Commits an edge-paint operation through the shared tile editor.</summary>
        public void CommitCrosserPaint(int edgeColumn, int edgeRow, bool verticalEdge)
        {
            if (_pendingTile is not { Crosser: not null } entry || _tileEditor == null)
                return;

            var label = $"Paint {entry.Label} at edge ({edgeColumn},{edgeRow})";
            var outcome = _tileEditor.CommitCrosserAt(edgeColumn, edgeRow, verticalEdge, label);
            if (outcome == AreaTileEditOutcome.MissingTileset)
                SceneStatus = "Crosser painting is unavailable (this area's tileset could not be read).";
            if (outcome == AreaTileEditOutcome.Changed)
            {
                InvalidateTilePlacementValidity();
                Interlocked.Increment(ref _sceneInputRevision);
                RequestSceneRefresh(immediate: true);
                AfterHistoryChange();
            }
        }

        /// <summary>Arms a palette blueprint for placement in the shared area scene.</summary>
        public bool ArmPlacement(
            ResourceType type,
            string resRef,
            Shell.Panels.PaletteSource source)
        {
            var section = Sections.FirstOrDefault(candidate => candidate.BlueprintType == type);
            if (section == null || string.IsNullOrWhiteSpace(resRef) || !CanArmObjectPlacement(type, resRef))
                return false;

            CancelTilePlacement();
            _pendingPlacementSection = section;
            _pendingPlacementResRef = resRef;
            _pendingPlacementUsesIndexedBlueprint = source == Shell.Panels.PaletteSource.Standard;
            _pendingPlacementCopy = null;
            PlacementGhost = BuildPlacementGhost(type, resRef, _pendingPlacementUsesIndexedBlueprint);
            NotifyPlacementChanged();
            return true;
        }

        /// <summary>
        /// Copies the selected placed object as an independent GIT/GIC snapshot.
        /// </summary>
        public bool CopySelectedSceneInstance()
        {
            if (SelectedSceneInstance is not { } instance ||
                SectionForKind(instance.Kind) is not { } section)
            {
                return false;
            }

            var index = IndexWithinKind(instance);
            var copy = index >= 0 ? section.CopyInstanceForPlacement(index, instance) : null;
            if (copy == null)
                return false;

            _instanceClipboard.Set(copy);
            SceneStatus = $"Copied {SelectionName}. Press Ctrl+V to place a copy.";
            return true;
        }

        /// <summary>
        /// Arms the copied object as the placement ghost. The next valid map click commits the clone.
        /// </summary>
        public bool PasteCopiedSceneInstance()
        {
            if (_instanceClipboard.Content is not { } copy ||
                !string.Equals(copy.ModuleRoot, _workspace.ModuleRoot, StringComparison.OrdinalIgnoreCase) ||
                Sections.FirstOrDefault(section => section.BlueprintType == copy.Type) is not { } section)
            {
                return false;
            }

            var resRef = InstanceFieldMap.GetTemplateResRef(copy.Type, copy.Instance);
            var label = !string.IsNullOrWhiteSpace(resRef)
                ? resRef
                : InstanceFieldMap.GetTag(copy.Instance) ?? copy.Type.SingularDisplayName();
            if (!CanArmObjectPlacement(copy.Type, label))
                return false;

            CancelTilePlacement();
            CancelPlacement();

            _pendingPlacementSection = section;
            _pendingPlacementResRef = label;
            _pendingPlacementUsesIndexedBlueprint = false;
            _pendingPlacementCopy = copy;
            PlacementGhost = copy.Preview;
            NotifyPlacementChanged();
            return true;
        }

        /// <summary>
        /// A door is hung in an empty doorway the tile declares, never on open floor and never in a
        /// doorway that already holds one. Other object kinds have no arm-time placement constraint.
        /// </summary>
        private bool CanArmObjectPlacement(ResourceType type, string label)
        {
            if (type != ResourceType.Utd || AreaScene is not { } scene || scene.HasEmptyDoorway())
                return true;

            _log.AppendLine(scene.DoorAnchors.Count == 0
                ? $"'{label}' cannot be placed: no tile in this area declares a doorway to hang a door in."
                : $"'{label}' cannot be placed: every doorway in this area already has a door in it.");
            return false;
        }

        private void NotifyPlacementChanged()
        {
            OnPropertyChanged(nameof(IsPlacementPending));
            OnPropertyChanged(nameof(PlacementStatus));
            OnPropertyChanged(nameof(HasViewportHud));
        }

        private InstanceMarker? _placementGhost;

        /// <summary>
        /// The object to draw under the cursor while a placement is armed, or null when nothing is
        /// armed. Only its geometry and kind are used; the viewport supplies the position from the
        /// pointer, so this is built once when placement is armed rather than per mouse move.
        /// </summary>
        public InstanceMarker? PlacementGhost
        {
            get => _placementGhost;
            private set
            {
                if (ReferenceEquals(_placementGhost, value))
                    return;

                _placementGhost = value;
                OnPropertyChanged(nameof(PlacementGhost));
            }
        }

        /// <summary>
        /// Resolves the armed blueprint's model so the ghost looks like what is about to be placed.
        /// A blueprint whose model cannot be resolved still gets a ghost - it just draws as the
        /// kind's marker, the same fallback the placed instance itself would use.
        /// </summary>
        /// <summary>
        /// Composes the creature from the fields embedded in its GIT instance. Those fields are the
        /// runtime creature and can differ from the source UTC, so neither template-only resolution nor
        /// a cache keyed solely by TemplateResRef is correct.
        /// </summary>
        private RenderModel? ResolveCreatureModel(JsonGffStruct instance)
        {
            if (_resolvePlacedCreatureModel == null)
                return null;

            try
            {
                return _resolvePlacedCreatureModel(instance);
            }
            catch (Exception ex)
            {
                var resRef = InstanceFieldMap.GetTemplateResRef(ResourceType.Utc, instance) ?? "(embedded)";
                _log.AppendLine($"Creature '{resRef}' could not be drawn and falls back to a marker: {ex.Message}");
                return null;
            }
        }

        private InstanceMarker? BuildPlacementGhost(
            ResourceType type,
            string resRef,
            bool useIndexedBlueprint)
        {
            var kind = MapSectionTypeToKind(type);
            if (kind == null)
                return null;

            var preview = default(BlueprintModelRenderResult);
            try
            {
                // A store has no appearance of its own. Aurora always previews it as the yellow
                // waypoint flag, which must be the same model the completed scene build uses.
                preview = kind == InstanceMarkerKind.Store
                    ? new BlueprintModelRenderResult(
                        _tileModelCache?.GetOrBuild(WaypointMarkerModel.MerchantModelResRef),
                        IsDoorTransition: false)
                    // Everything else is built through the same path as the palette thumbnail the
                    // builder just clicked, including segmented creatures whose body parts have to
                    // be composed.
                    : _resolveBlueprintModel?.Invoke(type, resRef, useIndexedBlueprint) ?? default;
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Placement preview for '{resRef}' fell back to a marker: {ex.Message}");
            }

            return new InstanceMarker
            {
                Kind = kind.Value,
                TemplateResRef = resRef,
                Tag = resRef,
                Position = Vector3.Zero,
                Orientation = new Vector2(1f, 0f),
                // The ghost has to be turned the same way the placed model will be, or creature and
                // waypoint artwork would swing a quarter turn the instant it was committed. Stores
                // use waypoint artwork too.
                VisualTransform = kind.Value switch
                {
                    InstanceMarkerKind.Creature => CreatureModelFacing.ForwardCorrection,
                    InstanceMarkerKind.Store => WaypointMarkerModel.ForwardCorrection,
                    InstanceMarkerKind.Waypoint => WaypointMarkerModel.ForwardCorrection,
                    _ => Matrix4x4.Identity
                },
                Model = preview.Model,
                IsDoorTransition = kind.Value == InstanceMarkerKind.Door && preview.IsDoorTransition,
                TintMapOverrides = preview.TintMapOverrides ??
                                   new Dictionary<string, int>(StringComparer.Ordinal)
            };
        }

        /// <summary>
        /// Called by the view when a viewport click resolves a pending placement
        /// (AreaViewportControl.PlacementPointPicked): creates the instance at the clicked ground
        /// position through the pending section's InstanceFieldMap-based Add path (one RunGitEdit
        /// transaction), then appends its one marker to the scene and selects it. A full rebuild is
        /// retained as a fallback when the scene was already stale.
        /// </summary>
        /// <param name="orientation">
        /// The heading to hang the new instance at, when the viewport chose one - a door takes the
        /// heading of the doorway it snapped into. Null leaves the blueprint's own default.
        /// </param>
        public void CommitPlacement(Vector3 position, Vector2? orientation = null)
        {
            var section = _pendingPlacementSection;
            var resRef = _pendingPlacementResRef;
            var useIndexedBlueprint = _pendingPlacementUsesIndexedBlueprint;
            var copiedInstance = _pendingPlacementCopy;
            _pendingPlacementSection = null;
            _pendingPlacementResRef = null;
            _pendingPlacementUsesIndexedBlueprint = false;
            _pendingPlacementCopy = null;
            PlacementGhost = null;
            NotifyPlacementChanged();

            if (section == null || (copiedInstance == null && resRef == null))
                return;

            // Orientation goes in with the add rather than as a follow-up edit, so hanging a door in a
            // doorway is one undo step and not two. A copied object retains its authored heading when
            // no placement rule supplied one.
            var copiedFacing = copiedInstance != null
                ? InstanceFieldMap.GetOrientation(section.BlueprintType, copiedInstance.Instance)
                : (1f, 0f);
            var facing = orientation ?? new Vector2(copiedFacing.Item1, copiedFacing.Item2);
            var previousRevision = Volatile.Read(ref _sceneInputRevision);
            var canUpdateCurrentScene =
                _sceneBuildRequested &&
                AreaScene != null &&
                Volatile.Read(ref _builtSceneInputRevision) == previousRevision;
            var added = copiedInstance != null
                ? section.AddCopiedInstanceAt(
                    copiedInstance,
                    position.X,
                    position.Y,
                    position.Z,
                    facing.X,
                    facing.Y)
                : section.AddInstanceAt(
                    resRef!,
                    position.X,
                    position.Y,
                    position.Z,
                    facing.X,
                    facing.Y,
                    useIndexedBlueprint);
            if (!added)
                return;

            var kind = MapSectionTypeToKind(section.BlueprintType);
            var reselect = kind != null ? (kind.Value, section.Rows.Count - 1) : ((InstanceMarkerKind, int)?)null;
            if (kind != null && canUpdateCurrentScene &&
                ApplyPlacementInPlace(section, section.Rows.Count - 1, previousRevision))
            {
                return;
            }

            _ = BuildSceneAsync(reselect);
        }

        /// <summary>
        /// Publishes one newly placed instance into the current scene without rebuilding the area.
        /// </summary>
        /// <remarks>
        /// Placement changes one GIT list entry. The tiles, walkmeshes, lighting, diagnostics, door
        /// anchors, and every pre-existing marker are already valid and are carried forward by
        /// reference. The revision checks make this an optimization only: if any other edit was
        /// pending, the caller falls back to authoritative full scene assembly.
        /// </remarks>
        private bool ApplyPlacementInPlace(
            InstanceListSectionViewModel section,
            int index,
            long previousRevision)
        {
            var currentRevision = Volatile.Read(ref _sceneInputRevision);
            if (currentRevision != previousRevision + 1 ||
                Volatile.Read(ref _builtSceneInputRevision) != previousRevision ||
                AreaScene is not { } scene ||
                _tileModelCache == null ||
                section.GetInstanceForScene(index) is not { } instance)
            {
                return false;
            }

            InstanceMarker marker;
            try
            {
                marker = AreaSceneBuilder.BuildInstanceMarker(
                    section.BlueprintType,
                    instance,
                    _tileModelCache,
                    _placeableAppearances,
                    _doorTypes,
                    _waypointAppearances,
                    ResolveCreatureModel,
                    scene.Tiles,
                    index);
            }
            catch (Exception ex)
            {
                _log.AppendLine(
                    $"Placed {section.BlueprintType.SingularDisplayName().ToLowerInvariant()} marker " +
                    $"could not be updated in place; rebuilding the scene: {ex.Message}");
                return false;
            }

            // A background build or another edit may have won while model resolution ran. Never append
            // to a scene that is no longer the one these revision checks described.
            if (!ReferenceEquals(AreaScene, scene) ||
                Volatile.Read(ref _sceneInputRevision) != currentRevision ||
                Volatile.Read(ref _builtSceneInputRevision) != previousRevision)
            {
                return false;
            }

            var updated = scene.WithInstanceAdded(marker);
            Volatile.Write(ref _builtSceneInputRevision, currentRevision);
            AreaScene = updated;
            ApplySelection(marker);
            SceneStatus = string.Empty;
            return true;
        }

        /// <summary>Called by the view when a pending placement is cancelled (Esc or right-click in the viewport, AreaViewportControl.PlacementCancelled).</summary>
        public void CancelPlacement()
        {
            if (_pendingPlacementSection == null)
                return;

            _pendingPlacementSection = null;
            _pendingPlacementResRef = null;
            _pendingPlacementUsesIndexedBlueprint = false;
            _pendingPlacementCopy = null;
            PlacementGhost = null;
            NotifyPlacementChanged();
        }

        /// <summary>Commits viewport instance transforms through the shared scene editor.</summary>
        public void MoveSelectedInstance(InstanceMarker instance, Vector3 newPosition)
        {
            if (AreaScene is not { } scene)
                return;

            var result = _sceneInstanceEditor.Move(scene, instance, newPosition);
            if (result.Outcome == AreaSceneInstanceEditOutcome.NoAvailableDoorway)
            {
                SceneStatus = $"\"{instance.Tag}\" can only be moved into an empty doorway.";
                return;
            }

            PublishInstanceEdit(result);
        }

        public void RotateSelectedInstance(InstanceMarker instance, Vector2 newOrientation)
        {
            if (AreaScene is not { } scene)
                return;
            PublishInstanceEdit(_sceneInstanceEditor.Rotate(scene, instance, newOrientation));
        }

        private void PublishInstanceEdit(AreaSceneInstanceEditResult result)
        {
            if (result.Outcome is AreaSceneInstanceEditOutcome.NoChange or
                AreaSceneInstanceEditOutcome.InvalidIdentity or
                AreaSceneInstanceEditOutcome.RotationNotSupported)
                return;

            RefreshGicDirty();
            Interlocked.Increment(ref _sceneInputRevision);
            AfterHistoryChange();
            if (result.Outcome == AreaSceneInstanceEditOutcome.Changed &&
                result.Scene is { } updated && result.UpdatedMarker is { } marker)
            {
                Volatile.Write(ref _builtSceneInputRevision, Volatile.Read(ref _sceneInputRevision));
                AreaScene = updated;
                ApplySelection(marker);
                return;
            }

            RequestSceneRefresh(immediate: true);
        }
        /// <summary>True when there is a selected instance this editor can actually rotate.</summary>
        /// <remarks>
        /// Ambient sounds are excluded even though they have an instance-list section like everything
        /// else: they carry no heading, so <c>InstanceFieldMap.SetOrientation</c> deliberately writes
        /// nothing for them. Left enabled, the buttons and the gizmo looked available, produced no edit,
        /// and the scene came back at the original heading - a control that answers every press by doing
        /// nothing reads as a broken editor, not an inapplicable one.
        /// </remarks>
        public bool CanRotateSelection =>
            SelectedSceneInstance is { } instance &&
            instance.Kind != InstanceMarkerKind.Sound &&
            instance.Kind != InstanceMarkerKind.Door &&
            SectionForKind(instance.Kind) != null;

        // ----- Terrain paint / rotate / raise-lower tools -----

        /// <summary>
        /// Memo of the corpus tile-frequency ranking per (module, tileset). The scan reads every
        /// .are in the module, so without this every area editor opened would repeat it for the same
        /// tileset. Shared across editors because the answer depends only on what is on disk.
        /// </summary>
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
            TileWalkmeshCache? tileWalkmeshCache = null,
            IEditorPromptService? prompts = null,
            Func<ResourceType?, string?, string?>? resolveBlueprintName = null,
            Action<ResourceType, string>? openBlueprint = null,
            Func<uint, string?>? resolveStrRef = null,
            WaypointAppearanceService? waypointAppearances = null,
            Func<ResourceType, string, bool, BlueprintModelRenderResult>? resolveBlueprintModel = null,
            IScriptSlotHost? scriptSlotHost = null,
            Func<JsonGffStruct, RenderModel?>? resolvePlacedCreatureModel = null,
            Doors.DoorEditorServices? doorEditorServices = null,
            Waypoints.WaypointEditorServices? waypointEditorServices = null,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveSoundChoices = null,
            IReadOnlyList<string>? audioResources = null,
            Services.SoundPreviewService? soundPreview = null,
            AreaEditorDocumentLoad? loadedDocuments = null,
            Func<ResourceType, string, string?>? editCopyBlueprint = null,
            ModuleMutationLock? mutationLock = null,
            AreaInstanceClipboard? instanceClipboard = null,
            Action<uint>? openTlkRow = null)
        {
            _scriptSlotHost = scriptSlotHost;
            _resolveBlueprintModel = resolveBlueprintModel;
            _resolvePlacedCreatureModel = resolvePlacedCreatureModel;
            _waypointAppearances = waypointAppearances;
            _resolveBlueprintName = resolveBlueprintName;
            _openBlueprint = openBlueprint;
            _editCopyBlueprint = editCopyBlueprint;
            _mutationLock = mutationLock;
            _instanceClipboard = instanceClipboard ?? new AreaInstanceClipboard();
            if (_mutationLock != null)
                _mutationLock.Changed += OnMutationLockChanged;
            _log = log;
            _lookups = lookups;
            _workspace = workspace;
            _areResRef = areResRef;
            _tilesetCatalog = tilesetCatalog;
            _tileModelCache = tileModelCache;
            _placeableAppearances = placeableAppearances;
            _doorTypes = doorTypes;
            _tileWalkmeshCache = tileWalkmeshCache;
            _prompts = prompts ?? throw new ArgumentNullException(nameof(prompts));
            ResourceIndex = resourceIndex;
            Id = $"area-editor:{areResRef}";

            var arePath = workspace.GetResourcePath(ResourceType.Area, areResRef);
            var gitPath = Path.Combine(workspace.ModuleRoot, "git", areResRef + ".git.json");
            var gicPath = Path.Combine(workspace.ModuleRoot, "gic", areResRef + ".gic.json");

            _areSession = loadedDocuments == null
                ? DocumentSession.Open(arePath)
                : DocumentSession.FromLoadedContent(
                    arePath, loadedDocuments.Are, loadedDocuments.AreBytes);
            _gitSession = loadedDocuments == null
                ? DocumentSession.Open(gitPath)
                : DocumentSession.FromLoadedContent(
                    gitPath, loadedDocuments.Git, loadedDocuments.GitBytes);
            _gicSession = loadedDocuments == null
                ? DocumentSession.Open(gicPath)
                : DocumentSession.FromLoadedContent(
                    gicPath, loadedDocuments.Gic, loadedDocuments.GicBytes);
            _documentEdits = new AreaDocumentEditSession(_areSession, _gitSession, _gicSession);
            _instanceEditor = new AreaInstanceEditor(_documentEdits);
            _sceneInstanceEditor = new AreaSceneInstanceEditor(_instanceEditor);
            _tileEditor = new AreaTileEditor(_documentEdits, ResolveTilesetForEditing);
            _tileEditor.PaintRejected += () => PaintRejected?.Invoke();
            _savedGicBytes = _gicSession.ToBytes();

            var areContext = new EditorFieldContext(
                _areSession.Document,
                (description, mutation) => RunAreEdit(description, mutation),
                resolveStrRef,
                openTlkRow);
            foreach (var group in AreSchema.Build().Groups)
            {
                var fields = group.Fields.Select(descriptor => CreateFieldViewModel(descriptor, areContext, lookups, scriptSlotHost)).ToList();
                AreaPropertyGroups.Add(new EditorGroup(group.Title, fields));
            }

            foreach (var config in InstanceListConfigs)
            {
                Sections.Add(new InstanceListSectionViewModel(
                    config.Title, config.ListFieldName, config.BlueprintType,
                    _gitSession, _gicSession, workspace, RunGitEdit, gameCodeIndex, log, _prompts, resolveStrRef,
                    config.BlueprintType == ResourceType.Utd ? doorEditorServices : null,
                    config.BlueprintType == ResourceType.Utw ? waypointEditorServices : null,
                    areResRef,
                    resolveSoundChoices,
                    audioResources,
                    soundPreview));
            }

            // A row click in any section should update the 3D-view highlight
            // (and clear every other section's own selection) via ApplySelection.
            foreach (var section in Sections)
            {
                section.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(InstanceListSectionViewModel.SelectedRow))
                        OnSectionSelectionChanged(section);

                };

                // Every path that changes a list ends in that section's RefreshFromDocument, so this
                // is the one place the Area Contents panel has to hear about to stay in step.
                section.RowsRefreshed += () => ContentsChanged?.Invoke();
            }

            UpdateTitle();
        }

        // ----- what the Area Contents panel reads and drives -----

        /// <summary>The area's resref, without the dirty marker the tab title carries.</summary>
        public string AreaResRef => _areResRef;

        /// <summary>Raised whenever any placed-instance list changed.</summary>
        public event Action? ContentsChanged;

        /// <summary>
        /// Reclassifies the selected placed waypoint, and supplies the same fresh catalog to any
        /// waypoint selected later in this open area.
        /// </summary>
        public void RefreshWaypointCatalog(
            Domain.Editors.Waypoints.WaypointBehaviorCatalog catalog)
        {
            Sections.FirstOrDefault(section => section.BlueprintType == ResourceType.Utw)
                ?.RefreshWaypointCatalog(catalog);
        }

        /// <summary>
        /// Asks the view to put the camera on a world position and show the map if it is not in
        /// front. Raised rather than acted on here because the camera belongs to the GL control,
        /// which this view model deliberately does not own.
        /// </summary>
        public event Action<Vector3>? CameraFocusRequested;

        /// <summary>Asks Area Contents to expand, select, and scroll to one exact placement.</summary>
        public event Action<ResourceType, int>? AreaContentsRevealRequested;

        /// <summary>Asks the view to scroll the requested instance editor into view.</summary>
        public event Action<InstanceListSectionViewModel>? InstancePropertiesRequested;

        /// <summary>
        /// Consumes the newest camera request. The request remains here when Go To opens an area
        /// whose visual tree has not been created yet, and is taken when that view attaches.
        /// </summary>
        public bool TryTakePendingCameraFocus(out Vector3 position)
        {
            if (_pendingCameraFocusRequest is not { } pending)
            {
                position = default;
                return false;
            }

            _pendingCameraFocusRequest = null;
            position = pending;
            return true;
        }

        /// <summary>
        /// Consumes the newest Area Contents reveal. This mirrors the camera hand-off so a newly
        /// activated area cannot outrun the singleton panel's active-document update.
        /// </summary>
        public bool TryTakePendingAreaContentsReveal(out ResourceType type, out int index)
        {
            if (_pendingAreaContentsReveal is not { } pending)
            {
                type = default;
                index = -1;
                return false;
            }

            _pendingAreaContentsReveal = null;
            type = pending.Type;
            index = pending.Index;
            return true;
        }

        private void RequestCameraFocus(Vector3 position)
        {
            _pendingCameraFocusRequest = position;
            CameraFocusRequested?.Invoke(position);
        }

        private void RequestAreaContentsReveal(ResourceType type, int index)
        {
            _pendingAreaContentsReveal = (type, index);
            AreaContentsRevealRequested?.Invoke(type, index);
        }

        /// <summary>
        /// The name to show for one placement: its own if it has one, then its blueprint's, then its
        /// tag, and the resref last. Never blank, so no row in the contents tree is nameless.
        /// </summary>
        public string ResolveInstanceName(ResourceType type, InstanceRow row)
        {
            if (!string.IsNullOrWhiteSpace(row.DisplayName))
                return row.DisplayName;

            var blueprintName = _resolveBlueprintName?.Invoke(type, row.TemplateResRef);
            if (!string.IsNullOrWhiteSpace(blueprintName))
                return blueprintName;

            if (!string.IsNullOrWhiteSpace(row.Tag))
                return row.Tag;

            return string.IsNullOrWhiteSpace(row.TemplateResRef)
                ? "(unnamed)"
                : row.TemplateResRef;
        }

        /// <summary>The section holding <paramref name="type"/>'s placements, or null.</summary>
        public InstanceListSectionViewModel? SectionFor(ResourceType type) =>
            Sections.FirstOrDefault(section => section.BlueprintType == type);

        /// <summary>
        /// Selects one placement and, when <paramref name="frameCamera"/> is set, flies the camera to
        /// it - what a double-click in the Area Contents tree does.
        /// </summary>
        /// <remarks>
        /// Selection goes through the section's own SelectedRow, the same property a grid row click
        /// sets, so both routes land in <see cref="OnSectionSelectionChanged"/> and there is one
        /// selection path rather than two. That path already handles a scene that has not finished
        /// building, holding the choice until the first build binds it to a marker.
        /// </remarks>
        public void RevealInstance(ResourceType type, int index, bool frameCamera)
        {
            if (SectionFor(type) is not { } section || index < 0 || index >= section.Rows.Count)
                return;

            EnsureSceneBuilt();

            var row = section.Rows[index];
            section.SelectedRow = row;

            if (frameCamera)
                RequestCameraFocus(new Vector3(row.X, row.Y, row.Z));
        }

        /// <summary>
        /// Opens the placed instance's own editable details, rather than the blueprint it was copied
        /// from. Used by both Area Contents and the scene's right-click menu.
        /// </summary>
        public void OpenInstanceProperties(ResourceType type, int index)
        {
            if (SectionFor(type) is not { } section || index < 0 || index >= section.Rows.Count)
                return;

            RevealInstance(type, index, frameCamera: false);
            section.IsExpanded = true;
            SelectedRootTabIndex = 1;
            InstancePropertiesRequested?.Invoke(section);
        }

        /// <summary>
        /// Reveals an indexed source placement. The saved list index is preferred, then a
        /// resref-and-position match protects navigation when an already-open area has unsaved
        /// insertions or deletions that shifted its indices.
        /// </summary>
        public void RevealPlacement(ObjectPlacement placement)
        {
            if (SectionFor(placement.BlueprintType) is not { } section)
                return;

            var index = placement.InstanceIndex;
            if (index < 0 || index >= section.Rows.Count ||
                !MatchesPlacementIdentity(section.Rows[index], placement))
            {
                index = section.Rows
                    .Where(row => MatchesPlacementIdentity(row, placement))
                    .Select(row => row.Index)
                    .FirstOrDefault(-1);
            }

            if (index >= 0)
            {
                RevealInstance(placement.BlueprintType, index, frameCamera: true);
                RequestAreaContentsReveal(placement.BlueprintType, index);
            }
        }

        private static bool MatchesPlacementIdentity(InstanceRow row, ObjectPlacement placement) =>
            string.Equals(
                row.TemplateResRef,
                placement.BlueprintResRef,
                StringComparison.OrdinalIgnoreCase) &&
            string.Equals(row.Tag, placement.Tag, StringComparison.OrdinalIgnoreCase) &&
            MathF.Abs(row.X - placement.X) < 0.001f &&
            MathF.Abs(row.Y - placement.Y) < 0.001f &&
            MathF.Abs(row.Z - placement.Z) < 0.001f;

        [RelayCommand(CanExecute = nameof(HasSceneSelection))]
        private void OpenSelectedInstanceProperties()
        {
            if (SelectedSceneInstance is not { } instance ||
                MapKindToSectionType(instance.Kind) is not { } type)
                return;

            var index = IndexWithinKind(instance);
            if (index >= 0)
                OpenInstanceProperties(type, index);
        }

        /// <summary>Deletes placements of one kind as a single undo entry.</summary>
        public bool DeleteInstances(ResourceType type, IReadOnlyList<int> indices) =>
            SectionFor(type)?.DeleteInstances(indices) ?? false;

        /// <summary>
        /// Deletes whatever the map has selected - what Delete does with focus in the viewport.
        /// Returns false when nothing is selected, so the key can fall through to anything else.
        /// </summary>
        public bool DeleteSelectedSceneInstance()
        {
            if (SelectedSceneInstance is not { } instance ||
                MapKindToSectionType(instance.Kind) is not { } type)
                return false;

            var index = IndexWithinKind(instance);
            return index >= 0 && DeleteInstances(type, new[] { index });
        }

        /// <summary>
        /// Builds the 3D scene. Called by the view as soon as the editor has a document, so opening
        /// an area shows its map rather than an empty viewport. Safe to call repeatedly and before
        /// game-data services are available.
        /// </summary>
        public void EnsureSceneBuilt()
        {
            if (IsSceneCurrent())
                return;

            var reselect = CaptureReselectKey();
            if (_sceneBuildRequested && AreaScene != null)
                AreaScene = null;
            _sceneBuildRequested = true;
            _ = BuildSceneAsync(reselect);
        }

        /// <summary>
        /// Re-resolves every tileset, model, walkmesh, and texture after the module HAK stack is
        /// replaced. The authored ARE/GIT data is untouched; only its resource-backed scene is rebuilt.
        /// </summary>
        public void ReloadGameResources()
        {
            if (_disposed)
                return;

            Interlocked.Increment(ref _sceneInputRevision);
            _sceneBuildRequested = true;
            AreaScene = null;
            GameResourceRevision++;
            _ = BuildSceneAsync(CaptureReselectKey());
        }

        /// <summary>
        /// Changes only when Module Properties replaces the active HAK stack. The view uses this
        /// signal to release GL-side models and textures before the rebuilt scene is displayed.
        /// </summary>
        public int GameResourceRevision
        {
            get => _gameResourceRevision;
            private set => SetProperty(ref _gameResourceRevision, value);
        }

        private int _gameResourceRevision;

        /// <summary>
        /// True when the scene already reflects the current documents, or a build for them is in
        /// flight - the guard that keeps repeated refresh requests from queueing duplicate builds.
        /// </summary>
        private bool IsSceneCurrent()
        {
            if (!_sceneBuildRequested)
                return false;

            var inputRevision = Volatile.Read(ref _sceneInputRevision);
            return Volatile.Read(ref _builtSceneInputRevision) == inputRevision ||
                   (IsBuildingScene && Volatile.Read(ref _buildingSceneInputRevision) == inputRevision);
        }

        /// <summary>
        /// How long an edit waits before the 3D view catches up. Long enough that dragging a
        /// NumericUpDown or holding Ctrl+Z does not queue a rebuild per keystroke, short enough that
        /// the map feels like it is tracking the edit.
        /// </summary>
        private static readonly TimeSpan SceneRefreshDelay = TimeSpan.FromMilliseconds(220);

        private CancellationTokenSource? _sceneRefreshCts;

        /// <summary>
        /// Brings the 3D view back in line with the documents after an edit, debounced.
        /// </summary>
        /// <remarks>
        /// This is what replaced the Rebuild button. Edits made anywhere - an instance's coordinates
        /// on the Properties tab, a local variable, an area property, undo/redo - all funnel through
        /// <see cref="RunEdit"/>, so hooking the refresh there covers every path instead of asking
        /// each command to remember. Paths that already refresh explicitly (placement and the
        /// gizmos, which also need to reselect what they touched) are not double-served: by the time
        /// the delay elapses their incremental update or fallback build has claimed the same revision,
        /// and <see cref="IsSceneCurrent"/> drops the duplicate.
        /// </remarks>
        private void RequestSceneRefresh(bool immediate = false)
        {
            // Whatever changed the grid also invalidated every answer about where a tile may go.
            // Hooked here rather than at each edit site because this is the one thing every path
            // through - paint, stamp, undo, redo, an external file change - has to call.
            InvalidateTilePlacementValidity();

            if (!_sceneBuildRequested || _disposed)
                return;

            _sceneRefreshCts?.Cancel();
            _sceneRefreshCts?.Dispose();

            // A viewport tile edit skips the debounce entirely: the reference toolset repaints the
            // grid the moment the click lands, and a fifth of a second of dead air after every dab
            // of terrain is what made painting feel laggy by comparison. The delay exists for
            // keyboard-repeat edit streams (NumericUpDown drags, held Ctrl+Z), which still take it.
            if (immediate)
            {
                _sceneRefreshCts = null;
                if (!IsSceneCurrent())
                    _ = BuildSceneAsync(CaptureReselectKey());
                return;
            }

            var cts = new CancellationTokenSource();
            _sceneRefreshCts = cts;

            _ = RefreshSceneAfterDelayAsync(cts.Token);
        }

        private async Task RefreshSceneAfterDelayAsync(CancellationToken token)
        {
            try
            {
                await Task.Delay(SceneRefreshDelay, token).ConfigureAwait(true);
            }
            catch (OperationCanceledException)
            {
                return; // Superseded by a later edit; that one owns the rebuild.
            }

            if (token.IsCancellationRequested || _disposed || IsSceneCurrent())
                return;

            _ = BuildSceneAsync(CaptureReselectKey());
        }

        /// <summary>
        /// Rebuilds the scene. When <paramref name="reselect"/> is supplied (a gizmo edit,
        /// placement, or an undo/redo of one), the instance at that kind+index in the freshly
        /// built scene is reselected (rebind by kind+index - the fresh scene's InstanceMarker
        /// objects never equal the old ones by reference); otherwise any stale selection is
        /// dropped, matching the behavior for a plain rebuild.
        /// </summary>
        private async Task BuildSceneAsync((InstanceMarkerKind Kind, int Index)? reselect = null)
        {
            if (_tilesetCatalog == null || _tileModelCache == null)
            {
                SceneStatus = "3D view unavailable (game data services not loaded).";
                return;
            }

            var generation = Interlocked.Increment(ref _sceneBuildGeneration);
            var inputRevision = Volatile.Read(ref _sceneInputRevision);
            Volatile.Write(ref _buildingSceneInputRevision, inputRevision);

            // The banner is deferred rather than shown outright: a build that finishes inside the
            // grace period never raises it. Showing it immediately meant a dark panel blinking over
            // the middle of the viewport on every edit, which read as the app fighting the builder
            // even when the rebuild itself was quick.
            var finished = false;
            _ = RevealBuildingBannerAsync();

            async Task RevealBuildingBannerAsync()
            {
                await Task.Delay(SceneBuildBannerDelay).ConfigureAwait(true);

                if (finished || generation != Volatile.Read(ref _sceneBuildGeneration))
                    return;

                SceneStatus = "Building scene...";
                IsBuildingScene = true;
            }

            var tilesetCatalog = _tilesetCatalog;
            var tileModelCache = _tileModelCache;

            try
            {
                // Serialize both sessions under their shared edit locks on the worker thread. The
                // builder then owns immutable bytes and never reads a live document graph.
                var scene = await Task.Run(() =>
                {
                    var snapshots = DocumentSession.CaptureSnapshots(_areSession, _gitSession);
                    return AreaSceneBuilder.Build(
                        AreDocument.Parse(snapshots[0]), GitDocument.Parse(snapshots[1]),
                        tilesetCatalog, tileModelCache, _placeableAppearances, _doorTypes, _tileWalkmeshCache,
                        _waypointAppearances, ResolveCreatureModel);
                });

                if (generation != Volatile.Read(ref _sceneBuildGeneration))
                    return;

                // An edit landed after this build captured its input revision. Do not publish a
                // stale scene even briefly; supersede it with a build against the current docs.
                if (inputRevision != Volatile.Read(ref _sceneInputRevision))
                {
                    _ = BuildSceneAsync(reselect ?? CaptureReselectKey());
                    return;
                }

                Volatile.Write(ref _builtSceneInputRevision, inputRevision);
                AreaScene = scene;

                InstanceMarker? toSelect = null;
                if (reselect is { } key)
                {
                    var kindInstances = scene.Instances.Where(i => i.Kind == key.Kind).ToList();
                    if (key.Index >= 0 && key.Index < kindInstances.Count)
                        toSelect = kindInstances[key.Index];
                }
                else if (_pendingSectionSelection is { } pending &&
                         MapSectionTypeToKind(pending.Type) is { } pendingKind)
                {
                    var kindInstances = scene.Instances.Where(i => i.Kind == pendingKind).ToList();
                    if (pending.Index >= 0 && pending.Index < kindInstances.Count)
                        toSelect = kindInstances[pending.Index];
                }

                // Every previous scene's InstanceMarker objects are gone now (Build returns a fresh
                // list each time) - a selection with no reselect key (or whose key no longer
                // resolves) must be dropped rather than left pointing at objects no longer in this
                // scene.
                ApplySelection(toSelect);
                // Nothing to say when a build succeeds. The caption used to describe the area here, and
                // before that to count the renderer's tiles and instances; neither was worth a permanent
                // strip across the map. What the area is belongs on the Properties tab, which has it.
                SceneStatus = string.Empty;
            }
            catch (Exception ex)
            {
                if (generation == Volatile.Read(ref _sceneBuildGeneration))
                {
                    SceneStatus = $"Failed to build 3D scene: {ex.Message}";
                    _log.AppendLine($"Area 3D scene build failed for {_areResRef}: {ex.Message}");
                }
            }
            finally
            {
                finished = true;
                if (generation == Volatile.Read(ref _sceneBuildGeneration))
                    IsBuildingScene = false;
            }
        }

        /// <summary>
        /// How long a scene build may run before it admits to it. Below this a build is faster than
        /// the eye reads as a wait, and announcing it is pure flicker; above it, silence would look
        /// like the viewport had frozen.
        /// </summary>
        private static readonly TimeSpan SceneBuildBannerDelay = TimeSpan.FromMilliseconds(250);

        private static FieldViewModel CreateFieldViewModel(
            FieldDescriptor descriptor, EditorFieldContext context, LookupOptionProvider lookups,
            IScriptSlotHost? scriptSlotHost)
        {
            return descriptor.Kind switch
            {
                EditorKind.Integer => new IntegerFieldViewModel(descriptor, context),
                EditorKind.Float => new FloatFieldViewModel(descriptor, context),
                EditorKind.Check => new CheckFieldViewModel(descriptor, context),
                EditorKind.LocString => new LocStringFieldViewModel(descriptor, context),
                EditorKind.TwoDaDropdown => new DropdownFieldViewModel(
                    descriptor, context, lookups.GetOptions(descriptor.LookupKey)),
                EditorKind.ScriptSlot => new ScriptFieldViewModel(descriptor, context, scriptSlotHost),
                _ => new TextFieldViewModel(descriptor, context)
            };
        }

        private bool RunAreEdit(string description, Action mutation, bool immediateSceneRefresh = false) =>
            RunEdit(_areSession, description, mutation, immediateSceneRefresh);

        private bool RunGitEdit(string description, Action mutation)
        {
            var result = RunEdit(_gitSession, description, mutation);
            RefreshGicDirty();
            return result;
        }

        private bool RunEdit(DocumentSession session, string description, Action mutation,
            bool immediateSceneRefresh = false)
        {
            try
            {
                var changed = ReferenceEquals(session, _areSession)
                    ? _documentEdits.ExecuteArea(description, mutation)
                    : _documentEdits.ExecuteInstances(description, mutation);

                // A no-op neither changes undo order nor clears the other document's redo history.
                if (!changed)
                    return true;

                if (ReferenceEquals(session, _areSession))
                    _tileEditor?.InvalidatePlacementValidity();
                Interlocked.Increment(ref _sceneInputRevision);
                RequestSceneRefresh(immediateSceneRefresh);
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
        private async Task Save()
        {
            await TrySaveAsync().ConfigureAwait(true);
        }

        /// <summary>Saves all area documents, returning false if any prompt is cancelled or write fails.</summary>
        /// <remarks>
        /// An area is one thing to the builder but three files on disk, so every external-change prompt is
        /// answered before anything is written. Prompting between the two writes meant that cancelling
        /// the second prompt still left the first file saved and its history marked clean, and no later
        /// Discard could take that back.
        /// </remarks>
        public async Task<bool> TrySaveAsync()
        {
            // A Map Note instance requires MapNoteEnabled before it is written. The blueprint
            // editor already performs this normalization; embedded waypoint editors must get the
            // same save hook or the area silently writes an incomplete placement.
            foreach (var section in Sections)
            {
                if (!section.PrepareForSave())
                    return false;
            }

            // The area's catalog entry is parsed exclusively from ARE metadata. GIT carries placed
            // instances, so treating a GIT-only save as an area catalog change makes Explorer and
            // Search rebuild their catalog views for a file they never read.
            var catalogEntryChanged = _areSession.UndoStack.IsDirty;
            var placementsChanged = _gitSession.UndoStack.IsDirty;
            var instancePairReloaded = false;
            var tilesetBefore = TilesetResRef;

            var arePlan = await PlanSaveAsync(_areSession).ConfigureAwait(true);
            if (arePlan == SavePlan.Cancel)
                return false;

            // GIT instances and GIC comments are parallel lists addressed by index, so whenever
            // either is going to be written both have to be checked. Editing a GIC comment while an
            // external tool reordered the GIT used to write the stale GIC straight over it without
            // noticing - exactly the correspondence ReloadInstancePair exists to protect - because
            // the clean half returned Nothing before it ever looked at the file.
            var pairWillBeWritten = _gitSession.UndoStack.IsDirty || _gicDirty;

            var gitPlan = await PlanSaveAsync(
                _gitSession, checkExternalWhenClean: pairWillBeWritten).ConfigureAwait(true);
            if (gitPlan == SavePlan.Cancel)
                return false;

            if (gitPlan == SavePlan.Reload)
            {
                if (!ReloadInstancePair())
                    return false;
                instancePairReloaded = true;
                gitPlan = SavePlan.Nothing;
            }

            var gicPlan = await PlanSaveAsync(
                _gicSession, _gicDirty, checkExternalWhenClean: pairWillBeWritten).ConfigureAwait(true);
            if (gicPlan == SavePlan.Cancel)
                return false;

            if (gicPlan == SavePlan.Reload)
            {
                if (!ReloadInstancePair())
                    return false;
                instancePairReloaded = true;
                gitPlan = SavePlan.Nothing;
                gicPlan = SavePlan.Nothing;
            }

            // All files are staged before any replaces its file. An area is one logical document
            // split across three files, and writing them in sequence meant a locked or unwritable .git
            // left the .are already replaced on disk and its history marked clean - a half-saved area
            // that no later Discard or Close could take back.
            if (!TryStageWrites(arePlan, gitPlan, gicPlan, out var staged))
                return false;

            // A Reload parses external bytes and can fail - the file may be malformed or gone by
            // the time the prompt is answered. Complete it before any staged companion write
            // replaces its file, so a failed ARE reload cannot leave the GIT/GIC committed at a
            // newer generation while this method returns false; the save stays all-or-nothing.
            var areReloadedEarly = false;
            if (arePlan == SavePlan.Reload)
            {
                var earlyResult = ApplySavePlan(_areSession, arePlan);
                if (!earlyResult.Success)
                {
                    foreach (var write in staged)
                        SwlorFileWriteAccess.Writer.Discard(write);
                    return false;
                }
                areReloadedEarly = true;
                arePlan = SavePlan.Nothing;
                RefreshAreaPropertyFields();
                if (_sceneBuildRequested)
                    _ = BuildSceneAsync(CaptureReselectKey());
                if (!string.Equals(tilesetBefore, TilesetResRef, StringComparison.OrdinalIgnoreCase))
                    TilesetChanged?.Invoke();
                AfterHistoryChange();
            }

            if (!CommitStagedWrites(staged, arePlan, gitPlan, gicPlan))
                return false;

            var areResult = ApplySavePlan(_areSession, arePlan);
            if (!areResult.Success)
                return false;
            areResult = (areResult.Success, areResult.Reloaded || areReloadedEarly);

            var gitResult = ApplySavePlan(_gitSession, gitPlan);
            if (!gitResult.Success)
                return false;

            var gicResult = ApplySavePlan(_gicSession, gicPlan);
            if (!gicResult.Success)
                return false;
            if (gicPlan == SavePlan.Write)
            {
                _savedGicBytes = _gicSession.ToBytes();
                _gicDirty = false;
            }

            if (areResult.Reloaded)
                RefreshAreaPropertyFields();
            if (gitResult.Reloaded || gicResult.Reloaded || instancePairReloaded)
                RefreshInstanceSections();
            if ((areResult.Reloaded || gitResult.Reloaded || gicResult.Reloaded ||
                 instancePairReloaded) && _sceneBuildRequested)
                _ = BuildSceneAsync(CaptureReselectKey());

            // Reloading the .are can bring in a different tileset, and the Tiles palette lists the
            // front area's tileset - without this it keeps offering the previous set's tiles.
            if (areResult.Reloaded &&
                !string.Equals(tilesetBefore, TilesetResRef, StringComparison.OrdinalIgnoreCase))
                TilesetChanged?.Invoke();

            AfterHistoryChange();
            if (catalogEntryChanged || areResult.Reloaded)
            {
                CatalogEntryChanged?.Invoke();
            }
            if (placementsChanged || gitResult.Reloaded || instancePairReloaded)
                PlacementsChanged?.Invoke();
            return true;
        }

        /// <summary>Raised when a reload replaces this area's tileset, so tile-facing UI can re-read it.</summary>
        public event Action? TilesetChanged;

        /// <summary>What a save should do with one session, once its external-change prompt is answered.</summary>
        private enum SavePlan
        {
            Nothing,
            Write,
            Reload,
            Cancel
        }

        /// <summary>Answers a session's external-change prompt without writing anything yet.</summary>
        /// <param name="checkExternalWhenClean">
        /// Asks about an external change even though this session has no edits of its own - for the
        /// clean half of the GIT/GIC pair, whose file is about to become inconsistent with its
        /// partner's. Choosing Overwrite then writes the clean half too, because the edit being
        /// saved was made against the version held in memory.
        /// </param>
        private async Task<SavePlan> PlanSaveAsync(
            DocumentSession session,
            bool? dirtyOverride = null,
            bool checkExternalWhenClean = false)
        {
            var isDirty = dirtyOverride ?? session.UndoStack.IsDirty;
            if (!isDirty && !checkExternalWhenClean)
                return SavePlan.Nothing;

            try
            {
                if (!session.HasExternalChange())
                    return isDirty ? SavePlan.Write : SavePlan.Nothing;
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not check {session.FilePath} for external changes: {ex.Message}");
                return SavePlan.Cancel;
            }

            var choice = await _prompts.ConfirmExternalChangeAsync(session.FilePath).ConfigureAwait(true);
            if (choice == ExternalChangeChoice.Overwrite)
            {
                // The builder accepted the version currently on disk. Make that the compare-and-swap
                // baseline so the final pre-commit recheck catches only a later change.
                session.RecordCurrentFileState();
            }

            return choice switch
            {
                ExternalChangeChoice.Cancel => SavePlan.Cancel,
                ExternalChangeChoice.Reload => SavePlan.Reload,
                _ => SavePlan.Write
            };
        }

        /// <summary>
        /// Serializes and writes every session that is being saved to its temporary file, touching no
        /// real file. Returns false - having thrown away anything it had already staged - if any of them
        /// could not be written, so a failure leaves the area exactly as it was.
        /// </summary>
        private bool TryStageWrites(
            SavePlan arePlan,
            SavePlan gitPlan,
            SavePlan gicPlan,
            out List<AtomicFileGroupWriter.StagedWrite> staged)
        {
            staged = new List<AtomicFileGroupWriter.StagedWrite>(3);

            foreach (var (session, plan) in new[]
                     {
                         (_areSession, arePlan),
                         (_gitSession, gitPlan),
                         (_gicSession, gicPlan)
                     })
            {
                if (plan != SavePlan.Write)
                    continue;

                try
                {
                    staged.Add(SwlorFileWriteAccess.Writer.Stage(session.FilePath, session.ToBytes()));
                }
                catch (Exception ex)
                {
                    _log.AppendLine($"Save failed for {session.FilePath}: {ex.Message}");
                    foreach (var done in staged)
                        SwlorFileWriteAccess.Writer.Discard(done);

                    staged.Clear();
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Replaces every staged file as one logical save, rolling all earlier replacements back if a
        /// later destination cannot be replaced.
        /// </summary>
        private bool CommitStagedWrites(
            List<AtomicFileGroupWriter.StagedWrite> staged,
            SavePlan arePlan,
            SavePlan gitPlan,
            SavePlan gicPlan)
        {
            try
            {
                // The last fingerprint check and the grouped replacement are one critical section.
                // Without this outer lease another toolset process can save after HasExternalChange
                // answers false but before CommitAll acquires its own (re-entrant) module lease,
                // and this save would silently replace that newer generation.
                using var moduleWriteLock = staged.Count == 0
                    ? null
                    : ModuleWriteLock.AcquireForResourcePath(staged[0].TargetPath);
                var instancePairBeingWritten =
                    gitPlan == SavePlan.Write || gicPlan == SavePlan.Write;

                foreach (var (session, plan, isInstancePairMember) in new[]
                         {
                             (_areSession, arePlan, false),
                             (_gitSession, gitPlan, true),
                             (_gicSession, gicPlan, true)
                         })
                {
                    var mustRecheck = plan == SavePlan.Write ||
                                      isInstancePairMember && instancePairBeingWritten;
                    if (mustRecheck && session.HasExternalChange())
                    {
                        foreach (var write in staged)
                            SwlorFileWriteAccess.Writer.Discard(write);
                        _log.AppendLine(
                            $"Area save stopped because {session.FilePath} changed while the save was being prepared.");
                        return false;
                    }
                }

                SwlorFileWriteAccess.Writer.CommitAll(staged);
                return true;
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Area save failed; the original files were restored: {ex.Message}");
                return false;
            }
        }

        private (bool Success, bool Reloaded) ApplySavePlan(DocumentSession session, SavePlan plan)
        {
            try
            {
                switch (plan)
                {
                    case SavePlan.Nothing:
                        return (true, false);

                    case SavePlan.Reload:
                        session.ReloadFromDisk();
                        _log.AppendLine($"Reloaded externally changed file {session.FilePath}.");
                        return (true, true);

                    default:
                        // The bytes are already on disk - TryStageWrites/CommitStagedWrites put them
                        // there before this ran. All that is left is to agree that they are.
                        session.UndoStack.MarkSaved();
                        session.RecordCurrentFileState(session.ToBytes());
                        _log.AppendLine($"Saved {session.FilePath}.");
                        return (true, false);
                }
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Save failed for {session.FilePath}: {ex.Message}");
                return (false, false);
            }
        }

        /// <summary>
        /// Undo/redo for the area-properties (.are) group's own small history. The .are
        /// history also carries tile paints, so this refreshes the 3D view too (when it has ever been
        /// built) - otherwise undoing a paint would leave the viewport showing the painted tiles.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanUndoAre))]
        private void UndoAre()
        {
            var reselect = CaptureReselectKey();

            _documentEdits.UndoArea();
            _tileEditor?.InvalidatePlacementValidity();
            RefreshAreaPropertyFields();

            if (_sceneBuildRequested)
                _ = BuildSceneAsync(reselect);

            AfterHistoryChange();
        }

        public bool CanUndoAre => _documentEdits.CanUndoArea;

        [RelayCommand(CanExecute = nameof(CanRedoAre))]
        private void RedoAre()
        {
            var reselect = CaptureReselectKey();

            _documentEdits.RedoArea();
            _tileEditor?.InvalidatePlacementValidity();
            RefreshAreaPropertyFields();

            if (_sceneBuildRequested)
                _ = BuildSceneAsync(reselect);

            AfterHistoryChange();
        }

        public bool CanRedoAre => _documentEdits.CanRedoArea;

        /// <summary>Undo/redo for the instance lists (.git) - the toolbar's primary pair, since
        /// placing/moving/removing instances is the bulk of this screen's editing. Also refreshes
        /// the 3D view when it has ever been built, rebinding the current selection by
        /// kind+index so undoing/redoing a 3D-view gizmo edit is visible without pressing Rebuild.</summary>
        [RelayCommand(CanExecute = nameof(CanUndoInstances))]
        private void UndoInstances()
        {
            var reselect = CaptureReselectKey();

            _documentEdits.UndoInstances();
            RefreshInstanceSections();
            RefreshGicDirty();

            if (_sceneBuildRequested)
                _ = BuildSceneAsync(reselect);

            AfterHistoryChange();
        }

        public bool CanUndoInstances => _documentEdits.CanUndoInstances;

        [RelayCommand(CanExecute = nameof(CanRedoInstances))]
        private void RedoInstances()
        {
            var reselect = CaptureReselectKey();

            _documentEdits.RedoInstances();
            RefreshInstanceSections();
            RefreshGicDirty();

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

        public bool CanRedoInstances => _documentEdits.CanRedoInstances;

        // ----- Shell Edit menu / Ctrl+Z / Ctrl+Y -----
        //
        // Implemented explicitly so the toolbar keeps its unambiguous per-session buttons: the two
        // histories stay separate, while this single-command view uses their shared ordering. That
        // view takes back the newest edit
        // whichever file it landed in, and falls back to whichever session still has history so it is
        // never a no-op while an undoable edit exists.

        bool IEditorDocument.CanUndo => _documentEdits.LastUndoableTarget != null;

        bool IEditorDocument.CanRedo => _documentEdits.LastRedoableTarget != null;

        void IEditorDocument.Undo()
        {
            var newest = _documentEdits.LastUndoableTarget;
            if (newest == AreaDocumentEditTarget.Area)
                UndoAre();
            else if (newest == AreaDocumentEditTarget.Instances)
                UndoInstances();
            else if (CanUndoInstances)
                UndoInstances();
            else if (CanUndoAre)
                UndoAre();
        }

        void IEditorDocument.Redo()
        {
            var newest = _documentEdits.LastRedoableTarget;
            if (newest == AreaDocumentEditTarget.Area)
                RedoAre();
            else if (newest == AreaDocumentEditTarget.Instances)
                RedoInstances();
            else if (CanRedoInstances)
                RedoInstances();
            else if (CanRedoAre)
                RedoAre();
        }

        /// <summary>Raised when the tab closes so the editor registry can forget this instance.</summary>
        public event Action<AreaEditorViewModel>? Closed;

        /// <summary>Raised after an async close prompt approves closing this tab.</summary>
        public event Action<AreaEditorViewModel>? CloseRequested;

        /// <summary>Raised after ARE data is saved or reloaded so the area's catalog metadata can refresh.</summary>
        public event Action? CatalogEntryChanged;

        /// <summary>Raised after the paired GIT is saved or reloaded so GIT-derived indexes can refresh.</summary>
        public event Action? PlacementsChanged;

        /// <summary>Suppresses a second tab-level prompt after the window-level discard decision.</summary>
        internal void ApproveApplicationClose() => _closeApproved = true;

        public override bool OnClose()
        {
            if (!_closeApproved && IsDirty)
            {
                if (!_closePromptOpen)
                {
                    _closePromptOpen = true;
                    _ = ConfirmCloseAsync();
                }

                return false;
            }

            if (_disposed)
                return base.OnClose();

            _disposed = true;
            if (_mutationLock != null)
                _mutationLock.Changed -= OnMutationLockChanged;
            foreach (var section in Sections)
                section.Dispose();
            _documentEdits.Dispose();
            Closed?.Invoke(this);
            return base.OnClose();
        }

        private async Task ConfirmCloseAsync()
        {
            try
            {
                var choice = await _prompts.ConfirmCloseAsync(Title ?? _areResRef).ConfigureAwait(true);
                var approved = choice == UnsavedChangesChoice.Discard ||
                    choice == UnsavedChangesChoice.Save && await TrySaveAsync().ConfigureAwait(true);
                if (!approved)
                    return;

                _closeApproved = true;
                CloseRequested?.Invoke(this);
            }
            finally
            {
                _closePromptOpen = false;
            }
        }

        private void RefreshAreaPropertyFields()
        {
            foreach (var group in AreaPropertyGroups)
            foreach (var field in group.Fields)
                field.RefreshFromDocument();
        }

        /// <summary>Re-resolves custom-TLK watermarks after the shared table is regenerated.</summary>
        public void RefreshTlkLabels()
        {
            var fields = AreaPropertyGroups.SelectMany(group => group.Fields).ToArray();
            foreach (var field in fields.OfType<LocStringFieldViewModel>())
                field.RefreshFromDocument();
            foreach (var field in fields.OfType<DropdownFieldViewModel>())
                field.RefreshOptions(_lookups.GetOptions(field.Descriptor.LookupKey));
            foreach (var section in Sections)
                section.RefreshTlkLabels();
        }

        private void RefreshInstanceSections()
        {
            foreach (var section in Sections)
                section.RefreshFromDocument();
        }

        /// <summary>
        /// True when an external blueprint refactor must not replace this area's GIT/GIC pair.
        /// ARE-only edits are deliberately excluded: reloading clean instance files does not touch
        /// unsaved terrain or area-property work.
        /// </summary>
        public bool HasUnsavedInstanceChanges =>
            _gitSession.UndoStack.IsDirty || _gicDirty;

        /// <summary>
        /// Picks up instance files written by a blueprint rename without closing a clean open area.
        /// </summary>
        public bool ReloadInstancesAfterBlueprintSave()
        {
            if (HasUnsavedInstanceChanges)
                return false;

            return ReloadInstancePair();
        }

        private void RefreshGicDirty()
        {
            _gicDirty = !_savedGicBytes.AsSpan().SequenceEqual(_gicSession.ToBytes());
            OnPropertyChanged(nameof(IsDirty));
        }

        /// <summary>
        /// GIT instances and GIC comments are parallel lists, so choosing Reload for either file
        /// discards the pair together. Reloading only one side would immediately corrupt their
        /// index correspondence.
        /// </summary>
        private bool ReloadInstancePair()
        {
            try
            {
                // Parse both externals before replacing either session: GIT instances and GIC
                // comments correspond by index, and a malformed or vanished GIC must not leave an
                // external GIT sitting beside the old locally edited comments.
                var externalGitBytes = File.ReadAllBytes(_gitSession.FilePath);
                var externalGicBytes = File.ReadAllBytes(_gicSession.FilePath);
                var externalGit = JsonGffDocument.Parse(externalGitBytes);
                var externalGic = JsonGffDocument.Parse(externalGicBytes);
                _gitSession.ReloadFrom(externalGit, externalGitBytes);
                _gicSession.ReloadFrom(externalGic, externalGicBytes);
                _savedGicBytes = _gicSession.ToBytes();
                _gicDirty = false;
                RefreshInstanceSections();
                _log.AppendLine(
                    $"Reloaded externally changed instance pair {_gitSession.FilePath} and {_gicSession.FilePath}.");
                return true;
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not reload the area's GIT/GIC pair: {ex.Message}");
                return false;
            }
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
