using FluentAssertions;
using NUnit.Framework;
using System.Text.Json;

namespace SWLOR.Game.Server.Tests.Feature;

public class CZ220BreakerYardPlacementTests
{
    // Breaker Bay (cz220shipbreakin) is the dungeon; Breaker Yard (cz220shipbreaker) is the boss arena.
    private const string DungeonArea = "cz220shipbreakin.git.json";
    private const string ArenaArea = "cz220shipbreaker.git.json";

    [Test]
    public void BreakerBayDungeon_UsesGeneratedCapstoneSpawnTable()
    {
        using var dungeon = LoadModuleJson("git", DungeonArea);
        GetLocalString(dungeon.RootElement, "CREATURE_SPAWN_TABLE_ID").Should().Be("CAPSTONE_CZ220_BREAKER_YARD");
        GetLocalInt(dungeon.RootElement, "IS_DUNGEON").Should().Be(1);
    }

    // Warden mini-bosses (quest step 3) spawn on demand inside the dungeon; final masters
    // (quest step 5) spawn in the boss arena.
    private static readonly (string Resref, string Quest, string Enemy, string Waypoint)[] Wardens =
    {
        ("adamg_wd_call", "adamantine_guard_breach", "cp_adamguard_wd", "CAPSTONE_ADAMGUARD_WD_SPAWN"),
        ("scrapl_wd_call", "scrapheap_lockdown_breach", "cp_scraplock_wd", "CAPSTONE_SCRAPLOCK_WD_SPAWN"),
        ("wbrk_wd_call", "worldbreaker_breach", "cp_worldbrk_wd", "CAPSTONE_WORLDBRK_WD_SPAWN"),
    };

    private static readonly (string Resref, string Quest, string Enemy, string Waypoint)[] Masters =
    {
        ("adamg_ms_call", "adamantine_guard_mastery", "cp_adamguard_ms", "CAPSTONE_ADAMGUARD_MS_SPAWN"),
        ("scrapl_ms_call", "scrapheap_lockdown_mastery", "cp_scraplock_ms", "CAPSTONE_SCRAPLOCK_MS_SPAWN"),
        ("wbrk_ms_call", "worldbreaker_mastery", "cp_worldbrk_ms", "CAPSTONE_WORLDBRK_MS_SPAWN"),
    };

    [Test]
    public void BreakerYardArena_HoldsOnlyTheThreeFinalMasters()
    {
        using var arena = LoadModuleJson("git", ArenaArea);
        AssertEncounters(arena, Masters);

        // The wardens (mini-bosses) must NOT be in the arena.
        var arenaActivators = EnumerateObjects(arena.RootElement)
            .Where(e => GetString(e, "OnUsed") == "quest_enc")
            .Select(e => GetString(e, "TemplateResRef"))
            .ToArray();
        arenaActivators.Should().BeEquivalentTo(Masters.Select(m => m.Resref));
    }

    [Test]
    public void BreakerBayDungeon_HoldsTheThreeWardenMiniBosses()
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
            var activator = EnumerateObjects(area.RootElement)
                .Single(e => GetString(e, "TemplateResRef") == resref);

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
