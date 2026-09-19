using System.Text.RegularExpressions;
using FluentAssertions;
using FluentAssertions.Execution;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.Game.Server.Service.SpawnService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public sealed partial class QuestConversationTests
{
    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }

    private static IEnumerable<JObject> ModuleDocuments(string extension) =>
        Directory.EnumerateFiles(Path.Combine(RepositoryRoot(), "Module", extension), $"*.{extension}.json")
            .Select(path => JObject.Parse(File.ReadAllText(path)));

    private static IEnumerable<JObject> WorldObjects()
    {
        foreach (var path in Directory.EnumerateFiles(Path.Combine(RepositoryRoot(), "Module", "git"), "*.git.json"))
        {
            using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
            foreach (var list in new[] { "Creature List", "Placeable List", "TriggerList", "Door List", "WaypointList" })
            {
                if (!document.RootElement.TryGetProperty(list, out var wrapper)) continue;
                foreach (var obj in wrapper.GetProperty("value").EnumerateArray())
                {
                    // Keep only routing/spawn metadata, not parent links to the entire area's GFF tree.
                    var metadata = new JObject();
                    foreach (var field in new[] { "TemplateResRef", "Tag", "Conversation", "OnUsed", "ScriptOnEnter", "VarTable" })
                        if (obj.TryGetProperty(field, out var value)) metadata[field] = JToken.Parse(value.GetRawText());
                    yield return metadata;
                }
            }
        }
    }

    private static string Field(JObject obj, string field) => obj[field]?["value"]?.ToString() ?? string.Empty;
    private static string Local(JObject obj, string name) =>
        (obj["VarTable"]?["value"] as JArray ?? new JArray()).OfType<JObject>()
            .Where(variable => Field(variable, "Name") == name)
            .Select(variable => Field(variable, "Value")).FirstOrDefault() ?? string.Empty;

    private static IEnumerable<ConversationAction> GraphActions(ConversationGraph graph) =>
        graph.OnStartActions.Concat(graph.OnEndActions).Concat(graph.OnAbortActions)
            .Concat(graph.Nodes.Values.SelectMany(node => node.OnEnterActions))
            .Concat(graph.Choices.Values.SelectMany(choice => choice.Actions));

    [Test]
    public void EveryStoryQuest_HasAPlacedGiverAndCanResumeEveryStage()
    {
        using var scope = new AssertionScope();
        var quests = BuildQuests().Values.Where(quest => quest.GuildType == GuildType.Invalid).ToArray();
        var graphs = ReadGraphs().ToArray();
        var world = WorldObjects().ToArray();
        var placedGraphs = world.Select(obj => Field(obj, "Conversation")).ToHashSet();
        var worldSteps = world.Where(obj => Field(obj, "OnUsed") == "quest_placeable" || Field(obj, "ScriptOnEnter") == "quest_trigger")
            .Select(obj => (Local(obj, "QUEST_ID"), Local(obj, "QUEST_STATE"))).ToHashSet();
        var checkedStates = 0;

        foreach (var quest in quests)
        {
            var questGraphs = graphs.Where(graph => GraphActions(graph).Any(action =>
                action.Arguments.SequenceEqual(new[] { quest.QuestId }))).ToArray();
            var offers = questGraphs.Where(graph => ReachableActions(graph, quest.QuestId, 0)
                .Any(action => action.Key == "action-accept-quest" && action.Arguments[0] == quest.QuestId));
            offers.Should().Contain(graph => placedGraphs.Contains(graph.Id), $"{quest.QuestId} needs an accessible offer");

            foreach (var (number, state) in quest.States)
            {
                var actions = questGraphs.Where(graph => placedGraphs.Contains(graph.Id))
                    .SelectMany(graph => ReachableActions(graph, quest.QuestId, number))
                    .Where(action => action.Arguments.SequenceEqual(new[] { quest.QuestId }))
                    .Select(action => action.Key).ToHashSet();
                var hasCollection = state.GetObjectives().OfType<CollectItemObjective>().Any();
                var hasKills = state.GetObjectives().OfType<KillTargetObjective>().Any();
                if (hasCollection)
                    actions.Should().Contain("action-request-quest-items", $"{quest.QuestId} state {number} needs an item hand-in");
                else
                    actions.Should().NotContain("action-request-quest-items", $"{quest.QuestId} state {number} has no collection objective");

                if (!hasCollection && !hasKills)
                {
                    var worldAdvance = worldSteps.Contains((quest.QuestId, number.ToString()));
                    (worldAdvance || actions.Contains("action-advance-quest")).Should().BeTrue(
                        $"{quest.QuestId} state {number} needs a reachable conversation or world interaction after reopening");
                }
                checkedStates++;
            }
        }
        TestContext.Out.WriteLine($"Audited {quests.Length} story quests and {checkedStates} resumable states.");
    }

    // Foreign quest/key/skill conditions are symbolic. Explore every feasible branch, but stop
    // after an unconditional ordered route. Target quest conditions use the exact saved state.
    private static IEnumerable<ConversationAction> ReachableActions(ConversationGraph graph, string questId, int state)
    {
        bool? Condition(ConversationCondition condition)
        {
            bool? result = null;
            var args = condition.Arguments;
            if (args.Count > 0 && args[0] == questId)
            {
                result = condition.Key switch
                {
                    "condition-has-quest" => state > 0,
                    "condition-on-quest-state" => args.Skip(1).Contains(state.ToString()),
                    "condition-can-accept-quest" => state == 0,
                    _ => null
                };
            }
            if (condition.Key == "condition-completed-quest" && args.Contains(questId)) result = false;
            return condition.IsNegated ? !result : result;
        }
        bool? Conditions(IEnumerable<ConversationCondition> conditions)
        {
            var values = conditions.Select(Condition).ToArray();
            return values.Contains(false) ? false : values.Contains(null) ? null : true;
        }
        IEnumerable<string> Routes(IEnumerable<ConversationLink> routes)
        {
            foreach (var route in routes)
            {
                var condition = Conditions(route.Conditions);
                if (condition != false) yield return route.TargetNodeId;
                if (condition == true) yield break;
            }
        }

        var pending = new Stack<string>(Routes(graph.EntryPoints));
        var visited = new HashSet<string>();
        foreach (var action in graph.OnStartActions) yield return action;
        while (pending.TryPop(out var id))
        {
            if (string.IsNullOrEmpty(id) || !visited.Add(id)) continue;
            var node = graph.Nodes[id];
            foreach (var action in node.OnEnterActions) yield return action;
            foreach (var link in node.Choices.Where(link => Conditions(link.Conditions) != false))
            {
                var choice = graph.Choices[link.ChoiceId];
                foreach (var action in choice.Actions) yield return action;
                if (!choice.EndsConversation)
                    foreach (var next in Routes(choice.Next)) pending.Push(next);
            }
        }
    }

    [Test]
    public void EveryCollectionObjective_HasAnItemSourceAndCraftedItemsHaveRecipes()
    {
        using var scope = new AssertionScope();
        var root = RepositoryRoot();
        var obtainable = EconomyObtainabilityCoverageTests.ReadObtainableResrefs(root);
        var craftable = Directory.EnumerateFiles(Path.Combine(root, "SWLOR.Game.Server", "Feature", "RecipeDefinition"),
                "*.cs", SearchOption.AllDirectories)
            .SelectMany(path => Regex.Matches(File.ReadAllText(path), "\\.Resref\\(\\s*\"([^\"]+)\"")
                .Select(match => match.Groups[1].Value)).ToHashSet();
        var objectives = 0;
        foreach (var quest in BuildQuests().Values)
        foreach (var state in quest.States.Values)
        foreach (var objective in state.GetObjectives().OfType<CollectItemObjective>())
        {
            File.Exists(Path.Combine(root, "Module", "uti", objective.Resref + ".uti.json"))
                .Should().BeTrue($"{quest.QuestId} needs {objective.Resref}");
            obtainable.Should().Contain(objective.Resref, $"{quest.QuestId} needs an acquisition source");
            if (objective.ProducerRequirement == CollectItemProducerRequirementType.PlayerProduced)
                craftable.Should().Contain(objective.Resref, $"{quest.QuestId} requires a player-crafted item");
            objective.Quantity.Should().BePositive(quest.QuestId);
            objectives++;
        }
        TestContext.Out.WriteLine($"Audited {objectives} collection objectives, including every guild crafting task.");
    }

    [Test]
    public void EveryKillObjective_HasAPlacedSpawnedOrTriggeredCreature()
    {
        using var scope = new AssertionScope();
        var world = WorldObjects().ToArray();
        var spawnTables = typeof(ISpawnListDefinition).Assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && typeof(ISpawnListDefinition).IsAssignableFrom(type))
            .SelectMany(type => ((ISpawnListDefinition)Activator.CreateInstance(type)!).BuildSpawnTables()).ToArray();
        var tableReferences = world.Select(obj => Field(obj, "Tag"))
            .Concat(ModuleDocuments("git").Select(area => Local(area, "CREATURE_SPAWN_TABLE_ID")))
            .ToHashSet();
        var creatures = world.Select(obj => Field(obj, "TemplateResRef"))
            .Concat(world.Where(obj => Field(obj, "OnUsed") == "quest_enc")
                .Select(obj => Local(obj, "QUEST_ENCOUNTER_RESREF")))
            .Concat(spawnTables.Where(table => tableReferences.Contains(table.Key))
                .SelectMany(table => table.Value.Spawns).Where(spawn => spawn.Type == ObjectType.Creature)
                .Select(spawn => spawn.Resref)).ToHashSet();
        var groups = ModuleDocuments("utc").Where(obj => creatures.Contains(Field(obj, "TemplateResRef")))
            .Concat(world).Select(obj => Local(obj, "QUEST_NPC_GROUP_ID")).ToHashSet();
        var count = 0;
        foreach (var quest in BuildQuests().Values)
        foreach (var objective in quest.States.Values.SelectMany(state => state.GetObjectives()).OfType<KillTargetObjective>())
        {
            groups.Should().Contain(((int)objective.Group).ToString(), $"{quest.QuestId} requires {objective.Group}");
            objective.Amount.Should().BePositive(quest.QuestId);
            count++;
        }
        TestContext.Out.WriteLine($"Audited {count} kill objectives against world placements, active spawn tables and encounters.");
    }

    [Test]
    public void AllGuildTasks_HaveObjectivesSupportedByTheGuildBoard()
    {
        using var scope = new AssertionScope();
        var quests = BuildQuests().Values.Where(quest => quest.GuildType != GuildType.Invalid).ToArray();
        foreach (var quest in quests)
        {
            quest.IsRepeatable.Should().BeTrue(quest.QuestId);
            quest.States.Count.Should().BeInRange(1, 2, quest.QuestId);
            quest.States[1].GetObjectives().Should().NotBeEmpty(quest.QuestId)
                .And.OnlyContain(objective => objective is KillTargetObjective || objective is CollectItemObjective);
            if (quest.States.Count == 2)
                quest.States[2].GetObjectives().Should().BeEmpty(quest.QuestId);
        }
        TestContext.Out.WriteLine($"Audited {quests.Length} guild task lifecycles.");
    }

    [Test]
    public void QuestPrerequisites_HaveNoCycles()
    {
        var quests = BuildQuests();
        foreach (var id in quests.Keys) Visit(id, new HashSet<string>());
        void Visit(string id, HashSet<string> path)
        {
            path.Add(id).Should().BeTrue($"{id} must not depend on itself through {string.Join(", ", path)}");
            foreach (var prerequisite in quests[id].Prerequisites.OfType<RequiredQuestPrerequisite>())
                Visit(prerequisite.QuestId, new HashSet<string>(path));
        }
    }

    [TestCase("meet_inquisitor", "korrdralquest", "entry-00002", "reply-00003")]
    [TestCase("sith_code_test", "korrdralquest", "entry-00024", "reply-00028")]
    public void InterruptedSithTraining_CanResumeAndFinishWithoutRepeatingTheTest(
        string questId, string graphId, string entryId, string replyId)
    {
        var graph = ReadGraphs().Single(graph => graph.Id == graphId);
        var runtime = new ConversationRuntime();
        var completed = false;
        var advances = 0;
        runtime.RegisterCondition("condition-has-quest", (_, args) => args[0] == questId && !completed);
        runtime.RegisterCondition("condition-on-quest-state", (_, args) => args[0] == questId && args.Skip(1).Contains("2") && !completed);
        runtime.RegisterCondition("condition-completed-quest", (_, args) => completed && args.All(id => id == questId));
        runtime.RegisterCondition("condition-can-accept-quest", (_, _) => false);
        runtime.RegisterAction("system.execute-owner-script", (_, args) =>
        {
            args.Should().Equal("nw_walk_wp");
            return true;
        });
        runtime.RegisterAction("action-advance-quest", (_, args) =>
        {
            args.Should().Equal(questId);
            advances++;
            completed = true;
            return true;
        });
        var session = new ConversationSession(graph, new ConversationContext(1, 2), runtime);
        session.Start().Should().BeTrue();
        session.CurrentText.Select(text => text.Text).Should().Equal(graph.Nodes[entryId].Text.Select(text => text.Text));
        advances.Should().Be(0, "reopening the conversation must not complete it automatically");
        var reply = session.VisibleChoices.ToList().FindIndex(choice => choice.Id == replyId);
        reply.Should().BeGreaterThanOrEqualTo(0);
        session.SelectChoice(reply);
        advances.Should().Be(1);
        completed.Should().BeTrue();
        session.EndReason.Should().NotBe(ConversationEndReason.RuntimeError);
    }

    [Test]
    public void RepublicOath_IsAvailableOnNahulusAssignedConversationOnlyAtItsQuestStage()
    {
        var graph = ReadGraphs().Single(graph => graph.Id == "repbase_sgtqon");
        var runtime = new ConversationRuntime();
        var state = 4;
        runtime.RegisterCondition("condition-on-quest-state", (_, args) =>
            args[0] == "joining_the_republic" && args.Skip(1).Contains(state.ToString()));
        runtime.RegisterAction("action-advance-quest", (_, args) =>
        {
            args.Should().Equal("joining_the_republic");
            state.Should().Be(5);
            state++;
            return true;
        });
        var early = new ConversationSession(graph, new ConversationContext(1, 2), runtime);
        early.Start().Should().BeTrue();
        early.VisibleChoices.Should().NotContain(choice => choice.Id == "oath-reply-00000");
        state = 5;
        var oath = new ConversationSession(graph, new ConversationContext(1, 2), runtime);
        oath.Start().Should().BeTrue();
        foreach (var number in new[] { 0, 1, 3, 5, 7, 9, 11 })
        {
            var index = oath.VisibleChoices.ToList().FindIndex(choice => choice.Id == $"oath-reply-{number:00000}");
            index.Should().BeGreaterThanOrEqualTo(0);
            oath.SelectChoice(index);
        }
        state.Should().Be(6);
        oath.EndReason.Should().NotBe(ConversationEndReason.RuntimeError);
    }
}
