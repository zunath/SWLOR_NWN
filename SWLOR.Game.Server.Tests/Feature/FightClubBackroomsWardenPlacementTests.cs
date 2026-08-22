using FluentAssertions;
using NUnit.Framework;
using System.Text.Json;

namespace SWLOR.Game.Server.Tests.Feature;

// The Fight Club Backrooms dungeon (pw_sc_emfbackr) carries the ambient capstone spawn table
// plus the three warden mini-bosses; the three final masters live in the attached Private Pit
// boss arena (pw_sc_smarena). This suite covers both.
public class FightClubBackroomsWardenPlacementTests
{
    private const string DungeonArea = "pw_sc_emfbackr.git.json";
    private const string ArenaArea = "pw_sc_smarena.git.json";

    private static readonly (string Resref, string Quest, string Enemy, string Waypoint)[] Wardens =
    {
        ("cripdef_wd_call", "crippling_defense_breach", "cp_cripdef_wd", "CAPSTONE_CRIPDEF_WD_SPAWN"),
        ("tempbloom_wd_call", "tempest_bloom_breach", "cp_tempbloom_wd", "CAPSTONE_TEMPBLOOM_WD_SPAWN"),
        ("redbloom_wd_call", "red_bloom_breach", "cp_redbloom_wd", "CAPSTONE_REDBLOOM_WD_SPAWN"),
    };

    private static readonly (string Resref, string Quest, string Enemy, string Waypoint)[] Masters =
    {
        ("cripdef_ms_call", "crippling_defense_mastery", "cp_cripdef_ms", "CAPSTONE_CRIPDEF_MS_SPAWN"),
        ("tempbloom_ms_call", "tempest_bloom_mastery", "cp_tempbloom_ms", "CAPSTONE_TEMPBLOOM_MS_SPAWN"),
        ("redbloom_ms_call", "red_bloom_mastery", "cp_redbloom_ms", "CAPSTONE_REDBLOOM_MS_SPAWN"),
    };

    [Test]
    public void PrivatePitArena_HoldsOnlyTheThreeFinalMasters()
    {
        using var arena = LoadModuleJson("git", ArenaArea);
        var waypointTags = arena.RootElement.GetProperty("WaypointList").GetProperty("value")
            .EnumerateArray().Select(w => GetString(w, "Tag")).ToArray();

        foreach (var (resref, quest, enemy, waypoint) in Masters)
        {
            // Match by Tag: resrefs over NWN's 16-char limit (tempbloom_ms_call) are truncated
            // on toolset save, but the Tag is preserved and is what gameplay uses.
            var activator = EnumerateObjects(arena.RootElement)
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

        // Only the three masters live in the arena (no wardens).
        var arenaActivators = EnumerateObjects(arena.RootElement)
            .Where(e => GetString(e, "OnUsed") == "quest_enc")
            .Select(e => GetString(e, "Tag"))
            .ToArray();
        arenaActivators.Should().BeEquivalentTo(Masters.Select(m => m.Resref));
    }

    [Test]
    public void FightClubDungeon_UsesGeneratedCapstoneSpawnTable()
    {
        using var dungeon = LoadModuleJson("git", DungeonArea);
        GetLocalString(dungeon.RootElement, "CREATURE_SPAWN_TABLE_ID")
            .Should().Be("CAPSTONE_SMUGGLERS_MOON_FIGHT_CLUB_BACKROOMS");
        GetLocalInt(dungeon.RootElement, "IS_DUNGEON").Should().Be(1);
    }

    [Test]
    public void FightClubDungeon_HoldsTheThreeWardenMiniBosses()
    {
        using var dungeon = LoadModuleJson("git", DungeonArea);
        var waypointTags = dungeon.RootElement.GetProperty("WaypointList").GetProperty("value")
            .EnumerateArray().Select(w => GetString(w, "Tag")).ToArray();

        foreach (var (resref, quest, enemy, waypoint) in Wardens)
        {
            // Match by Tag, not TemplateResRef: a resref over NWN's 16-char limit
            // (e.g. tempbloom_wd_call) is truncated when the toolset saves the area, but
            // the Tag is preserved and is the identifier gameplay actually uses.
            var activator = EnumerateObjects(dungeon.RootElement)
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
        foreach (var (resref, _, _, _) in Wardens)
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
