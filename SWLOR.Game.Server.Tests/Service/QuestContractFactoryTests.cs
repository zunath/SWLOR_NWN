using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.QuestContractService;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Tests.Service;

public class QuestContractFactoryTests
{
    [Test]
    public void BuildQuestId_PrependsContractPrefix()
    {
        QuestContractFactory.BuildQuestId("abc123").Should().Be("qcontract_abc123");
    }

    [Test]
    public void BuildQuest_MapsNameAndDisablesRepeatAndRewardSelection()
    {
        var contract = CreateContract();

        var quest = QuestContractFactory.BuildQuest(contract);

        quest.QuestId.Should().Be(QuestContractFactory.BuildQuestId(contract.Id));
        quest.Name.Should().Be($"Contract: {contract.Title}");
        quest.IsRepeatable.Should().BeFalse();
        quest.AllowRewardSelection.Should().BeFalse();
        quest.CountsTowardAchievements.Should().BeFalse();
        quest.CollectedItemHandler.Should().NotBeNull();
    }

    [Test]
    public void BuildQuest_CreatesSingleStateWithOneCollectItemObjectivePerContractObjective()
    {
        var contract = CreateContract();

        var quest = QuestContractFactory.BuildQuest(contract);

        quest.States.Should().ContainSingle().Which.Key.Should().Be(1);
        var objectives = quest.States[1].GetObjectives().Cast<CollectItemObjective>().ToList();
        objectives.Should().HaveCount(2);

        objectives[0].Resref.Should().Be("wpn_blaster");
        GetObjectiveQuantity(objectives[0]).Should().Be(3);
        objectives[0].ProducerRequirement.Should().Be(CollectItemProducerRequirementType.None);

        objectives[1].Resref.Should().Be("crafted_part");
        GetObjectiveQuantity(objectives[1]).Should().Be(5);
        objectives[1].ProducerRequirement.Should().Be(CollectItemProducerRequirementType.None);
    }

    [Test]
    public void BuildQuest_AddsContractPrerequisiteAndReward()
    {
        var contract = CreateContract();

        var quest = QuestContractFactory.BuildQuest(contract);

        quest.Prerequisites.Should().ContainSingle();
        var prerequisite = quest.Prerequisites[0].Should().BeOfType<QuestContractPrerequisite>().Which;
        GetPrivateField(prerequisite, "_contractId").Should().Be(contract.Id);

        quest.Rewards.Should().ContainSingle();
        var reward = quest.Rewards[0].Should().BeOfType<QuestContractReward>().Which;
        GetPrivateField(reward, "_contractId").Should().Be(contract.Id);
        reward.IsSelectable.Should().BeFalse();
    }

    [Test]
    public void BuildQuest_JournalTextContainsDescriptionAndObjectiveLines()
    {
        var contract = CreateContract();

        var quest = QuestContractFactory.BuildQuest(contract);

        var journalText = quest.States[1].JournalText;
        journalText.Should().Contain("This is a player-posted contract.");
        journalText.Should().Contain(contract.Description);
        journalText.Should().Contain("3x Blaster Rifle");
        journalText.Should().Contain("5x Crafted Part");
    }

    [Test]
    public void BuildQuest_PreservesMaximumStackQuantity()
    {
        var contract = CreateContract();
        contract.Objectives[0].Quantity = QuestContractBoard.MaxObjectiveQuantity;

        var quest = QuestContractFactory.BuildQuest(contract);
        var objective = quest.States[1].GetObjectives().Cast<CollectItemObjective>().First();

        GetObjectiveQuantity(objective).Should().Be(QuestContractBoard.MaxObjectiveQuantity);
        quest.States[1].JournalText.Should().Contain("99x Blaster Rifle");
    }

    private static QuestContract CreateContract()
    {
        return new QuestContract
        {
            Title = "Bounty: Blaster Rifles",
            Description = "Bring me what I need.",
            Objectives = new List<QuestContractObjective>
            {
                new QuestContractObjective
                {
                    ItemResref = "wpn_blaster",
                    ItemName = "Blaster Rifle",
                    Quantity = 3
                },
                new QuestContractObjective
                {
                    ItemResref = "crafted_part",
                    ItemName = "Crafted Part",
                    Quantity = 5
                }
            }
        };
    }

    private static int GetObjectiveQuantity(CollectItemObjective objective)
    {
        return (int)typeof(CollectItemObjective)
            .GetField("_quantity", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(objective)!;
    }

    private static string GetPrivateField(object instance, string fieldName)
    {
        return (string)instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(instance)!;
    }
}
