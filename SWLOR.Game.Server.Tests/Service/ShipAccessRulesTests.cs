using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.SpawnDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.SpawnService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Docking limits by hull size, the Small/Medium-only areas and the starlane graph (plan sections 7 and 12, W6b), the
/// three Small/Medium-only zones (W6c) and the six Wild Space systems (W12c).
/// </summary>
public class ShipAccessRulesTests
{
    // The five landing areas of the major ports, which take Capital hulls: Veles, Mon Cala, Anchorhead,
    // Smuggler's Moon Station and Korriban.
    private static readonly string[] CapitalDockAreas =
    {
        "velesinterior", "moncaladaccityex", "tat_anc_astropor", "spacenarshaddung", "korribanlandingp",
    };

    private static readonly string[] AbandonedStationAreas = { "zomb_abanstation", "zomb_abanstatio2", "zomb_abanstatio3" };

    // The W6c zones: (area, parent orbit, creature table, resource table).
    private static readonly (string Area, string Parent, string Creatures, string Resources)[] Zones =
    {
        ("space_viscbelt", "viscaraorbit", "SPACE_VISCARA_BELT", "SPACE_RESOURCES_VISCARA_BELT"),
        ("space_midrimneb", "hutlar_orbit", "SPACE_MIDRIM_NEBULA", "SPACE_RESOURCES_MIDRIM_NEBULA_ZONE"),
        ("space_korrdebris", "space_korriban", "SPACE_KORRIBAN_DEBRIS", "SPACE_RESOURCES_KORRIBAN_DEBRIS"),
    };

    private static StarportDockRefusal Dock(ShipSize size, bool player = false, bool capitalDock = false, int level = 0, ShipSize areaMax = ShipSize.Invalid) =>
        ShipAccessRules.GetStarportRefusal(new StarportDockRequest
        {
            ShipSize = size,
            IsPlayerStarport = player,
            CapitalDock = capitalDock,
            PlayerStarportLevel = level,
            AreaMaxSize = areaMax,
        });

    // Dock classes

    [TestCase(ShipSize.Small)]
    [TestCase(ShipSize.Medium)]
    [TestCase(ShipSize.Large)]
    public void EveryNpcDockTakesLargeShipsAndSmaller(ShipSize size)
    {
        Dock(size).Should().Be(StarportDockRefusal.None);
        Dock(size, capitalDock: true).Should().Be(StarportDockRefusal.None);
    }

    [Test]
    public void CapitalShipsDockOnlyAtTheMajorPorts()
    {
        Dock(ShipSize.Capital).Should().Be(StarportDockRefusal.NpcDockTooSmall);
        Dock(ShipSize.Capital, capitalDock: true).Should().Be(StarportDockRefusal.None);
    }

    [TestCase(ShipSize.Small, 1)]
    [TestCase(ShipSize.Medium, 1)]
    [TestCase(ShipSize.Large, 1)]
    [TestCase(ShipSize.Large, 0)]
    [TestCase(ShipSize.Large, 5)]
    public void APlayerStarportTakesLargeShipsAtAnyLevel(ShipSize size, int level)
    {
        Dock(size, player: true, level: level).Should().Be(StarportDockRefusal.None);
    }

    [TestCase(0, StarportDockRefusal.PlayerStarportTooSmall)]
    [TestCase(1, StarportDockRefusal.PlayerStarportTooSmall)]
    [TestCase(2, StarportDockRefusal.None)]
    [TestCase(3, StarportDockRefusal.None)]
    [TestCase(5, StarportDockRefusal.None)]
    public void APlayerStarportTakesCapitalShipsFromLevelTwo(int level, StarportDockRefusal expected)
    {
        Dock(ShipSize.Capital, player: true, level: level).Should().Be(expected);
        ShipAccessRules.CapitalStarportLevel.Should().Be(2);
    }

    [Test]
    public void ACapitalDockFlagMeansNothingAtAPlayerStarport()
    {
        // The flag belongs to NPC landing areas; a player starport is judged by its level alone.
        Dock(ShipSize.Capital, player: true, capitalDock: true, level: 1).Should().Be(StarportDockRefusal.PlayerStarportTooSmall);
    }

    [TestCase(ShipSize.Small, StarportDockRefusal.None)]
    [TestCase(ShipSize.Medium, StarportDockRefusal.None)]
    [TestCase(ShipSize.Large, StarportDockRefusal.AreaSizeLimit)]
    [TestCase(ShipSize.Capital, StarportDockRefusal.AreaSizeLimit)]
    public void ASmallAndMediumOnlyLocationTurnsBiggerShipsAway(ShipSize size, StarportDockRefusal expected)
    {
        Dock(size, areaMax: ShipSize.Medium).Should().Be(expected);
        Dock(size, capitalDock: true, areaMax: ShipSize.Medium).Should().Be(expected, "the area limit beats the major-port flag");
    }

    [Test]
    public void AShipWithNoHullSizeIsRefusedEverywhere()
    {
        Dock(ShipSize.Invalid).Should().Be(StarportDockRefusal.UnknownSize);
        Dock(ShipSize.Invalid, player: true, level: 5).Should().Be(StarportDockRefusal.UnknownSize);
        Dock(ShipSize.Invalid, capitalDock: true).Should().Be(StarportDockRefusal.UnknownSize);
    }

    [Test]
    public void EveryRefusalHasAClearMessage()
    {
        foreach (var refusal in Enum.GetValues<StarportDockRefusal>().Where(x => x != StarportDockRefusal.None))
        {
            var text = ShipAccessRules.DescribeStarportRefusal(refusal, ShipSize.Medium);
            text.Should().NotBeNullOrWhiteSpace(refusal.ToString());
            text.Should().NotContain("  ");
            text.Should().EndWith(".");
        }

        ShipAccessRules.DescribeStarportRefusal(StarportDockRefusal.None, ShipSize.Medium).Should().BeEmpty();
        ShipAccessRules.DescribeStarportRefusal(StarportDockRefusal.NpcDockTooSmall, ShipSize.Invalid)
            .Should().Contain("Veles").And.Contain("Mon Cala").And.Contain("Anchorhead")
            .And.Contain("Smuggler's Moon Station").And.Contain("Korriban");
        ShipAccessRules.DescribeStarportRefusal(StarportDockRefusal.PlayerStarportTooSmall, ShipSize.Invalid).Should().Contain("level 2");
        ShipAccessRules.DescribeStarportRefusal(StarportDockRefusal.AreaSizeLimit, ShipSize.Medium).Should().Be("Only Small and Medium ships can dock here.");
    }

    // Areas

    [TestCase(ShipSize.Small, ShipSize.Invalid, true)]
    [TestCase(ShipSize.Capital, ShipSize.Invalid, true)]
    [TestCase(ShipSize.Small, ShipSize.Medium, true)]
    [TestCase(ShipSize.Medium, ShipSize.Medium, true)]
    [TestCase(ShipSize.Large, ShipSize.Medium, false)]
    [TestCase(ShipSize.Capital, ShipSize.Medium, false)]
    [TestCase(ShipSize.Medium, ShipSize.Small, false)]
    [TestCase(ShipSize.Large, ShipSize.Large, true)]
    [TestCase(ShipSize.Capital, ShipSize.Large, false)]
    [TestCase(ShipSize.Invalid, ShipSize.Medium, false)]
    public void AnAreaAdmitsHullsUpToItsMaxShipSize(ShipSize ship, ShipSize areaMax, bool expected)
    {
        ShipAccessRules.AreaAdmits(ship, areaMax).Should().Be(expected);
    }

    [Test]
    public void TheAreaRefusalNamesTheAreaAndTheAdmittedHulls()
    {
        ShipAccessRules.DescribeAreaRefusal("Viscara Asteroid Belt", ShipSize.Medium)
            .Should().Be("Your ship is too large for Viscara Asteroid Belt. Only Small and Medium ships can enter.");
        ShipAccessRules.DescribeSizeLimit(ShipSize.Small).Should().Be("Small");
        ShipAccessRules.DescribeSizeLimit(ShipSize.Large).Should().Be("Small, Medium and Large");
    }

    [Test]
    public void TheAreaLocalsAreNamedForTheModule()
    {
        Space.MaxShipSizeVariable.Should().Be("MAX_SHIP_SIZE");
        Space.CapitalDockVariable.Should().Be("CAPITAL_DOCK");
        ((int)ShipSize.Medium).Should().Be(2, "MAX_SHIP_SIZE stores the ShipSize value");
    }

    // Wiring

    [Test]
    public void EveryWayIntoAnAreaOrADockAsksTheSizeRules()
    {
        var teleport = ReadServerSource("Feature", "PlaceableScripts.cs");
        teleport.Should().Contain("Space.CanFlownShipEnterArea(user, destinationArea)", "starlane jumps");

        var space = ReadServerSource("Service", "Space.cs");
        space.Should().Contain("CanShipEnterArea(player, dbShip, spaceArea)", "launches from a dock");

        var docking = ReadServerSource("Service", "SpaceDocking.cs");
        docking.Should().Contain("CanShipEnterArea(player, visitor, GetAreaFromLocation(hostLocation))", "undocking beside a host");
        docking.Should().Contain("GetStarportRefusal(visitor, hostProperty.Positions[PropertyLocationType.DockPosition])", "undocking into the host's starport");
        docking.Should().Contain("GetStarportRefusal(visitor, hostDock) == StarportDockRefusal.None", "undocking every ship of an unregistered host");

        var dialog = ReadServerSource("Feature", "DialogDefinition", "StarportDockDialog.cs");
        dialog.Should().Contain("Space.GetStarportRefusal(activeShip, landingAreaForSize");

        var separator = Path.DirectorySeparatorChar;
        foreach (var file in Directory.EnumerateFiles(ServerDirectory, "*.cs", SearchOption.AllDirectories)
                     .Where(x => !x.Contains($"{separator}obj{separator}") && !x.Contains($"{separator}bin{separator}")))
        {
            File.ReadAllText(file).Should().NotContain("FitsStarport", Path.GetFileName(file));
        }
    }

    // Module data

    [Test]
    public void OnlyTheFiveMajorPortsAreFlaggedAsCapitalDocks()
    {
        var dockAreas = AreasContaining("STARSHIP_DOCKPOINT");

        dockAreas.Should().Contain(CapitalDockAreas, "the flagged areas are real NPC landing areas");
        foreach (var area in dockAreas)
        {
            var flagged = AreaLocal(area, "CAPITAL_DOCK");
            if (CapitalDockAreas.Contains(area))
                flagged.Should().Be(1, area);
            else
                flagged.Should().BeNull(area);
        }

        // Nothing else carries the flag, so a typo cannot open a dock to Capital hulls.
        AreasContaining("CAPITAL_DOCK").Should().BeEquivalentTo(CapitalDockAreas);
    }

    [Test]
    public void TheAbandonedStationAdmitsOnlySmallAndMediumShips()
    {
        foreach (var area in AbandonedStationAreas)
            AreaLocal(area, "MAX_SHIP_SIZE").Should().Be((int)ShipSize.Medium, area);

        AreasContaining("MAX_SHIP_SIZE").Should().BeEquivalentTo(AbandonedStationAreas.Concat(Zones.Select(x => x.Area)),
            "only the Abandoned Station and the three Small/Medium zones limit hull size");
    }

    // Small/Medium-only zones (W6c)

    [Test]
    public void TheZonesAdmitOnlySmallAndMediumShips()
    {
        foreach (var zone in Zones)
        {
            AreaLocal(zone.Area, "SPACE").Should().Be(1, zone.Area);
            var max = AreaLocal(zone.Area, "MAX_SHIP_SIZE");
            max.Should().Be((int)ShipSize.Medium, zone.Area);
            ShipAccessRules.AreaAdmits(ShipSize.Medium, (ShipSize)max!.Value).Should().BeTrue();
            ShipAccessRules.AreaAdmits(ShipSize.Large, (ShipSize)max.Value).Should().BeFalse();
        }
    }

    [Test]
    public void TheZonesPayBetterThanOpenOrbit()
    {
        var creatures = new SpaceSpawnDefinition().BuildSpawnTables();
        var resources = new SpaceResourceSpawnDefinition().BuildSpawnTables();
        foreach (var zone in Zones)
        {
            AreaString(zone.Area, "CREATURE_SPAWN_TABLE_ID").Should().Be(zone.Creatures);
            AreaString(zone.Area, "RESOURCE_SPAWN_TABLE_ID").Should().Be(zone.Resources);
            creatures.Should().ContainKey(zone.Creatures);
            resources.Should().ContainKey(zone.Resources);

            // An open 32x32 orbit gets 50 resource nodes and 45 ships (Spawn.CalculateResourceSpawnCount and
            // CalculateCreatureSpawnCount): the zones take 1.5x the nodes and 1.25x the ships.
            AreaLocal(zone.Area, "RESOURCE_SPAWN_COUNT").Should().Be(75, zone.Area);
            AreaLocal(zone.Area, "CREATURE_SPAWN_COUNT").Should().Be(56, zone.Area);
        }

        Share(resources["SPACE_RESOURCES_VISCARA_BELT"], "asteroid_durel").Should().BeGreaterThan(0.5, "Durelium is the belt's main ore");
        Share(resources["SPACE_RESOURCES_MIDRIM_NEBULA_ZONE"], "spc_gascloud").Should().BeGreaterThan(
            Share(resources["SPACE_RESOURCES_MIDRIM_NEBULA"], "spc_gascloud"), "the nebula zone is denser in gas than Mid Rim 1");
        Share(resources["SPACE_RESOURCES_KORRIBAN_DEBRIS"], "asteroid_arda").Should().BeGreaterThan(
            Share(resources["SPACE_RESOURCES_KORRIBAN_ORBIT"], "asteroid_arda"));

        AreaLocal("space_korrdebris", ShipSalvageRules.AurodiumPercentVariable).Should().Be(15);
        ShipSalvageRules.AurodiumPercent(5, 15).Should().BeGreaterThan(ShipSalvageRules.AurodiumPercent(5, 0));
        creatures["SPACE_KORRIBAN_DEBRIS"].Spawns.Select(x => x.Resref)
            .Should().Contain(new[] { "t5derturret1", "t5derturret2", "t5minelayer" });
    }

    [Test]
    public void TheDerelictTurretsUseTheUnusedTurretRowsOnThePlatformPreset()
    {
        foreach (var (resref, appearance) in new[] { ("t5derturret1", 2153), ("t5derturret2", 2154) })
        {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepositoryRoot, "Module", "utc", $"{resref}.utc.json")));
            var root = document.RootElement;
            root.GetProperty("Appearance_Type").GetProperty("value").GetInt32().Should().Be(appearance);
            root.GetProperty("Tag").GetProperty("value").GetString().Should().Be("t5platform", "the space system finds a ship's preset by tag");
            root.GetProperty("WalkRate").GetProperty("value").GetInt32().Should().Be(1, "turrets are immobile");
        }
    }

    [Test]
    public void TheZonesAreRegisteredAreas()
    {
        var ifo = File.ReadAllText(Path.Combine(RepositoryRoot, "Module", "ifo", "module.ifo.json"));
        var planet = ReadServerSource("Service", "Planet.cs");
        foreach (var zone in Zones)
        {
            ifo.Should().Contain($"\"value\": \"{zone.Area}\"", "the module lists the area");
            planet.Should().Contain($"\"{zone.Area}\",", "Planet treats it as space");
            using var are = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepositoryRoot, "Module", "are", $"{zone.Area}.are.json")));
            are.RootElement.GetProperty("ResRef").GetProperty("value").GetString().Should().Be(zone.Area);
            are.RootElement.GetProperty("Tag").GetProperty("value").GetString().Should().Be(zone.Area);
            are.RootElement.GetProperty("Name").GetProperty("value").GetProperty("0").GetString().Should().StartWith("Space - ");
        }
    }

    // Wild Space (W12c)

    [Test]
    public void TheWildSpaceSystemsAreFlaggedTierFiveAndAHalf()
    {
        WildSpaceTuning.Systems.Should().HaveCount(6);
        WildSpaceTuning.Systems.Select(x => x.Environment).Should().OnlyHaveUniqueItems()
            .And.BeEquivalentTo(Enum.GetValues<WildSpaceEnvironment>().Where(x => x != WildSpaceEnvironment.Invalid));
        foreach (var system in WildSpaceTuning.Systems)
        {
            AreaLocal(system.AreaResref, "SPACE").Should().Be(1, system.AreaResref);
            AreaLocal(system.AreaResref, Space.WildSpaceVariable).Should().Be(1, system.AreaResref);
            AreaLocal(system.AreaResref, WildSpaceTuning.TierVariable).Should().Be(55, "tier 5.5 is stored in tenths");
            AreaLocal(system.AreaResref, WildSpaceTuning.EnvironmentVariable).Should().Be((int)system.Environment);
            AreaLocal(system.AreaResref, "MAX_SHIP_SIZE").Should().BeNull("every hull may enter Wild Space; rifts cap the size");
            AreaString(system.AreaResref, "CREATURE_SPAWN_TABLE_ID").Should().Be(system.SpawnTable);
            WildSpaceRules.ContentTier(AreaLocal(system.AreaResref, WildSpaceTuning.TierVariable)!.Value).Should().Be(WildSpaceTuning.ContentTier);
        }

        AreasContaining("\"WILD_SPACE\"").Should().BeEquivalentTo(WildSpaceTuning.Systems.Select(x => x.AreaResref),
            "only the Wild Space systems drop modules on a PvP death");
    }

    [Test]
    public void TheWildSpaceSystemsHaveNoStarlanesOrDocks()
    {
        foreach (var system in WildSpaceTuning.Systems)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepositoryRoot, "Module", "git", $"{system.AreaResref}.git.json")));
            var root = document.RootElement;
            root.GetProperty("Placeable List").GetProperty("value").EnumerateArray()
                .Select(x => x.GetProperty("TemplateResRef").GetProperty("value").GetString())
                .Should().NotContain(new[] { "tele_obj", "spc_dock" }, $"{system.AreaResref} is reached only through rifts");
            // Besides the stuck waypoint, only the player station anchor sites (W13a) and the W11o wreck sites, which hold
            // no starlane or dock.
            root.GetProperty("WaypointList").GetProperty("value").EnumerateArray()
                .Select(x => x.GetProperty("Tag").GetProperty("value").GetString())
                .Where(x => x != SpaceHazardTuning.WreckSiteTag)
                .Should().BeEquivalentTo(new[] { "STUCK_WAYPOINT" }.Concat(
                    SWLOR.Game.Server.Service.StationService.StationRules.GetAnchorsInArea(system.AreaResref).Select(x => x.Tag)));
        }

        var graph = LaneGraph();
        foreach (var system in WildSpaceTuning.Systems)
        {
            graph.Should().NotContainKey(system.AreaResref);
            graph.Values.Should().NotContain(x => x.Contains(system.AreaResref), "no starlane or Jump Beacon leads into Wild Space");
        }
    }

    [Test]
    public void TheWildSpaceSystemsAreRegisteredAreas()
    {
        var ifo = File.ReadAllText(Path.Combine(RepositoryRoot, "Module", "ifo", "module.ifo.json"));
        var planet = ReadServerSource("Service", "Planet.cs");
        var toolset = File.ReadAllText(Path.Combine(RepositoryRoot, "toolset", "categories.json"));
        var creatures = new SpaceSpawnDefinition().BuildSpawnTables();
        foreach (var system in WildSpaceTuning.Systems)
        {
            ifo.Should().Contain($"\"value\": \"{system.AreaResref}\"");
            planet.Should().Contain($"\"{system.AreaResref}\",");
            toolset.Should().Contain($"\"{system.AreaResref}\"");
            using var are = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepositoryRoot, "Module", "are", $"{system.AreaResref}.are.json")));
            are.RootElement.GetProperty("ResRef").GetProperty("value").GetString().Should().Be(system.AreaResref);
            are.RootElement.GetProperty("Tag").GetProperty("value").GetString().Should().Be(system.AreaResref);
            are.RootElement.GetProperty("Name").GetProperty("value").GetProperty("0").GetString().Should().Be(system.Name);
            File.Exists(Path.Combine(RepositoryRoot, "Module", "gic", $"{system.AreaResref}.gic.json")).Should().BeTrue();
            ScanningTuning.Areas.Should().ContainSingle(x => x.AreaResref == system.AreaResref && x.WildSpace && x.Tier == WildSpaceTuning.ContentTier);
        }

        foreach (var system in WildSpaceTuning.Systems)
        {
            creatures.Should().ContainKey(system.SpawnTable);
            creatures[system.SpawnTable].Spawns.Where(x => !x.IsRare).Select(x => x.Resref).Should().BeEquivalentTo(WildSpaceTuning.RiftRaiderTags);
        }
        WildSpaceTuning.KnownOrbits.Should().OnlyContain(x => ScanningTuning.Areas.Any(a => a.AreaResref == x && !a.WildSpace));
    }

    private static double Share(SpawnTable table, string prefix) =>
        (double)table.Spawns.Where(x => x.Resref.StartsWith(prefix)).Sum(x => x.Weight) / table.Spawns.Sum(x => x.Weight);

    // Starlanes

    private sealed record Lane(string Area, string Name, string Destination, double X, double Y);

    private sealed record Landing(string Area, string Tag, double X, double Y);

    private static (List<Lane> Lanes, List<Landing> Landings) ReadLanes()
    {
        var lanes = new List<Lane>();
        var landings = new List<Landing>();
        foreach (var area in AreasContaining("STARLANE_LANDING"))
        {
            var file = (Name: area, Text: File.ReadAllText(Path.Combine(RepositoryRoot, "Module", "git", $"{area}.git.json")));
            using var document = JsonDocument.Parse(file.Text);
            var root = document.RootElement;
            foreach (var placeable in root.GetProperty("Placeable List").GetProperty("value").EnumerateArray())
            {
                if (placeable.GetProperty("TemplateResRef").GetProperty("value").GetString() != "tele_obj")
                    continue;

                var destination = placeable.GetProperty("VarTable").GetProperty("value").EnumerateArray()
                    .Where(x => x.GetProperty("Name").GetProperty("value").GetString() == "DESTINATION")
                    .Select(x => x.GetProperty("Value").GetProperty("value").GetString()).SingleOrDefault();
                if (destination == null || !destination.StartsWith("STARLANE_LANDING"))
                    continue;

                lanes.Add(new Lane(file.Name,
                    placeable.GetProperty("LocName").GetProperty("value").GetProperty("0").GetString()!,
                    destination,
                    placeable.GetProperty("X").GetProperty("value").GetDouble(),
                    placeable.GetProperty("Y").GetProperty("value").GetDouble()));
            }

            foreach (var waypoint in root.GetProperty("WaypointList").GetProperty("value").EnumerateArray())
            {
                var tag = waypoint.GetProperty("Tag").GetProperty("value").GetString()!;
                if (tag.StartsWith("STARLANE_LANDING"))
                    landings.Add(new Landing(file.Name, tag,
                        waypoint.GetProperty("XPosition").GetProperty("value").GetDouble(),
                        waypoint.GetProperty("YPosition").GetProperty("value").GetDouble()));
            }
        }

        return (lanes, landings);
    }

    private static Dictionary<string, HashSet<string>> LaneGraph()
    {
        var (lanes, landings) = ReadLanes();
        var graph = new Dictionary<string, HashSet<string>>();
        foreach (var lane in lanes)
        {
            var landing = landings.Single(x => x.Tag == lane.Destination);
            if (!graph.TryGetValue(lane.Area, out var set))
                graph[lane.Area] = set = new HashSet<string>();
            set.Add(landing.Area);
        }

        return graph;
    }

    [Test]
    public void EveryLaneLandsOnOneWaypointInAnotherSystem()
    {
        var (lanes, landings) = ReadLanes();

        lanes.Should().HaveCountGreaterThan(15);
        landings.Select(x => x.Tag).Should().OnlyHaveUniqueItems("a lane finds its landing by tag across the module");
        foreach (var lane in lanes)
        {
            var landing = landings.Where(x => x.Tag == lane.Destination).ToList();
            landing.Should().ContainSingle($"{lane.Area} / {lane.Name} -> {lane.Destination}");
            landing[0].Area.Should().NotBe(lane.Area, $"{lane.Name} must lead to another system");
        }
    }

    [Test]
    public void EveryLaneReturnsTheWayItCame()
    {
        var (lanes, landings) = ReadLanes();
        foreach (var lane in lanes)
        {
            var arrival = landings.Single(x => x.Tag == lane.Destination);
            var back = lanes.Where(x => x.Area == arrival.Area && landings.Single(l => l.Tag == x.Destination).Area == lane.Area).ToList();
            back.Should().NotBeEmpty($"{lane.Area} -> {arrival.Area} has no lane back");

            // The ship arrives beside the lane that leads back to where it came from.
            back.Min(x => Math.Sqrt(Math.Pow(x.X - arrival.X, 2) + Math.Pow(x.Y - arrival.Y, 2)))
                .Should().BeLessThan(10, $"{lane.Destination} should sit beside the lane back to {lane.Area}");
        }
    }

    [Test]
    public void TheKorribanRouteReturnsThroughEverySystemItCrossed()
    {
        var graph = LaneGraph();

        graph["viscaraorbit"].Should().Contain("space_midrim1");
        graph["space_midrim1"].Should().Contain("space_midrim2").And.Contain("viscaraorbit");
        graph["space_midrim2"].Should().Contain("space_korriban").And.Contain("space_midrim1");
        graph["space_midrim2"].Should().NotContain("viscaraorbit", "the return skipped Mid Rim 1 (bug 22)");
        graph["space_korriban"].Should().BeEquivalentTo(new[] { "space_midrim2", "space_korrdebris" });
    }

    [Test]
    public void EachZoneIsALaneFromItsParentOrbitWithALaneBack()
    {
        var graph = LaneGraph();
        foreach (var zone in Zones)
        {
            graph[zone.Parent].Should().Contain(zone.Area);
            graph[zone.Area].Should().BeEquivalentTo(new[] { zone.Parent }, "a zone is a dead end off its parent orbit");
        }
    }

    [Test]
    public void SmugglersMoonIsReachableAndLeadsToHutlarAndTatooine()
    {
        var graph = LaneGraph();

        graph["narshadorbit"].Should().BeEquivalentTo(new[] { "hutlar_orbit", "tatooineorbit" });
        graph["hutlar_orbit"].Should().Contain("narshadorbit");
        graph["tatooineorbit"].Should().Contain("narshadorbit");

        foreach (var start in graph.Keys)
            Reachable(graph, start).Should().Contain("narshadorbit", $"{start} should reach Smuggler's Moon by starlane");
    }

    [Test]
    public void ThereIsNoDantooineKorribanLane()
    {
        var graph = LaneGraph();

        graph["dantooineorbit"].Should().NotContain("space_korriban");
        graph["space_korriban"].Should().NotContain("dantooineorbit");
        graph["dantooineorbit"].Should().BeEquivalentTo(new[] { "moncalaorbit" });
    }

    [Test]
    public void EverySpaceSystemWithALaneIsConnectedToTheRest()
    {
        var graph = LaneGraph();
        var all = graph.Keys.Concat(graph.Values.SelectMany(x => x)).ToHashSet();

        foreach (var start in all)
            Reachable(graph, start).Should().BeEquivalentTo(all, start);
    }

    private static HashSet<string> Reachable(Dictionary<string, HashSet<string>> graph, string start)
    {
        var seen = new HashSet<string> { start };
        var queue = new Queue<string>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var next = queue.Dequeue();
            if (!graph.TryGetValue(next, out var neighbors))
                continue;

            foreach (var neighbor in neighbors.Where(seen.Add))
                queue.Enqueue(neighbor);
        }

        return seen;
    }

    // Files

    private static string RepositoryRoot
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;
            return directory!.FullName;
        }
    }

    private static string ServerDirectory => Path.Combine(RepositoryRoot, "SWLOR.Game.Server");

    private static readonly string[] Needles = { "STARSHIP_DOCKPOINT", "CAPITAL_DOCK", "MAX_SHIP_SIZE", "STARLANE_LANDING", "\"WILD_SPACE\"" };

    // One pass over the area files (about 650 MB), keeping only which needles each contains.
    private static readonly Lazy<Dictionary<string, bool[]>> AreaScan = new(() =>
    {
        var needles = Needles.Select(System.Text.Encoding.ASCII.GetBytes).ToArray();
        var result = new Dictionary<string, bool[]>();
        foreach (var path in Directory.EnumerateFiles(Path.Combine(RepositoryRoot, "Module", "git"), "*.git.json"))
        {
            var bytes = File.ReadAllBytes(path);
            result[Path.GetFileName(path)[..^".git.json".Length]] = needles.Select(x => bytes.AsSpan().IndexOf(x) >= 0).ToArray();
        }

        return result;
    });

    private static List<string> AreasContaining(string needle)
    {
        var index = Array.IndexOf(Needles, needle);
        return AreaScan.Value.Where(x => x.Value[index]).Select(x => x.Key).OrderBy(x => x, StringComparer.Ordinal).ToList();
    }

    private static int? AreaLocal(string area, string name)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepositoryRoot, "Module", "git", $"{area}.git.json")));
        return document.RootElement.GetProperty("VarTable").GetProperty("value").EnumerateArray()
            .Where(x => x.GetProperty("Name").GetProperty("value").GetString() == name)
            .Select(x => (int?)x.GetProperty("Value").GetProperty("value").GetInt32())
            .SingleOrDefault();
    }

    private static string? AreaString(string area, string name)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepositoryRoot, "Module", "git", $"{area}.git.json")));
        return document.RootElement.GetProperty("VarTable").GetProperty("value").EnumerateArray()
            .Where(x => x.GetProperty("Name").GetProperty("value").GetString() == name)
            .Select(x => x.GetProperty("Value").GetProperty("value").GetString())
            .SingleOrDefault();
    }

    private static string ReadServerSource(params string[] path) =>
        File.ReadAllText(Path.Combine(new[] { ServerDirectory }.Concat(path).ToArray())).Replace("\r\n", "\n");
}