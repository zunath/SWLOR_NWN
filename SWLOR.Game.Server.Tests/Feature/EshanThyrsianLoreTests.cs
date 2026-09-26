using System.Text.Json;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.QuestDefinition;
using SWLOR.Game.Server.Service.NPCService;

namespace SWLOR.Game.Server.Tests.Feature;

public class EshanThyrsianLoreTests
{
    [Test]
    public void SecurityQuest_IdentifiesTheAttackersAsFormerRevaniteThyrsians()
    {
        var quest = new EshanQuestDefinition().BuildQuests()["eshan_sunguard_stand"];

        quest.Name.Should().Be("Thyrsian Incursion");
        quest.States[0].JournalText.Should().Contain("former Revanite Thyrsian warriors");
        quest.States.Values.Select(state => state.JournalText)
            .Should().NotContain(text => text.Contains("Sun Guard"));

        var conversation = File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName,
            "SWLOR.Game.Server", "ConversationData", "esh_sec_talia.conversation.json"));
        conversation.Should().Contain("Thyrsian warriors who once served the Revanite Order");
        conversation.Should().NotContain("Sun Guard");
    }

    [Test]
    public void ThyrsianEnemy_UsesTheCorrectLoreNameAndDescription()
    {
        using var creature = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName, "Module", "utc", "esh_sunguard.utc.json")));
        var root = creature.RootElement;

        root.GetProperty("FirstName").GetProperty("value").GetProperty("0").GetString()
            .Should().Be("Thyrsian Exile");
        root.GetProperty("Description").GetProperty("value").GetProperty("0").GetString()
            .Should().Contain("once served the Revanite Order");
    }

    [Test]
    public void LegacySunGuardKillProgress_StillDeserializes()
    {
        const string persistedQuest = "{\"KillProgresses\":{\"Eshan_SunGuard\":3}}";

        var quest = JsonConvert.DeserializeObject<PlayerQuest>(persistedQuest)!;

        quest.KillProgresses.Should().ContainSingle();
        quest.KillProgresses[NPCGroupType.Eshan_SunGuard].Should().Be(3);
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        return directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
