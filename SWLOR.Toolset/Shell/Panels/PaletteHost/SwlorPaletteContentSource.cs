using Nwn.Authoring.Categories;
using Nwn.Toolset.Avalonia.Palettes.Workflow;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Shell.Panels.PaletteHost
{
    /// <summary>
    /// SWLOR's palette content: the module's JSON blueprints through the workspace catalog, and the base
    /// game's <c>*palstd</c> palettes through <see cref="CategoryService"/>.
    /// </summary>
    internal sealed class SwlorPaletteContentSource : IPaletteContentSource
    {
        private readonly WorkspaceContext _workspaceContext;
        private readonly CategoryService _categories;

        /// <summary>Resref to display name for one type, rebuilt only when the catalog changes.</summary>
        private Dictionary<string, string>? _namesForType;

        /// <summary>The catalog snapshot <see cref="_namesForType"/> was built from.</summary>
        private object? _namesBuiltFrom;

        private ResourceType _namesBuiltForType;

        public SwlorPaletteContentSource(WorkspaceContext workspaceContext, CategoryService categories)
        {
            _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
            _categories = categories ?? throw new ArgumentNullException(nameof(categories));
        }

        /// <summary>Aurora's palette order, owned by <see cref="ResourceTypeExtensions.PaletteOrder"/>.</summary>
        public IReadOnlyList<ResourceType> OfferedTypes => ResourceTypeExtensions.PaletteOrder;

        public bool IsModuleOpen => _workspaceContext.Workspace != null;

        public IReadOnlySet<string> CustomResRefs(ResourceType type) => _categories.ExistingResRefs(type);

        /// <summary>
        /// A blueprint's catalog name, or null while the catalog is still building or for blueprints the
        /// module does not index. Backed by a per-type dictionary so a snapshot resolves every name without
        /// scanning the catalog once per resource.
        /// </summary>
        public string? CustomName(ResourceType type, string resRef)
        {
            var entries = _workspaceContext.Catalog?.Entries;
            if (entries == null)
                return null;

            if (!ReferenceEquals(entries, _namesBuiltFrom) || _namesBuiltForType != type || _namesForType == null)
            {
                _namesForType = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in entries)
                {
                    if (entry.ResourceType == type && !string.IsNullOrWhiteSpace(entry.Name))
                        _namesForType[entry.ResRef] = entry.Name!;
                }

                _namesBuiltFrom = entries;
                _namesBuiltForType = type;
            }

            return _namesForType.TryGetValue(resRef, out var name) ? name : null;
        }

        public StandardPalette Standard(ResourceType type) => _categories.Standard(type);

        public string PluralName(ResourceType type) => type.DisplayName();

        public string SingularName(ResourceType type) => type.SingularDisplayName();
    }
}
