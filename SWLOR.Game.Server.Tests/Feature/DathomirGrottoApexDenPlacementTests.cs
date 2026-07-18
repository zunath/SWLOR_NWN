using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.KeyItemService;

namespace SWLOR.Game.Server.Tests.Feature;

public class DathomirGrottoApexDenPlacementTests
{
    private const string CavernAreaFile = "dathgrottocavern.git.json";
    private const string DungeonAreaFile = "pw_sc_dath_apexd.git.json";
    private const string ArenaAreaFile = "pw_sc_dath_sden.git.json";

    [Test]
    public void ApexDenAccessLock_UsesStandardTeleportKeyItemGate()
    {
        ((int)KeyItemType.CapstoneDathomirGrottoApexDenKey).Should().Be(119);

        using var cavern = LoadModuleJson("git", CavernAreaFile);

        var gate = EnumerateObjects(cavern.RootElement)
            .Single(element =>
                GetString(element, "OnUsed") == "teleport" &&
                GetOptionalLocalString(element, "DESTINATION") == "DATH_APEX_DEN_INSIDE");

        gate.GetProperty("OnHeartbeat").GetProperty("value").GetString().Should().BeEmpty();
        gate.GetProperty("Tag").GetProperty("value").GetString().Should().Be("tele_obj");
        gate.GetProperty("TemplateResRef").GetProperty("value").GetString().Should().Be("tele_obj");
        gate.GetProperty("LocName").GetProperty("value").GetProperty("0").GetString()
            .Should().Be("Enter the Grotto Apex Den");
        GetLocalInt(gate, "KEY_ITEM_ID").Should().Be((int)KeyItemType.CapstoneDathomirGrottoApexDenKey);
        GetLocalInt(gate, "TELEPORT_PARTY_MEMBERS").Should().Be(1);
        GetLocalString(gate, "MISSING_KEY_ITEM_MESSAGE").Should().Contain("Grotto Apex Den");
        GetLocalString(gate, "MISSING_KEY_ITEM_MESSAGE").Should().Contain("key");
    }

    [Test]
    public void ApexDenEntryAndExit_ArePlacedWithMatchingWaypoints()
    {
        using var cavern = LoadModuleJson("git", CavernAreaFile);
        using var dungeon = LoadModuleJson("git", DungeonAreaFile);
        using var arena = LoadModuleJson("git", ArenaAreaFile);

        EnumerateWaypointTags(cavern.RootElement).Should().Contain("DATH_APEX_DEN_OUTSIDE");
        EnumerateWaypointTags(dungeon.RootElement).Should().Contain("DATH_APEX_DEN_INSIDE");
        EnumerateWaypointTags(dungeon.RootElement).Should().Contain("STUCK_WAYPOINT");
        EnumerateWaypointTags(arena.RootElement).Should().Contain("STUCK_WAYPOINT");

        var exit = EnumerateObjects(dungeon.RootElement)
            .Single(element =>
                GetString(element, "OnUsed") == "teleport" &&
                GetOptionalLocalString(element, "DESTINATION") == "DATH_APEX_DEN_OUTSIDE");

        exit.GetProperty("LocName").GetProperty("value").GetProperty("0").GetString()
            .Should().Be("Exit the Apex Den");
    }

    [Test]
    public void ApexDenDungeon_UsesGeneratedCapstoneSpawnTable()
    {
        using var dungeon = LoadModuleJson("git", DungeonAreaFile);

        GetLocalString(dungeon.RootElement, "CREATURE_SPAWN_TABLE_ID")
            .Should().Be("CAPSTONE_DATHOMIR_GROTTO_APEX_DEN");
        GetLocalInt(dungeon.RootElement, "IS_DUNGEON").Should().Be(1);
    }

    [Test]
    public void GrottoCaverns_HoldNoQuestGivers()
    {
        // Quest giver placement is owned by CapstoneQuestGiverPlacementTests; the spawn-bearing
        // Grotto Caverns (the gate camp) must never host a trainer.
        using var cavern = LoadModuleJson("git", CavernAreaFile);
        cavern.RootElement
            .GetProperty("Creature List")
            .GetProperty("value")
            .EnumerateArray()
            .Select(creature => GetString(creature, "TemplateResRef"))
            .Should()
            .NotContain(new[] { "cq_primover", "cq_untinst", "cq_forcebeast" });
    }

    // Warden mini-bosses (step 3) spawn on demand in the dungeon; final masters (step 5) in the arena.
    private static readonly (string Resref, string Quest, string Encounter, string Enemy, string Waypoint)[] Wardens =
    {
        ("primov_wd_call", "primal_overrun_breach", "primal_overrun_breach_warden", "cp_primover_wd", "CAPSTONE_PRIMOVER_WD_SPAWN"),
        ("untinst_wd_call", "untouchable_instinct_breach", "untouchable_instinct_breach_warden", "cp_untinst_wd", "CAPSTONE_UNTINST_WD_SPAWN"),
        ("fbeast_wd_call", "force_bonded_beast_breach", "force_bonded_beast_breach_warden", "cp_forcebeast_wd", "CAPSTONE_FORCEBEAST_WD_SPAWN"),
    };

    private static readonly (string Resref, string Quest, string Encounter, string Enemy, string Waypoint)[] Masters =
    {
        ("primov_ms_call", "primal_overrun_mastery", "primal_overrun_mastery_master", "cp_primover_ms", "CAPSTONE_PRIMOVER_MS_SPAWN"),
        ("untinst_ms_call", "untouchable_instinct_mastery", "untouchable_instinct_mastery_master", "cp_untinst_ms", "CAPSTONE_UNTINST_MS_SPAWN"),
        ("fbeast_ms_call", "force_bonded_beast_mastery", "force_bonded_beast_mastery_master", "cp_forcebeast_ms", "CAPSTONE_FORCEBEAST_MS_SPAWN"),
    };

    [Test]
    public void ApexDenWardenActivators_AreInTheDungeon()
    {
        using var dungeon = LoadModuleJson("git", DungeonAreaFile);
        AssertActivators(dungeon, Wardens);
    }

    [Test]
    public void ApexDenMasterActivators_AreInTheBossArena()
    {
        using var arena = LoadModuleJson("git", ArenaAreaFile);
        AssertActivators(arena, Masters);

        var arenaActivators = EnumerateObjects(arena.RootElement)
            .Where(e => GetString(e, "OnUsed") == "quest_enc")
            .Select(e => GetString(e, "TemplateResRef"));
        arenaActivators.Should().BeEquivalentTo(Masters.Select(m => m.Resref));
    }

    private static void AssertActivators(System.Text.Json.JsonDocument area, (string Resref, string Quest, string Encounter, string Enemy, string Waypoint)[] expected)
    {
        var waypoints = area.RootElement.GetProperty("WaypointList").GetProperty("value")
            .EnumerateArray().ToDictionary(w => GetString(w, "Tag"), w => GetString(w, "TemplateResRef"));

        foreach (var (resref, questId, encounterId, creatureResref, spawnWaypoint) in expected)
        {
            var activator = EnumerateObjects(area.RootElement)
                .Single(element => GetString(element, "TemplateResRef") == resref);

            activator.GetProperty("LocName").GetProperty("value").GetProperty("0").GetString().Should().Be("???");
            GetString(activator, "OnUsed").Should().Be("quest_enc");
            GetString(activator, "OnHeartbeat").Should().BeEmpty();
            GetLocalString(activator, "QUEST_ID").Should().Be(questId);
            GetLocalInt(activator, "QUEST_STATE").Should().Be(1);
            GetLocalInt(activator, "VISIBILITY_HIDDEN_DEFAULT").Should().Be(1);
            GetLocalString(activator, "VISIBILITY_OBJECT_ID").Should().Be(resref.ToUpperInvariant());
            GetLocalString(activator, "QUEST_ENCOUNTER_ID").Should().Be(encounterId);
            GetLocalString(activator, "QUEST_ENCOUNTER_RESREF").Should().Be(creatureResref);
            GetLocalString(activator, "QUEST_ENCOUNTER_WAYPOINT").Should().Be(spawnWaypoint);
            GetLocalInt(activator, "QUEST_ENCOUNTER_COOLDOWN_MINUTES").Should().Be(60);
            GetLocalInt(activator, "QUEST_ENCOUNTER_IDLE_MINUTES").Should().Be(10);

            waypoints.Should().ContainKey(spawnWaypoint, $"boss spawn waypoint for {resref} must be co-located");
        }

        using var palette = LoadModuleJson("itp", "placeablepalcus.itp.json");
        var paletteResrefs = EnumerateResrefs(palette.RootElement).ToArray();
        var root = FindRepositoryRoot();
        foreach (var (resref, _, _, _, _) in expected)
        {
            paletteResrefs.Should().NotContain(resref);
            File.Exists(Path.Combine(root.FullName, "Module", "utp", $"{resref}.utp.json")).Should().BeFalse();
        }
    }

    [Test]
    public void ApexDenAreas_UseDistinctAreaTags()
    {
        using var dungeonArea = LoadModuleJson("are", "pw_sc_dath_apexd.are.json");
        using var arenaArea = LoadModuleJson("are", "pw_sc_dath_sden.are.json");

        var dungeonTag = dungeonArea.RootElement.GetProperty("Tag").GetProperty("value").GetString();
        var arenaTag = arenaArea.RootElement.GetProperty("Tag").GetProperty("value").GetString();

        dungeonTag.Should().Be("pw_sc_dath_apexden");
        arenaTag.Should().Be("pw_sc_dath_sden");
        arenaTag.Should().NotBe(dungeonTag);
    }

    private static System.Text.Json.JsonDocument LoadModuleJson(string folder, string fileName)
    {
        var root = FindRepositoryRoot();
        return System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root.FullName,
            "Module",
            folder,
            fileName)));
    }

    private static IEnumerable<string> EnumerateWaypointTags(System.Text.Json.JsonElement area)
    {
        return area
            .GetProperty("WaypointList")
            .GetProperty("value")
            .EnumerateArray()
            .Select(waypoint => GetString(waypoint, "Tag"));
    }

    private static string GetLocalString(System.Text.Json.JsonElement json, string variableName)
    {
        return GetLocal(json, variableName).GetProperty("Value").GetProperty("value").GetString()!;
    }

    private static string GetOptionalLocalString(System.Text.Json.JsonElement json, string variableName)
    {
        if (!TryGetLocal(json, variableName, out var local))
            return string.Empty;

        return local.GetProperty("Value").GetProperty("value").GetString() ?? string.Empty;
    }

    private static int GetLocalInt(System.Text.Json.JsonElement json, string variableName)
    {
        return GetLocal(json, variableName).GetProperty("Value").GetProperty("value").GetInt32();
    }

    private static System.Text.Json.JsonElement GetLocal(System.Text.Json.JsonElement json, string variableName)
    {
        if (!TryGetLocal(json, variableName, out var local))
            throw new InvalidOperationException($"Missing local variable '{variableName}'.");

        return local;
    }

    private static bool TryGetLocal(
        System.Text.Json.JsonElement json,
        string variableName,
        out System.Text.Json.JsonElement local)
    {
        local = default;

        if (!json.TryGetProperty("VarTable", out var varTable) ||
            !varTable.TryGetProperty("value", out var variables) ||
            variables.ValueKind != System.Text.Json.JsonValueKind.Array)
        {
            return false;
        }

        foreach (var variable in variables.EnumerateArray())
        {
            if (variable.GetProperty("Name").GetProperty("value").GetString() == variableName)
            {
                local = variable;
                return true;
            }
        }

        return false;
    }

    private static string GetString(System.Text.Json.JsonElement json, string propertyName)
    {
        return json.TryGetProperty(propertyName, out var property) &&
               property.TryGetProperty("value", out var value) &&
               value.ValueKind == System.Text.Json.JsonValueKind.String
            ? value.GetString()!
            : string.Empty;
    }

    private static IEnumerable<System.Text.Json.JsonElement> EnumerateObjects(System.Text.Json.JsonElement element)
    {
        if (element.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            yield return element;

            foreach (var property in element.EnumerateObject())
            {
                foreach (var nested in EnumerateObjects(property.Value))
                {
                    yield return nested;
                }
            }
        }
        else if (element.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var nested in EnumerateObjects(item))
                {
                    yield return nested;
                }
            }
        }
    }

    private static IEnumerable<string> EnumerateResrefs(System.Text.Json.JsonElement element)
    {
        if (element.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var resref in EnumerateResrefs(item))
                {
                    yield return resref;
                }
            }
        }
        else if (element.ValueKind == System.Text.Json.JsonValueKind.Object)
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
