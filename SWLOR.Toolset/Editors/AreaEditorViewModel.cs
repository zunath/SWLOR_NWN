using System.Collections.ObjectModel;
using System.Numerics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Editors;
using SWLOR.Toolset.Domain.Editors.Schemas;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.Tilesets;
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
    /// Owns two independent DocumentSessions - one per file - because the .are and .git files
    /// are separate nwn_gff documents with separate undo histories. Undo/Redo is deliberately
    /// split rather than merged into one combined stack: the area-properties group gets its own
    /// small Undo/Redo pair (mirroring BlueprintEditorViewModel), while the toolbar's primary
    /// Undo/Redo acts on the .git session, since instance placement/deletion is the editing this
    /// screen is mostly used for. Save writes whichever session(s) are dirty; the title's dirty
    /// marker reflects either session being dirty.
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
        private readonly OutputLogService _log;
        private readonly ModuleWorkspace _workspace;
        private readonly string _areResRef;
        private readonly TilesetCatalog? _tilesetCatalog;
        private readonly TileModelCache? _tileModelCache;
        private readonly PlaceableAppearanceService? _placeableAppearances;
        private readonly DoorTypeService? _doorTypes;
        private readonly WaypointAppearanceService? _waypointAppearances;

        /// <summary>Builds an armed blueprint's geometry for the placement ghost. Null degrades the ghost to a marker.</summary>
        private readonly Func<ResourceType, string, RenderModel?>? _resolveBlueprintModel;
        private readonly TileWalkmeshCache? _tileWalkmeshCache;
        private readonly IEditorPromptService _prompts;

        /// <summary>Resolves a blueprint's display name from the catalog, so the selection bar can lead with it.</summary>
        private readonly Func<ResourceType?, string?, string?>? _resolveBlueprintName;

        /// <summary>Opens a blueprint in its own editor tab - the selection bar's one action.</summary>
        private readonly Action<ResourceType, string>? _openBlueprint;

        /// <summary>
        /// Which session each still-undoable edit went to, oldest first.
        /// </summary>
        /// <remarks>
        /// The toolbar keeps a dedicated button pair per session; only the shell's single Edit-menu
        /// Undo/Redo collapses the two into one, and it has to walk them in the order the edits were
        /// actually made. Remembering only the session touched last got that wrong as soon as the two
        /// interleaved: after GIT, ARE, GIT, two undos both came out of the GIT history and skipped
        /// the ARE edit that happened between them.
        /// </remarks>
        private readonly List<DocumentSession> _editOrder = new();

        /// <summary>Undone edits, newest first - the redo side of <see cref="_editOrder"/>.</summary>
        private readonly List<DocumentSession> _undoneOrder = new();

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

        public bool IsDirty =>
            _areSession.UndoStack.IsDirty ||
            _gitSession.UndoStack.IsDirty;

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
            (HasSceneStatus || HasSceneSelection || !string.IsNullOrEmpty(PlacementStatus));

        // ----- 3D-view <-> instance-list selection sync -----

        private bool _syncingSelection;

        private (ResourceType Type, int Index)? _pendingSectionSelection;

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
                OnPropertyChanged(nameof(HasSceneSelection));
                OnPropertyChanged(nameof(HasViewportHud));
                OnPropertyChanged(nameof(SelectionName));
                OnPropertyChanged(nameof(SelectionResRef));
                OnPropertyChanged(nameof(SelectionKindLabel));
                OnPropertyChanged(nameof(SelectionGlyph));
                OnPropertyChanged(nameof(SelectionCoordinates));
                OnPropertyChanged(nameof(IDocumentStatusSource.StatusDetail));
                EditSelectedBlueprintCommand.NotifyCanExecuteChanged();
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

        // ----- drag readout -----
        //
        // The numbers appear beside the map while a drag is in flight and disappear when it ends. Showing
        // the delta as well as the absolute is the point: "how far have I moved this" is the question a
        // builder is actually asking mid-drag, and it is the one a static coordinate box cannot answer.

        [ObservableProperty]
        private bool _isDragging;

        [ObservableProperty]
        private string _dragPosition = string.Empty;

        [ObservableProperty]
        private string _dragFacing = string.Empty;

        [ObservableProperty]
        private string _dragDelta = string.Empty;

        /// <summary>
        /// Called by the view as a manipulation drag updates. Both null ends the readout.
        /// </summary>
        public void ShowDragReadout(InstanceMarker? original, InstanceMarker? preview)
        {
            if (original == null || preview == null)
            {
                IsDragging = false;
                return;
            }

            DragPosition =
                $"x {preview.Position.X:0.00}   y {preview.Position.Y:0.00}   z {preview.Position.Z:0.00}";

            var headingDegrees = MathF.Atan2(preview.Orientation.Y, preview.Orientation.X) * 180f / MathF.PI;
            if (headingDegrees < 0)
                headingDegrees += 360f;
            DragFacing = $"facing {headingDegrees:0}°";

            var moved = Vector3.Distance(preview.Position, original.Position);
            var turned = MathF.Abs(
                MathF.Atan2(preview.Orientation.Y, preview.Orientation.X) -
                MathF.Atan2(original.Orientation.Y, original.Orientation.X)) * 180f / MathF.PI;

            DragDelta = moved > 1e-4f
                ? $"moved {moved:0.00} m"
                : turned > 1e-4f
                    ? $"turned {turned:0}°"
                    : string.Empty;

            IsDragging = true;
        }

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

        private bool CanEditSelectedBlueprint() =>
            SelectedSceneInstance is { TemplateResRef: not null } instance &&
            MapKindToSectionType(instance.Kind) != null &&
            _openBlueprint != null;

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

        /// <summary>True from the moment a palette blueprint is chosen for placement until the next viewport click (or Esc/right-click cancel) resolves it - drives GlAreaControl.IsPlacementActive.</summary>
        public bool IsPlacementPending => _pendingPlacementResRef != null;

        /// <summary>3D-view status line while a placement is pending, or empty otherwise.</summary>
        public string PlacementStatus =>
            IsPlacementPending ? $"Click to place {_pendingPlacementResRef}... (Esc or right-click to cancel)"
            : _pendingTile is { } tile
                ? CanRotatePendingTile
                    ? $"Click a cell to place {tile.Label} facing {PendingTileFacing}... " +
                      "(R to rotate, Esc or right-click to cancel)"
                    : $"Click a cell to place {tile.Label}... (Esc or right-click to cancel)"
            : string.Empty;

        /// <summary>This area's tileset resref, which is what the Tiles palette lists tiles from.</summary>
        public string? TilesetResRef => new AreDocument(_areSession.Document).Tileset;

        private TilePaletteEntry? _pendingTile;

        /// <summary>True while a tile or group is armed - drives GlAreaControl.IsTilePlacementActive.</summary>
        public bool IsTilePlacementPending => _pendingTile != null;

        /// <summary>The armed stamp's footprint in cells, for the viewport's cell highlight.</summary>
        public (int Columns, int Rows) TilePlacementFootprint =>
            _pendingTile is { } entry ? (entry.Columns, entry.Rows) : (1, 1);

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
        public bool CanRotatePendingTile => _pendingTile is { Columns: 1, Rows: 1, Terrain: null };

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
            if (!CanRotatePendingTile)
                return;

            _pendingTileOrientation = (_pendingTileOrientation + 1) % 4;
            OnPropertyChanged(nameof(PendingTileFacing));
            OnPropertyChanged(nameof(PlacementStatus));
        }

        /// <summary>
        /// Arms a tile stamp chosen in the Palette panel. The next map click writes it into the grid.
        /// </summary>
        public bool ArmTilePlacement(TilePaletteEntry entry)
        {
            if (entry == null || entry.TileIds.Count == 0)
                return false;

            // Arming a tile cancels any armed object and vice versa: two ghosts following one cursor
            // would leave the builder guessing which one a click resolves.
            CancelPlacement();

            _pendingTile = entry;
            _pendingTileOrientation = 0;
            InvalidateTilePlacementValidity();
            _tilePlacementModels = ResolveTileModels(entry);
            OnPropertyChanged(nameof(IsTilePlacementPending));
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

            _pendingTile = null;
            _tilePlacementModels = Array.Empty<RenderModel?>();
            InvalidateTilePlacementValidity();
            OnPropertyChanged(nameof(IsTilePlacementPending));
            OnPropertyChanged(nameof(TilePlacementModels));
            OnPropertyChanged(nameof(PlacementStatus));
            OnPropertyChanged(nameof(HasViewportHud));
        }

        /// <summary>
        /// Writes the armed stamp into the grid at the clicked anchor cell, as ONE undo step.
        /// </summary>
        /// <remarks>
        /// A specific tile or group is a raw write, not an edge-matched one: Aurora's tile palette
        /// places the piece you picked, and a builder who chose a doorway wants that doorway. Group
        /// cells carrying -1 are holes in the rectangle and are skipped, leaving whatever the area
        /// already had there. The Terrain category is the other half of that split - see
        /// <see cref="CommitTerrainPaint"/>.
        /// </remarks>
        public void CommitTilePlacement(int anchorColumn, int anchorRow)
        {
            var entry = _pendingTile;
            _pendingTile = null;
            OnPropertyChanged(nameof(IsTilePlacementPending));
            OnPropertyChanged(nameof(PlacementStatus));
            OnPropertyChanged(nameof(HasViewportHud));

            if (entry == null)
                return;

            if (entry.Terrain is { Length: > 0 } terrain)
            {
                CommitTerrainPaint(anchorColumn, anchorRow, entry, terrain);
                return;
            }

            var are = new AreDocument(_areSession.Document);
            var width = AreaTiles.Width(are);
            var height = AreaTiles.Height(are);

            var orientation = CanRotatePendingTile ? _pendingTileOrientation : 0;
            var writes = new List<(int Column, int Row, int TileId)>();
            for (var row = 0; row < entry.Rows; row++)
            {
                for (var column = 0; column < entry.Columns; column++)
                {
                    var tileId = entry.TileIds[row * entry.Columns + column];
                    if (tileId < 0)
                        continue;

                    var targetColumn = anchorColumn + column;
                    var targetRow = anchorRow + row;
                    if (targetColumn < 0 || targetRow < 0 || targetColumn >= width || targetRow >= height)
                    {
                        SceneStatus = $"'{entry.Label}' does not fit at ({anchorColumn},{anchorRow}).";
                        return;
                    }

                    writes.Add((targetColumn, targetRow, tileId));
                }
            }

            if (writes.Count == 0)
                return;

            var label = writes.Count == 1
                ? $"Place tile at ({anchorColumn},{anchorRow})"
                : $"Place {entry.Label} at ({anchorColumn},{anchorRow})";

            RunAreEdit(label, () =>
            {
                foreach (var (column, row, tileId) in writes)
                    AreaTiles.SetTile(are, column, row, tileId, orientation);
            });
        }


        /// <summary>
        /// Whether the armed palette entry would actually go down at this cell - the question the
        /// hovered cell is coloured green or red by.
        /// </summary>
        /// <remarks>
        /// For a terrain this is not a bounds check but the real answer: the solver is run against the
        /// live grid and asked whether it can produce a blend, exactly as the click would. It says no
        /// where the terrain has no tile that can meet what is already around the cell, which is a
        /// thing a builder otherwise only discovers by clicking and reading a status line.
        /// <para>
        /// A dry run, so nothing is written; the result is memoised per cell because the pointer asks
        /// this on every move and a solve walks the eight-neighbour ring. The memo is dropped whenever
        /// the grid changes, which is what <see cref="InvalidateTilePlacementValidity"/> is for.
        /// </para>
        /// </remarks>
        public bool CanPlaceArmedTileAt(int column, int row)
        {
            if (_pendingTile is not { } entry)
                return false;

            if (_tileValidity.TryGetValue((column, row), out var memo))
                return memo;

            var valid = SolveTilePlacementValidity(column, row, entry);
            _tileValidity[(column, row)] = valid;
            return valid;
        }

        private readonly Dictionary<(int Column, int Row), bool> _tileValidity = new();

        /// <summary>Drops the memoised answers - the grid they were computed against has changed.</summary>
        private void InvalidateTilePlacementValidity() => _tileValidity.Clear();

        private bool SolveTilePlacementValidity(int column, int row, TilePaletteEntry entry)
        {
            var are = new AreDocument(_areSession.Document);
            var width = AreaTiles.Width(are);
            var height = AreaTiles.Height(are);

            if (column < 0 || row < 0 || column >= width || row >= height)
                return false;

            // A fixed stamp only has to fit the grid: every one of its cells must be a real cell.
            if (string.IsNullOrWhiteSpace(entry.Terrain))
            {
                return column + entry.Columns <= width && row + entry.Rows <= height;
            }

            if (_tilesetCatalog == null ||
                string.IsNullOrWhiteSpace(TilesetResRef) ||
                !_tilesetCatalog.TryGetTileset(TilesetResRef, out var tileset))
            {
                return false;
            }

            return TilePainter.PaintTerrain(
                tileset, width, height, AreaTiles.Reader(are), column, row, entry.Terrain).Count > 0;
        }

        /// <summary>
        /// Fills the clicked cell with a terrain and re-blends its eight neighbours, as ONE undo step.
        /// </summary>
        /// <remarks>
        /// This is the brush half of the Tiles palette: <see cref="TilePainter.PaintTerrain"/> picks
        /// tiles whose corners and edge crossers agree with what is already around the cell, which is
        /// what makes a hand-laid area read as continuous ground rather than a patchwork. It can
        /// legitimately decline - a boundary whose neighbours cannot all be solved has no answer - and
        /// says so rather than writing a partial blend.
        /// </remarks>
        private void CommitTerrainPaint(int column, int row, TilePaletteEntry entry, string terrain)
        {
            if (_tilesetCatalog == null)
            {
                SceneStatus = "Terrain painting is unavailable (tileset data not loaded).";
                return;
            }

            var tilesetResRef = TilesetResRef;
            if (string.IsNullOrWhiteSpace(tilesetResRef) ||
                !_tilesetCatalog.TryGetTileset(tilesetResRef, out var tileset))
            {
                SceneStatus = "Terrain painting is unavailable (this area's tileset could not be read).";
                return;
            }

            var are = new AreDocument(_areSession.Document);
            var changes = TilePainter.PaintTerrain(
                tileset,
                AreaTiles.Width(are),
                AreaTiles.Height(are),
                AreaTiles.Reader(are),
                column,
                row,
                terrain);

            if (changes.Count == 0)
            {
                SceneStatus = $"'{entry.Label}' does not fit at ({column},{row}).";
                return;
            }

            RunAreEdit($"Paint {entry.Label} at ({column},{row})", () =>
            {
                foreach (var change in changes)
                    AreaTiles.SetTile(are, change.Col, change.Row, change.TileId, change.Orientation);
            });
        }
        /// <summary>
        /// Arms placement for a blueprint chosen in the Palette panel: the object then follows the
        /// cursor across the map as a translucent ghost until a click puts it down (Esc or a
        /// right-click cancels). This is the only way to place an instance - the editor no longer
        /// carries its own blueprint picker, because the Palette panel is already one.
        /// </summary>
        public bool ArmPlacement(ResourceType type, string resRef)
        {
            var section = Sections.FirstOrDefault(candidate => candidate.BlueprintType == type);
            if (section == null || string.IsNullOrWhiteSpace(resRef))
                return false;

            // A door is hung in a doorway the tile declares, never on open floor, so an area laid
            // entirely with doorless tiles has nowhere to put one - and arming a placement that can
            // never resolve would leave the builder clicking at a map that refuses every click.
            if (type == ResourceType.Utd && AreaScene is { } scene && scene.DoorAnchors.Count == 0)
            {
                _log.AppendLine(
                    $"'{resRef}' cannot be placed: no tile in this area declares a doorway to hang a door in.");
                return false;
            }

            // The other half of the exclusion in ArmTilePlacement: only one thing follows the cursor, so a
            // builder always knows what the next click resolves.
            CancelTilePlacement();

            _pendingPlacementSection = section;
            _pendingPlacementResRef = resRef;
            PlacementGhost = BuildPlacementGhost(type, resRef);
            OnPropertyChanged(nameof(IsPlacementPending));
            OnPropertyChanged(nameof(PlacementStatus));
            OnPropertyChanged(nameof(HasViewportHud));
            return true;
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
        /// A creature's composed geometry, one build per distinct blueprint.
        /// </summary>
        /// <remarks>
        /// Cached because composing a segmented body is not cheap and an area holds many instances of
        /// the same creature - the cache is keyed by template resref, so the cost is the number of
        /// distinct creatures in the area rather than the number placed. That distinction matters:
        /// <see cref="Workspace.BlueprintPreviewRenderer"/> deliberately caches nothing itself, because
        /// retaining every blueprint's expanded meshes at once is what once drove the preview build to a
        /// 37 GB working set. Bounding it per area keeps that from coming back.
        /// <para>
        /// Runs on the scene-build worker thread, so the dictionary is locked rather than left to luck.
        /// </para>
        /// </remarks>
        private RenderModel? ResolveCreatureModel(string resRef)
        {
            if (string.IsNullOrWhiteSpace(resRef) || _resolveBlueprintModel == null)
                return null;

            lock (_creatureModelGate)
            {
                if (_creatureModels.TryGetValue(resRef, out var cached))
                    return cached;
            }

            RenderModel? model = null;
            try
            {
                model = _resolveBlueprintModel(ResourceType.Utc, resRef);
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Creature '{resRef}' could not be drawn and falls back to a marker: {ex.Message}");
            }

            lock (_creatureModelGate)
            {
                _creatureModels[resRef] = model;
            }

            return model;
        }

        private readonly Dictionary<string, RenderModel?> _creatureModels = new(StringComparer.OrdinalIgnoreCase);

        private readonly object _creatureModelGate = new();

        private InstanceMarker? BuildPlacementGhost(ResourceType type, string resRef)
        {
            var kind = MapSectionTypeToKind(type);
            if (kind == null)
                return null;

            RenderModel? model = null;
            try
            {
                // Built through the same path as the palette thumbnail the builder just clicked, so
                // the two agree - including for segmented creatures, whose body parts have to be
                // composed and which an earlier local resolve could not produce at all (it passed no
                // appearance service and handled only the single-resref case, so every creature
                // ghosted as a bare marker).
                model = _resolveBlueprintModel?.Invoke(type, resRef);
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
                // The ghost has to be turned the same way the placed marker will be, or a waypoint
                // would swing a quarter turn the instant it was committed.
                VisualTransform = kind.Value == InstanceMarkerKind.Waypoint
                    ? WaypointMarkerModel.ForwardCorrection
                    : Matrix4x4.Identity,
                Model = model
            };
        }

        /// <summary>
        /// Called by the view when a viewport click resolves a pending placement
        /// (GlAreaControl.PlacementPointPicked): creates the instance at the clicked ground
        /// position through the pending section's InstanceFieldMap-based Add path (one RunGitEdit
        /// transaction), then rebuilds the scene and selects the new instance.
        /// </summary>
        /// <param name="orientation">
        /// The heading to hang the new instance at, when the viewport chose one - a door takes the
        /// heading of the doorway it snapped into. Null leaves the blueprint's own default.
        /// </param>
        public void CommitPlacement(Vector3 position, Vector2? orientation = null)
        {
            var section = _pendingPlacementSection;
            var resRef = _pendingPlacementResRef;
            _pendingPlacementSection = null;
            _pendingPlacementResRef = null;
            PlacementGhost = null;
            OnPropertyChanged(nameof(IsPlacementPending));
            OnPropertyChanged(nameof(PlacementStatus));
            OnPropertyChanged(nameof(HasViewportHud));

            if (section == null || resRef == null)
                return;

            // Orientation goes in with the add rather than as a follow-up edit, so hanging a door in a
            // doorway is one undo step and not two.
            var facing = orientation ?? new Vector2(1f, 0f);
            if (!section.AddInstanceAt(resRef, position.X, position.Y, position.Z, facing.X, facing.Y))
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
            PlacementGhost = null;
            OnPropertyChanged(nameof(IsPlacementPending));
            OnPropertyChanged(nameof(PlacementStatus));
            OnPropertyChanged(nameof(HasViewportHud));
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

        /// <summary>
        /// How far one press of the rotate buttons turns the selection. A quarter of a right angle:
        /// coarse enough to square something up in three presses, fine enough to angle a chair.
        /// </summary>
        private const float RotateStepRadians = MathF.PI / 8f;

        /// <summary>True when there is a selected instance this editor can actually rotate.</summary>
        public bool CanRotateSelection =>
            SelectedSceneInstance is { } instance && SectionForKind(instance.Kind) != null;

        [RelayCommand]
        private void RotateSelectionClockwise() => RotateSelectionBy(-RotateStepRadians);

        [RelayCommand]
        private void RotateSelectionCounterClockwise() => RotateSelectionBy(RotateStepRadians);

        /// <summary>
        /// Turns the selection to a random heading. Aurora has this because a row of identically
        /// angled crates reads as placed by a machine; one press per object breaks that up.
        /// </summary>
        [RelayCommand]
        private void RotateSelectionRandomly()
        {
            if (SelectedSceneInstance is not { } instance)
                return;

            var current = MathF.Atan2(instance.Orientation.Y, instance.Orientation.X);
            RotateSelectionBy(Random.Shared.NextSingle() * MathF.Tau - current);
        }

        private void RotateSelectionBy(float deltaRadians)
        {
            if (SelectedSceneInstance is not { } instance)
                return;

            var heading = MathF.Atan2(instance.Orientation.Y, instance.Orientation.X) + deltaRadians;
            RotateSelectedInstance(instance, new Vector2(MathF.Cos(heading), MathF.Sin(heading)));
        }

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
            Func<ResourceType, string, RenderModel?>? resolveBlueprintModel = null)
        {
            _resolveBlueprintModel = resolveBlueprintModel;
            _waypointAppearances = waypointAppearances;
            _resolveBlueprintName = resolveBlueprintName;
            _openBlueprint = openBlueprint;
            _log = log;
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
                    _gitSession, workspace, RunGitEdit, gameCodeIndex, log, _prompts, resolveStrRef));
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
            }

            UpdateTitle();
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
        /// each command to remember. Paths that already rebuild explicitly (placement and the
        /// gizmos, which also need to reselect what they touched) are not double-served: by the time
        /// the delay elapses their build has either finished or is in flight for the same revision,
        /// and <see cref="IsSceneCurrent"/> drops the duplicate.
        /// </remarks>
        private void RequestSceneRefresh()
        {
            // Whatever changed the grid also invalidated every answer about where a tile may go.
            // Hooked here rather than at each edit site because this is the one thing every path
            // through - paint, stamp, undo, redo, an external file change - has to call.
            InvalidateTilePlacementValidity();

            if (!_sceneBuildRequested || _disposed)
                return;

            _sceneRefreshCts?.Cancel();
            _sceneRefreshCts?.Dispose();
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
            IsBuildingScene = true;
            SceneStatus = "Building scene...";

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
                if (generation == Volatile.Read(ref _sceneBuildGeneration))
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
                session.Execute(description, mutation);

                // A fresh edit invalidates the redo side of both histories, exactly as each
                // session's own undo stack does.
                _editOrder.Add(session);
                _undoneOrder.Clear();
                Interlocked.Increment(ref _sceneInputRevision);
                RequestSceneRefresh();
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

        /// <summary>Saves both area documents, returning false if any prompt is cancelled or write fails.</summary>
        /// <remarks>
        /// An area is one thing to the builder but two files on disk, so every external-change prompt is
        /// answered before anything is written. Prompting between the two writes meant that cancelling
        /// the second prompt still left the first file saved and its history marked clean, and no later
        /// Discard could take that back.
        /// </remarks>
        public async Task<bool> TrySaveAsync()
        {
            var areaCatalogEntryChanged = _areSession.UndoStack.IsDirty;
            var tilesetBefore = TilesetResRef;

            var arePlan = await PlanSaveAsync(_areSession).ConfigureAwait(true);
            if (arePlan == SavePlan.Cancel)
                return false;

            var gitPlan = await PlanSaveAsync(_gitSession).ConfigureAwait(true);
            if (gitPlan == SavePlan.Cancel)
                return false;

            var areResult = ApplySavePlan(_areSession, arePlan);
            if (!areResult.Success)
                return false;

            var gitResult = ApplySavePlan(_gitSession, gitPlan);
            if (!gitResult.Success)
                return false;

            if (areResult.Reloaded)
                RefreshAreaPropertyFields();
            if (gitResult.Reloaded)
                RefreshInstanceSections();
            if ((areResult.Reloaded || gitResult.Reloaded) && _sceneBuildRequested)
                _ = BuildSceneAsync(CaptureReselectKey());

            // Reloading the .are can bring in a different tileset, and the Tiles palette lists the
            // front area's tileset - without this it keeps offering the previous set's tiles.
            if (areResult.Reloaded &&
                !string.Equals(tilesetBefore, TilesetResRef, StringComparison.OrdinalIgnoreCase))
                TilesetChanged?.Invoke();

            AfterHistoryChange();
            if (areaCatalogEntryChanged || areResult.Reloaded)
                CatalogEntryChanged?.Invoke();
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
        private async Task<SavePlan> PlanSaveAsync(DocumentSession session)
        {
            if (!session.UndoStack.IsDirty)
                return SavePlan.Nothing;

            try
            {
                if (!session.HasExternalChange())
                    return SavePlan.Write;
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not check {session.FilePath} for external changes: {ex.Message}");
                return SavePlan.Cancel;
            }

            var choice = await _prompts.ConfirmExternalChangeAsync(session.FilePath).ConfigureAwait(true);
            return choice switch
            {
                ExternalChangeChoice.Cancel => SavePlan.Cancel,
                ExternalChangeChoice.Reload => SavePlan.Reload,
                _ => SavePlan.Write
            };
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
                        Services.SaveService.WriteAtomic(session.FilePath, session.ToBytes());
                        session.UndoStack.MarkSaved();
                        session.RecordCurrentFileState();
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

            RecordUndo(_areSession);
            _areSession.Undo();
            RefreshAreaPropertyFields();

            if (_sceneBuildRequested)
                _ = BuildSceneAsync(reselect);

            AfterHistoryChange();
        }

        public bool CanUndoAre => _areSession.UndoStack.CanUndo;

        [RelayCommand(CanExecute = nameof(CanRedoAre))]
        private void RedoAre()
        {
            var reselect = CaptureReselectKey();

            RecordRedo(_areSession);
            _areSession.Redo();
            RefreshAreaPropertyFields();

            if (_sceneBuildRequested)
                _ = BuildSceneAsync(reselect);

            AfterHistoryChange();
        }

        public bool CanRedoAre => _areSession.UndoStack.CanRedo;

        /// <summary>Undo/redo for the instance lists (.git) - the toolbar's primary pair, since
        /// placing/moving/removing instances is the bulk of this screen's editing. Also refreshes
        /// the 3D view when it has ever been built, rebinding the current selection by
        /// kind+index so undoing/redoing a 3D-view gizmo edit is visible without pressing Rebuild.</summary>
        [RelayCommand(CanExecute = nameof(CanUndoInstances))]
        private void UndoInstances()
        {
            var reselect = CaptureReselectKey();

            RecordUndo(_gitSession);
            _gitSession.Undo();
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

            RecordRedo(_gitSession);
            _gitSession.Redo();
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

        // ----- Shell Edit menu / Ctrl+Z / Ctrl+Y -----
        //
        // Implemented explicitly so the toolbar keeps its unambiguous per-session buttons: the two
        // histories stay separate, and only this single-command view of them collapses to one. That
        // view walks the recorded edit order, so a shell Undo always takes back the newest edit
        // whichever file it landed in, and falls back to whichever session still has history so it is
        // never a no-op while an undoable edit exists.

        bool IEditorDocument.CanUndo => CanUndoInstances || CanUndoAre;

        bool IEditorDocument.CanRedo => CanRedoInstances || CanRedoAre;

        void IEditorDocument.Undo()
        {
            var newest = LastUndoable();
            if (newest == _areSession)
                UndoAre();
            else if (newest == _gitSession)
                UndoInstances();
            else if (CanUndoInstances)
                UndoInstances();
            else if (CanUndoAre)
                UndoAre();
        }

        void IEditorDocument.Redo()
        {
            var newest = LastRedoable();
            if (newest == _areSession)
                RedoAre();
            else if (newest == _gitSession)
                RedoInstances();
            else if (CanRedoInstances)
                RedoInstances();
            else if (CanRedoAre)
                RedoAre();
        }

        /// <summary>The session holding the newest undoable edit, or null when the order is unknown.</summary>
        private DocumentSession? LastUndoable()
        {
            for (var i = _editOrder.Count - 1; i >= 0; i--)
            {
                var session = _editOrder[i];
                if (session.UndoStack.CanUndo)
                    return session;
            }

            return null;
        }

        /// <summary>The session holding the most recently undone edit, or null when none is recorded.</summary>
        private DocumentSession? LastRedoable()
        {
            for (var i = _undoneOrder.Count - 1; i >= 0; i--)
            {
                var session = _undoneOrder[i];
                if (session.UndoStack.CanRedo)
                    return session;
            }

            return null;
        }

        /// <summary>Moves one entry from the edit order to the undone order, for either undo route.</summary>
        private void RecordUndo(DocumentSession session)
        {
            var index = _editOrder.LastIndexOf(session);
            if (index >= 0)
                _editOrder.RemoveAt(index);

            _undoneOrder.Add(session);
        }

        /// <summary>The inverse of <see cref="RecordUndo"/>.</summary>
        private void RecordRedo(DocumentSession session)
        {
            var index = _undoneOrder.LastIndexOf(session);
            if (index >= 0)
                _undoneOrder.RemoveAt(index);

            _editOrder.Add(session);
        }

        /// <summary>Raised when the tab closes so the editor registry can forget this instance.</summary>
        public event Action<AreaEditorViewModel>? Closed;

        /// <summary>Raised after an async close prompt approves closing this tab.</summary>
        public event Action<AreaEditorViewModel>? CloseRequested;

        /// <summary>Raised after the ARE resource is saved or reloaded so catalog views can re-index it.</summary>
        public event Action? CatalogEntryChanged;

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
            foreach (var section in Sections)
                section.ClosePalette();
            _areSession.Dispose();
            _gitSession.Dispose();
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
