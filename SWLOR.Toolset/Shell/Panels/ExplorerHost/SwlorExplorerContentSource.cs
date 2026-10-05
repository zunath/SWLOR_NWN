using Nwn.Toolset.Avalonia.Explorer;
using Nwn.Toolset.Avalonia.Explorer.Workflow;
using SWLOR.Toolset.Domain.Validation;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Shell.Panels.ExplorerHost
{
    /// <summary>
    /// SWLOR's Module Contents sections - Areas, Dialogs (conversation graphs plus explicit legacy DLGs)
    /// and Scripts - read from the module JSON folders and the background <see cref="BlueprintCatalog"/>.
    /// </summary>
    /// <remarks>
    /// Areas lead with their catalog display names once the background catalog publishes them. Dialogs and
    /// scripts stay enumeration-backed: creating one calls <c>RefreshCatalogEntry</c>, and that single
    /// insert would otherwise make the type look catalog-backed, hiding every pre-existing resource until
    /// restart. <see cref="WorkspaceContext.IsCatalogIndexedType"/> decides which is which.
    /// </remarks>
    internal sealed class SwlorExplorerContentSource : IModuleExplorerContentSource
    {
        private static readonly IReadOnlyList<ExplorerSection> SwlorSections = new[]
        {
            Section(ResourceType.Area),
            Section(ResourceType.Dlg),
            Section(ResourceType.Nss) with { IsCompilable = true }
        };

        private readonly WorkspaceContext _workspaceContext;
        private Dictionary<ResourceType, List<CatalogEntry>>? _catalogByType;

        public SwlorExplorerContentSource(WorkspaceContext workspaceContext)
        {
            _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));

            _workspaceContext.CatalogEntryRefreshed += (type, resRef) =>
            {
                ResourceChanged?.Invoke(type, resRef);

                // Dialogs and scripts are intentionally absent from the catalog. Their rows still follow
                // file changes, but no full catalog regroup is needed to do that.
                if (!WorkspaceContext.IsCatalogIndexedType(type))
                    ContentChanged?.Invoke();
            };
            _workspaceContext.CatalogEntriesChanged += (_, _) =>
            {
                if (_workspaceContext.Catalog is { } catalog)
                    UpdateCatalog(catalog);
            };
            _workspaceContext.CatalogLabelsChanged += () =>
            {
                if (_workspaceContext.Catalog is { } catalog)
                    UpdateCatalog(catalog);
            };
        }

        /// <summary>The Module Contents tabs, in the order Aurora listed them.</summary>
        public static IReadOnlyList<ResourceType> SectionTypes { get; } =
            SwlorSections.Select(section => section.Type).ToArray();

        public event Action<ResourceType, string>? ResourceChanged;

        public event Action? ContentChanged;

        public IReadOnlyList<ExplorerSection> Sections => SwlorSections;

        public bool IsModuleOpen => _workspaceContext.Workspace != null;

        /// <summary>True once the background catalog has published names, which area seeding files by.</summary>
        public bool HasCatalogNames => _catalogByType != null;

        /// <summary>Forgets the previous module's catalog names; the next refresh enumerates until they return.</summary>
        public void ResetCatalog() => _catalogByType = null;

        /// <summary>Takes the background catalog's names, then rebuilds the tree.</summary>
        public void UpdateCatalog(BlueprintCatalog catalog)
        {
            ArgumentNullException.ThrowIfNull(catalog);
            _catalogByType = catalog.Entries
                .GroupBy(entry => entry.ResourceType)
                .ToDictionary(group => group.Key, group => group.ToList());

            ContentChanged?.Invoke();
        }

        public IReadOnlyList<ExplorerItem> Items(ResourceType type)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return Array.Empty<ExplorerItem>();

            if (type == ResourceType.Dlg)
            {
                return ConversationResRefs(workspace)
                    .Select(resRef => new ExplorerItem(resRef, null, null))
                    .ToList();
            }

            if (CatalogEntries(type) is { } entries)
                return entries.Select(entry => new ExplorerItem(entry.ResRef, entry.Name, entry.Tag)).ToList();

            return workspace.EnumerateResRefs(type)
                .Select(resRef => new ExplorerItem(resRef, null, null))
                .ToList();
        }

        public int Count(ResourceType type)
        {
            if (type == ResourceType.Dlg && _workspaceContext.Workspace is { } workspace)
                return ConversationResRefs(workspace).Count;

            if (CatalogEntries(type) is { } entries)
                return entries.Count;

            return _workspaceContext.Workspace?.EnumerateResRefs(type).Count ?? 0;
        }

        public bool Exists(ResourceType type, string resRef)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return false;

            return type == ResourceType.Dlg
                ? ConversationResRefs(workspace).Contains(resRef, StringComparer.OrdinalIgnoreCase)
                : workspace.EnumerateResRefs(type).Contains(resRef, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Graph-native conversations and explicit legacy exceptions share the Dialogs tab. A graph wins a
        /// duplicate resref, and generated runtime shells are never authoring content.
        /// </summary>
        private static IReadOnlyList<string> ConversationResRefs(ModuleWorkspace workspace) =>
            workspace.EnumerateConversationGraphResRefs()
                .Concat(workspace.EnumerateResRefs(ResourceType.Dlg))
                .Where(resRef => !UnreferencedConversationRule.IsGeneratedShell(resRef))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(resRef => resRef, StringComparer.OrdinalIgnoreCase)
                .ToList();

        private List<CatalogEntry>? CatalogEntries(ResourceType type) =>
            WorkspaceContext.IsCatalogIndexedType(type) &&
            _catalogByType != null &&
            _catalogByType.TryGetValue(type, out var entries)
                ? entries
                : null;

        private static ExplorerSection Section(ResourceType type) =>
            new(type, type.DisplayName(), type.SingularDisplayName());
    }
}
