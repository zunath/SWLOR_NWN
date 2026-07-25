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
                _loadedForModuleRoot = moduleRoot;
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
            if (!_seeded.Add(type) || section.IsSeeded)
            {
                RepairPlaceholderNames(section);
                return section;
            }

            SeedFromPalette(type, section);
            section.IsSeeded = true;
            SaveChanges();
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

                var resolved = ResolveCategoryName(strRef);
                if (string.IsNullOrWhiteSpace(resolved))
                    continue;

                folder.Rename(resolved.Trim());
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
        public CategorySaveResult SaveChanges()
        {
            var catalog = Catalog;
            if (catalog == null)
                return CategorySaveResult.Ok();

            if (catalog.IsReadOnly)
            {
                var refusal = "These categories were written by a newer Toolset and will not be overwritten.";
                _log.AppendLine(refusal);
                return CategorySaveResult.Failed(refusal);
            }

            if (HasExternalChange(catalog))
            {
                var conflict =
                    $"'{catalog.FilePath}' changed outside the toolset; the change was not saved. " +
                    "Reopen the module to pick up the external version.";
                _log.AppendLine(conflict);
                return CategorySaveResult.Failed(conflict);
            }

            try
            {
                catalog.MarkDirty();
                catalog.Save();
                _sidecarWrittenUtc = LastWriteUtc(catalog.FilePath);
                Changed?.Invoke();
                return CategorySaveResult.Ok();
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not save categories: {ex.Message}");
                return CategorySaveResult.Failed($"Could not save categories: {ex.Message}");
            }
        }

        /// <summary>When this session last read or wrote the sidecar; null when it has never existed.</summary>
        private DateTime? _sidecarWrittenUtc;

        private bool HasExternalChange(CategoryCatalog catalog)
        {
            var current = LastWriteUtc(catalog.FilePath);

            // Never seen before (first save of a new file) is not a conflict.
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

        private void SeedFromPalette(ResourceType type, CategorySection section)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return;

            var itpPath = PalettePathFor(workspace.ModuleRoot, type);
            if (itpPath == null || !File.Exists(itpPath))
                return;

            try
            {
                var imported = ItpCategoryImporter.Import(ItpDocument.Load(itpPath), ResolveCategoryName);
                foreach (var folder in imported.Folders)
                    section.AddFolder(folder);

                var count = imported.AllFolders().Count();
                if (count > 0)
                    _log.AppendLine(
                        $"Seeded {count} {type.DisplayName().ToLowerInvariant()} categories from '{Path.GetFileName(itpPath)}'.");
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not read categories from '{Path.GetFileName(itpPath)}': {ex.Message}");
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
