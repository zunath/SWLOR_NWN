using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Feature.SpawnDefinition;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class TrialDungeonPlacementTests
{
    public record Dungeon(string Area, string Arena, string Hub, string SpawnBlueprint, int Key, int Planet, string[] Lines);

    private static readonly Dungeon[] Dungeons =
    {
        new("pw_sc_jedihalls", "pw_sc_jeditrial", "hutlar_testsite", "wp_cap_dantjedi", 108, 8,
            new[] { "sabstorm", "guardmst", "sabcycl" }),
        new("pw_sc_forgecav", "pw_sc_korrforge", "tat_anc_nminecli", "wp_cap_kforge", 109, 2,
            new[] { "absdef", "soulasc", "forcebane" }),
        new("pw_sc_canyonrng", "pw_sc_canyonpit", "tat_anc_rckpass2", "wp_cap_tatcan", 112, 2,
            new[] { "unmovctr", "lastword", "deadhand" }),
        new("pw_sc_qiontest", "pw_sc_qioncore", "sol_hutlarqcanyo", "wp_cap_hutlar", 114, 8,
            new[] { "perflurry", "thermdet", "overbarr" }),
        new("pw_sc_cryptdeep", "pw_sc_sithritual", "moncaladungeon1", "wp_cap_kcrypt", 115, 4,
            new[] { "lightstand", "darkhung", "eclipse" }),
        new("pw_sc_repubeng", "pw_sc_repubcmd", "viscaranwswamp", "wp_cap_vrepub", 116, 1,
            new[] { "killbeacon", "embunker", "deccommand" }),
        new("pw_sc_tarnpres", "pw_sc_tarnalpha", "dath_tarnjungles", "wp_cap_dtarn", 118, 64,
            new[] { "apexbite", "unbrbeast", "alpharhy" }),
    };

    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException("Module directory not found.");
        }
    }

    private static JsonElement Load(string type, string name)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "Module", type, $"{name}.{type}.json")));
        return doc.RootElement.Clone();
    }

    private static JsonElement Value(JsonElement o, string key) => o.GetProperty(key).GetProperty("value");
    private static string Text(JsonElement o, string key) => Value(o, key).GetString()!;
    private static double Number(JsonElement o, string key) => Value(o, key).GetDouble();
    private static JsonElement[] List(JsonElement o, string key) => Value(o, key).EnumerateArray().ToArray();
    private static JsonElement Local(JsonElement o, string key) => Value(List(o, "VarTable").Single(v => Text(v, "Name") == key), "Value");
    private static JsonElement Tagged(JsonElement o, string list, string tag) => List(o, list).Single(v => Text(v, "Tag") == tag);
    private static double Distance(JsonElement a, JsonElement b, bool waypoint = false) => Math.Sqrt(
        Math.Pow(Number(a, "X") - Number(b, waypoint ? "XPosition" : "X"), 2) +
        Math.Pow(Number(a, "Y") - Number(b, waypoint ? "YPosition" : "Y"), 2));

    [TestCaseSource(nameof(Dungeons))]
    public void Dungeon_IsRegisteredCategorizedAndUsesDedicatedLessonSpawns(Dungeon d)
    {
        var area = Load("are", d.Area);
        Text(area, "Tag").Should().Be(d.Area);
        Text(area, "ResRef").Should().Be(d.Area);
        Number(area, "Width").Should().Be(16);
        Number(area, "Height").Should().Be(16);
        List(area, "Tile_List").Should().HaveCount(256);
        List(Load("ifo", "module"), "Mod_Area_list").Count(a => Text(a, "Area_Name") == d.Area).Should().Be(1);
        using var categories = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "toolset", "categories.json")));
        IEnumerable<JsonElement> Folders(JsonElement folder)
        {
            yield return folder;
            if (folder.TryGetProperty("children", out var children))
                foreach (var child in children.EnumerateArray().SelectMany(Folders)) yield return child;
        }
        var folders = categories.RootElement.GetProperty("sections").GetProperty("are").GetProperty("folders");
        var folder = folders.EnumerateArray().SelectMany(Folders).Single(f => f.TryGetProperty("members", out var members) && members.EnumerateArray().Any(m => m.GetString() == d.Area));
        folder.GetProperty("members").EnumerateArray().Select(m => m.GetString()).Should().Contain(d.Arena);

        var git = Load("git", d.Area);
        Local(git, "IS_DUNGEON").GetInt32().Should().Be(1);
        Local(git, "MINI_MAP_DISABLED").GetInt32().Should().Be(1);
        Local(git, "PLANET_TYPE_ID").GetInt32().Should().Be(d.Planet);
        List(git, "Creature List").Should().BeEmpty();
        var spawnTag = Text(Load("utw", d.SpawnBlueprint), "Tag");
        List(git, "WaypointList").Count(w => Text(w, "Tag") == spawnTag).Should().Be(20);
        List(Load("git", d.Arena), "WaypointList").Should().NotContain(w => Text(w, "Tag") == spawnTag);
        List(git, "WaypointList").Should().ContainSingle(w => Text(w, "Tag") == "STUCK_WAYPOINT",
            $"{d.Area} needs a STUCK_WAYPOINT recovery point");
        var comments = Load("gic", d.Area);
        foreach (var p in comments.EnumerateObject().Where(p => p.Value.ValueKind == JsonValueKind.Object && p.Value.GetProperty("type").GetString() == "list"))
            List(comments, p.Name).Length.Should().Be(List(git, p.Name).Length);
    }

    [TestCaseSource(nameof(Dungeons))]
    public void WardenAltars_PreserveInvisibleQuestInteractionsAndBloodDressing(Dungeon d)
    {
        var git = Load("git", d.Area);
        var props = List(git, "Placeable List");
        var altars = props.Where(p => Number(p, "Appearance") == 2174).ToArray();
        altars.Should().HaveCount(3);
        props.Count(p => Text(p, "OnUsed") == "quest_enc").Should().Be(3);
        foreach (var code in d.Lines)
        {
            var call = Tagged(git, "Placeable List", code + "_wd_call");
            Value(call, "LocName").GetProperty("0").GetString().Should().Be("???");
            Number(call, "Appearance").Should().Be(479);
            Number(call, "Static").Should().Be(0);
            Number(call, "Useable").Should().Be(1);
            Number(call, "Plot").Should().Be(1);
            Text(call, "OnUsed").Should().Be("quest_enc");
            Local(call, "QUEST_ID").GetString().Should().EndWith("_breach");
            Local(call, "QUEST_STATE").GetInt32().Should().Be(1);
            Local(call, "VISIBILITY_HIDDEN_DEFAULT").GetInt32().Should().Be(1);
            Local(call, "VISIBILITY_OBJECT_ID").GetString().Should().Be(Text(call, "Tag"));
            Local(call, "QUEST_ENCOUNTER_ID").GetString().Should().Be(Local(call, "QUEST_ID").GetString() + "_warden");
            Local(call, "QUEST_ENCOUNTER_RESREF").GetString().Should().Be("cp_" + code + "_wd");
            Local(call, "QUEST_ENCOUNTER_COOLDOWN_MINUTES").GetInt32().Should().Be(60);
            Local(call, "QUEST_ENCOUNTER_IDLE_MINUTES").GetInt32().Should().Be(10);
            var spawn = Tagged(git, "WaypointList", Local(call, "QUEST_ENCOUNTER_WAYPOINT").GetString()!);
            Text(spawn, "Tag").Should().Be(Text(Load("utw", "wp_" + code + "_wd"), "Tag"));
            Distance(call, spawn, true).Should().BeGreaterThan(5);
            var altar = altars.Single(a => Distance(a, call) < 3);
            Distance(altar, call).Should().BeInRange(2.3, 2.5);
            var forward = (Number(call, "X") - Number(altar, "X")) * Math.Sin(Number(altar, "Bearing"))
                          - (Number(call, "Y") - Number(altar, "Y")) * Math.Cos(Number(altar, "Bearing"));
            forward.Should().BeGreaterThan(2);
            var blood = props.Where(p => new[] { 2711d, 2714d }.Contains(Number(p, "Appearance")) && Distance(p, altar) < 3).ToArray();
            blood.Count(p => Math.Abs(Number(p, "Z") - Number(altar, "Z") - .74) < .01).Should().Be(2);
            blood.Count(p => Math.Abs(Number(p, "Z") - Number(altar, "Z")) < .01).Should().BeGreaterThanOrEqualTo(2);
        }
        props.Where(p => Number(p, "Appearance") != 479).Should().OnlyContain(p => Number(p, "Static") == 1 && Number(p, "Useable") == 0 && Text(p, "OnUsed") == "");
    }

    [TestCaseSource(nameof(Dungeons))]
    public void Dungeon_HasSpacedSlicingAndSupplyCrateSpawnPoints(Dungeon d)
    {
        var waypoints = List(Load("git", d.Area), "WaypointList");
        var slicing = waypoints.Where(w => Text(w, "Tag") == "SLICING_TERMINAL_T5").ToArray();
        var supplies = waypoints.Where(w => Text(w, "Tag") == "SUPPLY_CRATE_T5").ToArray();
        slicing.Should().HaveCount(2, $"{d.Area} offers tier-five slicing at fixed wall-side spots");
        supplies.Length.Should().BeInRange(3, 4, $"{d.Area} offers supply crates for gathering");
        var points = slicing.Concat(supplies).ToArray();
        for (var i = 0; i < points.Length; i++)
        for (var j = i + 1; j < points.Length; j++)
            Math.Sqrt(Math.Pow(Number(points[i], "XPosition") - Number(points[j], "XPosition"), 2) +
                      Math.Pow(Number(points[i], "YPosition") - Number(points[j], "YPosition"), 2)).Should().BeGreaterThan(6,
                $"{d.Area} spreads its crates through the dungeon instead of piling them together");
    }

    [TestCaseSource(nameof(Dungeons))]
    public void SharedFootprints_AreStacksRestingOnTheirBase(Dungeon d)
    {
        // Model top heights of the props other dressing may rest on.
        var tops = new Dictionary<string, double>
        {
            ["_mdrn_pl_crgc1m"] = 0.75,
            ["swlor_0054"] = 1.09,
            ["zep_monument001"] = 0.54,
        };
        // Floor coverings (rugs, scorch marks) lie under other dressing by design.
        var floorCoverings = new[] { "aswtor_155", "aswtor_156", "_mdrn_pl_carpt04", "zep_bf_mark_001" };
        var props = List(Load("git", d.Area), "Placeable List")
            .Where(p => Number(p, "Appearance") != 479 && Text(p, "TemplateResRef") != "zep_altarevil3" &&
                        !Text(p, "TemplateResRef").StartsWith("zep_bloodstain") &&
                        !floorCoverings.Contains(Text(p, "TemplateResRef")))
            .ToArray();
        for (var i = 0; i < props.Length; i++)
        for (var j = i + 1; j < props.Length; j++)
        {
            if (Distance(props[i], props[j]) >= 0.35)
                continue;
            var (lower, upper) = Number(props[i], "Z") <= Number(props[j], "Z") ? (props[i], props[j]) : (props[j], props[i]);
            var resref = Text(lower, "TemplateResRef");
            tops.Should().ContainKey(resref, $"{d.Area} only stacks dressing on crates, tables and slabs");
            (Number(upper, "Z") - Number(lower, "Z")).Should().BeApproximately(tops[resref], 0.06,
                $"{Text(upper, "TemplateResRef")} must rest on top of {resref} in {d.Area}, not hover or sink");
        }
    }

    [Test]
    public void SupplyCrates_AreTierFiveScavengePointsWithEndgameResources()
    {
        var table = new SupplyCrateSpawnDefinition().BuildSpawnTables()["SUPPLY_CRATE_T5"];
        table.Spawns.Should().ContainSingle(s => s.Resref == "supply_crate_5" && s.Type == ObjectType.Placeable);
        table.ResourceDespawnMinutes.Should().BePositive();

        var crate = Load("utp", "supply_crate_5");
        Text(crate, "OnOpen").Should().Be("scav_opened");
        Text(crate, "OnInvDisturbed").Should().Be("scav_disturbed");
        Number(crate, "Useable").Should().Be(1);
        Number(crate, "HasInventory").Should().Be(1);
        Local(crate, "SCAVENGE_POINT_LEVEL").GetInt32().Should().Be(5);
        var lootTableId = Local(crate, "SCAVENGE_POINT_LOOT_TABLE_NAME").GetString();
        var loot = new HarvestingLootTableDefinition().BuildLootTables();
        loot.Should().ContainKey(lootTableId);
        loot[lootTableId].Should().NotBeEmpty();
        foreach (var item in loot[lootTableId])
            File.Exists(Path.Combine(Root, "Module", "uti", item.Resref + ".uti.json")).Should().BeTrue(item.Resref);
    }

    [TestCaseSource(nameof(Dungeons))]
    public void Travel_RequiresThePackageKeyAndConnectsBothDirections(Dungeon d)
    {
        var hub = Load("git", d.Hub);
        var dungeon = Load("git", d.Area);
        var arena = Load("git", d.Arena);
        var gate = List(hub, "Placeable List").Single(p => Text(p, "OnUsed") == "teleport" && Local(p, "DESTINATION").GetString() == d.Area + "_entry");
        Local(gate, "KEY_ITEM_ID").GetInt32().Should().Be(d.Key);
        Local(gate, "TELEPORT_PARTY_MEMBERS").GetInt32().Should().Be(1);
        var entrance = Tagged(dungeon, "WaypointList", d.Area + "_entry");
        var exit = Tagged(dungeon, "Door List", d.Area + "_exit");
        Text(exit, "LinkedTo").Should().Be(d.Area + "_hub");
        var returning = Tagged(hub, "WaypointList", Text(exit, "LinkedTo"));
        var entryVector = new[] { Number(entrance, "XPosition") - Number(exit, "X"), Number(entrance, "YPosition") - Number(exit, "Y") };
        var hubVector = new[] { Number(gate, "X") - Number(returning, "XPosition"), Number(gate, "Y") - Number(returning, "YPosition") };
        (entryVector[0] * hubVector[0] + entryVector[1] * hubVector[1]).Should().BeGreaterThan(0, "travel direction continues through the doorway");
        var door = Tagged(dungeon, "Door List", d.Area + "_arena");
        Text(door, "LinkedTo").Should().Be(d.Arena + "_entry");
        Tagged(arena, "WaypointList", Text(door, "LinkedTo"));
        var arenaExit = Tagged(arena, "Placeable List", d.Arena + "_exit");
        Text(arenaExit, "OnUsed").Should().Be("teleport");
        Local(arenaExit, "DESTINATION").GetString().Should().Be(d.Area + "_return");
        var bossReturn = Tagged(dungeon, "WaypointList", d.Area + "_return");
        Number(bossReturn, "YPosition").Should().BeLessThan(Number(door, "Y"));
        Number(bossReturn, "YOrientation").Should().Be(-1);
        foreach (var doorway in List(dungeon, "Door List"))
        {
            Number(doorway, "LinkedToFlags").Should().Be(2);
            Number(doorway, "Locked").Should().Be(0);
            Number(doorway, "Plot").Should().Be(1);
        }
    }
}
