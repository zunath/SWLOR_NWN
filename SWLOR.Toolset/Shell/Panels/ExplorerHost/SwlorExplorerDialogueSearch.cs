using Nwn.Toolset.Avalonia.Explorer.Workflow;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Shell.Panels.ExplorerHost
{
    /// <summary>
    /// Searches what is said in conversations, so the Dialogs tab finds a dialog by a line of dialogue as
    /// well as by its resref. SWLOR-only: other hosts may own conversations elsewhere.
    /// </summary>
    internal sealed class SwlorExplorerDialogueSearch : IModuleExplorerContentSearch
    {
        private const string SearchingText = "Searching dialogue...";
        private const string FailureText = "Dialogue search failed: ";

        private readonly WorkspaceContext _workspaceContext;
        private readonly Func<Editors.EditorService>? _editorService;

        public SwlorExplorerDialogueSearch(WorkspaceContext workspaceContext, Func<Editors.EditorService>? editorService)
        {
            _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
            _editorService = editorService;
        }

        public bool Supports(ResourceType type) => type == ResourceType.Dlg;

        public string SearchingLabel(ResourceType type) => SearchingText;

        public string FailureMessage(ResourceType type, Exception exception) => FailureText + exception.Message;

        /// <summary>
        /// Runs on the UI thread. Open conversation documents and graphs are deep-snapshotted here, so the
        /// worker-side scan sees unsaved edits without ever touching live editor state that the UI may be
        /// mutating at the same time.
        /// </summary>
        public IModuleExplorerContentSearchScan? Prepare(ResourceType type, string query)
        {
            var moduleRoot = _workspaceContext.Workspace?.ModuleRoot;
            if (moduleRoot == null)
                return null;

            var editors = _editorService?.Invoke();
            var openDialogs = editors?.SnapshotOpenConversationDocuments();
            var openGraphs = editors?.SnapshotOpenNuiConversationGraphs();

            return new SwlorDialogueSearchScan(
                Path.Combine(moduleRoot, "dlg"),
                _workspaceContext.Workspace?.ConversationDataRoot,
                query,
                openDialogs,
                openGraphs);
        }
    }
}
