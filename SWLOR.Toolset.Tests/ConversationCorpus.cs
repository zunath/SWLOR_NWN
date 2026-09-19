using Newtonsoft.Json;
using SWLOR.Game.Server.Service.ConversationService;

namespace SWLOR.Toolset.Tests;

internal static class ConversationCorpus
{
    public static string DirectoryPath => Path.Combine(
        CorpusLocator.RepositoryRoot, "SWLOR.Game.Server", "ConversationData");

    public static IEnumerable<(string File, ConversationGraph Graph)> ReadGraphs() =>
        Directory.EnumerateFiles(DirectoryPath, "*.conversation.json")
            .Select(path => (Path.GetFileName(path), JsonConvert.DeserializeObject<ConversationGraph>(File.ReadAllText(path))!));

    public static IEnumerable<(string File, string Key, string[] Arguments)> SnippetUsages()
    {
        foreach (var (file, graph) in ReadGraphs())
        {
            var conditions = graph.EntryPoints.SelectMany(link => link.Conditions)
                .Concat(graph.Nodes.Values.SelectMany(node => node.Choices).SelectMany(link => link.Conditions))
                .Concat(graph.Choices.Values.SelectMany(choice => choice.Next).SelectMany(link => link.Conditions));
            foreach (var condition in conditions.Where(condition => !condition.Key.StartsWith("system.", StringComparison.Ordinal)))
                yield return (file, (condition.IsNegated ? "!" : "") + condition.Key, condition.Arguments.ToArray());

            var actions = graph.OnStartActions.Concat(graph.OnEndActions).Concat(graph.OnAbortActions)
                .Concat(graph.Nodes.Values.SelectMany(node => node.OnEnterActions))
                .Concat(graph.Choices.Values.SelectMany(choice => choice.Actions));
            foreach (var action in actions.Where(action => !action.Key.StartsWith("system.", StringComparison.Ordinal)))
                yield return (file, action.Key, action.Arguments.ToArray());
        }
    }
}
