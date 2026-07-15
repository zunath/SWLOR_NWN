using FluentAssertions;
using NUnit.Framework;
using System.Text.Json;

namespace SWLOR.Game.Server.Tests.Feature;

public class CzerkaArmsTestRangePlacementTests
{
    // Czerka Weapons Testing Facility (pw_ar_czarmrange) is the dungeon;
    // Czerka Blast-Safe Cell (ka_ar_czweaparen) is the boss arena.
    private const string DungeonArea = "pw_ar_czarmrange.git.json";
    private const string ArenaArea = "ka_ar_czweaparen.git.json";

    [Test]
    public void CzerkaDungeon_UsesGeneratedCapstoneSpawnTable()
    {
        using var dungeon = LoadModuleJson("git", DungeonArea);
        GetLocalString(dungeon.RootElement, "CREATURE_SPAWN_TABLE_ID").Should().Be("CAPSTONE_CZERKA_ARMS_TEST_RANGE");
        GetLocalInt(dungeon.RootElement, "IS_DUNGEON").Should().Be(1);
    }

    // Warden mini-bosses (quest step 3) spawn on demand inside the dungeon; final masters
    // (quest step 5) spawn in the boss arena.
    private static readonly (string Resref, string Quest, string Enemy, string Waypoint)[] Wardens =
    {
        ("killbox_wd_call", "kill_box_breach", "cp_killbox_wd", "CAPSTONE_KILLBOX_WD_SPAWN"),
        ("oneshot_wd_call", "one_shot_breach", "cp_oneshot_wd", "CAPSTONE_ONESHOT_WD_SPAWN"),
        ("rainsteel_wd_call", "rain_of_steel_breach", "cp_rainsteel_wd", "CAPSTONE_RAINSTEEL_WD_SPAWN"),
    };

    private static readonly (string Resref, string Quest, string Enemy, string Waypoint)[] Masters =
    {
        ("killbox_ms_call", "kill_box_mastery", "cp_killbox_ms", "CAPSTONE_KILLBOX_MS_SPAWN"),
        ("oneshot_ms_call", "one_shot_mastery", "cp_oneshot_ms", "CAPSTONE_ONESHOT_MS_SPAWN"),
        ("rainsteel_ms_call", "rain_of_steel_mastery", "cp_rainsteel_ms", "CAPSTONE_RAINSTEEL_MS_SPAWN"),
    };

    [Test]
    public void CzerkaArena_HoldsOnlyTheThreeFinalMasters()
    {
        using var arena = LoadModuleJson("git", ArenaArea);
        AssertEncounters(arena, Masters);

        // The wardens (mini-bosses) must NOT be in the arena. Identify by Tag: a resref over
        // NWN's 16-char limit (e.g. rainsteel_ms_call) is truncated on toolset save; the Tag is not.
        var arenaActivators = EnumerateObjects(arena.RootElement)
            .Where(e => GetString(e, "OnUsed") == "quest_enc")
            .Select(e => GetString(e, "Tag"))
            .ToArray();
        arenaActivators.Should().BeEquivalentTo(Masters.Select(m => m.Resref));
    }

    [Test]
    public void CzerkaDungeon_HoldsTheThreeWardenMiniBosses()
    {
        using var dungeon = LoadModuleJson("git", DungeonArea);
        AssertEncounters(dungeon, Wardens);
    }

    private static void AssertEncounters(JsonDocument area, (string Resref, string Quest, string Enemy, string Waypoint)[] expected)
    {
        var waypointTags = area.RootElement.GetProperty("WaypointList").GetProperty("value")
            .EnumerateArray().Select(w => GetString(w, "Tag")).ToArray();

        foreach (var (resref, quest, enemy, waypoint) in expected)
        {
            // Match by Tag, not TemplateResRef: resrefs over NWN's 16-char limit are truncated
            // on toolset save, but the Tag is preserved and is what gameplay uses.
            var activator = EnumerateObjects(area.RootElement)
                .Single(e => GetString(e, "Tag") == resref);

            GetString(activator, "OnUsed").Should().Be("quest_enc");
            GetLocalString(activator, "QUEST_ID").Should().Be(quest);
            GetLocalInt(activator, "QUEST_STATE").Should().Be(1);
            GetLocalInt(activator, "VISIBILITY_HIDDEN_DEFAULT").Should().Be(1);
            GetLocalString(activator, "QUEST_ENCOUNTER_RESREF").Should().Be(enemy);
            GetLocalString(activator, "QUEST_ENCOUNTER_WAYPOINT").Should().Be(waypoint);
            GetLocalInt(activator, "QUEST_ENCOUNTER_COOLDOWN_MINUTES").Should().Be(60);

            waypointTags.Should().Contain(waypoint, $"boss spawn waypoint for {resref} must be placed");
        }

        using var palette = LoadModuleJson("itp", "placeablepalcus.itp.json");
        var paletteResrefs = EnumerateResrefs(palette.RootElement).ToArray();
        var root = FindRepositoryRoot();
        foreach (var (resref, _, _, _) in expected)
        {
            paletteResrefs.Should().NotContain(resref);
            File.Exists(Path.Combine(root.FullName, "Module", "utp", $"{resref}.utp.json")).Should().BeFalse();
        }
    }

    private static JsonDocument LoadModuleJson(string folder, string file)
    {
        var root = FindRepositoryRoot();
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, "Module", folder, file)));
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
                foreach (var r in EnumerateResrefs(item)) yield return r;
        }
        else if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("RESREF", out var resref))
                yield return resref.GetProperty("value").GetString()!;
            foreach (var property in element.EnumerateObject())
                foreach (var r in EnumerateResrefs(property.Value)) yield return r;
        }
    }

    private static string GetString(JsonElement json, string name) =>
        json.TryGetProperty(name, out var p) && p.TryGetProperty("value", out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()! : string.Empty;

    private static string GetLocalString(JsonElement json, string variableName) =>
        TryGetLocal(json, variableName, out var local)
            ? local.GetProperty("Value").GetProperty("value").GetString() ?? string.Empty : string.Empty;

    private static int GetLocalInt(JsonElement json, string variableName) =>
        TryGetLocal(json, variableName, out var local) ? local.GetProperty("Value").GetProperty("value").GetInt32() : 0;

    private static bool TryGetLocal(JsonElement json, string variableName, out JsonElement local)
    {
        local = default;
        if (!json.TryGetProperty("VarTable", out var varTable) ||
            !varTable.TryGetProperty("value", out var variables) ||
            variables.ValueKind != JsonValueKind.Array)
            return false;
        foreach (var variable in variables.EnumerateArray())
            if (variable.GetProperty("Name").GetProperty("value").GetString() == variableName)
            {
                local = variable;
                return true;
            }
        return false;
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
