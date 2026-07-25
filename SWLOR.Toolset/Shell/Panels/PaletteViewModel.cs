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

        private readonly WorkspaceContext _workspaceContext;
        private readonly CategoryService _categories;
        private readonly OutputLogService _log;
        private readonly Func<Editors.EditorService>? _editorService;
        private readonly Func<IAreaPlacementTarget?>? _placementTarget;

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

        [ObservableProperty]
        private string _newFolderName = string.Empty;

        public bool IsSearching => !string.IsNullOrWhiteSpace(Query);

        public bool HasCategoryMatches => CategoryMatches.Count > 0;

        public PaletteViewModel(
            WorkspaceContext workspaceContext,
            CategoryService categories,
            OutputLogService log,
            Func<Editors.EditorService>? editorService = null,
            Func<IAreaPlacementTarget?>? placementTarget = null)
        {
            _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
            _categories = categories ?? throw new ArgumentNullException(nameof(categories));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _editorService = editorService;
            _placementTarget = placementTarget;

            Id = "Palette";
            Title = "Palette";

            foreach (var type in OfferedTypes)
                Types.Add(new PaletteTypeChipViewModel(type) { IsSelected = type == SelectedType });

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

        // ----- folder editing -----

        [RelayCommand]
        private void NewFolder()
        {
            var section = _categories.Section(SelectedType);
            if (section == null)
                return;

            var name = string.IsNullOrWhiteSpace(NewFolderName) ? "New category" : NewFolderName.Trim();

            // A new folder goes inside the selection when there is one, which is how a builder builds
            // depth without a separate "nest this" gesture.
            if (SelectedRow?.Folder is { } parent)
                parent.AddChild(name);
            else
                section.AddFolder(name);

            NewFolderName = string.Empty;
            _categories.SaveChanges();
            StatusMessage = $"Added category '{name}'.";
            _log.AppendLine($"Added category '{name}' to the {SelectedType.DisplayName().ToLowerInvariant()} palette.");
        }

        [RelayCommand]
        private void RenameFolder()
        {
            if (SelectedRow?.Folder is not { } folder)
            {
                StatusMessage = "Select a category to rename.";
                return;
            }

            if (string.IsNullOrWhiteSpace(NewFolderName))
            {
                StatusMessage = "Type the new name first.";
                return;
            }

            var previous = folder.Name;
            folder.Rename(NewFolderName.Trim());
            NewFolderName = string.Empty;
            _categories.SaveChanges();
            Refresh();
            StatusMessage = $"Renamed '{previous}' to '{folder.Name}'.";
        }

        /// <summary>
        /// Removes an empty category. Deleting a full one is refused rather than confirmed, because the
        /// members would be silently unfiled and land in Unsorted with no way back.
        /// </summary>
        [RelayCommand]
        private void RemoveFolder()
        {
            var section = _categories.Section(SelectedType);
            if (section == null || SelectedRow?.Folder is not { } folder)
            {
                StatusMessage = "Select a category to remove.";
                return;
            }

            if (folder.MembersIncludingDescendants.Any())
            {
                StatusMessage = "Only an empty category can be removed.";
                return;
            }

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
                Tiles.Add(new PaletteTileViewModel(resRef, NameFor(resRef), null));
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
                Tiles.Add(new PaletteTileViewModel(resRef, NameFor(resRef), folder?.Name));
            }

            Breadcrumb = matches.Count >= MaxSearchResults
                ? $"First {MaxSearchResults} of many matches"
                : $"{matches.Count} match{(matches.Count == 1 ? string.Empty : "es")} across all categories";
        }

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
