using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using SWLOR.Toolset.Domain.Categories;
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
        /// <summary>Types offered, in the order a builder reaches for them when dressing an area.</summary>
        private static readonly ResourceType[] OfferedTypes =
        {
            ResourceType.Utp, ResourceType.Utc, ResourceType.Utd, ResourceType.Utm,
            ResourceType.Utw, ResourceType.Uti, ResourceType.Utt, ResourceType.Uts
        };

        private const int MaxSearchResults = 200;

        /// <summary>How many type chips show before More... - enough for one row at the panel's width.</summary>
        private const int PrimaryTypeCount = 3;

        private readonly WorkspaceContext _workspaceContext;
        private readonly CategoryService _categories;
        private readonly OutputLogService _log;
        private readonly Func<Editors.EditorService>? _editorService;
        private readonly Func<IAreaPlacementTarget?>? _placementTarget;
        private readonly ThumbnailService? _thumbnails;
        private readonly Services.IEditorPromptService? _prompts;

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

        [ObservableProperty]
        private bool _isOrganizing;

        /// <summary>Tile width in pixels. Idle while tiles are glyphs; the control the grid needs the moment they become rendered models.</summary>
        [ObservableProperty]
        private double _tileSize = 136;

        /// <summary>
        /// True once More... has been pressed. Only the four types a builder reaches for when dressing
        /// an area are shown up front, so the chip row stays one line instead of wrapping to two.
        /// </summary>
        [ObservableProperty]
        private bool _showAllTypes;

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

        partial void OnShowAllTypesChanged(bool value) => PublishTypeChips();

        /// <summary>Reveals the types behind More...</summary>
        [RelayCommand]
        private void ShowMoreTypes() => ShowAllTypes = true;

        public bool IsSearching => !string.IsNullOrWhiteSpace(Query);

        public bool HasCategoryMatches => CategoryMatches.Count > 0;

        public PaletteViewModel(
            WorkspaceContext workspaceContext,
            CategoryService categories,
            OutputLogService log,
            Func<Editors.EditorService>? editorService = null,
            Func<IAreaPlacementTarget?>? placementTarget = null,
            ThumbnailService? thumbnails = null,
            Services.IEditorPromptService? prompts = null)
        {
            _thumbnails = thumbnails;
            _prompts = prompts;
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

        /// <summary>Rebuilds the tree and grid for the current type. Safe to call whenever the module changes.</summary>
        public void Refresh()
        {
            _existing = _categories.ExistingResRefs(SelectedType);
            RebuildTree();
            RebuildTiles();
        }

        [RelayCommand]
        private void SelectType(PaletteTypeChipViewModel chip)
        {
            if (chip == null || chip.Type == SelectedType)
                return;

            SelectedType = chip.Type;
        }

        /// <summary>The four primary types, plus the rest once More... has been pressed.</summary>
        private void PublishTypeChips()
        {
            Types.Clear();
            var offered = ShowAllTypes ? OfferedTypes : OfferedTypes.Take(PrimaryTypeCount);

            foreach (var type in offered)
                Types.Add(new PaletteTypeChipViewModel(type) { IsSelected = type == SelectedType });

            // A hidden selection would leave no chip lit, so it joins the row regardless.
            if (!ShowAllTypes && Types.All(chip => chip.Type != SelectedType))
                Types.Add(new PaletteTypeChipViewModel(SelectedType) { IsSelected = true });

            OnPropertyChanged(nameof(HasMoreTypes));
        }

        public bool HasMoreTypes => !ShowAllTypes;

        partial void OnSelectedTypeChanged(ResourceType value)
        {
            foreach (var chip in Types)
                chip.IsSelected = chip.Type == value;

            SelectedRow = null;
            Refresh();
        }

        partial void OnQueryChanged(string value)
        {
            OnPropertyChanged(nameof(IsSearching));
            RebuildSearch();
            RebuildTiles();
        }

        partial void OnSelectedRowChanged(CategoryRowViewModel? value) => RebuildTiles();

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

            if (target.ArmPlacement(SelectedType, tile.ResRef))
                StatusMessage = $"Click the map to place {tile.Name}.";
            else
                StatusMessage = $"{SelectedType.DisplayName()} cannot be placed in this area.";
        }

        /// <summary>Opens the blueprint in its own editor tab.</summary>
        [RelayCommand]
        private void Edit(PaletteTileViewModel? tile)
        {
            if (tile == null)
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
            if (tile == null || _prompts == null)
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

        /// <summary>Adds a subcategory inside the selected one, or a top-level one when nothing is selected.</summary>
        [RelayCommand]
        private async Task NewCategoryAsync()
        {
            var section = _categories.Section(SelectedType);
            if (section == null || _prompts == null)
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
            var section = _categories.Section(SelectedType);

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

            var section = _categories.Section(SelectedType);
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
            var section = _categories.Section(SelectedType);
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
