using SWLOR.Toolset.Domain.Documents;
using Nwn.Toolset.Avalonia.Explorer.Workflow;
using SWLOR.Toolset.Domain.Conversations;
using ConversationGraph = SWLOR.Game.Server.Service.ConversationService.ConversationGraph;

namespace SWLOR.Toolset.Shell.Panels.ExplorerHost
{
    /// <summary>
    /// One dialogue-text scan over the module's legacy DLGs and conversation graphs, holding the open
    /// editors' snapshots taken on the UI thread. Runs on a worker.
    /// </summary>
    internal sealed class SwlorDialogueSearchScan : IModuleExplorerContentSearchScan
    {
        private readonly string _dialogDirectory;
        private readonly string? _graphDirectory;
        private readonly string _query;
        private readonly IReadOnlyDictionary<string, DlgDocument>? _openDialogs;
        private readonly IReadOnlyDictionary<string, ConversationGraph>? _openGraphs;

        public SwlorDialogueSearchScan(
            string dialogDirectory,
            string? graphDirectory,
            string query,
            IReadOnlyDictionary<string, DlgDocument>? openDialogs,
            IReadOnlyDictionary<string, ConversationGraph>? openGraphs)
        {
            _dialogDirectory = dialogDirectory;
            _graphDirectory = graphDirectory;
            _query = query;
            _openDialogs = openDialogs;
            _openGraphs = openGraphs;
        }

        public IReadOnlyCollection<string> Run(CancellationToken cancellationToken) =>
            DialogueSearch
                .Search(
                    _dialogDirectory,
                    _query,
                    cancellationToken: cancellationToken,
                    openDocument: resRef =>
                        _openDialogs != null && _openDialogs.TryGetValue(resRef, out var open) ? open : null,
                    conversationGraphDirectory: _graphDirectory,
                    openGraph: resRef =>
                        _openGraphs != null && _openGraphs.TryGetValue(resRef, out var graph) ? graph : null)
                .Select(hit => hit.ResRef)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
