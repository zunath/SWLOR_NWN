using System.Reflection;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.QuestDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Feature;

public class BeastStableQuestTests
{
    private const string TrialOne = "stablemaster_trial_1";
    private const string TrialTwo = "stablemaster_trial_2";
    private object _originalItemNames = null!;

    private static PropertyInfo ItemNameCache => typeof(Cache)
        .GetProperty("ItemNamesByResref", BindingFlags.NonPublic | BindingFlags.Static)!;

    [OneTimeSetUp]
    public void LoadRewardItemNames()
    {
        // Item rewards read their display names from the engine cache; seed it for the two field note rewards.
        _originalItemNames = ItemNameCache.GetValue(null)!;
        ItemNameCache.SetValue(null, new Dictionary<string, string>
        {
            ["fnote_2039"] = "Field Note: Dewback",
            ["fnote_2091"] = "Field Note: Ronto",
        });
    }

    [OneTimeTearDown]
    public void RestoreItemNames() => ItemNameCache.SetValue(null, _originalItemNames);

    [TestCase(0, 0, 1)]
    [TestCase(1, 0, 2)]
    [TestCase(5, 0, 6)]
    [TestCase(0, 1, 2)]
    [TestCase(5, 1, 7)]
    [TestCase(5, 2, 8)]
    [TestCase(5, 3, 8)]
    [TestCase(-1, -1, 1)]
    public void Capacity_IsOnePlusStablingPlusCompletedTrials(int stabling, int completedTrials, int expected)
    {
        BeastStableCapacity.Calculate(stabling, completedTrials).Should().Be(expected);
    }

    [Test]
    public void CompletedTrials_CountOnlyFinishedStablemasterQuests()
    {
        var player = new Player("stable-capacity-test");
        BeastStableCapacity.CountCompletedStableQuests(player).Should().Be(0);

        player.Quests[TrialOne] = new PlayerQuest { CurrentState = 1 };
        player.Quests["wardrobe_expansion_1"] = new PlayerQuest { CurrentState = 2, TimesCompleted = 1 };
        BeastStableCapacity.CountCompletedStableQuests(player).Should().Be(0, "an accepted trial and unrelated quests add no slot");

        player.Quests[TrialOne] = new PlayerQuest { CurrentState = 2, TimesCompleted = 1 };
        BeastStableCapacity.CountCompletedStableQuests(player).Should().Be(1);

        player.Quests[TrialTwo] = new PlayerQuest { CurrentState = 2, TimesCompleted = 1 };
        BeastStableCapacity.CountCompletedStableQuests(player).Should().Be(2);
        BeastStableCapacity.CountCompletedStableQuests(null).Should().Be(0);
    }

    [TestCase(39)]
    [TestCase(40, "stablemaster_dna_40")]
    [TestCase(44, "stablemaster_dna_40")]
    [TestCase(45, "stablemaster_dna_40", "stablemaster_dna_45")]
    [TestCase(50, "stablemaster_dna_40", "stablemaster_dna_45")]
    public void DnaExtraction_CreditsTheTrialsWhoseLevelItMeets(int beastLevel, params string[] expected)
    {
        BeastStableTrials.GetDnaObjectiveKeys(beastLevel).Should().Equal(expected);
    }

    [Test]
    public void Trials_AreAOneTimeBeastMasteryLineWithBeastFightsAndDnaWork()
    {
        var quests = new BeastStableQuestDefinition().BuildQuests();
        quests.Keys.Should().BeEquivalentTo(TrialOne, TrialTwo);

        foreach (var quest in quests.Values)
        {
            quest.IsRepeatable.Should().BeFalse("each part grants a permanent stable slot");
            var gate = quest.Prerequisites.OfType<RequiredSkillRankPrerequisite>().Single();
            gate.SkillType.Should().Be(SkillType.BeastMastery);
            gate.RequiredRank.Should().Be(50);
            quest.States.Keys.Should().Equal(1, 2);
            quest.States[2].GetObjectives().Should().BeEmpty("state 2 is the turn-in");
            quest.Rewards.OfType<XPReward>().Should().ContainSingle();
            quest.Rewards.OfType<GoldReward>().Should().ContainSingle();
            quest.Rewards.OfType<ItemReward>().Should().ContainSingle();
        }

        quests[TrialOne].Prerequisites.OfType<RequiredQuestPrerequisite>().Should().BeEmpty();
        quests[TrialTwo].Prerequisites.OfType<RequiredQuestPrerequisite>().Single().QuestId.Should().Be(TrialOne);

        var trialOne = quests[TrialOne].States[1].GetObjectives().ToArray();
        trialOne.OfType<BeastAssistedKillObjective>().Select(kill => (kill.Group, kill.Amount))
            .Should().Equal((NPCGroupType.Tatooine_SandDemon, 8));
        trialOne.OfType<CombatObjective>().Select(dna => (dna.Key, dna.Amount))
            .Should().Equal(("stablemaster_dna_40", 3));

        var trialTwo = quests[TrialTwo].States[1].GetObjectives().ToArray();
        trialTwo.OfType<BeastAssistedKillObjective>().Select(kill => (kill.Group, kill.Amount))
            .Should().BeEquivalentTo(new[] { (NPCGroupType.Dathomir_Ssurian, 6), (NPCGroupType.Tatooine_AncientSandworm, 1) });
        trialTwo.OfType<CombatObjective>().Select(dna => (dna.Key, dna.Amount))
            .Should().Equal(("stablemaster_dna_45", 3));
        quests[TrialTwo].OnCompleteActions.Should().ContainSingle("the final part grants the Stablemaster achievement");

        foreach (var quest in quests.Values)
        {
            quest.States[1].JournalText.Should().ContainEquivalentOf("your own beast must hit").And.Contain("Dorra Vesk");
            quest.States[2].JournalText.Should().Contain("Return to Dorra Vesk").And.Contain("stable slot");
        }

        var root = FindRoot();
        var source = File.ReadAllText(Path.Combine(root, "SWLOR.Game.Server", "Feature", "QuestDefinition", "BeastStableQuestDefinition.cs"));
        source.Should().Contain("AddItemReward(\"fnote_2039\", 1)").And.Contain("AddItemReward(\"fnote_2091\", 1)");
        File.Exists(Path.Combine(root, "Module", "uti", "fnote_2039.uti.json")).Should().BeTrue();
        File.Exists(Path.Combine(root, "Module", "uti", "fnote_2091.uti.json")).Should().BeTrue();
    }

    [Test]
    public void StableCapacity_IsComputedInOnePlace()
    {
        var root = FindRoot();
        var server = Path.Combine(root, "SWLOR.Game.Server");
        var callers = new[]
        {
            Path.Combine(server, "Feature", "AbilityDefinition", "Beastmaster", "TameAbilityDefinition.cs"),
            Path.Combine(server, "Feature", "ItemDefinition", "BeastEggItemDefinition.cs"),
            Path.Combine(server, "Feature", "GuiDefinition", "ViewModel", "StablesViewModel.cs"),
        };

        foreach (var caller in callers)
            File.ReadAllText(caller).Should().Contain("BeastStableCapacity.GetCapacity(", caller);

        foreach (var file in Directory.GetFiles(server, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            text.Should().NotMatchRegex(@"GetPerkLevel\([^)]*PerkType\.Stabling\)\s*\+\s*1", file);
            text.Should().NotMatchRegex(@"1\s*\+\s*Perk\.GetPerkLevel\([^)]*PerkType\.Stabling\)", file);
        }

        var stables = File.ReadAllText(callers[2]);
        stables.Should().Contain("IGuiRefreshable<QuestCompletedRefreshEvent>");
        stables.Should().Contain("BeastStableCapacity.IsStableSlotQuest(payload.QuestId)");
    }

    [Test]
    public void DorraVesk_IsPlacedInAnchorheadAndRunsBothTrials()
    {
        var root = FindRoot();
        var graph = JsonConvert.DeserializeObject<ConversationGraph>(File.ReadAllText(Path.Combine(root,
            "SWLOR.Game.Server", "ConversationData", "dorra_vesk.conversation.json")))!;
        ConversationGraphValidator.Validate(graph).Should().BeEmpty();

        var actions = graph.Choices.Values.SelectMany(choice => choice.Actions.Select(action => (action.Key, action.Arguments.Single())))
            .ToArray();
        actions.Should().BeEquivalentTo(new[]
        {
            ("action-accept-quest", TrialOne), ("action-advance-quest", TrialOne),
            ("action-accept-quest", TrialTwo), ("action-advance-quest", TrialTwo),
        });

        var allText = string.Join("\n", graph.Nodes.Values.SelectMany(node => node.Text.Select(text => text.Text))
            .Concat(graph.Choices.Values.Select(choice => choice.Text.Text)));
        allText.All(character => character < 128).Should().BeTrue("game text uses ASCII punctuation");
        allText.Should().Contain("Beast Mastery reaches 50").And.Contain("one more stable slot");

        var blueprint = JObject.Parse(File.ReadAllText(Path.Combine(root, "Module", "utc", "dorra_vesk.utc.json")));
        blueprint["Conversation"]!["value"]!.Value<string>().Should().Be("dorra_vesk");
        blueprint["ScriptDialogue"]!["value"]!.Value<string>().Should().Be("dialog_start");
        blueprint["Plot"]!["value"]!.Value<int>().Should().Be(1);

        var area = JObject.Parse(File.ReadAllText(Path.Combine(root, "Module", "git", "tat_anc_southdis.git.json")));
        var placed = area["Creature List"]!["value"]!.Children<JObject>()
            .Where(creature => creature["TemplateResRef"]?["value"]?.Value<string>() == "dorra_vesk").ToArray();
        placed.Should().ContainSingle("Dorra Vesk stands once in the neutral Anchorhead South District");
        placed[0]["Conversation"]!["value"]!.Value<string>().Should().Be("dorra_vesk");
        placed[0]["ScriptDialogue"]!["value"]!.Value<string>().Should().Be("dialog_start");
        placed[0]["Tag"]!["value"]!.Value<string>().Should().Be("dorra_vesk");
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
