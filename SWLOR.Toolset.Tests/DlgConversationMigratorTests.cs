using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
    public void Convert_MapsTrainingTerminalActionsToNuiOperations()
    {
        var result = DlgConversationMigrator.Convert("train_terminal", Load("train_terminal"));

        result.CanRunInNui.Should().BeTrue();
        AllActions(result.Graph).Select(action => action.Key).Should().Contain(
            "action-open-training-store",
            "action-open-stat-rebuild",
            "action-purchase-full-rebuild");
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
    public void Convert_MapsContrabandMerchantClassGateAndStores()
    {
        var result = DlgConversationMigrator.Convert("dt_cntr_magasin", Load("dt_cntr_magasin"));

        result.CanRunInNui.Should().BeTrue();
        AllConditions(result.Graph).Should().Contain(condition =>
            condition.Key == "condition-player-class" &&
            condition.Arguments.SequenceEqual(new[] { "Rogue" }));
        var storeTags = AllActions(result.Graph)
            .Where(action => action.Key == "action-open-store")
            .Select(action => action.Arguments.Single())
            .ToArray();
        storeTags.Should().BeEquivalentTo(
                "TATOOINE_GENERAL_STORE_MERCHANT",
                "visc_smuggler");

        new[]
            {
                Path.Combine(CorpusLocator.ModuleDirectory, "git", "tat_anc_southdis.git.json"),
                Path.Combine(CorpusLocator.ModuleDirectory, "git", "veles_exterior.git.json")
            }
            .SelectMany(path =>
                ((JArray?)JObject.Parse(File.ReadAllText(path))["StoreList"]?["value"] ?? new JArray())
                .Select(store => store["Tag"]?["value"]?.Value<string>()))
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Should().Contain(storeTags,
                "every mapped store tag must resolve to a placed module store");
    }

    [Test]
    public void Convert_DisablesSkyRaceRegistrationWithoutChargingPlayers()
    {
        var result = DlgConversationMigrator.Convert("dt_barman_gen", Load("dt_barman_gen"));

        result.CanRunInNui.Should().BeTrue();
        AllActions(result.Graph).Should().NotContain(action =>
            action.Key == "action-take-player-credits" ||
            action.Key == "action-adjust-local-number" &&
            action.Arguments.SequenceEqual(new[] { "module", "SWLOR_SKYRACE_PARTICIPANTS", "1" }));
        result.Graph.Nodes["entry-00000"].Choices
            .Single(link => link.ChoiceId == "reply-00005")
            .Conditions.Should().Contain(condition => condition.Key == "system.always-false");
        AllActions(result.Graph).Should().Contain(action =>
            action.Key == "action-notify-player" &&
            action.Arguments.SequenceEqual(new[] { "Sky races are not currently available." }));
    }

    [TestCase("spy2")]
    [TestCase("crystal")]
    [TestCase("refugee")]
    [TestCase("nw_convo_coopemm")]
    public void Convert_GatesEveryCreditSpendingReplyByAffordability(string id)
    {
        var result = DlgConversationMigrator.Convert(id, Load(id));

        foreach (var choice in result.Graph.Choices.Values)
        {
            var amounts = choice.Actions
                .Where(action => action.Key == "action-take-player-credits")
                .Select(action => action.Arguments.Single())
                .ToArray();
            if (amounts.Length == 0)
                continue;

            var incomingLinks = result.Graph.Nodes.Values
                .SelectMany(node => node.Choices)
                .Where(link => link.ChoiceId == choice.Id)
                .ToArray();
            incomingLinks.Should().NotBeEmpty(choice.Id);
            foreach (var amount in amounts)
            {
                incomingLinks.Should().OnlyContain(link => link.Conditions.Any(condition =>
                    condition.Key == "condition-player-credits" &&
                    condition.Arguments.SequenceEqual(new[] { amount })));
            }
        }
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
    public void Convert_LeavesDmfiOnItsNativeConversationPath()
    {
        var result = DlgConversationMigrator.Convert("dmfi_universal", Load("dmfi_universal"));

        result.CanRunInNui.Should().BeFalse();
        result.Issues.Should().ContainSingle(issue =>
            issue.Severity == ConversationMigrationIssueSeverity.RequiresLegacyException &&
            issue.Message.Contains("ActionStartConversation", StringComparison.Ordinal));
    }

    [Test]
    public void Convert_RetiresOmniDyeHooksOwnedByTheAppearanceEditor()
    {
        var result = DlgConversationMigrator.Convert("tk_omnidye", Load("tk_omnidye"));

        result.CanRunInNui.Should().BeTrue();
        AllActions(result.Graph).Should().BeEmpty(
            "the appearance editor replaced every OmniDye action and close hook");
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

    private static IEnumerable<ConversationAction> AllActions(ConversationGraph graph)
    {
        return graph.OnStartActions
            .Concat(graph.OnEndActions)
            .Concat(graph.OnAbortActions)
            .Concat(graph.Nodes.Values.SelectMany(node => node.OnEnterActions))
            .Concat(graph.Choices.Values.SelectMany(choice => choice.Actions));
    }

    private static IEnumerable<ConversationCondition> AllConditions(ConversationGraph graph)
    {
        return graph.EntryPoints.SelectMany(link => link.Conditions)
            .Concat(graph.Nodes.Values
                .SelectMany(node => node.Choices)
                .SelectMany(link => link.Conditions))
            .Concat(graph.Choices.Values
                .SelectMany(choice => choice.Next)
                .SelectMany(link => link.Conditions));
    }

    private static bool IsGeneratedShell(string id)
    {
        return id.StartsWith("dialog", StringComparison.OrdinalIgnoreCase) &&
               int.TryParse(id["dialog".Length..], out _);
    }
}
