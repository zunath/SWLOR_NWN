using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.Categories;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Tilesets;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Settings;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>
    /// The Palette panel: pick a blueprint type, browse or search its categories, and place a blueprint
    /// into the open area or open it for editing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two verbs and one search box. Placing and editing are the only things you can do with a blueprint,
    /// so they are the only two actions a tile offers. The single search box covers both categories and
    /// objects because a builder searching "console" does not know or care which of the two will answer -
    /// category hits come first as jump targets, then objects from every category, each labelled with
    /// where it lives.
    /// </para>
    /// <para>
    /// The category tree is a flattened, virtualized list rather than a TreeView; see
    /// <see cref="CategoryRowViewModel"/> for why. Counts sit on categories, never on types.
    /// </para>
    /// </remarks>
    public partial class PaletteViewModel : Tool
    {
        /// <summary>
        /// Types offered, in Aurora's palette order - see <see cref="ResourceTypeExtensions.PaletteOrder"/>,
        /// which owns the order so it can be pinned by a test.
        /// </summary>
        private static IReadOnlyList<ResourceType> OfferedTypes => ResourceTypeExtensions.PaletteOrder;

        private const int MaxSearchResults = 200;

        private readonly WorkspaceContext _workspaceContext;
        private readonly CategoryService _categories;
        private readonly OutputLogService _log;
        private readonly Func<Editors.EditorService>? _editorService;
        private readonly Func<IAreaPlacementTarget?>? _placementTarget;
        private readonly ThumbnailService? _thumbnails;
        private readonly Services.IEditorPromptService? _prompts;
        private readonly TilesetCatalog? _tilesets;
        private readonly Func<uint, string?>? _resolveStrRef;

        /// <summary>Where the panel's own preferences live, or null in a test with none.</summary>
        private readonly ToolsetSettings? _settings;

        /// <summary>
        /// True while the constructor is applying saved state, so restoring a preference does not
        /// immediately write it back and does not rebuild a tree that has not been built yet.
        /// </summary>
        private bool _restoring;

        /// <summary>Every row of the current type's tree, expanded or not, so collapsing need not re-derive counts.</summary>
        private readonly List<CategoryRowViewModel> _allRows = new();

        private IReadOnlySet<string> _existing = new HashSet<string>();

        public ObservableCollection<PaletteTypeChipViewModel> Types { get; } = new();

        /// <summary>The visible category rows - the flattened tree with collapsed branches omitted.</summary>
        public ObservableCollection<CategoryRowViewModel> Rows { get; } = new();

        /// <summary>Matching categories while searching; empty otherwise.</summary>
        public ObservableCollection<CategoryMatchViewModel> CategoryMatches { get; } = new();

        public ObservableCollection<PaletteTileViewModel> Tiles { get; } = new();

        [ObservableProperty]
        private ResourceType _selectedType = ResourceType.Utp;

        /// <summary>
        /// Whether the tree and grid show the module's own blueprints or the base game's.
        /// </summary>
        /// <remarks>
        /// Custom by default: it is where a builder spends effectively all their time, and it is what this
        /// panel showed before the split existed, so nobody's habits change on upgrade.
        /// </remarks>
        [ObservableProperty]
        private PaletteSource _source = PaletteSource.Custom;

        /// <summary>
        /// True when the Tiles entry is picked instead of a blueprint type.
        /// </summary>
        /// <remarks>
        /// Tiles are the one palette entry that is not a module resource: which tiles exist is a property
        /// of the open area's tileset, so this mode reads from the area in front rather than from the
        /// module, has no Custom/Standard split to make, and cannot create, rename or delete anything.
        /// </remarks>
        [ObservableProperty]
        private bool _isTileMode;

        private TilePalette _tiles = TilePalette.Empty;

        private TilePaletteCategory? _selectedTileCategory;

        [ObservableProperty]
        private string _query = string.Empty;

        [ObservableProperty]
        private CategoryRowViewModel? _selectedRow;

        [ObservableProperty]
        private PaletteTileViewModel? _selectedTile;

        [ObservableProperty]
        private string _breadcrumb = string.Empty;

        [ObservableProperty]
        private string? _statusMessage;

        /// <summary>Tile width in pixels. Idle while tiles are glyphs; the control the grid needs the moment they become rendered models.</summary>
        [ObservableProperty]
        private double _tileSize = 136;

        /// <summary>A size, not a pixel count - the number means nothing to the person dragging it.</summary>
        public string TileSizeLabel => TileSize switch
        {
            < 120 => "S",
            < 165 => "M",
            _ => "L"
        };

        /// <summary>
        /// Share of the panel's flexible height the category tree keeps, or 0 when that divider has not
        /// been moved. Read once by the view when it loads and written back when the divider is let go -
        /// it is a stored number rather than a bound one, because the Grid owns the live value.
        /// </summary>
        public double CategoryProportion
        {
            get => _settings?.PaletteCategoryProportion ?? 0;
            set
            {
                if (_settings != null)
                    _settings.PaletteCategoryProportion = value;
            }
        }

        partial void OnTileSizeChanged(double value)
        {
            OnPropertyChanged(nameof(TileSizeLabel));
            OnPropertyChanged(nameof(PreviewHeight));

            if (_settings != null && !_restoring)
                _settings.PalettePreviewSize = value;
        }

        /// <summary>
        /// Applies what the builder left set last time: preview size, which type was showing, and whether
        /// it was the module's content or the base game's.
        /// </summary>
        /// <remarks>
        /// Runs in the constructor, before any tree exists, so it only assigns fields - the first
        /// <see cref="Refresh"/> after the module opens builds against whatever it left behind. The
        /// <see cref="_restoring"/> flag is what stops each assignment from saving itself straight back
        /// and from triggering a rebuild per property.
        /// </remarks>
        private void RestoreSettings()
        {
            if (_settings == null)
                return;

            _restoring = true;
            try
            {
                if (_settings.PalettePreviewSize > 0)
                    TileSize = _settings.PalettePreviewSize;

                // Three outcomes, not two: nothing saved leaves the default type alone, the Tiles
                // sentinel restores Tiles mode, and anything else is a blueprint type. Collapsing the
                // first two is how a fresh install ended up opening in Tiles mode.
                var selection = _settings.PaletteSelection;
                if (string.Equals(selection, ToolsetSettings.TilesSelection, StringComparison.OrdinalIgnoreCase))
                {
                    IsTileMode = true;
                }
                else if (ResourceTypeExtensions.TryFromExtension(selection, out var type) &&
                         OfferedTypes.Contains(type))
                {
                    SelectedType = type;
                }

                Source = _settings.PaletteShowsStandard ? PaletteSource.Standard : PaletteSource.Custom;

                if (Enum.TryParse<TilePaintMode>(_settings.TilePaintMode, ignoreCase: true, out var paintMode))
                    TilePaintMode = paintMode;
            }
            finally
            {
                _restoring = false;
            }
        }

        /// <summary>
        /// False while the Standard palette is showing. The base game's content is not ours to rename,
        /// delete, refile or add to, so every command that would write is hidden rather than disabled -
        /// a menu of greyed-out items invites a builder to work out why, and the answer never changes.
        /// </summary>
        public bool IsCustomSource => Source == PaletteSource.Custom;

        partial void OnSourceChanged(PaletteSource value)
        {
            OnPropertyChanged(nameof(IsCustomSource));
            OnPropertyChanged(nameof(IsStandardSource));
            OnPropertyChanged(nameof(CanWrite));
            OnPropertyChanged(nameof(CanEditCopy));
            OnPropertyChanged(nameof(CanCreateBlueprint));
            OnPropertyChanged(nameof(ReadOnlyNotice));
            OnPropertyChanged(nameof(HasReadOnlyNotice));
            OnPropertyChanged(nameof(HasBlueprintActions));

            if (_settings != null && !_restoring)
                _settings.PaletteShowsStandard = IsStandardSource;

            if (_restoring)
                return;

            SelectedRow = null;
            SelectedTile = null;
            Refresh();
        }

        public bool IsStandardSource => !IsCustomSource;

        /// <summary>
        /// True only for this module's own blueprints - the one case where a palette command may write.
        /// Base-game blueprints are not ours, and a tile is a row in a .set file rather than a resource
        /// at all, so neither offers anything to create, rename, refile or delete.
        /// </summary>
        /// <summary>
        /// Whether a module-wide operation is running, or null in a test with no shell. Shared with
        /// every other panel and editor tab that writes to the module, so all of them grey out
        /// together rather than each holding its own opinion.
        /// </summary>
        private readonly Services.ModuleMutationLock? _mutationLock;

        /// <summary>
        /// True when this panel may write to the module: the Custom side of a blueprint type, and no
        /// module-wide operation in flight.
        /// </summary>
        /// <remarks>
        /// The lock was missing. Creating or deleting a blueprint writes straight to the module, and
        /// those controls stayed enabled through a pack - which reads the very files being written - so
        /// a click at the wrong moment could put a half-written resource into the .mod being built.
        /// </remarks>
        public bool CanWrite => IsCustomSource && IsBlueprintMode && _mutationLock?.IsLocked != true;

        /// <summary>
        /// Edit Copy writes a new module blueprint but never changes its source, so it is available on
        /// both Custom and Standard palette entries whenever ordinary module writes are available.
        /// </summary>
        public bool CanEditCopy => IsBlueprintMode && _mutationLock?.IsLocked != true;

        /// <summary>Whether a blueprint tile has anything useful to expose through its ellipsis.</summary>
        public bool HasBlueprintActions => CanWrite || CanEditCopy || HasReadOnlyNotice;

        /// <summary>Re-reads <see cref="CanWrite"/>, for when the module-wide lock has flipped.</summary>
        public void NotifyWriteAvailabilityChanged()
        {
            OnPropertyChanged(nameof(CanWrite));
            OnPropertyChanged(nameof(CanEditCopy));
            OnPropertyChanged(nameof(CanCreateBlueprint));
            OnPropertyChanged(nameof(HasBlueprintActions));
        }

        /// <summary>
        /// Creation is narrower than editing: types whose editor cannot finish a usable resource
        /// (currently merchants, whose StoreList inventory is not exposed) remain browsable/editable
        /// but do not offer a misleading "New" action.
        /// </summary>
        public bool CanCreateBlueprint => CanWrite && BlueprintTemplateFactory.Supports(SelectedType);

        /// <summary>
        /// Why a context menu is empty, so it never opens as a blank popup. Null when there is nothing to
        /// explain, which is exactly when the menu has real items on it.
        /// </summary>
        public string? ReadOnlyNotice =>
            IsTileMode ? "Tileset content - read-only"
            : IsStandardSource ? "Base game content - read-only"
            : null;

        public bool HasReadOnlyNotice => ReadOnlyNotice != null;

        [RelayCommand]
        private void ShowCustom() => Source = PaletteSource.Custom;

        [RelayCommand]
        private void ShowStandard() => Source = PaletteSource.Standard;

        public bool IsSearching => !string.IsNullOrWhiteSpace(Query);

        public bool HasCategoryMatches => CategoryMatches.Count > 0;

        public PaletteViewModel(
            WorkspaceContext workspaceContext,
            CategoryService categories,
            OutputLogService log,
            Func<Editors.EditorService>? editorService = null,
            Func<IAreaPlacementTarget?>? placementTarget = null,
            ThumbnailService? thumbnails = null,
            Services.IEditorPromptService? prompts = null,
            TilesetCatalog? tilesets = null,
            Domain.GameData.Tlk.TlkService? tlk = null,
            ToolsetSettings? settings = null,
            Services.ModuleMutationLock? mutationLock = null)
        {
            _mutationLock = mutationLock;
            if (_mutationLock != null)
                _mutationLock.Changed += NotifyWriteAvailabilityChanged;
            _thumbnails = thumbnails;
            if (_thumbnails != null)
                _thumbnails.InvalidatedForResRef += OnThumbnailInvalidated;
            _prompts = prompts;
            _tilesets = tilesets;
            _resolveStrRef = tlk == null ? null : tlk.GetString;
            _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
            _categories = categories ?? throw new ArgumentNullException(nameof(categories));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _editorService = editorService;
            _placementTarget = placementTarget;

            Id = "Palette";
            Title = "Palette";

            _settings = settings;
            RestoreSettings();
            PublishTypeChips();

            _categories.Changed += Refresh;
        }

        /// <summary>
        /// The category tree currently in play. Null only when the module has no section for this type;
        /// the standard side always returns a section, empty when the base game is unavailable.
        /// </summary>
        private CategorySection? CurrentSection() =>
            IsCustomSource ? _categories.Section(SelectedType) : _categories.StandardSection(SelectedType);

        /// <summary>
        /// Whether the panel should show its "no area open" state: Tiles is the selected type, and
        /// there is no area in front to take a tileset from.
        /// </summary>
        /// <remarks>
        /// A state rather than a status line. The message used to go to <see cref="StatusMessage"/>,
        /// which is a dim footnote at the bottom of the panel - easy to miss when the grid above it is
        /// simply empty, and it stayed on screen after switching to a blueprint type, where it was no
        /// longer true. Tiles is the only type this can apply to: every other one lists the module's
        /// own content, which does not depend on which tab has focus.
        /// </remarks>
        [ObservableProperty]
        private bool _needsOpenArea;

        /// <summary>Rebuilds the tree and grid for the current type. Safe to call whenever the module changes.</summary>
        public void Refresh()
        {
            if (IsTileMode)
            {
                RefreshTiles();
                return;
            }

            NeedsOpenArea = false;

            _existing = IsCustomSource
                ? _categories.ExistingResRefs(SelectedType)
                : _categories.StandardResRefs(SelectedType);
            RebuildTree();
            RebuildTiles();
        }

        /// <summary>
        /// The area in front changed. Only Tiles mode cares: blueprints are the module's, the same
        /// whichever tab has focus, while a tileset belongs to one area.
        /// </summary>
        public void OnActiveAreaChanged()
        {
            if (IsTileMode)
                RefreshTiles();
        }

        // ----- Tiles mode -----

        /// <summary>
        /// Rebuilds the tile tree from the tileset of whatever area is in front.
        /// </summary>
        /// <remarks>
        /// Re-read on every refresh rather than cached against the module, because the answer depends on
        /// which area has focus: two areas on different tilesets offer different tiles, so switching tabs
        /// has to change what this panel shows. The .set parse itself is already cached by
        /// <see cref="TilesetCatalog"/>, so the repeat cost is the palette shaping alone.
        /// </remarks>
        private void RefreshTiles()
        {
            _allRows.Clear();
            Rows.Clear();
            CategoryMatches.Clear();
            OnPropertyChanged(nameof(HasCategoryMatches));
            Tiles.Clear();
            _selectedTileCategory = null;
            _tiles = TilePalette.Empty;
            Breadcrumb = string.Empty;

            var tilesetResRef = _placementTarget?.Invoke()?.TilesetResRef;
            NeedsOpenArea = string.IsNullOrWhiteSpace(tilesetResRef);
            if (NeedsOpenArea)
            {
                StatusMessage = string.Empty;
                return;
            }

            var activeTilesetResRef = tilesetResRef!;
            if (_tilesets == null || !_tilesets.TryGetTileset(activeTilesetResRef, out var tileset) || tileset == null)
            {
                StatusMessage = $"Tileset '{tilesetResRef}' could not be loaded.";
                return;
            }

            _tiles = TilePaletteBuilder.Build(tileset, _resolveStrRef, _log.AppendLine);
            if (_tiles.IsEmpty)
            {
                StatusMessage = $"Tileset '{tilesetResRef}' lists no tiles.";
                return;
            }

            // Only what this mode is for. The palette itself still describes the whole tileset - the
            // mode decides which of its categories are a sensible thing to click right now.
            var offered = TilePaintModes.CategoriesFor(_tiles, TilePaintMode);
            if (offered.Count == 0)
            {
                StatusMessage = IsAutoTilePaint
                    ? $"'{_tilesets.GetDisplayName(activeTilesetResRef)}' declares no terrain to paint - switch to Manual."
                    : $"'{_tilesets.GetDisplayName(activeTilesetResRef)}' lists no individual tiles.";
                return;
            }

            foreach (var category in offered)
                _allRows.Add(new CategoryRowViewModel(folder: null, depth: 0, count: category.Entries.Count,
                    hasChildren: false)
                {
                    SyntheticName = category.Name
                });

            PublishVisibleRows();
            SelectedRow = _allRows[0];
            RebuildTileGrid();
            StatusMessage = IsAutoTilePaint
                ? $"{_tilesets.GetDisplayName(activeTilesetResRef)} - pick a terrain, then click a cell to paint it."
                : $"{_tilesets.GetDisplayName(activeTilesetResRef)} - pick a tile, then click a cell to stamp it.";
        }

        /// <summary>
        /// Publishes the tile grid: the picked category, or every match in the tileset while searching.
        /// </summary>
        /// <remarks>
        /// Search ignores which category is open, the same way blueprint search does. A builder looking
        /// for "door" does not know whether the answer is a single tile or one of the tileset's groups,
        /// and answering from only the open category would silently hide half the tileset. Matches carry
        /// the category they came from so the two are still told apart.
        /// </remarks>
        private void RebuildTileGrid()
        {
            Tiles.Clear();
            var query = Query.Trim();

            if (query.Length > 0)
            {
                var matches = _tiles.Categories
                    .SelectMany(category => category.Entries
                        .Where(entry => entry.Label.Contains(query, StringComparison.OrdinalIgnoreCase))
                        .Select(entry => (Category: category.Name, Entry: entry)))
                    .OrderBy(match => match.Entry.Label, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();

                foreach (var match in matches.Take(MaxSearchResults))
                    AddTile(new PaletteTileViewModel(match.Entry, match.Category));

                Breadcrumb = matches.Count > MaxSearchResults
                    ? $"First {MaxSearchResults} of {matches.Count} matches"
                    : $"{matches.Count} match{(matches.Count == 1 ? string.Empty : "es")} in this tileset";
                return;
            }

            if (_selectedTileCategory is not { } category)
            {
                Breadcrumb = string.Empty;
                return;
            }

            foreach (var entry in category.Entries.Take(MaxSearchResults))
                AddTile(new PaletteTileViewModel(entry));

            Breadcrumb = category.Entries.Count > MaxSearchResults
                ? $"{category.Name} - first {MaxSearchResults} of {category.Entries.Count}"
                : $"{category.Name} - {category.Entries.Count} tiles";
        }

        [RelayCommand]
        private void SelectType(PaletteTypeChipViewModel chip)
        {
            if (chip == null)
                return;

            if (chip.IsTiles)
            {
                if (IsTileMode)
                    return;

                IsTileMode = true;
                return;
            }

            if (!IsTileMode && chip.Type == SelectedType)
                return;

            IsTileMode = false;
            SelectedType = chip.Type!.Value;
        }

        /// <summary>
        /// Every type, always. As icons they all fit one row of a narrow panel, which is what removed the
        /// need for the More... overflow - and an overflow was the wrong shape for this anyway: it made
        /// half the types cost an extra click to reach and expanded the row past the panel's edge.
        /// </summary>
        private void PublishTypeChips()
        {
            Types.Clear();

            // Tiles leads, as it does in Aurora - it is the thing you reach for while the area is still
            // a grid of nothing, before there is anything to dress it with.
            var tiles = PaletteTypeChipViewModel.ForTiles(_thumbnails?.TileChipIcon());
            tiles.IsSelected = IsTileMode;
            Types.Add(tiles);

            foreach (var type in OfferedTypes)
                Types.Add(new PaletteTypeChipViewModel(type, _thumbnails?.TypeChipIcon(type))
                {
                    IsSelected = !IsTileMode && type == SelectedType
                });
        }

        partial void OnSelectedTypeChanged(ResourceType value)
        {
            if (_restoring)
                return;

            if (_settings != null && !IsTileMode)
                _settings.PaletteSelection = value.Extension();

            SyncChipSelection();
            OnPropertyChanged(nameof(NewBlueprintLabel));
            OnPropertyChanged(nameof(CanCreateBlueprint));
            SelectedRow = null;
            Refresh();
        }

        partial void OnIsTileModeChanged(bool value)
        {
            if (_restoring)
                return;

            if (_settings != null)
                _settings.PaletteSelection = value ? ToolsetSettings.TilesSelection : SelectedType.Extension();

            SyncChipSelection();
            OnPropertyChanged(nameof(IsBlueprintMode));
            OnPropertyChanged(nameof(ShowsSourceSwitch));
            OnPropertyChanged(nameof(ShowsTilePaintSwitch));
            OnPropertyChanged(nameof(CanWrite));
            OnPropertyChanged(nameof(CanEditCopy));
            OnPropertyChanged(nameof(CanCreateBlueprint));
            OnPropertyChanged(nameof(ReadOnlyNotice));
            OnPropertyChanged(nameof(HasReadOnlyNotice));
            OnPropertyChanged(nameof(HasBlueprintActions));
            SelectedRow = null;
            SelectedTile = null;
            Refresh();
        }

        private void SyncChipSelection()
        {
            foreach (var chip in Types)
                chip.IsSelected = chip.IsTiles ? IsTileMode : !IsTileMode && chip.Type == SelectedType;
        }

        /// <summary>Everything that writes to the module or the sidecar is blueprint-only.</summary>
        public bool IsBlueprintMode => !IsTileMode;

        /// <summary>
        /// Custom/Standard is meaningless for tiles: a tileset is game data either way, and which one is
        /// in play is decided by the area, not by the builder.
        /// </summary>
        public bool ShowsSourceSwitch => !IsTileMode;

        /// <summary>
        /// Whether a click picks the tile itself or only the terrain and lets the tileset choose.
        /// </summary>
        /// <remarks>
        /// Auto is the default because it is what Aurora does and what laying a floor actually means:
        /// the builder is saying "this is rich carpet", not "this is the outside corner piece of rich
        /// carpet, rotated once". Manual stays because the rules cannot express everything, and a
        /// tileset always holds a tile the solver would never pick on its own.
        /// </remarks>
        [ObservableProperty]
        private TilePaintMode _tilePaintMode = TilePaintMode.Auto;

        public bool IsAutoTilePaint => TilePaintMode == TilePaintMode.Auto;

        public bool IsManualTilePaint => TilePaintMode == TilePaintMode.Manual;

        /// <summary>The Auto/Manual switch replaces Custom/Standard while Tiles is showing - the two are never both meaningful.</summary>
        public bool ShowsTilePaintSwitch => IsTileMode;

        [RelayCommand]
        private void UseAutoTilePaint() => TilePaintMode = TilePaintMode.Auto;

        [RelayCommand]
        private void UseManualTilePaint() => TilePaintMode = TilePaintMode.Manual;

        partial void OnTilePaintModeChanged(TilePaintMode value)
        {
            OnPropertyChanged(nameof(IsAutoTilePaint));
            OnPropertyChanged(nameof(IsManualTilePaint));

            if (_restoring)
                return;

            if (_settings != null)
                _settings.TilePaintMode = value.ToString();

            if (IsTileMode)
                RefreshTiles();
        }

        partial void OnQueryChanged(string value)
        {
            OnPropertyChanged(nameof(IsSearching));

            // Tiles have no cross-category search: a tileset's two categories are already both visible,
            // so the box narrows the open one rather than becoming a mode of its own.
            if (IsTileMode)
            {
                RebuildTileGrid();
                return;
            }

            RebuildSearch();
            RebuildTiles();
        }

        partial void OnSelectedRowChanged(CategoryRowViewModel? value)
        {
            if (!IsTileMode)
            {
                RebuildTiles();
                return;
            }

            _selectedTileCategory = value == null
                ? null
                : _tiles.Categories.FirstOrDefault(category => category.Name == value.Name);

            RebuildTileGrid();
        }

        /// <summary>Expands or collapses a branch. Rebuilds the flat list rather than nesting containers.</summary>
        [RelayCommand]
        private void ToggleExpand(CategoryRowViewModel? row)
        {
            if (row is not { HasChildren: true })
                return;

            row.IsExpanded = !row.IsExpanded;
            PublishVisibleRows();
        }

        /// <summary>
        /// Jumps to a category found by search and clears the query - search is a way to travel, not a
        /// mode to escape from, so landing in the folder with an empty box is the state you wanted.
        /// </summary>
        [RelayCommand]
        private void GoToCategory(CategoryMatchViewModel? match)
        {
            if (match?.Folder == null)
                return;

            Query = string.Empty;
            ExpandTo(match.Folder);
            SelectedRow = _allRows.FirstOrDefault(row => ReferenceEquals(row.Folder, match.Folder));
        }

        /// <summary>Arms placement in the open area for the chosen blueprint; the next map click resolves it.</summary>
        [RelayCommand]
        private void Place(PaletteTileViewModel? tile)
        {
            if (tile == null)
                return;

            var target = _placementTarget?.Invoke();
            if (target == null)
            {
                StatusMessage = "Open an area first, then place into it.";
                return;
            }

            if (tile.Tile is { } entry)
            {
                StatusMessage = target.ArmTilePlacement(entry)
                    ? $"Click a cell to place {entry.Label}."
                    : "This area has no tile grid to paint.";
                return;
            }

            if (target.ArmPlacement(SelectedType, tile.ResRef, tile.Source))
                StatusMessage = $"Click the map to place {tile.Name}.";
            else
                StatusMessage = $"{SelectedType.DisplayName()} cannot be placed in this area.";
        }

        /// <summary>Opens the blueprint in its own editor tab.</summary>
        [RelayCommand]
        private void Edit(PaletteTileViewModel? tile)
        {
            // A tile is game data in a .set file, not a module resource - there is nothing to open.
            if (tile == null || tile.IsTile)
                return;

            _editorService?.Invoke().TryOpenEditor(SelectedType, tile.ResRef);
        }

        /// <summary>
        /// Creates an independent custom blueprint from the selected blueprint and opens that copy for
        /// editing. The source and all instances placed from it remain untouched.
        /// </summary>
        [RelayCommand]
        private void EditCopy(PaletteTileViewModel? tile)
        {
            if (tile == null || tile.IsTile || !CanEditCopy)
                return;

            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return;

            var sourceSection = tile.Source == PaletteSource.Standard
                ? _categories.StandardSection(SelectedType)
                : _categories.Section(SelectedType);
            var sourceFolder = SourceFolderForCopy(tile, sourceSection);
            var sourcePath = sourceFolder == null || sourceSection == null
                ? Array.Empty<string>()
                : sourceSection.PathTo(sourceFolder).ToArray();

            string copyResRef;
            string copyPath;
            try
            {
                copyResRef = BlueprintCopyFactory.NextResRef(
                    tile.ResRef,
                    workspace.EnumerateResRefs(SelectedType));
                copyPath = workspace.GetResourcePath(SelectedType, copyResRef);

                var source = tile.Source == PaletteSource.Standard
                    ? workspace.LoadIndexedBlueprint(SelectedType, tile.ResRef)
                    : workspace.LoadBlueprint(SelectedType, tile.ResRef);
                var content = BlueprintCopyFactory.CreateFileContent(
                    SelectedType,
                    source.Document,
                    copyResRef);

                Directory.CreateDirectory(Path.GetDirectoryName(copyPath)!);
                SaveService.WriteNewAtomic(copyPath, content);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Could not copy {tile.Name}: {ex.Message}";
                _log.AppendLine(
                    $"Edit Copy failed for {SelectedType.Extension()} blueprint '{tile.ResRef}': {ex.Message}");
                return;
            }

            _workspaceContext.RefreshCatalogEntry(SelectedType, copyResRef);

            string? targetPathKey = null;
            var filed = true;
            if (sourcePath.Length > 0 && _categories.Section(SelectedType) is { } customSection)
            {
                var targetFolder = EnsureFolderPath(customSection, sourcePath);
                targetFolder.AddMember(copyResRef);
                filed = SaveCategories();
                if (filed)
                    targetPathKey = customSection.PathKey(targetFolder);
            }

            // Edit Copy always lands on the Custom side. Reveal the new entry there before opening its
            // editor, matching Aurora and making the new blueprint immediately available for placement.
            if (!IsCustomSource)
                Source = PaletteSource.Custom;
            else
                Refresh();

            RevealCustomCopy(copyResRef, filed ? targetPathKey : null);

            if (filed)
            {
                StatusMessage = $"Copied {tile.Name} as {copyResRef}.";
                _log.AppendLine(
                    $"Copied {SelectedType.Extension()} blueprint '{tile.ResRef}' to '{copyResRef}' ({copyPath}).");
            }
            else
            {
                var category = sourcePath.Length == 0 ? "its source category" : sourcePath[^1];
                StatusMessage =
                    $"Copied {tile.Name} as {copyResRef}, but it could not be filed under '{category}' - " +
                    $"it is in Unsorted. {StatusMessage}";
                _log.AppendLine(
                    $"Copied {SelectedType.Extension()} blueprint '{tile.ResRef}' to '{copyResRef}', " +
                    $"but could not file the copy under '{category}'.");
            }

            _editorService?.Invoke().TryOpenEditor(SelectedType, copyResRef);
        }

        private CategoryFolder? SourceFolderForCopy(
            PaletteTileViewModel tile,
            CategorySection? sourceSection)
        {
            if (sourceSection == null)
                return null;

            var containing = sourceSection.FoldersContaining(tile.ResRef).ToList();
            if (containing.Count == 0)
                return null;

            // A parent row includes all descendants. Prefer the leaf below the row whose tile menu was
            // used; search has no category context, so its stable first filing is the best answer.
            if (!IsSearching && SelectedRow?.Folder is { } selectedFolder)
            {
                var selectedPath = sourceSection.PathTo(selectedFolder);
                var beneathSelection = containing.FirstOrDefault(folder =>
                {
                    var candidatePath = sourceSection.PathTo(folder);
                    return candidatePath.Count >= selectedPath.Count &&
                           candidatePath.Take(selectedPath.Count)
                               .SequenceEqual(selectedPath, StringComparer.OrdinalIgnoreCase);
                });

                if (beneathSelection != null)
                    return beneathSelection;
            }

            return containing[0];
        }

        /// <summary>Finds or creates the Custom category corresponding to a source category path.</summary>
        private static CategoryFolder EnsureFolderPath(
            CategorySection section,
            IReadOnlyList<string> path)
        {
            var current = section.Folders.FirstOrDefault(folder =>
                              string.Equals(folder.Name, path[0], StringComparison.OrdinalIgnoreCase))
                          ?? section.AddFolder(path[0]);

            for (var index = 1; index < path.Count; index++)
            {
                var segment = path[index];
                current = current.Children.FirstOrDefault(child =>
                              string.Equals(child.Name, segment, StringComparison.OrdinalIgnoreCase))
                          ?? current.AddChild(segment);
            }

            return current;
        }

        private void RevealCustomCopy(string copyResRef, string? targetPathKey)
        {
            var section = _categories.Section(SelectedType);
            var targetFolder = targetPathKey == null ? null : section?.FindByPathKey(targetPathKey);
            var row = targetFolder == null
                ? _allRows.FirstOrDefault(candidate => candidate.IsUnsorted)
                : _allRows.FirstOrDefault(candidate => ReferenceEquals(candidate.Folder, targetFolder));

            if (targetFolder != null)
                ExpandTo(targetFolder);

            SelectedRow = row;
            SelectedTile = Tiles.FirstOrDefault(tile =>
                string.Equals(tile.ResRef, copyResRef, StringComparison.OrdinalIgnoreCase));
        }

        // ----- context-menu actions -----
        //
        // Right-clicking a tile or a category row selects it first (see PaletteView's ContextRequested
        // handlers), so every command below acts on the selection and the menu items need no parameter
        // plumbing of their own.

        /// <summary>
        /// Deletes the blueprint's file from the module.
        /// </summary>
        /// <remarks>
        /// The confirmation names the file and says what it cannot undo, because this is the one palette
        /// action that destroys something outside the toolset's own sidecar: areas that placed this
        /// blueprint keep their instances, and those instances will no longer resolve.
        /// </remarks>
        [RelayCommand]
        private async Task DeleteTileAsync(PaletteTileViewModel? tile)
        {
            tile ??= SelectedTile;

            // A tile has no blueprint file behind it, so there is nothing here to delete.
            if (tile == null || tile.IsTile || _prompts == null)
                return;

            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return;

            var path = workspace.GetResourcePath(SelectedType, tile.ResRef);
            var kind = SelectedType.SingularDisplayName().ToLowerInvariant();

            // Refused rather than handled: an open editor holds a session on this file, and once the
            // file is gone that editor's next save either recreates the blueprint (Overwrite) or fails
            // outright (Reload). Closing it first is the builder's call, not something to do silently.
            if (_editorService?.Invoke().IsOpen(SelectedType, tile.ResRef) == true)
            {
                StatusMessage = $"'{tile.Name}' is open in an editor - close that tab first.";
                return;
            }

            // Asked before the delete, not after: removing the file is irreversible from here, and a
            // sidecar that cannot be written would leave the category pointing at a resource that no
            // longer exists with nothing the builder can do about it.
            if (_categories.Section(SelectedType)?.FoldersContaining(tile.ResRef).Any() == true)
            {
                var preflight = _categories.CanSaveChanges();
                if (!preflight.Saved)
                {
                    StatusMessage = $"'{tile.Name}' was not deleted: {preflight.Problem}";
                    _log.AppendLine($"Deleting blueprint '{tile.ResRef}' was refused: {preflight.Problem}");
                    return;
                }
            }

            byte[] expectedBlueprintHash;
            try
            {
                expectedBlueprintHash = SHA256.HashData(File.ReadAllBytes(path));
            }
            catch (Exception ex)
            {
                StatusMessage = $"'{tile.Name}' was not deleted: could not fingerprint its blueprint ({ex.Message}).";
                _log.AppendLine($"Deleting blueprint '{tile.ResRef}' was refused: {ex.Message}");
                return;
            }

            var confirmed = await _prompts.ConfirmDestructiveAsync(
                $"Delete the {kind} '{tile.Name}'?",
                $"This deletes {Path.GetFileName(path)} from the module. Any area that already placed " +
                "it keeps its instances, and those will no longer resolve. This cannot be undone from " +
                "the toolset.",
                "Delete").ConfigureAwait(true);

            if (!confirmed)
                return;

            // Rechecked here, not just at the CanWrite gate that greys the menu item: a pack,
            // validation, or Build All can start while the confirmation dialog is on screen, and this
            // delete - unlike blueprint creation, which always goes through
            // SaveService.WriteNewAtomic - had no guarded write path of its own to catch that.
            if (_mutationLock?.IsLocked == true)
            {
                StatusMessage = $"'{tile.Name}' was not deleted: the module is being packed, validated, or built.";
                _log.AppendLine($"Deleting blueprint '{tile.ResRef}' was refused: the module is locked.");
                return;
            }

            // The sidecar preflight above ran BEFORE the confirmation dialog, and the sidecar can
            // change externally while that dialog sits open. Discovering the conflict only at the
            // SaveCategories below would be too late - the blueprint would already be gone while
            // the (externally updated) sidecar still lists it - so the last check runs here,
            // immediately before the irreversible delete.
            if (_categories.Section(SelectedType)?.FoldersContaining(tile.ResRef).Any() == true)
            {
                var recheck = _categories.CanSaveChanges();
                if (!recheck.Saved)
                {
                    StatusMessage = $"'{tile.Name}' was not deleted: {recheck.Problem}";
                    _log.AppendLine($"Deleting blueprint '{tile.ResRef}' was refused: {recheck.Problem}");
                    return;
                }
            }

            ModuleWriteLock moduleWriteLock;
            try
            {
                // The same guard SaveService's write paths check before touching disk, so the delete
                // itself is refused the instant a module-wide operation starts - not just at the
                // recheck above, which still leaves a race between it and the file operation.
                Services.ModuleMutationLock.ThrowIfModuleLocked();
                moduleWriteLock = ModuleWriteLock.AcquireForResourcePath(path);
            }
            catch (Exception ex)
            {
                StatusMessage = $"'{tile.Name}' was not deleted: {ex.Message}";
                _log.AppendLine($"Deleting blueprint '{tile.ResRef}' failed: {ex.Message}");
                return;
            }

            using var heldModuleWriteLock = moduleWriteLock;
            try
            {

                if (!File.Exists(path) ||
                    !SHA256.HashData(File.ReadAllBytes(path))
                        .AsSpan()
                        .SequenceEqual(expectedBlueprintHash))
                {
                    throw new IOException(
                        $"{Path.GetFileName(path)} changed while the delete confirmation was open. " +
                        "Reload the palette and try again.");
                }

                File.Delete(path);
            }
            catch (Exception ex)
            {
                StatusMessage = $"'{tile.Name}' was not deleted: {ex.Message}";
                _log.AppendLine($"Deleting blueprint '{tile.ResRef}' failed: {ex.Message}");
                return;
            }

            // Out of the catalog, or Explorer and Search keep listing a resource whose file is gone and
            // opening that row fails against the missing file.
            _workspaceContext.RemoveCatalogEntry(SelectedType, tile.ResRef);

            // Drop it from the sidecar too, or the category keeps a member that resolves to nothing.
            // Preflighted above, so a failure here means the sidecar changed underneath us while the
            // confirmation was on screen - rare, and still worth saying out loud.
            var unfiled = true;
            if (_categories.Section(SelectedType) is { } section)
            {
                foreach (var folder in section.FoldersContaining(tile.ResRef).ToList())
                    folder.RemoveMember(tile.ResRef);

                unfiled = SaveCategories();
                if (!unfiled)
                {
                    StatusMessage =
                        $"Deleted {tile.Name}, but its category still lists it. {StatusMessage}";
                    _log.AppendLine(
                        $"Deleted blueprint '{tile.ResRef}' but its category entry could not be removed.");
                }
            }

            SelectedTile = null;
            Refresh();
            if (unfiled)
            {
                StatusMessage = $"Deleted {tile.Name}.";
                _log.AppendLine($"Deleted blueprint '{tile.ResRef}' ({path}).");
            }
        }

        /// <summary>The label for the type-specific create action, e.g. "New Placeable...".</summary>
        public string NewBlueprintLabel => $"New {SelectedType.SingularDisplayName()}...";

        /// <summary>
        /// Creates a blueprint of the active type and files it into the right-clicked category.
        /// </summary>
        /// <remarks>
        /// The new blueprint is built from the type's editor schema plus whatever else every real
        /// blueprint of that type carries (see <see cref="BlueprintTemplateFactory"/>), so it opens in the
        /// editor as a complete, valid object with defaults rather than a stub the editor cannot show. It
        /// opens straight away: nobody creates a blueprint in order to leave it alone.
        /// </remarks>
        [RelayCommand]
        private async Task NewBlueprintAsync()
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null || _prompts == null || !CanCreateBlueprint)
                return;

            var kind = SelectedType.SingularDisplayName();
            var name = await _prompts.PromptForTextAsync(
                $"New {kind}",
                "The ResRef is derived from this name: lowercase, no spaces, 16 characters at most - " +
                "NWN's own limit.",
                string.Empty,
                "Create").ConfigureAwait(true);

            if (name == null)
                return;

            var resRef = ToResRef(name);
            if (resRef.Length == 0)
            {
                StatusMessage = "That name has no letters or digits to build a ResRef from.";
                return;
            }

            var path = workspace.GetResourcePath(SelectedType, resRef);
            if (File.Exists(path))
            {
                StatusMessage = $"A {kind.ToLowerInvariant()} called '{resRef}' already exists.";
                return;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                SaveService.WriteNewAtomic(
                    path, BlueprintTemplateFactory.CreateFileContent(SelectedType, resRef, name));
            }
            catch (Exception ex)
            {
                StatusMessage = $"Could not create {resRef}: {ex.Message}";
                _log.AppendLine($"Creating {SelectedType.Extension()} blueprint '{resRef}' failed: {ex.Message}");
                return;
            }

            // Into the catalog straight away. It is a persistent snapshot, so without this the new
            // blueprint is missing from Explorer and Search, and the palette shows its resref instead of
            // the name that was just typed - opening the clean editor raises nothing on its own.
            _workspaceContext.RefreshCatalogEntry(SelectedType, resRef);

            // Filed where the builder asked for it, which is the whole reason this lives on the category's
            // menu rather than a global New button.
            var filed = true;
            if (SelectedRow?.Folder is { } folder)
            {
                folder.AddMember(resRef);
                filed = SaveCategories();

                // SaveCategories restored the persisted catalog, so the blueprint exists but is in
                // Unsorted. Said rather than overwritten with "Created ...": create-and-file was the
                // operation asked for, and only half of it happened.
                if (!filed)
                {
                    StatusMessage =
                        $"Created {name}, but it could not be filed under '{folder.Name}' - it is in Unsorted. {StatusMessage}";
                    _log.AppendLine(
                        $"Created {SelectedType.Extension()} blueprint '{resRef}' but could not file it under '{folder.Name}'.");
                }
            }

            Refresh();
            if (filed)
            {
                StatusMessage = $"Created {name}.";
                _log.AppendLine($"Created {SelectedType.Extension()} blueprint '{resRef}' ({path}).");
            }

            _editorService?.Invoke().TryOpenEditor(SelectedType, resRef);
        }

        /// <summary>
        /// Reduces a display name to a legal NWN resref: lowercase, alphanumerics and underscores, 16
        /// characters. Anything else is dropped rather than substituted, so the result stays readable.
        /// </summary>
        private static string ToResRef(string name)
        {
            var builder = new StringBuilder(name.Length);
            foreach (var character in name)
            {
                if (char.IsAsciiLetterOrDigit(character))
                    builder.Append(char.ToLowerInvariant(character));
                else if (character is ' ' or '_' or '-' && builder.Length > 0 && builder[^1] != '_')
                    builder.Append('_');

                if (builder.Length == 16)
                    break;
            }

            return builder.ToString().TrimEnd('_');
        }

        /// <summary>Adds a subcategory inside the selected one, or a top-level one when nothing is selected.</summary>
        [RelayCommand]
        private async Task NewCategoryAsync()
        {
            var section = _categories.Section(SelectedType);
            if (section == null || _prompts == null || !CanWrite)
                return;

            var parent = SelectedRow?.Folder;
            var name = await _prompts.PromptForTextAsync(
                parent == null ? "New category" : $"New category inside '{parent.Name}'",
                "Categories are the toolset's own organisation - they are stored beside the module, not in it.",
                string.Empty,
                "Create").ConfigureAwait(true);

            if (name == null)
                return;

            // Checked rather than sanitized: the builder typed this and is still here to retype it, so
            // say what is wrong instead of quietly hyphenating a name they did not ask for. The
            // constructor would throw, and an exception out of a command handler has nowhere to go.
            // Asked before the sibling check, so a name holding a separator is reported as that rather
            // than as a clash with whatever the split happened to land on.
            if (CategoryFolder.NameProblem(name) is { } problem)
            {
                StatusMessage = problem;
                return;
            }

            var nameAvailable = parent == null
                ? section.IsNameAvailable(name)
                : parent.IsNameAvailable(name);
            if (!nameAvailable)
            {
                StatusMessage = $"A category named '{name.Trim()}' already exists here.";
                return;
            }

            if (parent != null)
                parent.AddChild(name);
            else
                section.AddFolder(name);

            // Stop on a refused write rather than reporting success over it. SaveCategories has already
            // put the reason in StatusMessage; overwriting that with "Added category" told the builder
            // the edit had landed when it only existed in memory, and it was gone on restart.
            if (!SaveCategories())
            {
                Refresh();
                return;
            }

            Refresh();
            StatusMessage = $"Added category '{name}'.";
            _log.AppendLine($"Added category '{name}' to the {SelectedType.DisplayName().ToLowerInvariant()} palette.");
        }

        /// <summary>Renames the selected category, prompting with its current name.</summary>
        [RelayCommand]
        private async Task RenameCategoryAsync()
        {
            if (SelectedRow?.Folder is not { } folder || _prompts == null)
                return;

            var name = await _prompts.PromptForTextAsync(
                $"Rename '{folder.Name}'",
                string.Empty,
                folder.Name,
                "Rename").ConfigureAwait(true);

            if (name == null || name == folder.Name)
                return;

            if (CategoryFolder.NameProblem(name) is { } problem)
            {
                StatusMessage = problem;
                return;
            }

            var previous = folder.Name;

            var section = CurrentSection();
            if (section == null || !section.TryRenameFolder(folder, name))
            {
                StatusMessage = $"A category named '{name.Trim()}' already exists here.";
                return;
            }

            if (!SaveCategories())
            {
                Refresh();
                // Refresh() rebuilds every row, so the pre-rebuild SelectedRow is now orphaned. Rename
                // mutates the CategoryFolder in place, so the same reference finds its rebuilt row.
                SelectedRow = _allRows.FirstOrDefault(row => ReferenceEquals(row.Folder, folder));
                return;
            }

            Refresh();
            SelectedRow = _allRows.FirstOrDefault(row => ReferenceEquals(row.Folder, folder));
            StatusMessage = $"Renamed '{previous}' to '{folder.Name}'.";
        }

        /// <summary>
        /// Deletes the selected category. Refuses a category that still holds blueprints rather than
        /// confirming it: the members would be silently unfiled into Unsorted with no way back, and
        /// nothing about "Delete" suggests that.
        /// </summary>
        [RelayCommand]
        private async Task DeleteCategoryAsync()
        {
            var section = _categories.Section(SelectedType);
            if (section == null || SelectedRow?.Folder is not { } folder || _prompts == null)
                return;

            if (folder.MembersIncludingDescendants.Any())
            {
                StatusMessage = $"'{folder.Name}' still holds blueprints - empty it first.";
                return;
            }

            // Child categories count as contents too. Without this, a branch of empty-but-organised
            // categories has no members, so the check above passes and the whole subtree goes with the
            // parent - the opposite of what the prompt promises, and there is no undo for it.
            if (folder.Children.Count > 0)
            {
                StatusMessage = $"'{folder.Name}' still holds sub-categories - remove them first.";
                return;
            }

            var confirmed = await _prompts.ConfirmDestructiveAsync(
                $"Delete the category '{folder.Name}'?",
                "The category is removed from this palette. No blueprints are deleted.",
                "Delete").ConfigureAwait(true);

            if (!confirmed)
                return;

            section.RemoveFolder(folder);
            SelectedRow = null;
            if (!SaveCategories())
            {
                Refresh();
                return;
            }

            Refresh();
            StatusMessage = $"Removed category '{folder.Name}'.";
        }

        /// <summary>Files the selected blueprint into the selected category - the move half of organizing.</summary>
        [RelayCommand]
        private void FileSelectedTile()
        {
            if (!CanWrite)
                return;

            if (SelectedTile == null)
            {
                StatusMessage = "Select a blueprint first.";
                return;
            }

            if (SelectedRow?.Folder is not { } folder)
            {
                StatusMessage = "Select the category to file it into.";
                return;
            }

            var section = _categories.Section(SelectedType);
            if (section == null)
                return;

            // Captured before saving. SaveChanges raises Changed, Refresh clears Tiles, and the bound
            // ListBox nulls SelectedTile - so reading it after the save dereferences null.
            var resRef = SelectedTile.ResRef;
            var label = SelectedTile.Name;

            // Filing is a move, not a copy: the same resref sitting in two folders is legal but is not
            // what a drag onto a folder means.
            foreach (var previous in section.FoldersContaining(resRef).ToList())
                previous.RemoveMember(resRef);

            folder.AddMember(resRef);
            if (!SaveCategories())
            {
                Refresh();
                return;
            }

            Refresh();
            StatusMessage = $"Filed {label} into '{folder.Name}'.";
        }


        /// <summary>
        /// Writes the category sidecar and reports a refusal in the status line.
        /// </summary>
        /// <remarks>
        /// The sidecar can legitimately decline a write - it is read-only when a newer Toolset wrote it,
        /// and it will not clobber an edit made outside the app. Every command here has already told the
        /// builder what it did, so a silent refusal would leave them believing it.
        /// </remarks>
        private bool SaveCategories()
        {
            var result = _categories.SaveChanges();
            if (!result.Saved)
                StatusMessage = result.Problem;

            return result.Saved;
        }

        [RelayCommand]
        private void TogglePin()
        {
            var section = _categories.Section(SelectedType);
            if (section == null || SelectedRow?.Folder is not { } folder)
                return;

            // By path, not by name: two branches may hold folders of the same name, and pinning by name
            // showed one while unpinning the other.
            var pathKey = section.PathKey(folder);
            if (section.Pinned.Contains(pathKey, StringComparer.OrdinalIgnoreCase))
                section.Unpin(pathKey);
            else
                section.Pin(pathKey);

            SaveCategories();
            Refresh();
            // Refresh() rebuilds every row, so the pre-rebuild SelectedRow is now orphaned. Pinning
            // does not replace the CategoryFolder, so the same reference finds its rebuilt row.
            SelectedRow = _allRows.FirstOrDefault(row => ReferenceEquals(row.Folder, folder));
        }

        // ----- tree assembly -----

        private void RebuildTree()
        {
            _allRows.Clear();
            var section = CurrentSection();

            if (section != null)
            {
                foreach (var pathKey in section.Pinned)
                {
                    var pinned = section.FindByPathKey(pathKey);
                    if (pinned != null)
                        _allRows.Add(new CategoryRowViewModel(pinned, 0, section.CountIn(pinned, _existing), false)
                        {
                            IsPinned = true
                        });
                }

                foreach (var folder in section.Folders)
                    AddRows(section, folder, 0);

                // Unsorted is generated, always last, and always present - an unfiled blueprint must never
                // be invisible just because no rule matched it.
                _allRows.Add(new CategoryRowViewModel(null, 0, section.UnsortedResRefs(_existing).Count, false));
            }

            PublishVisibleRows();
        }

        private void AddRows(CategorySection section, CategoryFolder folder, int depth)
        {
            var row = new CategoryRowViewModel(
                folder, depth, section.CountIn(folder, _existing), folder.Children.Count > 0);

            _allRows.Add(row);
            foreach (var child in folder.Children)
                AddRows(section, child, depth + 1);
        }

        /// <summary>
        /// Publishes the rows whose ancestors are all expanded. Walking the flat list and skipping
        /// collapsed subtrees keeps this O(rows) with no nested containers to realise.
        /// </summary>
        private void PublishVisibleRows()
        {
            Rows.Clear();
            var hiddenBelowDepth = int.MaxValue;

            foreach (var row in _allRows)
            {
                if (row.Depth > hiddenBelowDepth)
                    continue;

                hiddenBelowDepth = int.MaxValue;
                Rows.Add(row);

                if (row.HasChildren && !row.IsExpanded)
                    hiddenBelowDepth = row.Depth;
            }
        }

        private void ExpandTo(CategoryFolder folder)
        {
            // The folder belongs to whichever side is showing. Asking the Custom section to path a
            // Standard folder returned no ancestors, so clearing a Standard search left the matched
            // category's parents collapsed and the selected row invisible.
            var section = CurrentSection();
            if (section == null)
                return;

            var path = section.PathTo(folder);
            foreach (var row in _allRows.Where(candidate => candidate.HasChildren))
            {
                var rowPath = row.Folder == null ? Array.Empty<string>() : section.PathTo(row.Folder);
                if (rowPath.Count > 0 && rowPath.Count < path.Count &&
                    rowPath.SequenceEqual(path.Take(rowPath.Count), StringComparer.OrdinalIgnoreCase))
                    row.IsExpanded = true;
            }

            PublishVisibleRows();
        }

        // ----- search -----

        private void RebuildSearch()
        {
            CategoryMatches.Clear();

            var section = CurrentSection();
            if (section == null || !IsSearching)
            {
                OnPropertyChanged(nameof(HasCategoryMatches));
                return;
            }

            var query = Query.Trim();
            foreach (var folder in section.AllFolders())
            {
                if (!folder.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                    continue;

                var path = section.PathTo(folder);
                var parentPath = path.Count > 1
                    ? string.Join(" › ", path.Take(path.Count - 1)) + " ›"
                    : string.Empty;

                CategoryMatches.Add(new CategoryMatchViewModel(
                    folder, parentPath, section.CountIn(folder, _existing)));

                if (CategoryMatches.Count >= 40)
                    break;
            }

            OnPropertyChanged(nameof(HasCategoryMatches));
        }

        // ----- grid assembly -----

        private void RebuildTiles()
        {
            Tiles.Clear();
            var section = CurrentSection();
            if (section == null)
            {
                Breadcrumb = string.Empty;
                return;
            }

            if (IsSearching)
            {
                RebuildSearchTiles(section);
                return;
            }

            var resRefs = ResRefsForSelectedRow(section);
            Breadcrumb = BreadcrumbFor(section);

            foreach (var resRef in resRefs.OrderBy(NameFor, StringComparer.CurrentCultureIgnoreCase))
                AddTile(new PaletteTileViewModel(resRef, NameFor(resRef), null, Source));
        }

        private void RebuildSearchTiles(CategorySection section)
        {
            var query = Query.Trim();
            var matches = _existing
                .Where(resRef =>
                    resRef.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    NameFor(resRef).Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderBy(NameFor, StringComparer.CurrentCultureIgnoreCase)
                .Take(MaxSearchResults)
                .ToList();

            foreach (var resRef in matches)
            {
                var folder = section.FoldersContaining(resRef).FirstOrDefault();
                AddTile(new PaletteTileViewModel(resRef, NameFor(resRef), folder?.Name, Source));
            }

            Breadcrumb = matches.Count >= MaxSearchResults
                ? $"First {MaxSearchResults} of many matches"
                : $"{matches.Count} match{(matches.Count == 1 ? string.Empty : "es")} across all categories";
        }

        /// <summary>
        /// Publishes a tile, with its preview if that is already decoded and waiting.
        /// </summary>
        /// <remarks>
        /// Only the free half of the work. A cell whose image is not already in memory is left to
        /// <see cref="EnsurePreview"/>, which the view calls as the cell comes within reach of the
        /// viewport - a category can hold a couple of thousand blueprints against the forty or so cells on
        /// screen, and fetching every one of them the moment the category opens made opening it cost
        /// seconds of work for images nobody had scrolled to yet.
        /// </remarks>
        private void AddTile(PaletteTileViewModel tile)
        {
            Tiles.Add(tile);

            tile.Preview = tile.IsTile
                ? _thumbnails?.CachedTile(
                    tile.ResRef, tile.Tile?.FootprintModelResRefs, tile.Tile?.Columns ?? 1, tile.Tile?.Rows ?? 1)
                : _thumbnails?.Cached(SelectedType, tile.ResRef, tile.Source == PaletteSource.Standard);

            if (tile.Preview != null)
                tile.PreviewRequested = true;
        }

        /// <summary>
        /// Fetches a cell's preview unless it already has one or has already asked. Called by the view
        /// when the cell comes within reach of the viewport; safe to call as often as it likes.
        /// </summary>
        public void EnsurePreview(PaletteTileViewModel? tile)
        {
            if (tile == null || tile.PreviewRequested)
                return;

            tile.PreviewRequested = true;

            if (tile.IsTile)
            {
                _thumbnails?.RequestTileAsync(
                    tile.ResRef,
                    bitmap => tile.Preview = bitmap,
                    tile.Tile?.FootprintModelResRefs,
                    tile.Tile?.Columns ?? 1,
                    tile.Tile?.Rows ?? 1);
                return;
            }

            _thumbnails?.RequestAsync(
                SelectedType,
                tile.ResRef,
                tile.Source == PaletteSource.Standard,
                bitmap => tile.Preview = bitmap);
        }

        /// <summary>
        /// Drops a visible tile's stale preview when its blueprint is saved - from its own editor tab,
        /// or as a dependent of an edited item another creature equips - and asks for a fresh one right
        /// away.
        /// </summary>
        /// <remarks>
        /// Without this, appearance and icon edits stayed invisible until the category was closed and
        /// reopened: <see cref="ThumbnailService.Invalidate"/> only clears its own memory/disk caches
        /// and drops an in-flight render, and a tile that already had a delivered preview - or was
        /// mid-render when the invalidation landed - had nothing telling it to ask again.
        /// </remarks>
        private void OnThumbnailInvalidated(ResourceType type, string resRef)
        {
            if (IsTileMode || type != SelectedType)
                return;

            foreach (var tile in Tiles)
            {
                if (tile.IsTile || !string.Equals(tile.ResRef, resRef, StringComparison.OrdinalIgnoreCase))
                    continue;

                tile.Preview = null;
                tile.PreviewRequested = false;
                EnsurePreview(tile);
            }
        }

        /// <summary>
        /// The preview box scales with the tile, keeping every tile the same proportions. Close to square
        /// on purpose: model thumbnails are rendered square and inventory icons are as tall as the
        /// inventory slot they were drawn for (a rifle is 32x96), so a wide letterbox wasted most of the
        /// tile on both.
        /// </summary>
        public double PreviewHeight => Math.Round(TileSize * 0.72);

        private IEnumerable<string> ResRefsForSelectedRow(CategorySection section)
        {
            if (SelectedRow == null)
                return Array.Empty<string>();

            if (SelectedRow.IsUnsorted)
                return section.UnsortedResRefs(_existing);

            // Descendants included, always. The count on a category row has always counted them, so
            // showing only direct members made a parent read "NPCs 368" over an empty grid - which is
            // what selecting a parent category did after the incl. sub toggle was removed. Including them
            // is also the answer the toggle was left on for.
            return SelectedRow.Folder!.MembersIncludingDescendants.Where(_existing.Contains);
        }

        private string BreadcrumbFor(CategorySection section)
        {
            if (SelectedRow == null)
                return "Select a category";

            if (SelectedRow.IsUnsorted)
                return CategorySection.UnsortedFolderName;

            var path = section.PathTo(SelectedRow.Folder!);
            return path.Count == 0 ? SelectedRow.Name : string.Join(" › ", path);
        }

        /// <summary>Resref to display name for the current type, rebuilt when the catalog changes.</summary>
        private Dictionary<string, string>? _namesForType;

        /// <summary>The catalog snapshot <see cref="_namesForType"/> was built from.</summary>
        private object? _namesBuiltFrom;

        private ResourceType _namesBuiltForType;

        /// <summary>
        /// A blueprint's display name, falling back to its resref while the catalog is still building or
        /// for blueprints the module does not index.
        /// </summary>
        /// <remarks>
        /// Backed by a per-type dictionary rather than a scan of the whole catalog. Search calls this for
        /// every candidate resref and again while sorting, so against ~17,900 catalog entries and 8,355
        /// placeables a linear scan meant tens of millions of comparisons on the UI thread per keystroke.
        /// The dictionary is rebuilt only when the catalog publishes a new snapshot or the type changes.
        /// </remarks>
        private string NameFor(string resRef)
        {
            // Base-game blueprints are not in the module, so the catalog knows nothing about them; their
            // only name is the one the palette file declares.
            if (IsStandardSource)
            {
                return _categories.StandardNames(SelectedType).TryGetValue(resRef, out var standardName)
                    ? standardName
                    : resRef;
            }

            var entries = _workspaceContext.Catalog?.Entries;
            if (entries == null)
                return resRef;

            if (!ReferenceEquals(entries, _namesBuiltFrom) || _namesBuiltForType != SelectedType || _namesForType == null)
            {
                _namesForType = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in entries)
                {
                    if (entry.ResourceType == SelectedType && !string.IsNullOrWhiteSpace(entry.Name))
                        _namesForType[entry.ResRef] = entry.Name!;
                }

                _namesBuiltFrom = entries;
                _namesBuiltForType = SelectedType;
            }

            return _namesForType.TryGetValue(resRef, out var name) ? name : resRef;
        }
    }
}
