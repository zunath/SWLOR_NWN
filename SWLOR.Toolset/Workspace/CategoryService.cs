using SWLOR.Toolset.Domain.Categories;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Workspace
{
    /// <summary>
    /// Owns the open module's category sidecar: loads it, seeds it from the module's existing
    /// <c>.itp</c> palettes the first time a type is used, and saves it.
    /// </summary>
    /// <remarks>
    /// The seed matters more than it sounds. This module's palettes already file roughly 17,000
    /// blueprints across ~345 categories - 190 categories over 8,344 placeables alone - so starting from
    /// an empty tree would throw away work somebody did. The <c>.itp</c> files are read for that seed and
    /// never written, because the game and Aurora rewrite them.
    /// </remarks>
    public sealed class CategoryService
    {
        private readonly WorkspaceContext _workspaceContext;
        private readonly OutputLogService _log;

        /// <summary>
        /// Resolves the TLK-referenced category names the base-game palettes use. Without it every
        /// imported category reads "Category 16810847" - legible and renameable, but useless.
        /// </summary>
        private readonly TlkService? _tlk;

        /// <summary>Supplies the base game's own palettes and blueprints for the Standard group.</summary>
        private readonly ResourceIndex? _resourceIndex;

        private readonly HashSet<ResourceType> _seeded = new();

        /// <summary>
        /// The standard palettes, cached per type. Held here rather than in <see cref="Catalog"/> on
        /// purpose: <see cref="SaveChanges"/> writes the catalog, and base-game content must never end up
        /// in the sidecar. Not keyed by module either, because what the base game ships does not change
        /// when a different module is opened.
        /// </summary>
        private readonly Dictionary<ResourceType, StandardPalette> _standardPalettes = new();

        private CategoryCatalog? _catalog;
        private CategoryCatalog? _persistedCatalog;
        private string? _loadedForModuleRoot;

        public CategoryService(
            WorkspaceContext workspaceContext,
            OutputLogService log,
            TlkService? tlk = null,
            ResourceIndex? resourceIndex = null)
        {
            _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _tlk = tlk;
            _resourceIndex = resourceIndex;
        }

        /// <summary>Raised after categories change, so open views can re-read the tree.</summary>
        public event Action? Changed;

        /// <summary>
        /// The catalog for the open module, loaded on first use and reloaded if the module changes.
        /// Null only when no module is open.
        /// </summary>
        public CategoryCatalog? Catalog
        {
            get
            {
                var moduleRoot = _workspaceContext.Workspace?.ModuleRoot;
                if (moduleRoot == null)
                    return null;

                if (_catalog != null && _loadedForModuleRoot == moduleRoot)
                    return _catalog;

                var path = CategoryCatalog.DefaultPathFor(moduleRoot);
                _catalog = CategoryCatalog.Load(path, out var warning);
                _persistedCatalog = _catalog.DeepClone();
                _loadedForModuleRoot = moduleRoot;
                _sidecarStateKnown = true;
                _sidecarExistedWhenLoaded = File.Exists(path);
                _sidecarWrittenUtc = LastWriteUtc(path);
                _seeded.Clear();

                if (warning != null)
                    _log.AppendLine(warning);

                return _catalog;
            }
        }

        /// <summary>
        /// The section for a type, seeded from the matching <c>.itp</c> palette the first time it is
        /// asked for and still empty. Returns null when no module is open.
        /// </summary>
        public CategorySection? Section(ResourceType type)
        {
            var catalog = Catalog;
            if (catalog == null)
                return null;

            var section = catalog.Section(type);

            // IsSeeded, not "has folders". A builder who deliberately empties a section and restarts was
            // otherwise handed the imported hierarchy straight back, with no way to keep it empty.
            if (_seeded.Contains(type) || section.IsSeeded)
            {
                _seeded.Add(type);
                RepairPlaceholderNames(section);
                return _catalog?.Section(type) ?? section;
            }

            var importedFolders = ReadPaletteSeed(type);
            if (importedFolders == null)
                return section;

            foreach (var folder in importedFolders)
                section.AddFolder(folder);

            section.IsSeeded = true;
            var save = SaveChanges();
            if (!save.Saved)
            {
                foreach (var folder in importedFolders)
                    section.RemoveFolder(folder);
                section.IsSeeded = false;
                return section;
            }

            _seeded.Add(type);
            return section;
        }

        /// <summary>
        /// Placeholder folder names left in the sidecar match <c>Category 1234</c> and nothing else a
        /// person would type.
        /// </summary>
        private static readonly System.Text.RegularExpressions.Regex PlaceholderName =
            new(@"^Category (\d+)$", System.Text.RegularExpressions.RegexOptions.Compiled);

        /// <summary>
        /// Renames categories that were imported before their TLK was available.
        /// </summary>
        /// <remarks>
        /// Category names are resolved once, at import, and then persisted - so a module first opened
        /// without the base game's dialog.tlk has "Category 6782" written into its sidecar permanently,
        /// and simply supplying the TLK later fixes nothing. Repairing on load is preferable to bumping
        /// the sidecar version and re-seeding, which would also discard every category a builder made.
        /// Names that are not placeholders are never touched, so a deliberate "Category 7" survives.
        /// </remarks>
        private void RepairPlaceholderNames(CategorySection section)
        {
            if (_tlk == null)
                return;

            var repaired = 0;
            foreach (var folder in section.AllFolders().ToList())
            {
                var match = PlaceholderName.Match(folder.Name);
                if (!match.Success || !uint.TryParse(match.Groups[1].Value, out var strRef))
                    continue;

                // Sanitized like every other name that comes out of the TLK: several of the base game's
                // category names carry a path separator, and this repair runs over a tree that is already
                // loaded and on screen, so a throw here would take the open module with it.
                var resolved = CategoryFolder.Sanitize(ResolveCategoryName(strRef));
                if (resolved == null)
                    continue;

                // A pin is stored by path, and a path is built from names. Renaming a folder therefore
                // moves every pin at or below it and the stored keys need to move with the folder.
                if (section.TryRenameFolder(folder, resolved))
                    repaired++;
            }

            if (repaired == 0)
                return;

            _log.AppendLine($"Resolved {repaired} category name(s) that were imported before the TLK was available.");
            SaveChanges();
        }

        /// <summary>
        /// The base game's own category tree for a type - the Standard half of the palette, next to the
        /// module's Custom content. Never null and never throws: without a base game, or for a type the
        /// game ships no palette for, this is an empty section.
        /// </summary>
        /// <remarks>
        /// Read-only by construction. This section is not part of <see cref="Catalog"/>, so no amount of
        /// editing it can reach the sidecar <see cref="SaveChanges"/> writes.
        /// </remarks>
        public CategorySection StandardSection(ResourceType type) => StandardPaletteFor(type).Section;

        /// <summary>
        /// Every resref the Standard section can actually offer for a type: what its palette lists,
        /// filtered to what really resolves in the resource index. The counterpart of
        /// <see cref="ExistingResRefs"/> for base-game content.
        /// </summary>
        public IReadOnlySet<string> StandardResRefs(ResourceType type) => StandardPaletteFor(type).ResRefs;

        /// <summary>
        /// Display names for the base game's blueprints, by resref. The module catalog has none - these
        /// blueprints are not in the module - so the palette file is the only source.
        /// </summary>
        public IReadOnlyDictionary<string, string> StandardNames(ResourceType type) =>
            StandardPaletteFor(type).Names;

        /// <summary>Every resref of a type that actually exists in the module, for counts and Unsorted.</summary>
        public IReadOnlySet<string> ExistingResRefs(ResourceType type)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var resRefs = type == ResourceType.Area
                ? workspace.EnumerateAreaResRefs()
                : workspace.EnumerateResRefs(type);

            return resRefs.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Records a change and writes the sidecar, reporting whether the write actually happened.
        /// </summary>
        /// <remarks>
        /// Returns a result rather than swallowing the failure. A locked or unwritable sidecar used to be
        /// logged and nothing else, so the palette reported that a category had been added, renamed or
        /// filed while it existed only in memory - and shutdown had no idea anything was unsaved.
        /// <para>
        /// The file is also re-checked before writing. Nothing watches the sidecar (ModuleFileWatcher
        /// covers the module directory next door), so a git pull while the toolset is open would
        /// otherwise be overwritten by whatever this session happened to be holding.
        /// </para>
        /// </remarks>
        /// <summary>
        /// Whether <see cref="SaveChanges"/> would be refused, without writing anything or discarding
        /// pending edits.
        /// </summary>
        /// <remarks>
        /// For callers whose real work is irreversible. Deleting a blueprint also has to drop it from
        /// the sidecar; if that write is going to be refused, the honest order is to find out first
        /// rather than to delete the file and then discover the category still lists it.
        /// </remarks>
        public CategorySaveResult CanSaveChanges()
        {
            var catalog = Catalog;
            if (catalog == null)
                return CategorySaveResult.Ok();

            if (catalog.IsReadOnly)
                return CategorySaveResult.Failed(
                    catalog.ReadOnlyReason ?? "These categories will not be overwritten.");

            if (HasExternalChange(catalog))
                return CategorySaveResult.Failed(
                    $"'{catalog.FilePath}' changed outside the toolset; the change was not saved. " +
                    "Reopen the module to pick up the external version.");

            return CategorySaveResult.Ok();
        }

        public CategorySaveResult SaveChanges()
        {
            var catalog = Catalog;
            if (catalog == null)
                return CategorySaveResult.Ok();

            var preflight = CanSaveChanges();
            if (!preflight.Saved)
            {
                _log.AppendLine(preflight.Problem!);
                RestorePersistedCatalog();
                return preflight;
            }

            try
            {
                catalog.MarkDirty();
                catalog.Save();
                _sidecarStateKnown = true;
                _sidecarExistedWhenLoaded = true;
                _sidecarWrittenUtc = LastWriteUtc(catalog.FilePath);
                _persistedCatalog = catalog.DeepClone();
                Changed?.Invoke();
                return CategorySaveResult.Ok();
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not save categories: {ex.Message}");
                RestorePersistedCatalog();
                return CategorySaveResult.Failed($"Could not save categories: {ex.Message}");
            }
        }

        /// <summary>
        /// Discards mutations made since the last successful load or save.
        /// </summary>
        /// <remarks>
        /// Commands mutate the live folder tree before calling <see cref="SaveChanges"/>. Replacing it
        /// with a fresh clone keeps a refused rename, move, pin, or folder edit from leaking into a later
        /// successful save. Views already refresh after a failed save and therefore bind to this restored
        /// tree rather than retaining references to the rejected one.
        /// </remarks>
        private void RestorePersistedCatalog()
        {
            if (_persistedCatalog != null)
                _catalog = _persistedCatalog.DeepClone();
        }

        /// <summary>When this session last read or wrote the sidecar; null when it has never existed.</summary>
        private DateTime? _sidecarWrittenUtc;
        private bool _sidecarStateKnown;
        private bool _sidecarExistedWhenLoaded;

        private bool HasExternalChange(CategoryCatalog catalog)
        {
            var exists = File.Exists(catalog.FilePath);
            if (_sidecarStateKnown && exists != _sidecarExistedWhenLoaded)
                return true;

            if (!exists)
                return false;

            var current = LastWriteUtc(catalog.FilePath);

            // An unreadable timestamp is not evidence of a conflict.
            if (_sidecarWrittenUtc == null || current == null)
                return false;

            return current != _sidecarWrittenUtc;
        }

        private static DateTime? LastWriteUtc(string? path)
        {
            try
            {
                return path != null && File.Exists(path) ? File.GetLastWriteTimeUtc(path) : null;
            }
            catch (Exception)
            {
                // An unreadable timestamp is not evidence of a conflict.
                return null;
            }
        }

        /// <summary>Re-reads the tree in open views without writing anything.</summary>
        public void NotifyChanged() => Changed?.Invoke();

        private StandardPalette StandardPaletteFor(ResourceType type)
        {
            if (_standardPalettes.TryGetValue(type, out var cached))
                return cached;

            var palette = StandardPaletteLoader.Load(_resourceIndex, type, ResolveCategoryName, _log.AppendLine);
            _standardPalettes[type] = palette;

            if (!palette.IsEmpty)
                _log.AppendLine(
                    $"Loaded {palette.Section.AllFolders().Count()} standard " +
                    $"{type.DisplayName().ToLowerInvariant()} categories ({palette.ResRefs.Count} blueprints).");

            return palette;
        }

        /// <summary>
        /// Reads the initial category roots for a type. An empty list is a successful seed (the type
        /// deliberately has no palette, or the palette has no folders); null means a mapped palette
        /// was missing or unreadable and must be retried rather than permanently marked complete.
        /// </summary>
        private IReadOnlyList<CategoryFolder>? ReadPaletteSeed(ResourceType type)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return null;

            var itpPath = PalettePathFor(workspace.ModuleRoot, type);
            if (itpPath == null)
                return Array.Empty<CategoryFolder>();
            if (!File.Exists(itpPath))
                return null;

            try
            {
                var imported = ItpCategoryImporter.Import(ItpDocument.Load(itpPath), ResolveCategoryName);
                var count = imported.AllFolders().Count();
                if (count > 0)
                    _log.AppendLine(
                        $"Seeded {count} {type.DisplayName().ToLowerInvariant()} categories from '{Path.GetFileName(itpPath)}'.");
                return imported.Folders.ToList();
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not read categories from '{Path.GetFileName(itpPath)}': {ex.Message}");
                return null;
            }
        }

        private string? ResolveCategoryName(uint strRef) => _tlk?.GetString(strRef);

        /// <summary>
        /// NWN's custom palette file for a blueprint type. Areas have none - the base game never shipped
        /// an area palette - so they start from the automatic grouping rule instead of a seed.
        /// </summary>
        private static string? PalettePathFor(string moduleRoot, ResourceType type)
        {
            var stem = type switch
            {
                ResourceType.Utc => "creaturepalcus",
                ResourceType.Utd => "doorpalcus",
                ResourceType.Uti => "itempalcus",
                ResourceType.Utp => "placeablepalcus",
                ResourceType.Uts => "soundpalcus",
                ResourceType.Utm => "storepalcus",
                ResourceType.Utt => "triggerpalcus",
                ResourceType.Utw => "waypointpalcus",
                _ => null
            };

            return stem == null ? null : Path.Combine(moduleRoot, "itp", stem + ".itp.json");
        }
    }
}
