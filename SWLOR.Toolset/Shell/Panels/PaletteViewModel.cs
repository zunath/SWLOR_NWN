using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using SWLOR.Toolset.Domain.Categories;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Tilesets;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Workspace;
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
        private bool _includeSubcategories;

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

        partial void OnTileSizeChanged(double value)
        {
            OnPropertyChanged(nameof(TileSizeLabel));
            OnPropertyChanged(nameof(PreviewHeight));
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
            OnPropertyChanged(nameof(ReadOnlyNotice));
            OnPropertyChanged(nameof(HasReadOnlyNotice));
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
        public bool CanWrite => IsCustomSource && IsBlueprintMode;

        /// <summary>
        /// Why a context menu is empty, so it never opens as a blank popup. Null when there is nothing to
        /// explain, which is exactly when the menu has real items on it.
        /// </summary>
        public string? ReadOnlyNotice =>
            IsTileMode ? "Tileset content - read-only"
            : IsStandardSource ? "Base game content - read-only"
            : null;

        public bool HasReadOnlyNotice => ReadOnlyNotice != null;

        /// <summary>
        /// The "incl. sub" toggle only has something to do in the blueprint tree: a tileset's categories
        /// are flat, and a search already reaches across all of them.
        /// </summary>
        public bool ShowsIncludeSubcategories => IsBlueprintMode && !IsSearching;

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
            Domain.GameData.Tlk.TlkService? tlk = null)
        {
            _thumbnails = thumbnails;
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

            PublishTypeChips();

            _categories.Changed += Refresh;
        }

        /// <summary>
        /// The category tree currently in play. Null only when the module has no section for this type;
        /// the standard side always returns a section, empty when the base game is unavailable.
        /// </summary>
        private CategorySection? CurrentSection() =>
            IsCustomSource ? _categories.Section(SelectedType) : _categories.StandardSection(SelectedType);

        /// <summary>Rebuilds the tree and grid for the current type. Safe to call whenever the module changes.</summary>
        public void Refresh()
        {
            if (IsTileMode)
            {
                RefreshTiles();
                return;
            }

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
            if (string.IsNullOrWhiteSpace(tilesetResRef))
            {
                StatusMessage = "Open an area to see the tiles its tileset offers.";
                return;
            }

            if (_tilesets == null || !_tilesets.TryGetTileset(tilesetResRef, out var tileset))
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

            foreach (var category in _tiles.Categories)
                _allRows.Add(new CategoryRowViewModel(folder: null, depth: 0, count: category.Entries.Count,
                    hasChildren: false)
                {
                    SyntheticName = category.Name
                });

            PublishVisibleRows();
            SelectedRow = _allRows[0];
            RebuildTileGrid();
            StatusMessage = $"{_tilesets.GetDisplayName(tilesetResRef)} - pick a tile, then click a cell.";
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
            SyncChipSelection();
            OnPropertyChanged(nameof(NewBlueprintLabel));
            SelectedRow = null;
            Refresh();
        }

        partial void OnIsTileModeChanged(bool value)
        {
            SyncChipSelection();
            OnPropertyChanged(nameof(IsBlueprintMode));
            OnPropertyChanged(nameof(ShowsSourceSwitch));
            OnPropertyChanged(nameof(CanWrite));
            OnPropertyChanged(nameof(ReadOnlyNotice));
            OnPropertyChanged(nameof(HasReadOnlyNotice));
            OnPropertyChanged(nameof(ShowsIncludeSubcategories));
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

        partial void OnQueryChanged(string value)
        {
            OnPropertyChanged(nameof(IsSearching));
            OnPropertyChanged(nameof(ShowsIncludeSubcategories));

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

        partial void OnIncludeSubcategoriesChanged(bool value) => RebuildTiles();

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

            if (target.ArmPlacement(SelectedType, tile.ResRef))
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

            var confirmed = await _prompts.ConfirmDestructiveAsync(
                $"Delete the {kind} '{tile.Name}'?",
                $"This deletes {Path.GetFileName(path)} from the module. Any area that already placed " +
                "it keeps its instances, and those will no longer resolve. This cannot be undone from " +
                "the toolset.",
                "Delete").ConfigureAwait(true);

            if (!confirmed)
                return;

            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Could not delete {tile.ResRef}: {ex.Message}";
                _log.AppendLine($"Deleting blueprint '{tile.ResRef}' failed: {ex.Message}");
                return;
            }

            // Drop it from the sidecar too, or the category keeps a member that resolves to nothing.
            if (_categories.Section(SelectedType) is { } section)
            {
                foreach (var folder in section.FoldersContaining(tile.ResRef).ToList())
                    folder.RemoveMember(tile.ResRef);

                _categories.SaveChanges();
            }

            SelectedTile = null;
            Refresh();
            StatusMessage = $"Deleted {tile.Name}.";
            _log.AppendLine($"Deleted blueprint '{tile.ResRef}' ({path}).");
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
            if (workspace == null || _prompts == null || !CanWrite)
                return;

            if (!BlueprintTemplateFactory.Supports(SelectedType))
            {
                StatusMessage = $"{SelectedType.DisplayName()} cannot be created here yet.";
                return;
            }

            var kind = SelectedType.SingularDisplayName();
            var name = await _prompts.PromptForTextAsync(
                $"New {kind}",
                "The resref is derived from this name: lowercase, no spaces, 16 characters at most - " +
                "NWN's own limit.",
                string.Empty,
                "Create").ConfigureAwait(true);

            if (name == null)
                return;

            var resRef = ToResRef(name);
            if (resRef.Length == 0)
            {
                StatusMessage = "That name has no letters or digits to build a resref from.";
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
                File.WriteAllBytes(path, BlueprintTemplateFactory.CreateFileContent(SelectedType, resRef, name));
            }
            catch (Exception ex)
            {
                StatusMessage = $"Could not create {resRef}: {ex.Message}";
                _log.AppendLine($"Creating {SelectedType.Extension()} blueprint '{resRef}' failed: {ex.Message}");
                return;
            }

            // Filed where the builder asked for it, which is the whole reason this lives on the category's
            // menu rather than a global New button.
            if (SelectedRow?.Folder is { } folder)
            {
                folder.AddMember(resRef);
                _categories.SaveChanges();
            }

            Refresh();
            StatusMessage = $"Created {name}.";
            _log.AppendLine($"Created {SelectedType.Extension()} blueprint '{resRef}' ({path}).");
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

            if (parent != null)
                parent.AddChild(name);
            else
                section.AddFolder(name);

            _categories.SaveChanges();
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

            var previous = folder.Name;
            folder.Rename(name);
            _categories.SaveChanges();
            Refresh();
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

            var confirmed = await _prompts.ConfirmDestructiveAsync(
                $"Delete the category '{folder.Name}'?",
                "The category is removed from this palette. No blueprints are deleted.",
                "Delete").ConfigureAwait(true);

            if (!confirmed)
                return;

            section.RemoveFolder(folder);
            SelectedRow = null;
            _categories.SaveChanges();
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

            // Filing is a move, not a copy: the same resref sitting in two folders is legal but is not
            // what a drag onto a folder means.
            foreach (var previous in section.FoldersContaining(SelectedTile.ResRef).ToList())
                previous.RemoveMember(SelectedTile.ResRef);

            folder.AddMember(SelectedTile.ResRef);
            _categories.SaveChanges();
            Refresh();
            StatusMessage = $"Filed {SelectedTile.Name} into '{folder.Name}'.";
        }

        [RelayCommand]
        private void TogglePin()
        {
            var section = _categories.Section(SelectedType);
            if (section == null || SelectedRow?.Folder is not { } folder)
                return;

            if (section.Pinned.Contains(folder.Name, StringComparer.OrdinalIgnoreCase))
                section.Unpin(folder.Name);
            else
                section.Pin(folder.Name);

            _categories.SaveChanges();
            Refresh();
        }

        // ----- tree assembly -----

        private void RebuildTree()
        {
            _allRows.Clear();
            var section = CurrentSection();

            if (section != null)
            {
                foreach (var name in section.Pinned)
                {
                    var pinned = section.AllFolders()
                        .FirstOrDefault(folder => string.Equals(folder.Name, name, StringComparison.OrdinalIgnoreCase));
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
            var section = _categories.Section(SelectedType);
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
                AddTile(new PaletteTileViewModel(resRef, NameFor(resRef), null));
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
                AddTile(new PaletteTileViewModel(resRef, NameFor(resRef), folder?.Name));
            }

            Breadcrumb = matches.Count >= MaxSearchResults
                ? $"First {MaxSearchResults} of many matches"
                : $"{matches.Count} match{(matches.Count == 1 ? string.Empty : "es")} across all categories";
        }

        /// <summary>
        /// Publishes a tile and asks for its thumbnail. The grid appears immediately and fills in as
        /// renders complete, rather than the panel stalling while thousands of models load.
        /// </summary>
        private void AddTile(PaletteTileViewModel tile)
        {
            Tiles.Add(tile);

            if (tile.IsTile)
            {
                tile.Preview = _thumbnails?.CachedTile(tile.ResRef);
                if (tile.Preview == null)
                    _thumbnails?.RequestTileAsync(tile.ResRef, bitmap => tile.Preview = bitmap);

                return;
            }

            var cached = _thumbnails?.Cached(SelectedType, tile.ResRef);
            if (cached != null)
            {
                tile.Preview = cached;
                return;
            }

            _thumbnails?.RequestAsync(SelectedType, tile.ResRef, bitmap => tile.Preview = bitmap);
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

            var folder = SelectedRow.Folder!;
            var members = IncludeSubcategories ? folder.MembersIncludingDescendants : folder.Members;
            return members.Where(_existing.Contains);
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

        /// <summary>
        /// A blueprint's display name from the background catalog, falling back to its resref while the
        /// catalog is still building or for blueprints the module does not index.
        /// </summary>
        private string NameFor(string resRef)
        {
            var entry = _workspaceContext.Catalog?.Entries
                .FirstOrDefault(candidate =>
                    candidate.ResourceType == SelectedType &&
                    string.Equals(candidate.ResRef, resRef, StringComparison.OrdinalIgnoreCase));

            return string.IsNullOrWhiteSpace(entry?.Name) ? resRef : entry.Name!;
        }
    }
}
