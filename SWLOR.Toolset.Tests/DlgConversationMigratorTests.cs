using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Toolset.Domain.Conversations;
using SWLOR.Toolset.Domain.Documents;

namespace SWLOR.Toolset.Tests;

public sealed class DlgConversationMigratorTests
{
    [Test]
    public void Convert_PreservesSharedReplyIdentityAndOrderedRouteConditions()
    {
        var document = Load("avixtatham");

        var result = DlgConversationMigrator.Convert("avixtatham", document);

        result.CanRunInNui.Should().BeTrue();
        result.Graph.Nodes.Should().HaveCount(document.Entries.Count);
        result.Graph.Choices.Should().HaveCount(document.Replies.Count);
        result.Graph.EntryPoints.Select(link => link.TargetNodeId)
            .Should().Equal(document.Openings.Select(link => $"entry-{link.TargetIndex:D5}"));

        foreach (var entry in document.Entries)
        {
            var graphNode = result.Graph.Nodes[$"entry-{entry.Index:D5}"];
            graphNode.Choices.Select(link => link.ChoiceId)
                .Should().Equal(entry.Links.Select(link => $"reply-{link.TargetIndex:D5}"));

            for (var index = 0; index < entry.Links.Count; index++)
            {
                graphNode.Choices[index].Conditions.Select(condition => condition.Key)
                    .Should().Equal(entry.Links[index].Conditions.Select(condition => condition.SnippetKey));
            }
        }
    }

    [Test]
    public void Convert_TurnsMarkupAndTokensIntoNuiNativeText()
    {
        var result = DlgConversationMigrator.Convert("cz_receptionist", Load("cz_receptionist"));

        result.CanRunInNui.Should().BeTrue();
        result.Graph.Nodes.Values.SelectMany(node => node.Text)
            .Should().Contain(block => block.Style == ConversationTextStyle.Highlight);
        result.Graph.Nodes.Values.SelectMany(node => node.Text)
            .Should().NotContain(block => block.Text.Contains("<StartHighlight>", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public void Convert_ReportsCustomNwScriptAsAnExplicitLegacyException()
    {
        var result = DlgConversationMigrator.Convert("train_terminal", Load("train_terminal"));

        result.CanRunInNui.Should().BeFalse();
        result.Issues.Should().Contain(issue =>
            issue.Severity == ConversationMigrationIssueSeverity.RequiresLegacyException &&
            issue.Message.Contains("open_train_store", StringComparison.Ordinal));
    }

    [Test]
    public void GeneratedCorpus_ExactlyMatchesEverySafeAuthoredDialogAndEveryGraphValidates()
    {
        var conversationDirectory = Path.Combine(
            CorpusLocator.RepositoryRoot,
            "SWLOR.Game.Server",
            "ConversationData");
        var generatedIds = Directory.EnumerateFiles(conversationDirectory, "*.conversation.json")
            .Select(path => Path.GetFileName(path)[..^".conversation.json".Length])
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var expectedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in Directory.EnumerateFiles(
                     Path.Combine(CorpusLocator.ModuleDirectory, "dlg"),
                     "*.dlg.json"))
        {
            var id = Path.GetFileName(path)[..^".dlg.json".Length];
            if (IsGeneratedShell(id))
                continue;

            var result = DlgConversationMigrator.Convert(id, DlgDocument.Load(path));
            if (result.CanRunInNui)
                expectedIds.Add(id);
        }

        generatedIds.Should().BeEquivalentTo(expectedIds);

        foreach (var id in generatedIds)
        {
            var path = Path.Combine(conversationDirectory, id + ".conversation.json");
            var graph = JsonConvert.DeserializeObject<ConversationGraph>(File.ReadAllText(path));
            graph.Should().NotBeNull();
            ConversationGraphValidator.Validate(graph!).Should().BeEmpty(id);
        }
    }

    private static DlgDocument Load(string id)
    {
        return DlgDocument.Load(Path.Combine(CorpusLocator.ModuleDirectory, "dlg", id + ".dlg.json"));
    }

    private static bool IsGeneratedShell(string id)
    {
        return id.StartsWith("dialog", StringComparison.OrdinalIgnoreCase) &&
               int.TryParse(id["dialog".Length..], out _);
    }
}
