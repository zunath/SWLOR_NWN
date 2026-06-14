using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.SnippetDefinition;

namespace SWLOR.Game.Server.Tests.Feature;

public class QuestSnippetDefinitionTests
{
    [Test]
    public void QuestItemRecoveryQuantity_UsesRemainingQuestProgress()
    {
        var quest = new PlayerQuest();
        quest.ItemProgresses["qi_dantooine_004"] = 1;

        CalculateQuestItemRecoveryQuantity(quest, "qi_dantooine_004", markerQuantity: 1, inventoryQuantity: 0)
            .Should()
            .Be(1, "an active collect objective with no carried item should generate the pickup item");

        CalculateQuestItemRecoveryQuantity(quest, "qi_dantooine_004", markerQuantity: 1, inventoryQuantity: 1)
            .Should()
            .Be(0, "the pickup gate should count matching items already in the player's inventory");

        CalculateQuestItemRecoveryQuantity(quest, "qi_dantooine_004", markerQuantity: 3, inventoryQuantity: 0)
            .Should()
            .Be(1, "the pickup gate should never create more than the active quest still requires");

        quest.ItemProgresses["qi_dantooine_004"] = 0;
        CalculateQuestItemRecoveryQuantity(quest, "qi_dantooine_004", markerQuantity: 1, inventoryQuantity: 0)
            .Should()
            .Be(0, "a recovered and turned-in pickup item should not be generated again while other objectives remain");

        CalculateQuestItemRecoveryQuantity(quest, "missing_item", markerQuantity: 1, inventoryQuantity: 0)
            .Should()
            .Be(0, "items outside the active quest progress should not be generated");
    }

    private static int CalculateQuestItemRecoveryQuantity(
        PlayerQuest quest,
        string itemResref,
        int markerQuantity,
        int inventoryQuantity)
    {
        var method = typeof(QuestSnippetDefinition).GetMethod(
            "CalculateQuestItemRecoveryQuantity",
            BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull();
        return (int)method.Invoke(null, new object[] { quest, itemResref, markerQuantity, inventoryQuantity });
    }
}
