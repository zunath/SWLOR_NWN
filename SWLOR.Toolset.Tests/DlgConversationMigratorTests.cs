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
    public void Convert_MapsCaptainSluuksExitShipActionToTheStartingWaypoint()
    {
        var result = DlgConversationMigrator.Convert("capn_sluuk", Load("capn_sluuk"));

        result.CanRunInNui.Should().BeTrue();
        var exitShip = result.Graph.Choices.Values.Single(choice =>
            choice.Text.Text.Contains("EXIT SHIP", StringComparison.Ordinal));
        exitShip.EndsConversation.Should().BeTrue();
        exitShip.Actions.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Key = "action-teleport",
            Arguments = new[] { "ENTRY_STARTING_WP" }
        });
    }

    [TestCase("dt_barman_gen03")]
    [TestCase("dt_doc_velpo")]
    [TestCase("q1_nikka_larson")]
    [TestCase("quest_example")]
    [TestCase("red_journal_mand")]
    [TestCase("spawn_banner")]
    [TestCase("zomb_telconv")]
    public void Convert_TranslatesKnownModuleScriptsIntoNuiOperations(string id)
    {
        var result = DlgConversationMigrator.Convert(id, Load(id));

        result.CanRunInNui.Should().BeTrue();
        result.Issues.Should().NotContain(issue =>
            issue.Severity == ConversationMigrationIssueSeverity.RequiresLegacyException);
        result.Graph.Nodes.Values.SelectMany(node => node.Text)
            .Concat(result.Graph.Choices.Values.Select(choice => choice.Text))
            .Should().NotContain(block => block.Text.Contains("<CUSTOM", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public void Convert_KeepsContrabandMerchantAsLegacyWhenItsPredicateAndStoresAreUnknown()
    {
        var result = DlgConversationMigrator.Convert("dt_cntr_magasin", Load("dt_cntr_magasin"));

        result.CanRunInNui.Should().BeFalse();
        result.Issues.Where(issue =>
                issue.Severity == ConversationMigrationIssueSeverity.RequiresLegacyException)
            .Select(issue => issue.Message)
            .Should().Contain(message => message.Contains("dt_test_canaille", StringComparison.Ordinal))
            .And.Contain(message => message.Contains("ouvmag_cntrbande", StringComparison.Ordinal))
            .And.Contain(message => message.Contains("ouvmag_cntrbnd_c", StringComparison.Ordinal));
    }

    [Test]
    public void Convert_KeepsSkyRaceAsLegacyUntilRaceStartGameplayExists()
    {
        var result = DlgConversationMigrator.Convert("dt_barman_gen", Load("dt_barman_gen"));

        result.CanRunInNui.Should().BeFalse();
        result.Issues.Should().Contain(issue =>
            issue.Severity == ConversationMigrationIssueSeverity.RequiresLegacyException &&
            issue.Message.Contains("launch_race", StringComparison.Ordinal));
    }

    [Test]
    public void Convert_UsesPerceptionForTheJournalLockPromptAndCondition()
    {
        var result = DlgConversationMigrator.Convert("red_journal_mand", Load("red_journal_mand"));
        var choice = result.Graph.Choices["reply-00001"];

        choice.Text.Text.Should().Contain("Perception 14");
        result.Graph.Nodes["entry-00001"].Text.Select(block => block.Text)
            .Should().Contain(text => text.Contains("perception", StringComparison.OrdinalIgnoreCase));
        result.Graph.Nodes["entry-00000"].Choices
            .Single(link => link.ChoiceId == choice.Id)
            .Conditions.Should().ContainSingle()
            .Which.Arguments.Should().Equal("Perception", "14");
    }

    [Test]
    public void Convert_KeepsDmfiAsTheExplicitNativeException()
    {
        var result = DlgConversationMigrator.Convert("dmfi_universal", Load("dmfi_universal"));

        result.CanRunInNui.Should().BeFalse();
        result.Issues.Should().Contain(issue =>
            issue.Severity == ConversationMigrationIssueSeverity.RequiresLegacyException);
    }

    [Test]
    public void Convert_DiscardsObsoleteOnceMarkersInsteadOfAddingGraphMetadata()
    {
        var document = Load("avixtatham");
        var node = document.Entries.Concat(document.Replies)
            .First(candidate => candidate.Actions.Any(action => !action.IsOncePerPlayerMarker));
        var action = node.Actions.First(candidate => !candidate.IsOncePerPlayerMarker);
        node.AddAction("once-" + action.SnippetKey, "legacy:marker");

        var result = DlgConversationMigrator.Convert("avixtatham", document);
        var json = JsonConvert.SerializeObject(result.Graph);

        result.CanRunInNui.Should().BeTrue();
        json.Should().NotContain("OncePerPlayerId");
        result.Graph.Nodes.Values.SelectMany(item => item.OnEnterActions)
            .Concat(result.Graph.Choices.Values.SelectMany(item => item.Actions))
            .Should().NotContain(item => item.Key.StartsWith("once-", StringComparison.Ordinal));
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
