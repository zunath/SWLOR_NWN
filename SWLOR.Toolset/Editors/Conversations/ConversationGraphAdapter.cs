using System.Collections.ObjectModel;
using Nwn.Toolset.Avalonia.Graph;
using SWLOR.Toolset.Domain.Documents;
using Nwn.Authoring.Documents.Native;

namespace SWLOR.Toolset.Editors.Conversations;

/// <summary>Maps SWLOR's indexed dialogue lists without changing their native identities or bytes.</summary>
public static class ConversationGraphAdapter
{
    public static ConversationGraphSnapshot Build(DlgDocument dialog)
    {
        ArgumentNullException.ThrowIfNull(dialog);
        var start = new GraphNodeId("openings");
        var nodes = new List<GraphNode> { new(start, "Opening routes", $"{dialog.Openings.Count} openings") };
        var edges = new List<GraphEdge>();
        var fullText = new Dictionary<GraphNodeId, string> { [start] = "Opening routes are tested in authored order." };
        var entries = dialog.Entries;
        var replies = dialog.Replies;
        if ((long)entries.Count + replies.Count + 1 > GraphDocument.MaximumNodes)
            throw new ArgumentOutOfRangeException(nameof(dialog), "The dialogue exceeds the graph node budget.");
        foreach (var (node, index) in entries.Select((node, index) => (node, index))
                     .Concat(replies.Select((node, index) => (node, index))))
        {
            var id = Id(node.Kind.ToString(), index);
            var text = node.Text;
            var subtitle = text.Replace('\r', ' ').Replace('\n', ' ');
            if (subtitle.Length > 2048) subtitle = subtitle[..2048];
            nodes.Add(new GraphNode(id, $"{(node.IsEntry ? "NPC" : "Reply")} {index + 1}", subtitle));
            fullText.Add(id, text);
            AddLinks(id, node.Links);
        }
        AddLinks(start, dialog.Openings);
        return new(new GraphDocument(nodes, edges), new ReadOnlyDictionary<GraphNodeId, string>(fullText));

        void AddLinks(GraphNodeId source, IReadOnlyList<DlgLink> links)
        {
            foreach (var (link, index) in links.Select((link, index) => (link, index)))
            {
                if (edges.Count >= GraphDocument.MaximumEdges)
                    throw new ArgumentOutOfRangeException(nameof(dialog), "The dialogue exceeds the graph edge budget.");
                var label = $"{(link.IsOpening ? "Opening" : link.IsChild ? "Reuse" : "Route")} {index + 1}";
                if (link.Conditions.Count > 0) label += " · conditional";
                if (!dialog.HasNode(link.TargetKind, link.TargetIndex)) label += " · unresolved";
                edges.Add(new GraphEdge(new GraphEdgeId($"{source.Value}/route/{index}"), source, Id(link.TargetKind.ToString(), link.TargetIndex), label));
            }
        }
    }

    private static GraphNodeId Id(string kind, int index) => new($"{kind}/{index}");
}
