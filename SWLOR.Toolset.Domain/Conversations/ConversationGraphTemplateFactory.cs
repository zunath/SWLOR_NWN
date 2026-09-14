using System.Text;
using Newtonsoft.Json;
using SWLOR.Game.Server.Service.ConversationService;

namespace SWLOR.Toolset.Domain.Conversations;

/// <summary>
/// Creates a brand-new NUI conversation. The graph is the authored resource; no Aurora DLG or
/// generated runtime shell is produced alongside it.
/// </summary>
public static class ConversationGraphTemplateFactory
{
    public const string PlaceholderNpcText = "Enter dialogue here.";

    public static ConversationGraph Create(string resRef, string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resRef);

        const string nodeId = "node-00001";
        var node = new ConversationNode
        {
            Id = nodeId,
            Text = new List<ConversationTextBlock>
            {
                new() { Text = PlaceholderNpcText }
            }
        };

        return new ConversationGraph
        {
            Id = resRef,
            Title = string.IsNullOrWhiteSpace(displayName) ? resRef : displayName.Trim(),
            EntryPoints = new List<ConversationLink>
            {
                new() { TargetNodeId = nodeId }
            },
            Nodes = new Dictionary<string, ConversationNode>(StringComparer.Ordinal)
            {
                [nodeId] = node
            }
        };
    }

    public static byte[] CreateFileContent(string resRef, string displayName)
    {
        var json = JsonConvert.SerializeObject(Create(resRef, displayName), Formatting.Indented);
        return Encoding.UTF8.GetBytes(json);
    }
}
