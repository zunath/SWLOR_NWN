using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Feature.SpawnDefinition;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class HutlarDroidFoundryPlacementTests
{
    private const string Outpost = "hutlar_smuggleba";
    private const string OutpostDoor = "HUT_FDY_OUTPOST_DOOR";
    private const string OverseerTable = "HUTLAR_FOUNDRY_OVERSEER";

    private static readonly (string Resref, string Name, string SpawnTable, string[] Roster)[] Floors =
    {
        ("hut_fdy_intake", "Hutlar - Droid Foundry Intake", "HUTLAR_FOUNDRY_INTAKE",
            new[] { "fdy_hauler", "fdy_sentry", "fdy_welder", "fdy_smelter" }),
        ("hut_fdy_assembly", "Hutlar - Droid Foundry Assembly Line", "HUTLAR_FOUNDRY_ASSEMBLY",
            new[] { "fdy_linedef", "fdy_riveter", "fdy_reclaim", "fdy_forgewd" }),
        ("hut_fdy_core", "Hutlar - Droid Foundry Core", "HUTLAR_FOUNDRY_CORE",
            new[] { "fdy_wardroid", "fdy_siege", "fdy_hunter", "fdy_juggernt" }),
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
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "Module", type, $"{name}.{type}.json")));
        return document.RootElement.Clone();
    }

    private static JsonElement Value(JsonElement o, string key) => o.GetProperty(key).GetProperty("value");
    private static string Text(JsonElement o, string key) => Value(o, key).GetString()!;
    private static double Number(JsonElement o, string key) => Value(o, key).GetDouble();
    private static JsonElement[] List(JsonElement o, string key) => Value(o, key).EnumerateArray().ToArray();
    private static JsonElement[] Locals(JsonElement o) => o.TryGetProperty("VarTable", out _) ? List(o, "VarTable") : Array.Empty<JsonElement>();
    private static JsonElement? Local(JsonElement o, string key) =>
        Locals(o).Where(v => Text(v, "Name") == key).Select(v => (JsonElement?)Value(v, "Value")).SingleOrDefault();
    private static JsonElement Tagged(JsonElement o, string list, string tag) => List(o, list).Single(v => Text(v, "Tag") == tag);
    private static (double X, double Y) Position(JsonElement o) =>
        o.TryGetProperty("XPosition", out _) ? (Number(o, "XPosition"), Number(o, "YPosition")) : (Number(o, "X"), Number(o, "Y"));
    private static double Distance(JsonElement a, JsonElement b)
    {
        var (ax, ay) = Position(a);
        var (bx, by) = Position(b);
        return Math.Sqrt(Math.Pow(ax - bx, 2) + Math.Pow(ay - by, 2));
    }

    [Test]
    public void Floors_AreRegisteredInteriorHutlarAreasThatShareTheFoundryMap()
    {
        var areaList = List(Load("ifo", "module"), "Mod_Area_list");
        using var categories = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "toolset", "categories.json")));
        IEnumerable<JsonElement> Folders(JsonElement folder)
        {
            yield return folder;
            if (folder.TryGetProperty("children", out var children))
                foreach (var child in children.EnumerateArray().SelectMany(Folders)) yield return child;
        }
        var folder = categories.RootElement.GetProperty("sections").GetProperty("are").GetProperty("folders")
            .EnumerateArray().SelectMany(Folders)
            .Single(f => f.GetProperty("name").GetString() == "Droid Foundry");

        foreach (var (resref, name, _, _) in Floors)
        {
            var area = Load("are", resref);
            var git = Load("git", resref);
            Value(area, "Name").GetProperty("0").GetString().Should().Be(name);
            ((int)Number(area, "Flags") & 3).Should().Be(3, $"{resref} is an interior, underground floor that weather must not reach");
            areaList.Count(a => Text(a, "Area_Name") == resref).Should().Be(1);
            folder.GetProperty("members").EnumerateArray().Select(m => m.GetString()).Should().Contain(resref);
            Local(git, "PLANET_TYPE_ID")!.Value.GetInt32().Should().Be(8);
            Local(git, "MAP_KEY_ITEM_ID")!.Value.GetInt32().Should().Be((int)KeyItemType.HutlarDroidFoundryMap);
            Local(git, "IS_DUNGEON").Should().BeNull($"{resref} is an open zone");
            List(git, "Creature List").Should().BeEmpty("enemies come from respawning spawn waypoints");

            var comments = Load("gic", resref);
            foreach (var property in comments.EnumerateObject().Where(p => p.Value.ValueKind == JsonValueKind.Object && p.Value.GetProperty("type").GetString() == "list"))
                List(comments, property.Name).Length.Should().Be(List(git, property.Name).Length, $"{resref} {property.Name}");
        }
    }

    [Test]
    public void Floors_SpawnTheirOwnDroidsAndResourcesAwayFromArrivals()
    {
        foreach (var (resref, _, spawnTable, _) in Floors)
        {
            var git = Load("git", resref);
            var waypoints = List(git, "WaypointList");
            var arrivals = waypoints.Where(w => Text(w, "Tag").StartsWith("PG_"))
                .Concat(List(git, "Door List"))
                .Concat(List(git, "Placeable List").Where(p => Text(p, "OnUsed") == "teleport"))
                .ToArray();
            var spawns = waypoints.Where(w => Text(w, "Tag") == spawnTable || Text(w, "Tag") == OverseerTable).ToArray();

            waypoints.Count(w => Text(w, "Tag") == spawnTable).Should().BeGreaterThanOrEqualTo(20, $"{resref} needs open-zone enemy density");
            waypoints.Count(w => Text(w, "Tag") == OverseerTable).Should().Be(resref == "hut_fdy_core" ? 1 : 0);
            waypoints.Count(w => Text(w, "Tag") == "STUCK_WAYPOINT").Should().Be(1);
            foreach (var spawn in spawns)
                arrivals.Min(arrival => Distance(spawn, arrival)).Should().BeGreaterThanOrEqualTo(15, $"{resref} arrivals must stay clear of spawns");

            var resources = waypoints.Where(w => Text(w, "Tag") is "SLICING_TERMINAL_T5" or "SUPPLY_CRATE_T5").ToArray();
            resources.Count(w => Text(w, "Tag") == "SLICING_TERMINAL_T5").Should().Be(2);
            resources.Count(w => Text(w, "Tag") == "SUPPLY_CRATE_T5").Should().Be(4);
            for (var i = 0; i < resources.Length; i++)
                for (var j = i + 1; j < resources.Length; j++)
                    Distance(resources[i], resources[j]).Should().BeGreaterThan(6, $"{resref} resource points should be spread out");
        }
    }

    [Test]
    public void Travel_ChainsFromTheOutpostThroughEveryFloorAndBack()
    {
        var outpost = Load("git", Outpost);
        var intake = Load("git", "hut_fdy_intake");
        var assembly = Load("git", "hut_fdy_assembly");
        var core = Load("git", "hut_fdy_core");

        var entrance = Tagged(outpost, "Door List", OutpostDoor);
        Value(entrance, "LocName").GetProperty("0").GetString().Should().Be("Foundry Access");
        AssertDoorLink(entrance, intake, "PG_hut_fdy_intake_DOOR_ENT_1", 1);
        AssertDoorLink(Tagged(intake, "Door List", "PG_hut_fdy_intake_DOOR_ENT_1"), outpost, OutpostDoor, 1);
        AssertDoorLink(Tagged(intake, "Door List", "PG_hut_fdy_intake_DOOR_EXIT_1"), assembly, "PG_hut_fdy_assembly_ENT_1", 2);
        AssertTeleport(Tagged(assembly, "Placeable List", "PG_hut_fdy_assembly_TRANS_ENT_1"), intake, "PG_hut_fdy_intake_EXIT_1");
        AssertTeleport(Tagged(assembly, "Placeable List", "PG_hut_fdy_assembly_TRANS_EXIT_1"), core, "PG_hut_fdy_core_ENT_1");
        AssertDoorLink(Tagged(core, "Door List", "PG_hut_fdy_core_DOOR_ENT_1"), assembly, "PG_hut_fdy_assembly_EXIT_1", 2);
        AssertDoorLink(Tagged(core, "Door List", "PG_hut_fdy_core_DOOR_EXIT_1"), outpost, OutpostDoor, 1);

        var outpostExit = Tagged(outpost, "Placeable List", "InvisibleObjectLarge");
        Text(outpostExit, "OnUsed").Should().Be("teleport", "the Outpost is the foundry's hub and must lead back out");
        Local(outpostExit, "DESTINATION")!.Value.GetString().Should().Be("H_CloneBase_Exit");
        Local(outpostExit, "TELEPORT_PARTY_MEMBERS")!.Value.GetInt32().Should().Be(0);
    }

    [Test]
    public void SpawnTables_UseEachFloorsDroidRosterAndLeashHome()
    {
        var tables = new HutlarSpawnDefinition().BuildSpawnTables();
        var loot = new HutlarLootTableDefinition().BuildLootTables();

        foreach (var (_, _, spawnTable, roster) in Floors)
            AssertTable(spawnTable, roster);
        AssertTable(OverseerTable, new[] { "fdy_overseer" });

        void AssertTable(string tableId, string[] roster)
        {
            var spawns = tables[tableId].Spawns.Where(s => !s.IsRare).ToList();
            spawns.Select(s => s.Resref).Should().BeEquivalentTo(roster);
            spawns.Should().OnlyContain(s => s.Type == ObjectType.Creature && s.AIFlags.HasFlag(AIFlag.ReturnHome),
                $"{tableId} droids must leash home");

            foreach (var resref in roster)
            {
                var creature = Load("utc", resref);
                Number(creature, "Race").Should().Be((int)RacialType.Droid, resref);
                Number(creature, "FactionID").Should().Be(1, resref);
                var lootTables = Locals(creature)
                    .Where(v => Text(v, "Name").StartsWith("LOOT_TABLE_"))
                    .Select(v => Value(v, "Value").GetString()!.Split(',')[0])
                    .ToArray();
                lootTables.Should().NotBeEmpty(resref);
                lootTables.Should().OnlyContain(id => loot.ContainsKey(id), $"{resref} must drop from defined Hutlar loot tables");
            }
        }
    }

    private static void AssertDoorLink(JsonElement door, JsonElement targetArea, string target, int flags)
    {
        Text(door, "LinkedTo").Should().Be(target);
        Number(door, "LinkedToFlags").Should().Be(flags);
        Number(door, "Locked").Should().Be(0);
        Number(door, "Plot").Should().Be(1);
        var list = flags == 1 ? "Door List" : "WaypointList";
        List(targetArea, list).Count(o => Text(o, "Tag") == target).Should().Be(1, $"{target} must resolve to one destination");
    }

    private static void AssertTeleport(JsonElement placeable, JsonElement targetArea, string destination)
    {
        Text(placeable, "OnUsed").Should().Be("teleport");
        Number(placeable, "Plot").Should().Be(1);
        Local(placeable, "DESTINATION")!.Value.GetString().Should().Be(destination);
        Local(placeable, "TELEPORT_PARTY_MEMBERS")!.Value.GetInt32().Should().Be(0);
        List(targetArea, "WaypointList").Count(w => Text(w, "Tag") == destination).Should().Be(1);
    }
}
