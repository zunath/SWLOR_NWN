using Nwn.Toolset.Avalonia.Explorer.Workflow;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Settings;

namespace SWLOR.Toolset.Shell.Panels.ExplorerHost
{
    /// <summary>The selected tab in SWLOR's existing <c>moduleContentsTab</c> setting, stored as a resource extension.</summary>
    internal sealed class SwlorExplorerSettings : IModuleExplorerSettings
    {
        private readonly ToolsetSettings _settings;

        public SwlorExplorerSettings(ToolsetSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public ResourceType? SelectedSection
        {
            get => ResourceTypeExtensions.TryFromExtension(_settings.ModuleContentsTab, out var type) ? type : null;
            set => _settings.ModuleContentsTab = value?.Extension() ?? string.Empty;
        }
    }
}
