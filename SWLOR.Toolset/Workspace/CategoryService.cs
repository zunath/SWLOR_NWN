using SWLOR.Toolset.Domain.Categories;
using SWLOR.Toolset.Domain.Documents;
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
        private readonly HashSet<ResourceType> _seeded = new();
        private CategoryCatalog? _catalog;
        private string? _loadedForModuleRoot;

        public CategoryService(WorkspaceContext workspaceContext, OutputLogService log, TlkService? tlk = null)
        {
            _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _tlk = tlk;
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
            if (!_seeded.Add(type) || section.Folders.Count > 0)
                return section;

            SeedFromPalette(type, section);
            return section;
        }

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

        /// <summary>Records a change and writes the sidecar. Saving eagerly keeps a crash from losing an arrangement.</summary>
        public void SaveChanges()
        {
            var catalog = Catalog;
            if (catalog == null)
                return;

            try
            {
                catalog.MarkDirty();
                catalog.Save();
                Changed?.Invoke();
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not save categories: {ex.Message}");
            }
        }

        /// <summary>Re-reads the tree in open views without writing anything.</summary>
        public void NotifyChanged() => Changed?.Invoke();

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
