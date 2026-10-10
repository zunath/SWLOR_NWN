using System.Text.Json;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Feature.QuestDefinition;
using SWLOR.Game.Server.Feature.SpawnDefinition;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Tests.Feature;

public class EshanGorakVeshTests
{
    private const string QuestId = "eshan_icebound_hunt";

    [Test]
    public void BossLoot_GuaranteesOneShardAndEmeraldWithoutWolfDrops()
    {
        using var creature = ReadJson("Module", "utc", "esh_gorakvesh.utc.json");
        var tables = new EshanLootTableDefinition().BuildLootTables();
        var rolls = creature.RootElement.GetProperty("VarTable").GetProperty("value")
            .EnumerateArray()
            .Where(variable => variable.GetProperty("Name").GetProperty("value").GetString()!
                .StartsWith("LOOT_TABLE_"))
            .Select(variable => variable.GetProperty("Value").GetProperty("value").GetString()!
                .Split(','))
            .ToArray();

        rolls.Select(roll => roll[0]).Should().OnlyHaveUniqueItems();
        foreach (var resref in new[] { "chiro_shard", "emerald" })
        {
            var roll = rolls.Single(candidate => tables[candidate[0]].Any(item => item.Resref == resref));
            roll[1].Should().Be("100");
            roll[2].Should().Be("1");
            tables[roll[0]].Should().ContainSingle();
            tables[roll[0]][0].MaxQuantity.Should().Be(1);
        }

        rolls.SelectMany(roll => tables[roll[0]]).Should().NotContain(item =>
            item.Resref == "esh_wolf_meat" || item.Resref == "esh_wolf_pelt" ||
            item.Resref == "esh_frost_fang");
        rolls.Should().Contain(roll => roll.SequenceEqual(new[] { "ESHAN_MAP_RARES", "5", "1" }));
    }

    [Test]
    public void Hunt_RequiresTheBossGroupAndThenReturnsToTalia()
    {
        var quests = new EshanQuestDefinition().BuildQuests();
        var quest = quests[QuestId];
        var objective = quest.States[1].GetObjectives().Should().ContainSingle().Subject
            .Should().BeOfType<KillTargetObjective>().Subject;
        objective.Group.Should().Be(NPCGroupType.Eshan_GorakVesh);
        objective.Amount.Should().Be(1);
        quest.States[2].GetObjectives().Should().BeEmpty();
        quest.States[2].JournalText.Should().Contain("Talia Venn");
        quest.IsRepeatable.Should().BeFalse();
        quest.Rewards.Should().ContainSingle(reward => reward is XPReward);
        quest.Rewards.Should().ContainSingle(reward => reward is GoldReward);

        using var creature = ReadJson("Module", "utc", "esh_gorakvesh.utc.json");
        GetLocal(creature.RootElement, "QUEST_NPC_GROUP_ID").GetInt32()
            .Should().Be((int)objective.Group);
        quests["eshan_alpha_hunt"].States[1].GetObjectives().OfType<KillTargetObjective>()
            .Should().NotContain(kill => kill.Group == objective.Group);

        new EshanSpawnDefinition().BuildSpawnTables()["ESHAN_SILVERWOOD_EXPANSE"].Spawns
            .Should().ContainSingle(spawn => spawn.Resref == "esh_gorakvesh");
    }

    [Test]
    public void Talia_HasValidOfferAcceptKillReportAndCompletedRoutes()
    {
        var graph = JsonConvert.DeserializeObject<ConversationGraph>(File.ReadAllText(Path.Combine(
            FindRepositoryRoot(), "SWLOR.Game.Server", "ConversationData", "esh_sec_talia.conversation.json")))!;
        ConversationGraphValidator.Validate(graph).Should().BeEmpty();

        foreach (var (choiceId, conditionKey, arguments) in new[]
                 {
                     ("reply-offer-11", "condition-can-accept-quest", new[] { QuestId }),
                     ("reply-remind-11", "condition-on-quest-state", new[] { QuestId, "1" }),
                     ("reply-ready-11", "condition-on-quest-state", new[] { QuestId, "2" }),
                     ("reply-done-11", "condition-completed-quest", new[] { QuestId })
                 })
        {
            var route = graph.Nodes["entry-hub"].Choices.Single(link => link.ChoiceId == choiceId);
            var condition = route.Conditions.Should().ContainSingle().Subject;
            condition.Key.Should().Be(conditionKey);
            condition.Arguments.Should().Equal(arguments);
            condition.IsNegated.Should().BeFalse();
            graph.Nodes.Should().ContainKey(graph.Choices[choiceId].Next.Single().TargetNodeId);
        }

        graph.Nodes["offer-11"].Choices.Should().Contain(link => link.ChoiceId == "reply-accept-11");
        var accept = graph.Choices["reply-accept-11"].Actions.Should().ContainSingle().Subject;
        accept.Key.Should().Be("action-accept-quest");
        accept.Arguments.Should().Equal(QuestId);
        graph.Nodes["ready-11"].Choices.Should().Contain(link => link.ChoiceId == "reply-turnin-11");
        var turnIn = graph.Choices["reply-turnin-11"].Actions.Should().ContainSingle().Subject;
        turnIn.Key.Should().Be("action-advance-quest");
        turnIn.Arguments.Should().Equal(QuestId);
        graph.Nodes.Values.Should().OnlyContain(node => node.OnEnterActions.Count == 0,
            "only explicit player choices should accept quests or claim rewards");

        using var talia = ReadJson("Module", "utc", "esh_sec_talia.utc.json");
        talia.RootElement.GetProperty("Conversation").GetProperty("value").GetString().Should().Be(graph.Id);
        talia.RootElement.GetProperty("ScriptDialogue").GetProperty("value").GetString().Should().Be("dialog_start");
    }

    private static JsonElement GetLocal(JsonElement root, string name) => root.GetProperty("VarTable")
        .GetProperty("value").EnumerateArray()
        .Single(variable => variable.GetProperty("Name").GetProperty("value").GetString() == name)
        .GetProperty("Value").GetProperty("value");

    private static JsonDocument ReadJson(params string[] path) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(new[] { FindRepositoryRoot() }.Concat(path).ToArray())));

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
