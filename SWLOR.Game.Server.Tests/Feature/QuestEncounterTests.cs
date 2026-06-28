using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.QuestService;

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
    public void KessActivatorBlueprint_UsesQuestEncounterScriptsAndQuestState()
    {
        var root = FindRepositoryRoot();
        using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root.FullName,
            "Module",
            "utp",
            "bf_kess_call.utp.json")));
        using var palette = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root.FullName,
            "Module",
            "itp",
            "placeablepalcus.itp.json")));

        var json = blueprint.RootElement;
        json.GetProperty("OnUsed").GetProperty("value").GetString().Should().Be("quest_enc");
        json.GetProperty("OnHeartbeat").GetProperty("value").GetString().Should().BeEmpty();
        json.GetProperty("LocName").GetProperty("value").GetProperty("0").GetString().Should().Be("Blood Frenzy Challenge Marker");
        json.GetProperty("Tag").GetProperty("value").GetString().Should().Be("bf_kess_call");
        json.GetProperty("TemplateResRef").GetProperty("value").GetString().Should().Be("bf_kess_call");
        json.GetProperty("TemplateResRef").GetProperty("value").GetString()!.Length.Should().BeLessThanOrEqualTo(16);
        GetLocalString(json, "QUEST_ID").Should().Be("blood_frenzy_mastery");
        GetLocalInt(json, "QUEST_STATE").Should().Be(1);
        GetLocalInt(json, "VISIBILITY_HIDDEN_DEFAULT").Should().Be(1);
        GetLocalString(json, "QUEST_ENCOUNTER_ID").Should().Be("blood_frenzy_mastery_kess");
        GetLocalString(json, "QUEST_ENCOUNTER_RESREF").Should().Be("bf_kess");
        GetLocalString(json, "QUEST_ENCOUNTER_WAYPOINT").Should().Be("BF_KESS_SPAWN");
        GetLocalInt(json, "QUEST_ENCOUNTER_COOLDOWN_MINUTES").Should().Be(60);
        GetLocalInt(json, "QUEST_ENCOUNTER_IDLE_MINUTES").Should().Be(10);

        json.GetProperty("VarTable")
            .GetProperty("value")
            .EnumerateArray()
            .Select(entry => entry.GetProperty("Name").GetProperty("value").GetString())
            .Should()
            .NotContain(name => name!.StartsWith("CAPSTONE_"));

        EnumerateResrefs(palette.RootElement).Should().Contain("bf_kess_call");
    }

    [Test]
    public void ButcherActivatorBlueprint_UsesQuestEncounterScriptsAndQuestState()
    {
        var root = FindRepositoryRoot();
        using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root.FullName,
            "Module",
            "utp",
            "bf_butch_call.utp.json")));
        using var palette = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root.FullName,
            "Module",
            "itp",
            "placeablepalcus.itp.json")));

        var json = blueprint.RootElement;
        json.GetProperty("OnUsed").GetProperty("value").GetString().Should().Be("quest_enc");
        json.GetProperty("OnHeartbeat").GetProperty("value").GetString().Should().BeEmpty();
        json.GetProperty("LocName").GetProperty("value").GetProperty("0").GetString().Should().Be("Blood Frenzy Butcher Marker");
        json.GetProperty("Tag").GetProperty("value").GetString().Should().Be("bf_butch_call");
        json.GetProperty("TemplateResRef").GetProperty("value").GetString().Should().Be("bf_butch_call");
        json.GetProperty("TemplateResRef").GetProperty("value").GetString()!.Length.Should().BeLessThanOrEqualTo(16);
        GetLocalString(json, "QUEST_ID").Should().Be("blood_frenzy_glass");
        GetLocalInt(json, "QUEST_STATE").Should().Be(1);
        GetLocalInt(json, "VISIBILITY_HIDDEN_DEFAULT").Should().Be(1);
        GetLocalString(json, "QUEST_ENCOUNTER_ID").Should().Be("blood_frenzy_glass_butcher");
        GetLocalString(json, "QUEST_ENCOUNTER_RESREF").Should().Be("bf_butcher");
        GetLocalString(json, "QUEST_ENCOUNTER_WAYPOINT").Should().Be("BF_BUTCHER_SPAWN");
        GetLocalInt(json, "QUEST_ENCOUNTER_COOLDOWN_MINUTES").Should().Be(60);
        GetLocalInt(json, "QUEST_ENCOUNTER_IDLE_MINUTES").Should().Be(10);

        EnumerateResrefs(palette.RootElement).Should().Contain("bf_butch_call");
    }

    [Test]
    public void KessSpawnWaypoint_IsAvailableInWaypointPalette()
    {
        var root = FindRepositoryRoot();
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
        var root = FindRepositoryRoot();
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
        var root = FindRepositoryRoot();
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
        var root = FindRepositoryRoot();
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

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
