using NwnResRef = Nwn.Formats.Resources.ResourceReferenceRules;
using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using PalettePresentationState = Nwn.Toolset.Avalonia.Palettes.PalettePresentationState;
using IPaletteActions = Nwn.Toolset.Avalonia.Palettes.IPaletteActions;
using PaletteSnapshot = Nwn.Toolset.Avalonia.Palettes.PaletteSnapshot;
using PaletteTypeOption = Nwn.Toolset.Avalonia.Palettes.PaletteTypeOption;
using PaletteCategorySnapshot = Nwn.Toolset.Avalonia.Palettes.PaletteCategorySnapshot;
using PaletteEntrySnapshot = Nwn.Toolset.Avalonia.Palettes.PaletteEntrySnapshot;
using PaletteCategoryId = Nwn.Toolset.Avalonia.Palettes.PaletteCategoryId;
using PaletteEntryId = Nwn.Toolset.Avalonia.Palettes.PaletteEntryId;
using PaletteCategoryCapabilities = Nwn.Toolset.Avalonia.Palettes.PaletteCategoryCapabilities;
using PaletteEntryCapabilities = Nwn.Toolset.Avalonia.Palettes.PaletteEntryCapabilities;
using PaletteCapabilities = Nwn.Toolset.Avalonia.Palettes.PaletteCapabilities;
using PaletteEntryKind = Nwn.Toolset.Avalonia.Palettes.PaletteEntryKind;
using ModuleResourceType = Nwn.Authoring.Resources.ModuleResourceType;
using SharedPaletteSource = Nwn.Toolset.Avalonia.Palettes.PaletteSource;
using SharedPaletteMode = Nwn.Toolset.Avalonia.Palettes.PaletteMode;
using SharedTilePaintMode = Nwn.Toolset.Avalonia.Palettes.PaletteTilePaintMode;
using System.Security.Cryptography;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.Categories;
using Nwn.Authoring.Categories;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Tilesets;
using Nwn.Formats.Tilesets;
using Nwn.Authoring.Areas.Tiles;
using SWLOR.Toolset.Domain.Documents;
using Nwn.Authoring.Documents.Native;
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
    /// <para>Counts sit on categories, never on types.</para>
    /// </remarks>
    public partial class PaletteViewModel : Tool, IPaletteActions
    {
        /// <summary>
        /// Types offered, in Aurora's palette order - see <see cref="ResourceTypeExtensions.PaletteOrder"/>,
        /// which owns the order so it can be pinned by a test.
        /// </summary>
        private static IReadOnlyList<ResourceType> OfferedTypes => ResourceTypeExtensions.PaletteOrder;


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

        private IReadOnlySet<string> _existing = new HashSet<string>();
        private long _presentationRevision;
        private readonly Dictionary<PaletteCategoryId, CategoryFolder?> _presentationCategoryTargets = new();
        private readonly Dictionary<PaletteCategoryId, TilePaletteCategory> _presentationTileCategories = new();
        private readonly Dictionary<PaletteEntryId, TilePaletteEntry> _presentationTileEntries = new();
        private readonly Dictionary<PaletteEntryId, PaletteEntrySnapshot> _presentationEntries = new();
        private CategoryFolder? _selectedFolder;
        private PaletteCategoryId? _selectedCategoryId;

        public PalettePresentationState PresentationState { get; }

        public ObservableCollection<PaletteTypeChipViewModel> Types { get; } = new();



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
        private string? _statusMessage;

        partial void OnStatusMessageChanged(string? value)
        {
            if (PresentationState is not null)
                PublishPresentationSnapshot();
        }

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

            _selectedFolder = null;
            _selectedCategoryId = null;
            _selectedTileCategory = null;
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
            PublishPresentationSnapshot();
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
            PresentationState = new PalettePresentationState(this);

            _categories.Changed += Refresh;
        }

        /// <summary>
        /// The category tree currently in play. Null only when the module has no section for this type;
        /// the standard side always returns a section, empty when the base game is unavailable.
        /// </summary>
        private CategorySection? CurrentSection() =>
            IsCustomSource ? _categories.Section(SelectedType) : _categories.StandardSection(SelectedType);

        private bool TryGetCurrentEntry(
            PaletteEntrySnapshot entry,
            out PaletteEntrySnapshot current)
        {
            if (!_presentationEntries.TryGetValue(entry.Id, out current!) ||
                !ReferenceEquals(current, entry))
            {
                return false;
            }

            if (current.Kind == PaletteEntryKind.Tile)
                return IsTileMode && current.ResourceType == null &&
                       current.Source == SharedPaletteSource.Custom;

            var source = IsStandardSource ? SharedPaletteSource.Standard : SharedPaletteSource.Custom;
            return !IsTileMode && current.ResourceType == SelectedType && current.Source == source;
        }

        private void PublishPresentationSnapshot()
        {
            _presentationCategoryTargets.Clear();
            _presentationTileCategories.Clear();
            _presentationTileEntries.Clear();
            _presentationEntries.Clear();

            var typeOptions = Types.Select(chip =>
            {
                ModuleResourceType? resourceType = chip.Type;
                var newBlueprintLabel = chip.Type is { } type
                    ? $"New {type.SingularDisplayName()}..."
                    : "New";
                return new PaletteTypeOption(resourceType, chip.Label, chip.Initial, newBlueprintLabel, chip.Icon);
            }).ToArray();

            var categories = new List<PaletteCategorySnapshot>();
            var entries = new List<PaletteEntrySnapshot>();
            PaletteCategoryId? initialSelectedCategory = null;

            if (IsTileMode)
            {
                var offered = TilePaintModes.CategoriesFor(_tiles, TilePaintMode);

                for (var categoryIndex = 0; categoryIndex < offered.Count; categoryIndex++)
                {
                    var category = offered[categoryIndex];
                    var categoryId = new PaletteCategoryId($"tiles/{categoryIndex}/{category.Name}");
                    var tileIds = new List<PaletteEntryId>();
                    _presentationTileCategories[categoryId] = category;

                    for (var entryIndex = 0; entryIndex < category.Entries.Count; entryIndex++)
                    {
                        var tile = category.Entries[entryIndex];
                        var entryId = new PaletteEntryId(
                            $"tile/{categoryIndex}/{entryIndex}/{tile.PreviewModelResRef}");
                        tileIds.Add(entryId);
                        _presentationTileEntries[entryId] = tile;
                        var tileSnapshot = new PaletteEntrySnapshot(
                            entryId,
                            PaletteEntryKind.Tile,
                            null,
                            SharedPaletteSource.Custom,
                            tile.PreviewModelResRef,
                            tile.Label,
                            string.Empty,
                            new[] { categoryId },
                            tile.Columns,
                            tile.Rows,
                            tile.FootprintModelResRefs ?? Array.Empty<string>(),
                            new PaletteEntryCapabilities(
                                !NeedsOpenArea,
                                false,
                                false,
                                false,
                                "Tileset content - read-only"),
                            SupportsPreview: tile.Crosser is not { Length: 0 });
                        _presentationEntries[entryId] = tileSnapshot;
                        entries.Add(tileSnapshot);
                    }

                    categories.Add(new PaletteCategorySnapshot(
                        categoryId,
                        category.Name,
                        category.Entries.Count,
                        false,
                        categoryIndex,
                        Array.Empty<PaletteCategorySnapshot>(),
                        tileIds,
                        new PaletteCategoryCapabilities(false, false, false, false, false, false,
                            "Tileset content - read-only")));
                }

                initialSelectedCategory = _selectedTileCategory is null
                    ? categories.FirstOrDefault()?.Id
                    : categories.FirstOrDefault(category =>
                        _presentationTileCategories.TryGetValue(category.Id, out var tileCategory) &&
                        ReferenceEquals(tileCategory, _selectedTileCategory))?.Id;
            }
            else if (CurrentSection() is { } section)
            {
                foreach (var folder in section.Folders)
                {
                    categories.Add(BuildFolderSnapshot(folder, section));
                }

                var unsortedId = UnsortedCategoryId();
                var unsortedEntryIds = section.UnsortedResRefs(_existing)
                    .Select(resRef => BlueprintEntryId(resRef))
                    .ToArray();
                categories.Add(new PaletteCategorySnapshot(
                    unsortedId,
                    CategorySection.UnsortedFolderName,
                    unsortedEntryIds.Length,
                    false,
                    int.MaxValue,
                    Array.Empty<PaletteCategorySnapshot>(),
                    unsortedEntryIds,
                    CreateCategoryCapabilities(isFolder: false)));
                _presentationCategoryTargets[unsortedId] = null;

                foreach (var resRef in _existing)
                {
                    var entry = CreateBlueprintEntry(resRef, section);
                    entries.Add(entry);
                    _presentationEntries[entry.Id] = entry;
                }

                if (_selectedFolder is { } selectedFolder)
                {
                    initialSelectedCategory = FolderCategoryId(section, selectedFolder);
                }
                else if (_selectedCategoryId is { } selectedCategoryId &&
                         _presentationCategoryTargets.ContainsKey(selectedCategoryId))
                {
                    initialSelectedCategory = selectedCategoryId;
                }
            }

            var snapshot = new PaletteSnapshot(
                Interlocked.Increment(ref _presentationRevision),
                IsTileMode ? SharedPaletteMode.Tiles : SharedPaletteMode.Blueprints,
                IsTileMode ? null : SelectedType,
                IsStandardSource ? SharedPaletteSource.Standard : SharedPaletteSource.Custom,
                typeOptions,
                categories,
                entries,
                TilePaintMode == TilePaintMode.Auto ? SharedTilePaintMode.Auto : SharedTilePaintMode.Manual,
                !NeedsOpenArea,
                string.IsNullOrWhiteSpace(StatusMessage) ? null : StatusMessage,
                new PaletteCapabilities(true, true, true, true),
                TileSize,
                CategoryProportion,
                initialSelectedCategory);
            PresentationState.SetSnapshot(snapshot);
        }

        private PaletteCategorySnapshot BuildFolderSnapshot(CategoryFolder folder, CategorySection section)
        {
            var categoryId = FolderCategoryId(section, folder);
            var memberIds = folder.Members
                .Where(_existing.Contains)
                .Select(BlueprintEntryId)
                .ToArray();
            var children = folder.Children
                .Select(child => BuildFolderSnapshot(child, section))
                .ToArray();
            var pathKey = section.PathKey(folder);
            var pinOrder = section.Pinned.ToList().FindIndex(
                pin => string.Equals(pin, pathKey, StringComparison.OrdinalIgnoreCase));
            var isPinned = pinOrder >= 0;

            _presentationCategoryTargets[categoryId] = folder;
            return new PaletteCategorySnapshot(
                categoryId,
                folder.Name,
                section.CountIn(folder, _existing),
                isPinned,
                isPinned ? pinOrder : int.MaxValue,
                children,
                memberIds,
                CreateCategoryCapabilities(isFolder: true));
        }

        private PaletteCategoryCapabilities CreateCategoryCapabilities(bool isFolder)
        {
            return new PaletteCategoryCapabilities(
                CanCreateBlueprint,
                CanWrite,
                isFolder && CanWrite,
                isFolder && CanWrite,
                isFolder && CanWrite,
                isFolder && CanWrite,
                ReadOnlyNotice);
        }

        private PaletteEntrySnapshot CreateBlueprintEntry(string resRef, CategorySection section)
        {
            var categoryIds = section.FoldersContaining(resRef)
                .Select(folder => FolderCategoryId(section, folder))
                .ToArray();
            if (categoryIds.Length == 0)
            {
                categoryIds = new[] { UnsortedCategoryId() };
            }

            var source = IsStandardSource ? SharedPaletteSource.Standard : SharedPaletteSource.Custom;
            return new PaletteEntrySnapshot(
                BlueprintEntryId(resRef),
                PaletteEntryKind.Blueprint,
                SelectedType,
                source,
                resRef,
                NameFor(resRef),
                resRef,
                categoryIds,
                null,
                null,
                Array.Empty<string>(),
                new PaletteEntryCapabilities(
                    _placementTarget?.Invoke() is not null,
                    CanWrite,
                    CanEditCopy,
                    CanWrite && _prompts is not null,
                    IsStandardSource ? ReadOnlyNotice : null));
        }

        private PaletteCategoryId FolderCategoryId(CategorySection section, CategoryFolder folder) =>
            new($"{SelectedType.Extension()}/{Source}/{section.PathKey(folder)}");

        private PaletteCategoryId UnsortedCategoryId() => new($"{SelectedType.Extension()}/{Source}/unsorted");

        private PaletteEntryId BlueprintEntryId(string resRef) =>
            new($"{SelectedType.Extension()}/{Source}/{resRef.ToLowerInvariant()}");

        public void SelectType(PaletteTypeOption type)
        {
            var chip = Types.FirstOrDefault(candidate =>
                candidate.IsTiles == type.IsTiles && (candidate.IsTiles || candidate.Type == type.Type!.Value));
            if (chip is not null)
            {
                SelectTypeCommand.Execute(chip);
            }
        }

        public void SelectSource(SharedPaletteSource source)
        {
            Source = source == SharedPaletteSource.Standard ? PaletteSource.Standard : PaletteSource.Custom;
        }

        public void SelectMode(SharedPaletteMode mode)
        {
            IsTileMode = mode == SharedPaletteMode.Tiles;
        }

        public void SelectTilePaintMode(SharedTilePaintMode mode)
        {
            TilePaintMode = mode == SharedTilePaintMode.Auto ? TilePaintMode.Auto : TilePaintMode.Manual;
        }

        public void SelectCategory(PaletteCategoryId? categoryId)
        {
            _selectedCategoryId = categoryId;
            _selectedFolder = categoryId is { } id && _presentationCategoryTargets.TryGetValue(id, out var folder)
                ? folder
                : null;
            _selectedTileCategory = categoryId is { } tileId &&
                                    _presentationTileCategories.TryGetValue(tileId, out var tileCategory)
                ? tileCategory
                : null;
        }

        public void SetTileSize(double tileSize) => TileSize = tileSize;

        public void SetCategoryProportion(double categoryProportion) => CategoryProportion = categoryProportion;

        public void Place(PaletteEntrySnapshot entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            if (!TryGetCurrentEntry(entry, out var current) || !current.Capabilities.CanPlace)
                return;

            var target = _placementTarget?.Invoke();
            if (target is null)
            {
                StatusMessage = "Open an area first, then place into it.";
                PublishPresentationSnapshot();
                return;
            }

            if (current.Kind == PaletteEntryKind.Tile)
            {
                if (_presentationTileEntries.TryGetValue(current.Id, out var tile))
                {
                    StatusMessage = target.ArmTilePlacement(tile)
                        ? $"Click a cell to place {tile.Label}."
                        : "This area has no tile grid to paint.";
                }
            }
            else if (target.ArmPlacement(SelectedType, current.ResRef,
                         current.Source == SharedPaletteSource.Standard ? PaletteSource.Standard : PaletteSource.Custom))
            {
                StatusMessage = $"Click the map to place {current.Name}.";
            }
            else
            {
                StatusMessage = $"{SelectedType.DisplayName()} cannot be placed in this area.";
            }

            PublishPresentationSnapshot();
        }

        public void Edit(PaletteEntrySnapshot entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            if (TryGetCurrentEntry(entry, out var current) &&
                current.Kind == PaletteEntryKind.Blueprint &&
                current.Capabilities.CanEdit)
            {
                _editorService?.Invoke().TryOpenEditor(SelectedType, current.ResRef);
            }
        }

        public void EditCopy(PaletteEntrySnapshot entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            if (!TryGetCurrentEntry(entry, out var current) ||
                current.Kind != PaletteEntryKind.Blueprint ||
                !current.Capabilities.CanEditCopy ||
                !CanEditCopy)
            {
                return;
            }

            EditCopyEntry(current);
        }

        public Task DeleteAsync(PaletteEntrySnapshot entry, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(entry);
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryGetCurrentEntry(entry, out var current) ||
                current.Kind != PaletteEntryKind.Blueprint ||
                !current.Capabilities.CanDelete ||
                !CanWrite)
            {
                return Task.CompletedTask;
            }

            var resourceType = SelectedType;
            var source = Source;
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return Task.CompletedTask;

            return DeleteEntryAsync(current, resourceType, source, workspace);
        }

        public void NewBlueprint(PaletteCategoryId? categoryId)
        {
            SelectCategory(categoryId);
            NewBlueprintCommand.Execute(null);
        }

        public async Task NewCategoryAsync(PaletteCategoryId? categoryId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SelectCategory(categoryId);
            await NewCategoryAsync().ConfigureAwait(true);
        }

        public async Task RenameCategoryAsync(PaletteCategoryId categoryId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SelectCategory(categoryId);
            await RenameCategoryAsync().ConfigureAwait(true);
        }

        public async Task DeleteCategoryAsync(PaletteCategoryId categoryId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SelectCategory(categoryId);
            await DeleteCategoryAsync().ConfigureAwait(true);
        }

        public Task TogglePinAsync(PaletteCategoryId categoryId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SelectCategory(categoryId);
            TogglePin();
            return Task.CompletedTask;
        }

        public void FileSelectedEntry(PaletteCategoryId categoryId)
        {
            SelectCategory(categoryId);
            if (!CanWrite)
                return;

            if (PresentationState.SelectedTile?.Snapshot is not { Kind: PaletteEntryKind.Blueprint } entry)
            {
                StatusMessage = "Select a blueprint first.";
                PublishPresentationSnapshot();
                return;
            }

            if (_selectedFolder is not { } folder)
            {
                StatusMessage = "Select the category to file it into.";
                PublishPresentationSnapshot();
                return;
            }

            var section = _categories.Section(SelectedType);
            if (section is null)
                return;

            var resRef = entry.ResRef;
            var label = entry.Name;
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
            PublishPresentationSnapshot();
        }

        public ValueTask<Bitmap?> LoadPreviewAsync(PaletteEntrySnapshot entry, CancellationToken cancellationToken)
        {
            if (_thumbnails is null || !_thumbnails.IsAvailable)
            {
                return ValueTask.FromResult<Bitmap?>(null);
            }

            var completion = new TaskCompletionSource<Bitmap?>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (entry.Kind == PaletteEntryKind.Tile && _presentationTileEntries.TryGetValue(entry.Id, out var tile))
            {
                _thumbnails.RequestTileAsync(
                    tile.PreviewModelResRef,
                    bitmap => completion.TrySetResult(bitmap),
                    tile.FootprintModelResRefs,
                    tile.Columns,
                    tile.Rows,
                    onFailed: () => completion.TrySetResult(null));
            }
            else if (entry.ResourceType is { } resourceType)
            {
                _thumbnails.RequestAsync(resourceType, entry.ResRef,
                    entry.Source == SharedPaletteSource.Standard,
                    bitmap => completion.TrySetResult(bitmap));
            }
            else
            {
                return ValueTask.FromResult<Bitmap?>(null);
            }

            return new ValueTask<Bitmap?>(completion.Task.WaitAsync(cancellationToken));
        }


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
            PublishPresentationSnapshot();
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
            _presentationCategoryTargets.Clear();
            _presentationTileCategories.Clear();
            _presentationTileEntries.Clear();
            _selectedFolder = null;
            _selectedCategoryId = null;
            _selectedTileCategory = null;
            _tiles = TilePalette.Empty;

            var tilesetResRef = _placementTarget?.Invoke()?.TilesetResRef;
            NeedsOpenArea = string.IsNullOrWhiteSpace(tilesetResRef);
            if (NeedsOpenArea)
            {
                StatusMessage = string.Empty;
                PublishPresentationSnapshot();
                return;
            }

            var activeTilesetResRef = tilesetResRef!;
            if (_tilesets == null || !_tilesets.TryGetTileset(activeTilesetResRef, out var tileset) || tileset == null)
            {
                StatusMessage = $"Tileset '{tilesetResRef}' could not be loaded.";
                PublishPresentationSnapshot();
                return;
            }

            _tiles = TilePaletteBuilder.Build(tileset, _resolveStrRef, _log.AppendLine);
            if (_tiles.IsEmpty)
            {
                StatusMessage = $"Tileset '{tilesetResRef}' lists no tiles.";
                PublishPresentationSnapshot();
                return;
            }

            var offered = TilePaintModes.CategoriesFor(_tiles, TilePaintMode);
            if (offered.Count == 0)
            {
                StatusMessage = IsAutoTilePaint
                    ? $"'{_tilesets.GetDisplayName(activeTilesetResRef)}' declares no terrain to paint - switch to Manual."
                    : $"'{_tilesets.GetDisplayName(activeTilesetResRef)}' lists no individual tiles.";
                PublishPresentationSnapshot();
                return;
            }

            _selectedTileCategory = offered[0];
            StatusMessage = IsAutoTilePaint
                ? $"{_tilesets.GetDisplayName(activeTilesetResRef)} - pick a terrain, then click a cell to paint it."
                : $"{_tilesets.GetDisplayName(activeTilesetResRef)} - pick a tile, then click a cell to stamp it.";
            PublishPresentationSnapshot();
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

            _selectedFolder = null;
            _selectedCategoryId = null;

            if (_settings != null && !IsTileMode)
                _settings.PaletteSelection = value.Extension();

            SyncChipSelection();
            OnPropertyChanged(nameof(NewBlueprintLabel));
            OnPropertyChanged(nameof(CanCreateBlueprint));
            _selectedFolder = null;
            _selectedCategoryId = null;
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
            _selectedFolder = null;
            _selectedCategoryId = null;
            _selectedTileCategory = null;
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

        /// <summary>
        /// Creates an independent custom blueprint from the selected blueprint and opens that copy for
        /// editing. The source and all instances placed from it remain untouched.
        /// </summary>
        private void EditCopyEntry(PaletteEntrySnapshot entry)
        {
            if (entry.Kind != PaletteEntryKind.Blueprint || !CanEditCopy)
                return;

            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return;

            var isStandard = entry.Source == SharedPaletteSource.Standard;
            var sourceSection = isStandard
                ? _categories.StandardSection(SelectedType)
                : _categories.Section(SelectedType);
            var sourceFolder = SourceFolderForCopy(entry, sourceSection);
            var sourcePath = sourceFolder == null || sourceSection == null
                ? Array.Empty<string>()
                : sourceSection.PathTo(sourceFolder).ToArray();

            string copyResRef;
            string copyPath;
            try
            {
                copyResRef = BlueprintCopyFactory.NextResRef(
                    workspace,
                    SelectedType,
                    entry.ResRef);
                copyPath = workspace.GetResourcePath(SelectedType, copyResRef);

                var source = isStandard
                    ? workspace.LoadIndexedBlueprint(SelectedType, entry.ResRef)
                    : workspace.LoadBlueprint(SelectedType, entry.ResRef);
                var content = BlueprintCopyFactory.CreateFileContent(
                    SelectedType,
                    source.Document,
                    copyResRef);

                Directory.CreateDirectory(Path.GetDirectoryName(copyPath)!);
                SwlorFileWriteAccess.Writer.WriteNewAtomic(copyPath, content);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Could not copy {entry.Name}: {ex.Message}";
                _log.AppendLine(
                    $"Edit Copy failed for {SelectedType.Extension()} blueprint '{entry.ResRef}': {ex.Message}");
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
                StatusMessage = $"Copied {entry.Name} as {copyResRef}.";
                _log.AppendLine(
                    $"Copied {SelectedType.Extension()} blueprint '{entry.ResRef}' to '{copyResRef}' ({copyPath}).");
            }
            else
            {
                var category = sourcePath.Length == 0 ? "its source category" : sourcePath[^1];
                StatusMessage =
                    $"Copied {entry.Name} as {copyResRef}, but it could not be filed under '{category}' - " +
                    $"it is in Unsorted. {StatusMessage}";
                _log.AppendLine(
                    $"Copied {SelectedType.Extension()} blueprint '{entry.ResRef}' to '{copyResRef}', " +
                    $"but could not file the copy under '{category}'.");
            }

            _editorService?.Invoke().TryOpenEditor(SelectedType, copyResRef);
        }

        private CategoryFolder? SourceFolderForCopy(
            PaletteEntrySnapshot entry,
            CategorySection? sourceSection)
        {
            if (sourceSection == null)
                return null;

            var containing = sourceSection.FoldersContaining(entry.ResRef).ToList();
            if (containing.Count == 0)
                return null;

            // A parent row includes all descendants. Prefer the leaf below the row whose tile menu was
            // used; search has no category context, so its stable first filing is the best answer.
            if (!PresentationState.IsSearching && _selectedFolder is { } selectedFolder)
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
            _selectedFolder = targetPathKey == null ? null : section?.FindByPathKey(targetPathKey);
            _selectedCategoryId = _selectedFolder is { } folder
                ? FolderCategoryId(section!, folder)
                : UnsortedCategoryId();
            Refresh();

            PresentationState.SelectedRow = PresentationState.Rows.FirstOrDefault(
                row => row.Id == _selectedCategoryId);
            var copyEntryId = BlueprintEntryId(copyResRef);
            PresentationState.SelectedTile = PresentationState.Tiles.FirstOrDefault(
                tile => tile.Id == copyEntryId);
        }


        /// <summary>
        /// Deletes the blueprint's file from the module.
        /// </summary>
        /// <remarks>
        /// The confirmation names the file and says what it cannot undo, because this is the one palette
        /// action that destroys something outside the toolset's own sidecar: areas that placed this
        /// blueprint keep their instances, and those instances will no longer resolve.
        /// </remarks>
        private async Task DeleteEntryAsync(
            PaletteEntrySnapshot entry,
            ResourceType resourceType,
            PaletteSource source,
            ModuleWorkspace workspace)
        {
            if (entry.Kind != PaletteEntryKind.Blueprint || _prompts == null)
                return;

            var path = workspace.GetResourcePath(resourceType, entry.ResRef);
            var kind = resourceType.SingularDisplayName().ToLowerInvariant();

            // Refused rather than handled: an open editor holds a session on this file, and once the
            // file is gone that editor's next save either recreates the blueprint (Overwrite) or fails
            // outright (Reload). Closing it first is the builder's call, not something to do silently.
            if (_editorService?.Invoke().IsOpen(resourceType, entry.ResRef) == true)
            {
                StatusMessage = $"'{entry.Name}' is open in an editor - close that tab first.";
                return;
            }

            // Asked before the delete, not after: removing the file is irreversible from here, and a
            // sidecar that cannot be written would leave the category pointing at a resource that no
            // longer exists with nothing the builder can do about it.
            if (_categories.Section(resourceType)?.FoldersContaining(entry.ResRef).Any() == true)
            {
                var preflight = _categories.CanSaveChanges();
                if (!preflight.Saved)
                {
                    StatusMessage = $"'{entry.Name}' was not deleted: {preflight.Problem}";
                    _log.AppendLine($"Deleting blueprint '{entry.ResRef}' was refused: {preflight.Problem}");
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
                StatusMessage = $"'{entry.Name}' was not deleted: could not fingerprint its blueprint ({ex.Message}).";
                _log.AppendLine($"Deleting blueprint '{entry.ResRef}' was refused: {ex.Message}");
                return;
            }

            var confirmed = await _prompts.ConfirmDestructiveAsync(
                $"Delete the {kind} '{entry.Name}'?",
                $"This deletes {Path.GetFileName(path)} from the module. Any area that already placed " +
                "it keeps its instances, and those will no longer resolve. This cannot be undone from " +
                "the toolset.",
                "Delete").ConfigureAwait(true);

            if (!confirmed)
                return;

            if (!ReferenceEquals(_workspaceContext.Workspace, workspace) ||
                SelectedType != resourceType || Source != source || IsTileMode)
            {
                return;
            }

            // Rechecked here, not just at the CanWrite gate that greys the menu item: a pack,
            // validation, or Build All can start while the confirmation dialog is on screen, and this
            // delete - unlike blueprint creation, which always goes through
            // SaveService.WriteNewAtomic - had no guarded write path of its own to catch that.
            if (_mutationLock?.IsLocked == true)
            {
                StatusMessage = $"'{entry.Name}' was not deleted: the module is being packed, validated, or built.";
                _log.AppendLine($"Deleting blueprint '{entry.ResRef}' was refused: the module is locked.");
                return;
            }

            // The sidecar preflight above ran BEFORE the confirmation dialog, and the sidecar can
            // change externally while that dialog sits open. Discovering the conflict only at the
            // SaveCategories below would be too late - the blueprint would already be gone while
            // the (externally updated) sidecar still lists it - so the last check runs here,
            // immediately before the irreversible delete.
            if (_categories.Section(resourceType)?.FoldersContaining(entry.ResRef).Any() == true)
            {
                var recheck = _categories.CanSaveChanges();
                if (!recheck.Saved)
                {
                    StatusMessage = $"'{entry.Name}' was not deleted: {recheck.Problem}";
                    _log.AppendLine($"Deleting blueprint '{entry.ResRef}' was refused: {recheck.Problem}");
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
                StatusMessage = $"'{entry.Name}' was not deleted: {ex.Message}";
                _log.AppendLine($"Deleting blueprint '{entry.ResRef}' failed: {ex.Message}");
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
                StatusMessage = $"'{entry.Name}' was not deleted: {ex.Message}";
                _log.AppendLine($"Deleting blueprint '{entry.ResRef}' failed: {ex.Message}");
                return;
            }

            // Out of the catalog, or Explorer and Search keep listing a resource whose file is gone and
            // opening that row fails against the missing file.
            _workspaceContext.RemoveCatalogEntry(resourceType, entry.ResRef);

            // Drop it from the sidecar too, or the category keeps a member that resolves to nothing.
            // Preflighted above, so a failure here means the sidecar changed underneath us while the
            // confirmation was on screen - rare, and still worth saying out loud.
            var unfiled = true;
            if (_categories.Section(resourceType) is { } section)
            {
                foreach (var folder in section.FoldersContaining(entry.ResRef).ToList())
                    folder.RemoveMember(entry.ResRef);

                unfiled = SaveCategories();
                if (!unfiled)
                {
                    StatusMessage =
                        $"Deleted {entry.Name}, but its category still lists it. {StatusMessage}";
                    _log.AppendLine(
                        $"Deleted blueprint '{entry.ResRef}' but its category entry could not be removed.");
                }
            }

            Refresh();
            if (unfiled)
            {
                StatusMessage = $"Deleted {entry.Name}.";
                _log.AppendLine($"Deleted blueprint '{entry.ResRef}' ({path}).");
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
                $"The ResRef is derived from this name: lowercase, no spaces, " +
                $"{NwnResRef.MaxLength} characters at most - " +
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
                SwlorFileWriteAccess.Writer.WriteNewAtomic(
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
            if (_selectedFolder is { } folder)
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

                if (builder.Length == NwnResRef.MaxLength)
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

            var parent = _selectedFolder;
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
            if (_selectedFolder is not { } folder || _prompts == null)
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
                _selectedFolder = folder;
                _selectedCategoryId = FolderCategoryId(CurrentSection()!, folder);
                return;
            }

            Refresh();
            _selectedFolder = folder;
            _selectedCategoryId = FolderCategoryId(CurrentSection()!, folder);
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
            if (section == null || _selectedFolder is not { } folder || _prompts == null)
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
            _selectedFolder = null;
            _selectedCategoryId = null;
            if (!SaveCategories())
            {
                Refresh();
                return;
            }

            Refresh();
            StatusMessage = $"Removed category '{folder.Name}'.";
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
            if (section == null || _selectedFolder is not { } folder)
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
            // The selected folder remains the identity source when the next snapshot is published.
            _selectedFolder = folder;
            _selectedCategoryId = FolderCategoryId(CurrentSection()!, folder);
        }

        private void OnThumbnailInvalidated(ResourceType type, string resRef)
        {
            if (IsTileMode || type != SelectedType)
                return;

            PresentationState.InvalidatePreview(BlueprintEntryId(resRef));
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
        /// Backed by a per-type dictionary so publishing a snapshot can resolve every blueprint name
        /// without scanning the catalog once per resource. The dictionary is rebuilt only when the
        /// catalog publishes a new snapshot or the type changes.
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
