using System.Text.Json;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.QuestDefinition;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Tests.Feature;

public class EshanSupplyQuestTests
{
    private static readonly DateTime CompletedAt = new(2026, 10, 13, 12, 0, 0, DateTimeKind.Utc);

    private static readonly Dictionary<string, string> Givers = new()
    {
        ["eshan_starwort_hands"] = "hc_kael",
        ["eshan_fountain_parts"] = "hc_tellis",
        ["eshan_service_meds"] = "hc_aelis",
        ["eshan_practice_sleeves"] = "hc_vesa",
        ["eshan_shift_greens"] = "hc_nema",
        ["eshan_grove_seedlings"] = "hc_sivra"
    };

    [Test]
    public void Tellis_BlueprintAndPlacedQuestGiverRemainStationary()
    {
        var root = FindRepositoryRoot();
        using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName,
            "Module", "utc", "hc_tellis.utc.json")));
        using var highcrest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName,
            "Module", "git", "pw_ar_sc_eshancm.git.json")));
        var placed = highcrest.RootElement.GetProperty("Creature List").GetProperty("value").EnumerateArray()
            .Single(creature => creature.GetProperty("TemplateResRef").GetProperty("value").GetString() == "hc_tellis");

        foreach (var creature in new[] { blueprint.RootElement, placed })
        {
            var variables = creature.GetProperty("VarTable").GetProperty("value").EnumerateArray().ToArray();
            var flags = variables.Single(variable => variable.GetProperty("Name").GetProperty("value").GetString() == "AI_FLAGS");
            flags.GetProperty("Value").GetProperty("value").GetInt32().Should().Be(0,
                "random-walk and return-home actions can take Tellis beyond the quest window's interaction range");
            variables.Should().NotContain(variable =>
                variable.GetProperty("Name").GetProperty("value").GetString() == "ESHAN_PATROL_ROUTE");
            creature.GetProperty("Conversation").GetProperty("value").GetString().Should().Be("hc_tellis");
            creature.GetProperty("ScriptDialogue").GetProperty("value").GetString().Should().Be("dialog_start");
        }
    }

    [Test]
    public void SupplyQuests_UseFetchObjectivesAndModestRewards()
    {
        var quests = new EshanSupplyQuestDefinition().BuildQuests();
        quests.Keys.Should().BeEquivalentTo(Givers.Keys);
        AssertOrder(quests["eshan_starwort_hands"], false, 2200, 1500, ("dant_starwort", 3));
        AssertOrder(quests["eshan_fountain_parts"], false, 2000, 1400, ("scrap_metal", 6), ("elec_good", 4));
        AssertOrder(quests["eshan_service_meds"], false, 1800, 1200, ("kolto_injection", 4), ("medisyringes", 2));
        AssertOrder(quests["eshan_practice_sleeves"], true, 650, 400, ("fiberp_good", 8));
        AssertOrder(quests["eshan_shift_greens"], true, 600, 400, ("esh_greens", 6), ("esh_scallion", 4));
        AssertOrder(quests["eshan_grove_seedlings"], true, 700, 350, ("esh_seeds", 6), ("fiberp_good", 4));

        var dailyXP = quests.Values.Where(quest => quest.IsRepeatable)
            .Sum(quest => quest.Rewards.OfType<XPReward>().Single().Amount);
        var wolfXP = new EshanQuestDefinition().BuildQuests()["eshan_wolf_line"]
            .Rewards.OfType<XPReward>().Single().Amount;
        dailyXP.Should().BeLessThan(wolfXP, "all three supply orders together should pay less XP than one Eshan hunting quest");
    }

    private static void AssertOrder(QuestDetail quest, bool repeatable, int xp, int credits,
        params (string Resref, int Quantity)[] items)
    {
        quest.IsRepeatable.Should().Be(repeatable, quest.QuestId);
        quest.States.Keys.Should().Equal(1, 2);
        quest.States[1].GetObjectives().Should().OnlyContain(objective => objective is CollectItemObjective);
        quest.States[1].GetObjectives().OfType<CollectItemObjective>()
            .Select(objective => (objective.Resref, objective.Quantity)).Should().BeEquivalentTo(items);
        quest.States[2].GetObjectives().Should().BeEmpty("the collector advances to the giver's payment step");
        quest.Rewards.OfType<XPReward>().Should().ContainSingle().Which.Amount.Should().Be(xp);
        quest.Rewards.OfType<GoldReward>().Should().ContainSingle().Which.Amount.Should().Be(credits);
        quest.States.Values.Should().OnlyContain(state => state.JournalText.Contains("Highcrest Quarter"));
        if (repeatable)
        {
            quest.Prerequisites.Should().ContainSingle().Which.Should().BeOfType<EshanDailySupplyPrerequisite>();
            quest.States.Values.Should().OnlyContain(state => state.JournalText.Contains("24 real-world hours"));
        }
        else
            quest.Prerequisites.Should().BeEmpty();
    }

    [Test]
    public void DailyOrder_FirstAttemptIsAllowedButActiveAttemptsAreRejected()
    {
        EshanDailySupplyPrerequisite.IsEligible(null, CompletedAt).Should().BeTrue();
        EshanDailySupplyPrerequisite.IsEligible(new PlayerQuest { CurrentState = 1 }, CompletedAt)
            .Should().BeFalse();
        EshanDailySupplyPrerequisite.IsEligible(new PlayerQuest { CurrentState = 1, TimesCompleted = 4 }, CompletedAt)
            .Should().BeFalse("acceptance clears the date even after previous completions");
        EshanDailySupplyPrerequisite.IsEligible(new PlayerQuest { CurrentState = 2, TimesCompleted = 4 }, CompletedAt)
            .Should().BeFalse("delivered supplies do not permit another order before collecting payment");
    }

    [TestCase(0, false)]
    [TestCase(86399, false)]
    [TestCase(86400, true)]
    [TestCase(86401, true)]
    public void DailyOrder_RequiresTwentyFourElapsedHours(int secondsElapsed, bool expected)
    {
        var quest = new PlayerQuest { CurrentState = 2, TimesCompleted = 1, DateLastCompleted = CompletedAt };
        EshanDailySupplyPrerequisite.IsEligible(quest, CompletedAt.AddSeconds(secondsElapsed)).Should().Be(expected);
    }

    [Test]
    public void DailyOrder_ClockRollbackAndCalendarRolloverDoNotOpenAnOrderEarly()
    {
        var quest = new PlayerQuest { TimesCompleted = 1, DateLastCompleted = CompletedAt };
        EshanDailySupplyPrerequisite.IsEligible(quest, CompletedAt.AddSeconds(-1)).Should().BeFalse();
        EshanDailySupplyPrerequisite.IsEligible(quest, CompletedAt.Date.AddDays(1)).Should().BeFalse(
            "passing midnight is not a full day since the previous payment");
    }

    [Test]
    public void DailyOrder_ReloadedCompletionHistoryKeepsTheSameWait()
    {
        var quest = new PlayerQuest { CurrentState = 2, TimesCompleted = 3, DateLastCompleted = CompletedAt };
        var reloaded = JsonConvert.DeserializeObject<PlayerQuest>(JsonConvert.SerializeObject(quest));
        EshanDailySupplyPrerequisite.IsEligible(reloaded, CompletedAt.AddHours(23)).Should().BeFalse();
        EshanDailySupplyPrerequisite.IsEligible(reloaded, CompletedAt.AddHours(24)).Should().BeTrue();
    }

    [Test]
    public void DailyOrder_CancellationTimestampRestartsTheWait()
    {
        // QuestDetail.Abandon preserves completion counts and stamps a repeated order with now.
        var cancelledAt = CompletedAt.AddDays(2);
        var cancelled = new PlayerQuest { TimesCompleted = 1, DateLastCompleted = cancelledAt };
        EshanDailySupplyPrerequisite.IsEligible(cancelled, cancelledAt.AddHours(23)).Should().BeFalse();
        EshanDailySupplyPrerequisite.IsEligible(cancelled, cancelledAt.AddHours(24)).Should().BeTrue();
    }

    [Test]
    public void SupplyConversations_ResumeCollectionAndPaymentWithoutReacceptingActiveOrders()
    {
        foreach (var (questId, graphId) in Givers)
        {
            var graph = ReadGraph(graphId);
            ConversationGraphValidator.Validate(graph).Should().BeEmpty(graphId);
            graph.Nodes.Values.SelectMany(node => node.OnEnterActions).Should().BeEmpty(
                "lore and greetings must not accept, advance, or consume quest supplies");
            ReachableActions(graph, questId, 0, true).Should().Equal("action-accept-quest");
            ReachableActions(graph, questId, 1, false).Should().Equal("action-request-quest-items");
            ReachableActions(graph, questId, 2, false).Should().Equal("action-advance-quest");
            ReachableActions(graph, questId, 0, false).Should().BeEmpty(
                "completed one-time quests and daily orders on cooldown cannot be accepted or paid again");

            graph.Choices.Where(pair => !pair.Key.StartsWith("supply-"))
                .SelectMany(pair => pair.Value.Actions).Should().OnlyContain(action => action.Key == "action-open-store",
                    "existing lore and shop choices retain their original behavior");
        }
    }

    private static IEnumerable<string> ReachableActions(ConversationGraph graph, string questId, int state, bool canAccept)
    {
        bool Matches(IEnumerable<ConversationCondition> conditions) => conditions.All(condition =>
        {
            condition.Arguments[0].Should().Be(questId);
            var result = condition.Key switch
            {
                "condition-on-quest-state" => state > 0 && condition.Arguments.Skip(1).Contains(state.ToString()),
                "condition-has-quest" => state > 0,
                "condition-can-accept-quest" => canAccept,
                _ => throw new InvalidOperationException($"Unexpected supply condition {condition.Key}")
            };
            return condition.IsNegated ? !result : result;
        });

        var first = graph.EntryPoints.First(route => Matches(route.Conditions));
        var pending = new Stack<string>([first.TargetNodeId]);
        var visited = new HashSet<string>();
        while (pending.TryPop(out var id))
        {
            if (!visited.Add(id)) continue;
            foreach (var link in graph.Nodes[id].Choices.Where(link => Matches(link.Conditions)))
            {
                var choice = graph.Choices[link.ChoiceId];
                foreach (var action in choice.Actions.Where(action => action.Arguments.SequenceEqual(new[] { questId })))
                    yield return action.Key;
                var next = choice.Next.FirstOrDefault(route => Matches(route.Conditions));
                if (next != null) pending.Push(next.TargetNodeId);
            }
        }
    }

    private static ConversationGraph ReadGraph(string id)
    {
        var directory = FindRepositoryRoot();
        return JsonConvert.DeserializeObject<ConversationGraph>(File.ReadAllText(Path.Combine(directory.FullName,
            "SWLOR.Game.Server", "ConversationData", id + ".conversation.json")));
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module")))
            directory = directory.Parent;
        return directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
