using Nwn.Authoring.Categories;
using Nwn.Toolset.Avalonia.Explorer;
using Nwn.Toolset.Avalonia.Explorer.Workflow;
using SWLOR.Toolset.Domain.Categories;

namespace SWLOR.Toolset.Shell.Panels.ExplorerHost
{
    /// <summary>
    /// SWLOR's starting folders and row labels, from <see cref="ModuleFolderSeeder"/>: areas by their
    /// "Planet - Place" names, dialogs and scripts by their resref conventions.
    /// </summary>
    internal sealed class SwlorExplorerOrganization : IModuleExplorerOrganization
    {
        private readonly SwlorExplorerContentSource _content;

        public SwlorExplorerOrganization(SwlorExplorerContentSource content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        /// <summary>
        /// Areas are filed by display name, which arrives with the background catalog. Seeding off bare
        /// resrefs would put every area in Unsorted and then never try again.
        /// </summary>
        public bool IsReadyToSeed(ResourceType type) => type != ResourceType.Area || _content.HasCatalogNames;

        public int Seed(CategorySection section, ResourceType type, IReadOnlyList<ExplorerItem> items) =>
            ModuleFolderSeeder.Seed(section, type, items.Select(Seedable));

        public string LeafLabel(ResourceType type, ExplorerItem item) =>
            ModuleFolderSeeder.LeafLabel(type, Seedable(item));

        private static SeedableResource Seedable(ExplorerItem item) => new(item.ResRef, item.Name);
    }
}
