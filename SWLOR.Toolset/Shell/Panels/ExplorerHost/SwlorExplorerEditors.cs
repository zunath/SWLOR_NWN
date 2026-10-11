using Nwn.Toolset.Avalonia.Explorer.Workflow;

namespace SWLOR.Toolset.Shell.Panels.ExplorerHost
{
    /// <summary>
    /// SWLOR's editors as Module Contents uses them. Resolved on demand because the editor service depends
    /// on the dock factory, which depends on this panel.
    /// </summary>
    internal sealed class SwlorExplorerEditors : IModuleExplorerEditors
    {
        private readonly Func<Editors.EditorService>? _editorService;

        public SwlorExplorerEditors(Func<Editors.EditorService>? editorService)
        {
            _editorService = editorService;
        }

        /// <summary>All three kinds open: areas in the area editor, scripts in the script editor, dialogs in Play-it.</summary>
        public bool CanOpen(ResourceType type) => type is ResourceType.Area or ResourceType.Nss or ResourceType.Dlg;

        public void Open(ResourceType type, string resRef) => _editorService?.Invoke().TryOpenEditor(type, resRef);

        public bool IsOpen(ResourceType type, string resRef) => _editorService?.Invoke().IsOpen(type, resRef) == true;

        public bool IsModulePropertiesOpen => _editorService?.Invoke().IsModulePropertiesOpen == true;

        public bool TryCloseForDeletion(ResourceType type, string resRef) =>
            _editorService?.Invoke().TryCloseResourceForDeletion(type, resRef) == true;

        public Task CompileAsync(ResourceType type, string resRef) =>
            _editorService?.Invoke().CompileScriptAsync(resRef) ?? Task.CompletedTask;
    }
}
