using Nwn.Toolset.Avalonia.Explorer;
using Nwn.Toolset.Avalonia.Explorer.Workflow;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Shell.Panels.ExplorerHost
{
    /// <summary>
    /// Shows the selected resource in the Properties panel. The preview is left showing whatever the
    /// Palette last put there: nothing in Module Contents has a model.
    /// </summary>
    internal sealed class SwlorExplorerSelection : IModuleExplorerSelection
    {
        private readonly PropertiesViewModel _properties;

        public SwlorExplorerSelection(PropertiesViewModel properties)
        {
            _properties = properties ?? throw new ArgumentNullException(nameof(properties));
        }

        public void Selected(ResourceType type, ExplorerItem item) =>
            _properties.ShowEntry(new CatalogEntry(type, item.ResRef, item.Name, item.Tag, string.Empty));
    }
}
