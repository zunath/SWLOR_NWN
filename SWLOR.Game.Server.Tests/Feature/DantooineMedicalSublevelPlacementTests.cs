using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.KeyItemService;
using System.Text.Json;

namespace SWLOR.Game.Server.Tests.Feature;

public class DantooineMedicalSublevelPlacementTests
{
    private const string AccessArea = "dan_warehouse.git.json";
    private const string DungeonArea = "pw_sc_dantmedsub.git.json";
    private const string ArenaArea = "pw_sc_dantprowar.git.json";

    private static readonly string[] InstanceListNames =
    {
        "Creature List",
        "Door List",
        "Encounter List",
        "List",
        "Placeable List",
        "SoundList",
        "StoreList",
        "TriggerList",
        "WaypointList",
    };

    private static readonly (string Tag, string Quest, string Encounter, string Enemy, string Waypoint)[] Wardens =
    {
        ("emcocktail_wd_call", "emergency_cocktail_breach", "emergency_cocktail_breach_warden", "cp_emcocktail_wd", "CAPSTONE_EMCOCKTAIL_WD_SPAWN"),
        ("holdline_wd_call", "hold_the_line_breach", "hold_the_line_breach_warden", "cp_holdline_wd", "CAPSTONE_HOLDLINE_WD_SPAWN"),
        ("infconduit_wd_call", "infinite_conduit_breach", "infinite_conduit_breach_warden", "cp_infconduit_wd", "CAPSTONE_INFCONDUIT_WD_SPAWN"),
    };

    private static readonly (string Tag, string Quest, string Encounter, string Enemy, string Waypoint)[] Masters =
    {
        ("holdline_ms_call", "hold_the_line_mastery", "hold_the_line_mastery_master", "cp_holdline_ms", "CAPSTONE_HOLDLINE_MS_SPAWN"),
        ("emcocktail_ms_call", "emergency_cocktail_mastery", "emergency_cocktail_mastery_master", "cp_emcocktail_ms", "CAPSTONE_EMCOCKTAIL_MS_SPAWN"),
        ("infconduit_ms_call", "infinite_conduit_mastery", "infinite_conduit_mastery_master", "cp_infconduit_ms", "CAPSTONE_INFCONDUIT_MS_SPAWN"),
    };

    [Test]
    public void MedicalSublevelAreas_AreRegisteredWithDistinctTags()
    {
        using var module = LoadModuleJson("ifo", "module.ifo.json");
        var areaResrefs = module.RootElement
            .GetProperty("Mod_Area_list")
            .GetProperty("value")
            .EnumerateArray()
            .Select(area => GetString(area, "Area_Name"));

        areaResrefs.Should().Contain(new[] { "pw_sc_dantmedsub", "pw_sc_dantprowar" });

        using var dungeon = LoadModuleJson("are", "pw_sc_dantmedsub.are.json");
        using var arena = LoadModuleJson("are", "pw_sc_dantprowar.are.json");

        GetString(dungeon.RootElement, "Name").Should().Be("Dantooine - Medical Sublevel");
        GetString(arena.RootElement, "Name").Should().Be("Dantooine - Protected Ward");
        GetString(dungeon.RootElement, "Tag").Should().Be("pw_sc_dantmedsubl");
        GetString(arena.RootElement, "Tag").Should().Be("pw_sc_dantproward");
        GetString(arena.RootElement, "Tag").Should().NotBe(GetString(dungeon.RootElement, "Tag"));
    }

    [Test]
    public void MedicalSublevel_UsesTheGeneratedDungeonAndRareSpawnTables()
    {
        using var dungeon = LoadModuleJson("git", DungeonArea);

        GetLocalString(dungeon.RootElement, "CREATURE_SPAWN_TABLE_ID")
            .Should().Be("CAPSTONE_DANTOOINE_MEDICAL_SUBLEVEL");
        GetLocalInt(dungeon.RootElement, "IS_DUNGEON").Should().Be(1);
        GetLocalInt(dungeon.RootElement, "MINI_MAP_DISABLED").Should().Be(1);
        GetLocalInt(dungeon.RootElement, "MAP_KEY_ITEM_ID")
            .Should().Be((int)KeyItemType.DantooineWarehouseMap);
        GetLocalInt(dungeon.RootElement, "PLANET_TYPE_ID").Should().Be((int)PlanetType.Dantooine);

        EnumerateWaypointTags(dungeon.RootElement)
            .Should().Contain("DANTOOINE_MEDICAL_SUBLEVEL_RARES");
    }

    [Test]
    public void MedicalSublevelAreaComments_StayAlignedWithPlacedInstances()
    {
        foreach (var area in new[] { "pw_sc_dantmedsub", "pw_sc_dantprowar" })
        {
            using var git = LoadModuleJson("git", $"{area}.git.json");
            using var gic = LoadModuleJson("gic", $"{area}.gic.json");

            foreach (var listName in InstanceListNames)
            {
                var placedCount = git.RootElement.GetProperty(listName).GetProperty("value").GetArrayLength();
                var commentCount = gic.RootElement.GetProperty(listName).GetProperty("value").GetArrayLength();

                commentCount.Should().Be(placedCount,
                    $"{area}'s {listName} GIC comments must remain aligned with its GIT instances");
            }
        }
    }

    [Test]
    public void MedicalSublevelAccess_UsesTheStandardCapstoneKeyGate()
    {
        ((int)KeyItemType.CapstoneDantooineMedicalSublevelKey).Should().Be(117);

        using var accessArea = LoadModuleJson("git", AccessArea);
        var gate = FindTeleport(accessArea.RootElement, "to_medsublevel");
        var accessAreaProperties = accessArea.RootElement.GetProperty("AreaProperties").GetProperty("value");

        GetLocalInt(accessAreaProperties, "PLANET_TYPE_ID").Should().Be((int)PlanetType.Dantooine);
        GetString(gate, "Tag").Should().Be("tele_obj");
        GetString(gate, "TemplateResRef").Should().Be("tele_obj");
        GetString(gate, "OnHeartbeat").Should().BeEmpty();
        GetLocalizedString(gate, "LocName").Should().Be("Dantooine Medical Sublevel");
        GetLocalInt(gate, "KEY_ITEM_ID")
            .Should().Be((int)KeyItemType.CapstoneDantooineMedicalSublevelKey);
        GetLocalInt(gate, "TELEPORT_PARTY_MEMBERS").Should().Be(1);
        GetLocalString(gate, "MISSING_KEY_ITEM_MESSAGE").Should().Contain("Medical Sublevel");
        GetLocalString(gate, "MISSING_KEY_ITEM_MESSAGE").Should().Contain("key");

        using var dungeon = LoadModuleJson("git", DungeonArea);
        EnumerateWaypointTags(dungeon.RootElement).Should().Contain("to_medsublevel");
    }

    [Test]
    public void MedicalSublevelAndProtectedWard_HaveCompleteTwoWayTravelAndRecoveryWaypoints()
    {
        using var accessArea = LoadModuleJson("git", AccessArea);
        using var dungeon = LoadModuleJson("git", DungeonArea);
        using var arena = LoadModuleJson("git", ArenaArea);

        EnumerateWaypointTags(accessArea.RootElement).Should().Contain("dant_to_abandonedlabs");
        EnumerateWaypointTags(dungeon.RootElement).Should().Contain(new[]
        {
            "to_medsublevel",
            "dant_medsublevelup",
            "STUCK_WAYPOINT",
        });
        EnumerateWaypointTags(arena.RootElement).Should().Contain(new[]
        {
            "dant_protect_ward",
            "STUCK_WAYPOINT",
        });

        var dungeonExit = FindTeleport(dungeon.RootElement, "dant_to_abandonedlabs");
        var arenaEntrance = FindTeleport(dungeon.RootElement, "dant_protect_ward");
        var arenaExit = FindTeleport(arena.RootElement, "dant_medsublevelup");

        GetLocalizedString(dungeonExit, "LocName")
            .Should().Be("Exit the Medical Sublevel");
        GetLocalizedString(arenaEntrance, "LocName")
            .Should().Be("Enter the Protected Ward");
        GetLocalizedString(arenaExit, "LocName")
            .Should().Be("Exit the Protected Ward");
        GetLocalInt(dungeonExit, "TELEPORT_PARTY_MEMBERS").Should().Be(0);
        GetLocalInt(arenaEntrance, "TELEPORT_PARTY_MEMBERS").Should().Be(0);
        GetLocalInt(arenaExit, "TELEPORT_PARTY_MEMBERS").Should().Be(0);

        GetLocalInt(arena.RootElement, "PLANET_TYPE_ID").Should().Be((int)PlanetType.Dantooine);
    }

    [Test]
    public void MedicalSublevel_HoldsTheThreeWardenEncounters()
    {
        using var dungeon = LoadModuleJson("git", DungeonArea);
        AssertEncounters(dungeon.RootElement, Wardens);

        EnumerateQuestEncounterTags(dungeon.RootElement)
            .Should().BeEquivalentTo(Wardens.Select(encounter => encounter.Tag));
    }

    [Test]
    public void ProtectedWard_HoldsTheThreeMasterEncounters()
    {
        using var arena = LoadModuleJson("git", ArenaArea);
        AssertEncounters(arena.RootElement, Masters);

        EnumerateQuestEncounterTags(arena.RootElement)
            .Should().BeEquivalentTo(Masters.Select(encounter => encounter.Tag));
    }

    private static void AssertEncounters(
        JsonElement area,
        IEnumerable<(string Tag, string Quest, string Encounter, string Enemy, string Waypoint)> expected)
    {
        var waypointTags = EnumerateWaypointTags(area).ToArray();

        foreach (var (tag, quest, encounter, enemy, waypoint) in expected)
        {
            var activator = EnumerateObjects(area).Single(element => GetString(element, "Tag") == tag);

            GetLocalizedString(activator, "LocName").Should().Be("???");
            GetString(activator, "OnUsed").Should().Be("quest_enc");
            GetString(activator, "OnHeartbeat").Should().BeEmpty();
            GetLocalString(activator, "QUEST_ID").Should().Be(quest);
            GetLocalInt(activator, "QUEST_STATE").Should().Be(1);
            GetLocalInt(activator, "VISIBILITY_HIDDEN_DEFAULT").Should().Be(1);
            GetLocalString(activator, "VISIBILITY_OBJECT_ID").Should().Be(tag.ToUpperInvariant());
            GetLocalString(activator, "QUEST_ENCOUNTER_ID").Should().Be(encounter);
            GetLocalString(activator, "QUEST_ENCOUNTER_RESREF").Should().Be(enemy);
            GetLocalString(activator, "QUEST_ENCOUNTER_WAYPOINT").Should().Be(waypoint);
            GetLocalInt(activator, "QUEST_ENCOUNTER_COOLDOWN_MINUTES").Should().Be(60);
            GetLocalInt(activator, "QUEST_ENCOUNTER_IDLE_MINUTES").Should().Be(10);
            waypointTags.Should().Contain(waypoint);

            using var palette = LoadModuleJson("itp", "placeablepalcus.itp.json");
            EnumerateResrefs(palette.RootElement).Should().NotContain(tag);
            File.Exists(Path.Combine(FindRepositoryRoot().FullName, "Module", "utp", $"{tag}.utp.json"))
                .Should().BeFalse();
        }
    }

    private static JsonElement FindTeleport(JsonElement area, string destination)
    {
        return EnumerateObjects(area).Single(element =>
            GetString(element, "OnUsed") == "teleport" &&
            GetOptionalLocalString(element, "DESTINATION") == destination);
    }

    private static IEnumerable<string> EnumerateQuestEncounterTags(JsonElement area)
    {
        return EnumerateObjects(area)
            .Where(element => GetString(element, "OnUsed") == "quest_enc")
            .Select(element => GetString(element, "Tag"));
    }

    private static IEnumerable<string> EnumerateWaypointTags(JsonElement area)
    {
        return area.GetProperty("WaypointList").GetProperty("value")
            .EnumerateArray()
            .Select(waypoint => GetString(waypoint, "Tag"));
    }

    private static JsonDocument LoadModuleJson(string folder, string fileName)
    {
        var root = FindRepositoryRoot();
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, "Module", folder, fileName)));
    }

    private static string GetLocalizedString(JsonElement json, string propertyName)
    {
        return json.GetProperty(propertyName).GetProperty("value").GetProperty("0").GetString()!;
    }

    private static string GetString(JsonElement json, string propertyName)
    {
        if (!json.TryGetProperty(propertyName, out var property) ||
            !property.TryGetProperty("value", out var value))
        {
            return string.Empty;
        }

        if (value.ValueKind == JsonValueKind.String)
            return value.GetString()!;

        if (value.ValueKind == JsonValueKind.Object && value.TryGetProperty("0", out var localized))
            return localized.GetString()!;

        return string.Empty;
    }

    private static string GetLocalString(JsonElement json, string variableName)
    {
        return GetLocal(json, variableName).GetProperty("Value").GetProperty("value").GetString()!;
    }

    private static string GetOptionalLocalString(JsonElement json, string variableName)
    {
        return TryGetLocal(json, variableName, out var local)
            ? local.GetProperty("Value").GetProperty("value").GetString() ?? string.Empty
            : string.Empty;
    }

    private static int GetLocalInt(JsonElement json, string variableName)
    {
        return GetLocal(json, variableName).GetProperty("Value").GetProperty("value").GetInt32();
    }

    private static JsonElement GetLocal(JsonElement json, string variableName)
    {
        if (TryGetLocal(json, variableName, out var local))
            return local;

        throw new InvalidOperationException($"Missing local variable '{variableName}'.");
    }

    private static bool TryGetLocal(JsonElement json, string variableName, out JsonElement local)
    {
        local = default;
        if (!json.TryGetProperty("VarTable", out var varTable) ||
            !varTable.TryGetProperty("value", out var variables) ||
            variables.ValueKind != JsonValueKind.Array)
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

    private static IEnumerable<JsonElement> EnumerateObjects(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            yield return element;
            foreach (var property in element.EnumerateObject())
            foreach (var nested in EnumerateObjects(property.Value))
                yield return nested;
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            foreach (var nested in EnumerateObjects(item))
                yield return nested;
        }
    }

    private static IEnumerable<string> EnumerateResrefs(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            foreach (var resref in EnumerateResrefs(item))
                yield return resref;
        }
        else if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("RESREF", out var resref))
                yield return resref.GetProperty("value").GetString()!;

            foreach (var property in element.EnumerateObject())
            foreach (var nestedResref in EnumerateResrefs(property.Value))
                yield return nestedResref;
        }
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        return directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
