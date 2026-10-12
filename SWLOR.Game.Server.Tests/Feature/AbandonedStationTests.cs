using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Feature.QuestDefinition;
using SWLOR.Game.Server.Feature.SpawnDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Tests.Feature;

[NonParallelizable]
public class AbandonedStationTests
{
    private const string Main = "zomb_abanstation";
    private const string Restricted = "zomb_abanstatio2";
    private const string Chambers = "zomb_abanstatio3";
    private const string QuestId = "put_them_to_rest";
    private const int ItemPropertyNpcLevel = 99;
    private static readonly string[] StationAreas = { Main, Restricted, Chambers };
    private static readonly string[] Arrivals =
        { "ABAN_STATION_LANDING", "ABANDONED_STATION_LANDING", "DIRECTORS_QUARTERS_LANDING", "ABAN_STATION_RES_TO_MAIN_LANDING" };

    // Spawn table -> area, minimum spawn points, inclusive level band.
    private static readonly (string Table, string Area, int MinPoints, int MinLevel, int MaxLevel)[] Floors =
    {
        ("ABANDONED_STATION_MAIN", Main, 24, 20, 24),
        ("ABANDONED_STATION_RESTRICTED", Restricted, 18, 25, 28),
        ("ABANDONED_STATION_RANCOR", Restricted, 1, 30, 30),
        ("ABANDONED_STATION_CHAMBERS", Chambers, 2, 29, 29),
        ("ABANDONED_STATION_DIRECTOR", Chambers, 1, 30, 30),
    };

    private object _originalItemNames = null!;

    private static PropertyInfo ItemNameCache => typeof(Cache)
        .GetProperty("ItemNamesByResref", BindingFlags.NonPublic | BindingFlags.Static)!;

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

    [OneTimeSetUp]
    public void LoadItemNamesForGuildTaskTitles()
    {
        Guild.LoadData();
        NPCGroup.CacheData();
        var names = Directory.EnumerateFiles(Path.Combine(Root, "Module", "uti"), "*.uti.json")
            .ToDictionary(path => Path.GetFileName(path)[..^".uti.json".Length], path =>
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                return document.RootElement.GetProperty("LocalizedName").GetProperty("value")
                    .TryGetProperty("0", out var name) ? name.GetString() ?? string.Empty : string.Empty;
            });
        _originalItemNames = ItemNameCache.GetValue(null)!;
        ItemNameCache.SetValue(null, names);
    }

    [OneTimeTearDown]
    public void RestoreItemNames() => ItemNameCache.SetValue(null, _originalItemNames);

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
    private static (double X, double Y) Position(JsonElement o) =>
        o.TryGetProperty("XPosition", out _) ? (Number(o, "XPosition"), Number(o, "YPosition")) : (Number(o, "X"), Number(o, "Y"));
    private static double Distance((double X, double Y) a, (double X, double Y) b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));

    // Every waypoint a station door or teleport placeable delivers players to.
    private static HashSet<string> TransitionTargets => StationAreas.Select(area => Load("git", area))
        .SelectMany(git => List(git, "Door List").Where(d => Number(d, "LinkedToFlags") == 2).Select(d => Text(d, "LinkedTo"))
            .Concat(List(git, "Placeable List").Select(p => Local(p, "DESTINATION")?.GetString()).OfType<string>()))
        .ToHashSet();

    [Test]
    public void Station_IsReachedOnlyByShipFromViscaraOrbit()
    {
        foreach (var area in StationAreas)
            Local(Load("git", area), "PLANET_TYPE_ID")!.Value.GetInt32().Should().Be((int)PlanetType.Viscara, area);

        var docks = List(Load("git", Main), "WaypointList").Where(w => Text(w, "Tag") == "STARSHIP_DOCKPOINT").ToArray();
        docks.Should().ContainSingle("pilots land in the docking bay from Viscara orbit");
        Value(docks[0], "LocalizedName").GetProperty("0").GetString().Should().Be("Abandoned Station");

        // No shuttle, door, trigger or conversation outside the station sends players in.
        var outsideReferences = Directory.EnumerateFiles(Path.Combine(Root, "Module", "git"), "*.git.json")
            .Where(path => !StationAreas.Any(area => Path.GetFileName(path) == $"{area}.git.json"))
            .Where(path => Arrivals.Any(File.ReadAllText(path).Contains))
            .Select(Path.GetFileName);
        outsideReferences.Should().BeEmpty();
        Directory.EnumerateFiles(Path.Combine(Root, "SWLOR.Game.Server", "ConversationData"), "*.conversation.json")
            .Where(path => Arrivals.Any(File.ReadAllText(path).Contains))
            .Select(Path.GetFileName).Should().BeEmpty("no NPC teleports players to the station");
    }

    [Test]
    public void EscapePod_ReturnsStrandedPlayersToVeles()
    {
        var pod = List(Load("git", Main), "Placeable List").Single(p => Text(p, "Tag") == "aban_shuttle_exit");
        Value(pod, "LocName").GetProperty("0").GetString().Should().Be("Escape Pod");
        Text(pod, "OnUsed").Should().Be("teleport");
        Number(pod, "Useable").Should().Be(1);
        Number(pod, "Plot").Should().Be(1);
        Local(pod, "DESTINATION")!.Value.GetString().Should().Be("ABAN_STATION_EXIT_LANDING");
        List(Load("git", "velesinterior"), "WaypointList").Count(w => Text(w, "Tag") == "ABAN_STATION_EXIT_LANDING")
            .Should().Be(1, "the pod lands at the Veles starport on Viscara");
    }

    [Test]
    public void StationTransitions_UseNativeLinksToExistingWaypoints()
    {
        var waypoints = StationAreas.SelectMany(area => List(Load("git", area), "WaypointList"))
            .Select(w => Text(w, "Tag")).ToList();
        foreach (var area in StationAreas)
        {
            foreach (var door in List(Load("git", area), "Door List"))
            {
                Local(door, "DESTINATION_WAYPOINT").Should().BeNull($"{area} {Text(door, "Tag")} relies on a deleted legacy door script");
                Local(door, "DESTINATION_AREA_TAG").Should().BeNull();
                if (Number(door, "LinkedToFlags") == 2)
                    waypoints.Count(t => t == Text(door, "LinkedTo")).Should().Be(1, $"{area} {Text(door, "Tag")}");
            }
        }

        var main = Load("git", Main);
        var elevator = List(main, "Door List").Single(d => Text(d, "Tag") == "_mdrn_dt_elev");
        Text(elevator, "LinkedTo").Should().Be("ABANDONED_STATION_LANDING");
        Number(elevator, "Locked").Should().Be(0, "the open zone has no key-card gate");

        var turbolift = List(Load("git", Restricted), "Placeable List").Single(p => Text(p, "Tag") == "aban_ele_to_office");
        Number(turbolift, "Useable").Should().Be(1);
        Local(turbolift, "DESTINATION")!.Value.GetString().Should().Be("DIRECTORS_QUARTERS_LANDING");

        var exit = List(Load("git", Chambers), "Door List").Single(d => Text(d, "Tag") == "aban_director_exit");
        Text(exit, "LinkedTo").Should().Be("aban_cham_to_main");
        Number(exit, "Locked").Should().Be(0);

        // The rancor containment has no walkmesh between its two doors, so each door transitions to the far side.
        var restricted = Load("git", Restricted);
        var doors = List(restricted, "Door List");
        Text(doors.Single(d => Text(d, "Tag") == "aban_to_rancor_exit"), "LinkedTo").Should().Be("aban_rancor_inside");
        Text(doors.Single(d => Text(d, "Tag") == "aban_to_rancor_entrance"), "LinkedTo").Should().Be("aban_rancor_outside");
        var rancor = Position(List(restricted, "WaypointList").Single(w => Text(w, "Tag") == "ABANDONED_STATION_RANCOR"));
        rancor.Y.Should().BeGreaterThan(Position(List(restricted, "WaypointList").Single(w => Text(w, "Tag") == "aban_rancor_inside")).Y,
            "the rancor spawns inside its containment");
    }

    [Test]
    public void Floors_SpawnTheirRosterAwayFromArrivals()
    {
        var tables = new AbandonedStationSpawnDefinition().BuildSpawnTables();
        var loot = new AbandonedStationLootTableDefinition().BuildLootTables().Keys
            .Concat(new MusicLootTableDefinition().BuildLootTables().Keys).ToHashSet();

        foreach (var (table, area, minPoints, minLevel, maxLevel) in Floors)
        {
            var git = Load("git", area);
            var points = List(git, "WaypointList").Where(w => Text(w, "Tag") == table).ToArray();
            points.Length.Should().BeGreaterThanOrEqualTo(minPoints, table);
            if (area != Chambers)
            {
                var arrivals = List(git, "WaypointList").Where(w => Arrivals.Contains(Text(w, "Tag")) || TransitionTargets.Contains(Text(w, "Tag")))
                    .Select(Position).ToArray();
                foreach (var point in points)
                    arrivals.Min(a => Distance(Position(point), a)).Should().BeGreaterThanOrEqualTo(15, $"{table} spawns must stay clear of arrivals");
            }

            var spawns = tables[table].Spawns;
            spawns.Should().NotBeEmpty();
            spawns.Should().OnlyContain(s => s.AIFlags.HasFlag(AIFlag.ReturnHome), $"{table} must leash home");
            foreach (var spawn in spawns)
            {
                var creature = Load("utc", spawn.Resref);
                Local(creature, "QUEST_NPC_GROUP_ID")!.Value.GetInt32().Should().Be((int)NPCGroupType.AbandonedStation_Horror, spawn.Resref);
                foreach (var lootTable in Locals(creature).Where(v => Text(v, "Name").StartsWith("LOOT_TABLE_")))
                    loot.Should().Contain(Value(lootTable, "Value").GetString()!.Split(',')[0], spawn.Resref);

                var level = List(Load("uti", Text(List(creature, "Equip_ItemList").Single(e => e.GetProperty("__struct_id").GetInt32() == 131072), "EquippedRes")), "PropertiesList")
                    .Single(p => Number(p, "PropertyName") == ItemPropertyNpcLevel);
                Number(level, "CostValue").Should().BeInRange(minLevel, maxLevel, $"{spawn.Resref} belongs to the {table} level band");
            }
        }
    }

    [Test]
    public void ResourceNodes_UseTierThreeTerminalsAndCrates()
    {
        foreach (var area in new[] { Main, Restricted })
        {
            var tags = List(Load("git", area), "WaypointList").Select(w => Text(w, "Tag")).ToList();
            tags.Count(t => t == "SLICING_TERMINAL_T3").Should().Be(1, area);
            tags.Count(t => t == "SUPPLY_CRATE_T3").Should().Be(2, area);
        }

        new SupplyCrateSpawnDefinition().BuildSpawnTables()["SUPPLY_CRATE_T3"].Spawns.Single().Resref.Should().Be("supply_crate_3");
        var crate = Load("utp", "supply_crate_3");
        Local(crate, "SCAVENGE_POINT_LOOT_TABLE_NAME")!.Value.GetString().Should().Be("SUPPLY_CRATE_3");
        new HarvestingLootTableDefinition().BuildLootTables().Should().ContainKey("SUPPLY_CRATE_3");
    }

    [Test]
    public void PutThemToRest_IsASmallRepeatableKillQuestFromTelford()
    {
        var quest = new ViscaraQuestDefinition().BuildQuests()[QuestId];
        quest.IsRepeatable.Should().BeTrue();
        var kill = quest.States[1].GetObjectives().Should().ContainSingle().Which.Should().BeOfType<KillTargetObjective>().Subject;
        kill.Group.Should().Be(NPCGroupType.AbandonedStation_Horror);
        kill.Amount.Should().Be(20);
        quest.States[2].GetObjectives().Should().BeEmpty();
        quest.Rewards.Should().HaveCount(2);
        quest.Rewards.OfType<GoldReward>().Single().Amount.Should().Be(400);
        quest.Rewards.OfType<XPReward>().Single().Amount.Should().Be(950);

        // Telford waits at the Veles starport, where the station's escape pods come down.
        var starport = Load("git", "velesinterior");
        var telford = List(starport, "Creature List").Single(c => Text(c, "TemplateResRef") == "zomb_telford");
        Text(telford, "ScriptDialogue").Should().Be("dialog_start");
        var podLanding = List(starport, "WaypointList").Single(w => Text(w, "Tag") == "ABAN_STATION_EXIT_LANDING");
        Distance(Position(telford), Position(podLanding)).Should().BeLessThan(10);
        Text(telford, "Conversation").Should().Be("zomb_telconv");
        var graph = File.ReadAllText(Path.Combine(Root, "SWLOR.Game.Server", "ConversationData", "zomb_telconv.conversation.json"));
        graph.Should().Contain("action-accept-quest").And.Contain("action-advance-quest").And.Contain(QuestId);
        graph.Should().NotContain("action-teleport", "the station is reached by ship, not by Telford's shuttle");
    }

    [Test]
    public void HuntersGuild_OffersAStationTaskAtTierTwo()
    {
        var task = new HuntersGuildQuestDefinition().BuildQuests().Values
            .Single(q => q.States.Values.SelectMany(s => s.GetObjectives()).OfType<KillTargetObjective>()
                .Any(o => o.Group == NPCGroupType.AbandonedStation_Horror));
        task.GuildType.Should().Be(GuildType.HuntersGuild);
        task.GuildRank.Should().Be(1, "level 20-30 enemies sit with the other rank 2 hunting tasks");
    }
}
