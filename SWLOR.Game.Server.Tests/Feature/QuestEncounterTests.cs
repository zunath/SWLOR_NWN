using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Feature;

public class QuestEncounterTests
{
    [Test]
    public void IsPlayerOnQuestState_RequiresExactQuestState()
    {
        var player = new Player("player-id");
        player.Quests["blood_frenzy_mastery"] = new PlayerQuest
        {
            CurrentState = 1,
            TimesCompleted = 0,
        };

        QuestEncounter.IsPlayerOnQuestState(player, "blood_frenzy_mastery", 1)
            .Should()
            .BeTrue();

        QuestEncounter.IsPlayerOnQuestState(player, "blood_frenzy_mastery", 2)
            .Should()
            .BeFalse();
    }

    [Test]
    public void GetCooldownRemaining_ReturnsRemainingEncounterCooldown()
    {
        var now = new DateTime(2026, 6, 27, 12, 0, 0, DateTimeKind.Utc);
        var player = new Player("player-id");
        player.EncounterCooldowns["blood_frenzy_mastery_kess"] = now.AddMinutes(60);

        QuestEncounter.GetCooldownRemaining(player, "blood_frenzy_mastery_kess", now.AddMinutes(15))
            .Should()
            .Be(TimeSpan.FromMinutes(45));

        QuestEncounter.GetCooldownRemaining(player, "blood_frenzy_mastery_kess", now.AddMinutes(60))
            .Should()
            .Be(TimeSpan.Zero);
    }

    [Test]
    public void IsIdleExpired_RequiresFullIdleTimeout()
    {
        var lastActivity = new DateTime(2026, 6, 27, 12, 0, 0, DateTimeKind.Utc);

        QuestEncounter.IsIdleExpired(lastActivity, lastActivity.AddMinutes(9), TimeSpan.FromMinutes(10))
            .Should()
            .BeFalse();

        QuestEncounter.IsIdleExpired(lastActivity, lastActivity.AddMinutes(10), TimeSpan.FromMinutes(10))
            .Should()
            .BeTrue();
    }

    [Test]
    public void KessSpawnWaypoint_IsAvailableInWaypointPalette()
    {
        var root = RepoPaths.FindRepositoryRoot();
        using var waypoint = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root.FullName,
            "Module",
            "utw",
            "bf_kess_spawn.utw.json")));
        using var palette = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root.FullName,
            "Module",
            "itp",
            "waypointpalcus.itp.json")));

        var json = waypoint.RootElement;
        json.GetProperty("__data_type").GetString().Should().Be("UTW ");
        json.GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString().Should().Be("Viscara Sewers Depths - Kess Spawn");
        json.GetProperty("Tag").GetProperty("value").GetString().Should().Be("BF_KESS_SPAWN");
        json.GetProperty("TemplateResRef").GetProperty("value").GetString().Should().Be("bf_kess_spawn");

        EnumerateResrefs(palette.RootElement).Should().Contain("bf_kess_spawn");
    }

    [Test]
    public void ButcherSpawnWaypoint_IsAvailableInWaypointPalette()
    {
        var root = RepoPaths.FindRepositoryRoot();
        using var waypoint = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root.FullName,
            "Module",
            "utw",
            "bf_butch_spawn.utw.json")));
        using var palette = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root.FullName,
            "Module",
            "itp",
            "waypointpalcus.itp.json")));

        var json = waypoint.RootElement;
        json.GetProperty("__data_type").GetString().Should().Be("UTW ");
        json.GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString().Should().Be("Viscara Sewers Depths - Butcher Spawn");
        json.GetProperty("Tag").GetProperty("value").GetString().Should().Be("BF_BUTCHER_SPAWN");
        json.GetProperty("TemplateResRef").GetProperty("value").GetString().Should().Be("bf_butch_spawn");
        json.GetProperty("TemplateResRef").GetProperty("value").GetString()!.Length.Should().BeLessThanOrEqualTo(16);

        EnumerateResrefs(palette.RootElement).Should().Contain("bf_butch_spawn");
    }

    [Test]
    public void QuestKillDispatch_UsesParticipantCreditForQuestEncounterCreatures()
    {
        var root = RepoPaths.FindRepositoryRoot();
        var questSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Quest.cs"));

        questSource.Should().Contain("QuestEncounter.IsQuestEncounterCreature(creature)");
        questSource.Should().Contain("QuestEncounter.ProgressKillCredit(creature, npcGroupType, possibleQuests);");
        questSource.Should().Contain("QuestEncounter.ClearEncounterForCreature(creature);");
    }

    [Test]
    public void QuestEncounterVisibility_DoesNotUseHeartbeat()
    {
        var root = RepoPaths.FindRepositoryRoot();
        var scriptNames = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Core",
            "ScriptName.cs"));
        var encounterSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "QuestService",
            "QuestEncounter.cs"));
        var questDetailSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "QuestService",
            "QuestDetail.cs"));

        scriptNames.Should().NotContain("OnQuestEncounterHeartbeat");
        encounterSource.Should().NotContain("OnQuestEncounterHeartbeat");
        encounterSource.Should().NotContain("Capstone");
        encounterSource.Should().NotContain("CAPSTONE_");
        encounterSource.Should().NotContain("Quest" + "EncounterCooldowns");
        encounterSource.Should().Contain("IsQuestEncounterActivator(obj)");
        encounterSource.Should().Contain("Log.Write(LogGroup.Error");
        questDetailSource.Should().Contain("QuestEncounter.RefreshVisibilityForPlayer(player);");
    }

    private static string GetLocalString(JsonElement json, string variableName)
    {
        return GetLocal(json, variableName).GetProperty("Value").GetProperty("value").GetString()!;
    }

    private static int GetLocalInt(JsonElement json, string variableName)
    {
        return GetLocal(json, variableName).GetProperty("Value").GetProperty("value").GetInt32();
    }

    private static JsonElement GetLocal(JsonElement json, string variableName)
    {
        return json.GetProperty("VarTable")
            .GetProperty("value")
            .EnumerateArray()
            .Single(entry => entry.GetProperty("Name").GetProperty("value").GetString() == variableName);
    }

    private static IEnumerable<string> EnumerateResrefs(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var resref in EnumerateResrefs(item))
                {
                    yield return resref;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("RESREF", out var resref))
            {
                yield return resref.GetProperty("value").GetString()!;
            }

            foreach (var property in element.EnumerateObject())
            {
                foreach (var nestedResref in EnumerateResrefs(property.Value))
                {
                    yield return nestedResref;
                }
            }
        }
    }

}
