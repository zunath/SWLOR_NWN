using Nwn.Toolset.Avalonia.Graph;

namespace SWLOR.Toolset.Editors.Conversations;

public sealed record ConversationGraphSnapshot(GraphDocument Diagram, IReadOnlyDictionary<GraphNodeId, string> FullText);
