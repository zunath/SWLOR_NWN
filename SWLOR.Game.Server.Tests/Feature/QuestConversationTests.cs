using System.Reflection;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Tests.Feature;

[NonParallelizable]
public sealed partial class QuestConversationTests
{
    private object _originalItemNames = null!;
    private static PropertyInfo ItemNameCache => typeof(Cache)
        .GetProperty("ItemNamesByResref", BindingFlags.NonPublic | BindingFlags.Static)!;

    [OneTimeSetUp]
    public void LoadItemNamesFromBlueprintsForGuildQuestDefinitions()
    {
        Guild.LoadData();
        NPCGroup.CacheData();
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module", "uti")))
            directory = directory.Parent;
        directory.Should().NotBeNull();
        var names = Directory.EnumerateFiles(Path.Combine(directory!.FullName, "Module", "uti"), "*.uti.json")
            .ToDictionary(path => Path.GetFileName(path)[..^".uti.json".Length], path =>
                JObject.Parse(File.ReadAllText(path))["LocalizedName"]?["value"]?["0"]?.Value<string>() ?? string.Empty);
        _originalItemNames = ItemNameCache.GetValue(null)!;
        ItemNameCache.SetValue(null, names);
    }

    [OneTimeTearDown]
    public void RestoreItemNames() => ItemNameCache.SetValue(null, _originalItemNames);

    private static Dictionary<string, QuestDetail> BuildQuests() => typeof(IQuestListDefinition).Assembly
        .GetTypes()
        .Where(type => type.IsClass && !type.IsAbstract && typeof(IQuestListDefinition).IsAssignableFrom(type))
        .SelectMany(type => ((IQuestListDefinition)Activator.CreateInstance(type)!).BuildQuests())
        .ToDictionary(pair => pair.Key, pair => pair.Value);

    private static IEnumerable<ConversationGraph> ReadGraphs()
    {
        var assembly = typeof(ConversationGraph).Assembly;
        foreach (var name in assembly.GetManifestResourceNames().Where(name =>
                     name.Contains(".ConversationData.") && name.EndsWith(".conversation.json")))
        {
            using var stream = assembly.GetManifestResourceStream(name);
            using var reader = new StreamReader(stream!);
            yield return JsonConvert.DeserializeObject<ConversationGraph>(reader.ReadToEnd())!;
        }
    }

    [Test]
    public void AllQuests_HaveUniqueIdsContiguousJournalStatesAndValidPrerequisiteQuests()
    {
        using var scope = new AssertionScope();
        var quests = BuildQuests();
        quests.Should().NotBeEmpty();
        foreach (var (id, quest) in quests)
        {
            quest.QuestId.Should().Be(id);
            quest.Name.Should().NotBeNullOrWhiteSpace(id);
            quest.States.Keys.Order().Should().Equal(Enumerable.Range(1, quest.States.Count), id);
            quest.States.Should().NotBeEmpty(id);
            foreach (var state in quest.States.Values)
            {
                state.JournalText.Should().NotBeNullOrWhiteSpace(id);
                state.GetObjectives().OfType<CollectItemObjective>().Select(objective => objective.Resref)
                    .Should().OnlyHaveUniqueItems($"{id} uses one saved counter per item");
                state.GetObjectives().OfType<KillTargetObjective>().Select(objective => objective.Group)
                    .Should().OnlyHaveUniqueItems($"{id} uses one saved counter per enemy group");
            }
            foreach (var prerequisite in quest.Prerequisites.OfType<RequiredQuestPrerequisite>())
                quests.ContainsKey(prerequisite.QuestId).Should().BeTrue($"{id} requires {prerequisite.QuestId}");
        }
        TestContext.Out.WriteLine($"Audited {quests.Count} quest definitions.");
    }

    [Test]
    public void AllConversationQuestOperations_ReferenceExistingQuestsAndStates()
    {
        using var scope = new AssertionScope();
        var quests = BuildQuests();
        var operationCount = 0;
        foreach (var graph in ReadGraphs())
        {
            var actions = graph.OnStartActions.Concat(graph.OnEndActions).Concat(graph.OnAbortActions)
                .Concat(graph.Nodes.Values.SelectMany(node => node.OnEnterActions))
                .Concat(graph.Choices.Values.SelectMany(choice => choice.Actions));
            var conditions = graph.EntryPoints.SelectMany(link => link.Conditions)
                .Concat(graph.Nodes.Values.SelectMany(node => node.Choices).SelectMany(link => link.Conditions))
                .Concat(graph.Choices.Values.SelectMany(choice => choice.Next).SelectMany(link => link.Conditions));

            foreach (var action in actions.Where(action => action.Key is
                         "action-accept-quest" or "action-advance-quest" or "action-request-quest-items"))
            {
                action.Arguments.Should().ContainSingle(graph.Id);
                quests.ContainsKey(action.Arguments[0]).Should().BeTrue($"{graph.Id} references {action.Arguments[0]}");
                if (action.Key == "action-request-quest-items" && quests.TryGetValue(action.Arguments[0], out var quest))
                    quest.States.Values.SelectMany(state => state.GetObjectives()).OfType<CollectItemObjective>()
                        .Should().NotBeEmpty($"{graph.Id} opens an item hand-in for {quest.QuestId}");
                operationCount++;
            }
            foreach (var condition in conditions.Where(condition => condition.Key is
                         "condition-has-quest" or "condition-on-quest-state" or
                         "condition-completed-quest" or "condition-can-accept-quest"))
            {
                condition.Arguments.Should().NotBeEmpty(graph.Id);
                var questIds = condition.Key == "condition-completed-quest"
                    ? condition.Arguments : condition.Arguments.Take(1);
                foreach (var id in questIds)
                    quests.ContainsKey(id).Should().BeTrue($"{graph.Id} references {id}");
                if (condition.Key == "condition-on-quest-state" && quests.ContainsKey(condition.Arguments[0]))
                {
                    condition.Arguments.Should().HaveCountGreaterThan(1, graph.Id);
                    foreach (var state in condition.Arguments.Skip(1))
                        quests[condition.Arguments[0]].States.Should().ContainKey(int.Parse(state), graph.Id);
                }
                operationCount++;
            }
        }
        TestContext.Out.WriteLine($"Audited {operationCount} quest actions and conditions.");
    }

    [Test]
    public void AlchemizedFrog_CollectsProofAfterTheKillAndReturnsToCamila()
    {
        var quest = BuildQuests()["alchemized_frog"];
        quest.States[1].GetObjectives().Should().ContainSingle().Which.Should().BeOfType<KillTargetObjective>();
        quest.States[2].GetObjectives().Should().ContainSingle().Which.Should().BeOfType<CollectItemObjective>();
        quest.States[2].JournalText.Should().Contain("Frog Guts").And.Contain("Camila");
        quest.States[3].JournalText.Should().Contain("Camila");

        var graph = ReadGraphs().Single(graph => graph.Id == "tulak_start");
        var handIn = graph.Choices.Values.Single(choice => choice.Actions.Any(action =>
            action.Key == "action-request-quest-items"));
        var incoming = graph.Nodes.Values.SelectMany(node => node.Choices)
            .Where(link => link.ChoiceId == handIn.Id).ToArray();
        incoming.Should().NotBeEmpty();
        incoming.Should().OnlyContain(link => link.Conditions.Any(condition =>
            condition.Key == "condition-on-quest-state" && !condition.IsNegated &&
            condition.Arguments.SequenceEqual(new[] { "alchemized_frog", "2" })));
    }

    [TestCase(1)]
    [TestCase(3)]
    public void EveryKillThenCollectQuest_GrantsEachCreditedPlayerTheirRequiredProof(int playerCount)
    {
        var players = Enumerable.Range(1, playerCount).Select(id => (uint)id).ToArray();
        const uint corpse = 100;
        var audited = 0;
        foreach (var definition in BuildQuests().Values)
        foreach (var (number, state) in definition.States.Where(pair => pair.Key > 1))
        {
            var proof = state.GetObjectives().OfType<CollectItemObjective>().ToArray();
            if (proof.Length == 0 || !definition.States[number - 1].GetObjectives().OfType<KillTargetObjective>().Any())
                continue;
            var (quest, grants) = BuildQuestWithItemRecorder(definition.QuestId);
            foreach (var player in players)
            foreach (var action in quest.OnAdvanceActions)
                action(player, corpse, number);

            grants.Should().BeEquivalentTo(players.SelectMany(player => proof.Select(item =>
                    (item.Resref, player, item.Quantity))),
                $"{quest.QuestId}: every credited player needs separate proof");

            grants.Clear();
            foreach (var player in players)
            {
                foreach (var action in quest.OnAcceptActions) action(player, 200);
                foreach (var action in quest.OnAdvanceActions) action(player, 200, number + 1);
                foreach (var action in quest.OnCompleteActions) action(player, 200);
            }
            grants.Should().BeEmpty($"{quest.QuestId}: acceptance, hand-in, and completion must not grant more proof");
            audited++;
        }
        audited.Should().BeGreaterThanOrEqualTo(3);
    }

    private static (QuestDetail Quest, List<(string Resref, uint Player, int Quantity)> Grants)
        BuildQuestWithItemRecorder(string questId)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        directory.Should().NotBeNull();

        // Compile the real definition against the real builder, replacing only the native item call.
        var definitionType = typeof(IQuestListDefinition).Assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && typeof(IQuestListDefinition).IsAssignableFrom(type))
            .Single(type => ((IQuestListDefinition)Activator.CreateInstance(type)!).BuildQuests().ContainsKey(questId));
        var definition = Directory.EnumerateFiles(Path.Combine(directory!.FullName, "SWLOR.Game.Server",
                "Feature", "QuestDefinition"), "*.cs")
            .Select(File.ReadAllText).Single(source => source.Contains($"class {definitionType.Name}"));
        var recorder = """
            public static class QuestItemRecorder
            {
                public static readonly System.Collections.Generic.List<(string, uint, int)> Grants = new();
                public static uint CreateItemOnObject(string resref, uint player, int quantity = 1)
                {
                    Grants.Add((resref, player, quantity));
                    return 500;
                }
                public static void GiveKeyItem(uint player, SWLOR.Game.Server.Service.KeyItemService.KeyItemType type) { }
            }
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Append(typeof(QuestBuilder).Assembly.Location)
            .Distinct()
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("QuestProofHarness_" + Guid.NewGuid().ToString("N"),
            [CSharpSyntaxTree.ParseText("using static QuestItemRecorder;\nusing KeyItem = QuestItemRecorder;\n" + definition),
                CSharpSyntaxTree.ParseText(recorder)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        var assembly = Assembly.Load(stream.ToArray());
        var quests = ((IQuestListDefinition)Activator.CreateInstance(assembly.GetType(definitionType.FullName!)!)!).BuildQuests();
        var grants = (List<(string, uint, int)>)assembly.GetType("QuestItemRecorder")!
            .GetField("Grants")!.GetValue(null)!;
        return (quests[questId], grants);
    }

    [Test]
    public void EveryCapstoneConversation_AcceptsAndTurnsInEachStepAndExplainsItsAccessKey()
    {
        var quests = BuildQuests();
        var graphs = ReadGraphs().ToDictionary(graph => graph.Id);
        foreach (var line in CapstoneQuestDefinitionTestData.Lines)
        {
            var graph = graphs[line.QuestGiver.DialogueResref];
            graph.Nodes.Values.SelectMany(node => node.Text)
                .Concat(graph.Choices.Values.Select(choice => choice.Text))
                .Should().NotContain(text => text.Text.Contains("Key Items", StringComparison.OrdinalIgnoreCase),
                    "NPC dialogue should describe the key handoff in-world without referring to a UI window");
            var active = new Dictionary<string, int>();
            var completed = new HashSet<string>();
            var accepted = new List<string>();
            var turnedIn = new List<string>();
            var runtime = new ConversationRuntime();
            runtime.RegisterCondition("condition-has-quest", (_, args) => active.ContainsKey(args[0]));
            runtime.RegisterCondition("condition-on-quest-state", (_, args) =>
                active.TryGetValue(args[0], out var state) && args.Skip(1).Contains(state.ToString()));
            runtime.RegisterCondition("condition-completed-quest", (_, args) => args.All(completed.Contains));
            runtime.RegisterCondition("condition-can-accept-quest", (_, args) =>
                !active.ContainsKey(args[0]) && !completed.Contains(args[0]) &&
                quests[args[0]].Prerequisites.OfType<RequiredQuestPrerequisite>()
                    .All(prerequisite => completed.Contains(prerequisite.QuestId)));
            runtime.RegisterAction("action-accept-quest", (_, args) =>
            {
                accepted.Add(args[0]);
                active.Add(args[0], 1);
                return true;
            });
            runtime.RegisterAction("action-advance-quest", (_, args) =>
            {
                active[args[0]].Should().Be(2);
                turnedIn.Add(args[0]);
                active.Remove(args[0]);
                completed.Add(args[0]);
                return true;
            });

            var keyName = typeof(KeyItemType).GetField(line.AreaGroup.AccessKeyItem.ToString())!
                .GetCustomAttribute<KeyItemAttribute>()!.Name;
            quests[line.GetQuestId(0)].States[1].JournalText.Should()
                .Contain(keyName).And.Contain(line.QuestGiver.Name).And.NotContain("Key Items");

            foreach (var questId in line.GetQuestIds())
            {
                var session = new ConversationSession(graph, new ConversationContext(1, 2), runtime);
                session.Start().Should().BeTrue(questId);
                SelectQuestAction(session, "action-accept-quest", questId);
                active.Should().ContainKey(questId);
                if (questId == line.GetQuestId(0))
                    string.Concat(session.CurrentText.Select(text => text.Text)).Should().ContainEquivalentOf("key");

                var reminder = new ConversationSession(graph, new ConversationContext(1, 2), runtime);
                reminder.Start().Should().BeTrue(questId);
                reminder.VisibleChoices.Should().NotContain(choice => choice.Actions.Any(action =>
                    action.Key == "action-accept-quest" || action.Key == "action-advance-quest"));

                active[questId] = 2;
                var turnIn = new ConversationSession(graph, new ConversationContext(1, 2), runtime);
                turnIn.Start().Should().BeTrue(questId);
                SelectQuestAction(turnIn, "action-advance-quest", questId);
            }
            accepted.Should().Equal(line.GetQuestIds());
            turnedIn.Should().Equal(line.GetQuestIds());
            var finished = new ConversationSession(graph, new ConversationContext(1, 2), runtime);
            finished.Start().Should().BeTrue(line.Id);
            finished.VisibleChoices.Should().BeEmpty(line.Id);
        }
    }

    private static void SelectQuestAction(ConversationSession session, string actionKey, string questId)
    {
        var index = session.VisibleChoices.ToList().FindIndex(choice => choice.Actions.Any(action =>
            action.Key == actionKey && action.Arguments.SequenceEqual(new[] { questId })));
        index.Should().BeGreaterThanOrEqualTo(0, $"{questId} must expose its {actionKey} reply");
        session.SelectChoice(index);
        session.EndReason.Should().NotBe(ConversationEndReason.RuntimeError);
    }
}
