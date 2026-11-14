using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.TradeDefinition;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Dangerous starlanes (plan section 12): the classification of a lane by the system it leaves and the one it reaches,
/// the texts, the real module lanes and the wiring.
/// </summary>
public class StarlaneDangerTests
{
    private static string Root()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); directory != null; directory = directory.Parent)
            if (Directory.Exists(Path.Combine(directory.FullName, "Module", "git")) && Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
                return directory.FullName;
        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }

    private static string ReadSource(params string[] relativePath) =>
        File.ReadAllText(relativePath.Aggregate(Path.Combine(Root(), "SWLOR.Game.Server"), Path.Combine));

    // Classification

    [TestCase("viscaraorbit", "moncalaorbit", StarlaneDanger.HigherTier)]
    [TestCase("moncalaorbit", "viscaraorbit", StarlaneDanger.None)]
    [TestCase("moncalaorbit", "moncalaorbit", StarlaneDanger.None)]
    [TestCase("viscaraorbit", "space_viscbelt", StarlaneDanger.HigherTier | StarlaneDanger.Zone)]
    [TestCase("space_viscbelt", "viscaraorbit", StarlaneDanger.None)]
    [TestCase("hutlar_orbit", "space_midrimneb", StarlaneDanger.HigherTier | StarlaneDanger.Zone)]
    [TestCase("space_korriban", "space_korrdebris", StarlaneDanger.Zone)]
    [TestCase("space_korrdebris", "space_korriban", StarlaneDanger.HigherTier)]
    [TestCase("space_midrim2", "space_wspulsar", StarlaneDanger.HigherTier | StarlaneDanger.WildSpace)]
    [TestCase("space_korriban", "space_wsblkhole", StarlaneDanger.WildSpace)]
    [TestCase("space_wspulsar", "space_wsblkhole", StarlaneDanger.None)]
    [TestCase("space_wspulsar", "moncalaorbit", StarlaneDanger.None)]
    public void Classify_FollowsTierZoneAndWildSpace(string from, string to, StarlaneDanger expected)
    {
        StarlaneDangerRules.Classify(from, to).Should().Be(expected);
    }

    [TestCase("not_a_space_area", "moncalaorbit")]
    [TestCase("viscaraorbit", "not_a_space_area")]
    [TestCase("viscaraorbit", "")]
    [TestCase("", "")]
    [TestCase(null, null)]
    public void Classify_NeverFlagsAnUnknownSystem(string from, string to)
    {
        StarlaneDangerRules.Classify(from, to).Should().Be(StarlaneDanger.None);
    }

    [Test]
    public void AnUnknownOrigin_IsNeverHigherTierButStillWarnsOfZonesAndWildSpace()
    {
        StarlaneDangerRules.Classify("some_ground_area", "space_viscbelt").Should().Be(StarlaneDanger.Zone);
        StarlaneDangerRules.Classify("some_ground_area", "space_wspulsar").Should().Be(StarlaneDanger.WildSpace);
    }

    [Test]
    public void Tiers_UseTheOrbitCatalogAndWildSpaceIsFivePointFive()
    {
        foreach (var orbit in TradeCatalog.Orbits)
            StarlaneDangerRules.GetTierTenths(orbit.AreaResref).Should().Be(orbit.Tier * 10, orbit.AreaResref);

        foreach (var system in WildSpaceTuning.Systems)
            StarlaneDangerRules.GetTierTenths(system.AreaResref).Should().Be(55);

        StarlaneDangerRules.FormatTier(30).Should().Be("3");
        StarlaneDangerRules.FormatTier(55).Should().Be("5.5");
    }

    [Test]
    public void TheZoneList_IsTheThreeSmallAndMediumZones()
    {
        StarlaneDangerRules.ZoneAreaResrefs.Should().BeEquivalentTo("space_viscbelt", "space_midrimneb", "space_korrdebris");
        foreach (var zone in StarlaneDangerRules.ZoneAreaResrefs)
            TradeLookup.GetOrbit(zone).Should().NotBeNull(zone);
    }

    // Texts

    [Test]
    public void TheNameTag_ListsTheReasons()
    {
        StarlaneDangerRules.GetNameTag(StarlaneDanger.None, "moncalaorbit").Should().BeEmpty();
        StarlaneDangerRules.GetNameTag(StarlaneDanger.HigherTier, "moncalaorbit").Should().Be(" [Danger: Tier 2]");
        StarlaneDangerRules.GetNameTag(StarlaneDanger.HigherTier | StarlaneDanger.Zone, "space_viscbelt").Should().Be(" [Danger: Tier 2, Zone]");
        StarlaneDangerRules.GetNameTag(StarlaneDanger.Zone, "space_korrdebris").Should().Be(" [Danger: Zone]");
        StarlaneDangerRules.GetNameTag(StarlaneDanger.HigherTier | StarlaneDanger.WildSpace, "space_wspulsar")
            .Should().Be(" [Danger: Tier 5.5, Wild Space]");
        StarlaneDangerRules.GetNameTag(StarlaneDanger.Zone, "space_korrdebris").Should().StartWith(StarlaneDangerRules.NameMarker);
    }

    [Test]
    public void TheWarning_NamesTheDestinationAndEveryReason()
    {
        var warning = StarlaneDangerRules.GetWarning(
            StarlaneDanger.HigherTier | StarlaneDanger.Zone, "viscaraorbit", "space_viscbelt", "Space - Viscara Asteroid Belt");
        warning.Should().StartWith("Warning: Space - Viscara Asteroid Belt is dangerous.");
        warning.Should().Contain("tier 2 space, above the tier 1 you are in");
        warning.Should().Contain("Small and Medium hulls");

        var wild = StarlaneDangerRules.GetWarning(StarlaneDanger.WildSpace, "space_korriban", "space_wsblkhole", "Wild Space - The Maw");
        wild.Should().Contain("other players can attack you").And.Contain("loses every module").And.Contain("the rest are destroyed");

        StarlaneDangerRules.GetWarning(StarlaneDanger.None, "viscaraorbit", "moncalaorbit", "Mon Cala").Should().BeEmpty();
    }

    [Test]
    public void TheDescription_KeepsTheOriginalTextAndAddsTheWarning()
    {
        var description = StarlaneDangerRules.GetDescription("A starlane.", StarlaneDanger.HigherTier, "viscaraorbit", "moncalaorbit");
        description.Should().StartWith("A starlane.").And.Contain("Dangerous lane.").And.Contain("tier 2 space");

        StarlaneDangerRules.GetDescription(string.Empty, StarlaneDanger.Zone, "space_korriban", "space_korrdebris")
            .Should().StartWith("Dangerous lane.");
        StarlaneDangerRules.GetDescription("A starlane.", StarlaneDanger.None, "viscaraorbit", "viscaraorbit").Should().Be("A starlane.");
    }

    [Test]
    public void TheDescription_DropsTheEngineStockText()
    {
        const string stock = "There is nothing special about this object.";

        StarlaneDangerRules.GetDescription(stock, StarlaneDanger.HigherTier, "viscaraorbit", "moncalaorbit", stock)
            .Should().StartWith("Dangerous lane.").And.NotContain("nothing special");
        StarlaneDangerRules.GetDescription("A starlane.", StarlaneDanger.HigherTier, "viscaraorbit", "moncalaorbit", stock)
            .Should().StartWith("A starlane.");
    }

    [Test]
    public void EveryText_IsAscii()
    {
        foreach (var danger in new[]
                 {
                     StarlaneDanger.HigherTier, StarlaneDanger.Zone, StarlaneDanger.WildSpace,
                     StarlaneDanger.HigherTier | StarlaneDanger.Zone | StarlaneDanger.WildSpace,
                 })
        {
            var texts = new[]
            {
                StarlaneDangerRules.GetNameTag(danger, "space_wspulsar"),
                StarlaneDangerRules.GetWarning(danger, "space_midrim2", "space_wspulsar", "Wild Space - Vesh Pulsar"),
                StarlaneDangerRules.GetDescription("A starlane.", danger, "space_midrim2", "space_wspulsar"),
            };
            foreach (var text in texts)
                text.Should().MatchRegex("^[\\x09\\x0A\\x20-\\x7E]*$");
        }
    }

    // The module's lanes

    private sealed record ModuleLane(string Area, string Destination);

    private static (List<ModuleLane> Lanes, Dictionary<string, string> WaypointAreas) ReadModuleLanes()
    {
        var areas = TradeCatalog.Orbits.Select(x => x.AreaResref).Concat(StarlaneDangerRules.ZoneAreaResrefs).Distinct().ToList();
        var lanes = new List<ModuleLane>();
        var waypointAreas = new Dictionary<string, string>();
        foreach (var area in areas)
        {
            var path = Path.Combine(Root(), "Module", "git", $"{area}.git.json");
            if (!File.Exists(path))
                continue;

            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            foreach (var placeable in root.GetProperty("Placeable List").GetProperty("value").EnumerateArray())
            {
                if (placeable.GetProperty("TemplateResRef").GetProperty("value").GetString() != "tele_obj")
                    continue;

                var destination = placeable.GetProperty("VarTable").GetProperty("value").EnumerateArray()
                    .Where(x => x.GetProperty("Name").GetProperty("value").GetString() == "DESTINATION")
                    .Select(x => x.GetProperty("Value").GetProperty("value").GetString()).SingleOrDefault();
                if (destination != null && destination.StartsWith("STARLANE_LANDING"))
                    lanes.Add(new ModuleLane(area, destination));
            }

            foreach (var waypoint in root.GetProperty("WaypointList").GetProperty("value").EnumerateArray())
            {
                var tag = waypoint.GetProperty("Tag").GetProperty("value").GetString()!;
                if (tag.StartsWith("STARLANE_LANDING"))
                    waypointAreas[tag] = area;
            }
        }

        return (lanes, waypointAreas);
    }

    [Test]
    public void TheModuleLanes_AreClassifiedByWhereTheyLead()
    {
        var (lanes, waypointAreas) = ReadModuleLanes();
        lanes.Should().NotBeEmpty();

        foreach (var lane in lanes)
        {
            waypointAreas.Should().ContainKey(lane.Destination, $"{lane.Area} lane to {lane.Destination}");
            var to = waypointAreas[lane.Destination];
            var danger = StarlaneDangerRules.Classify(lane.Area, to);

            danger.HasFlag(StarlaneDanger.Zone).Should().Be(StarlaneDangerRules.IsZone(to), $"{lane.Area} to {to}");
            danger.HasFlag(StarlaneDanger.WildSpace).Should().BeFalse($"no starlane leads into Wild Space ({lane.Area} to {to})");
            danger.HasFlag(StarlaneDanger.HigherTier).Should().Be(
                StarlaneDangerRules.GetTierTenths(to) > StarlaneDangerRules.GetTierTenths(lane.Area), $"{lane.Area} to {to}");
        }
    }

    [Test]
    public void TheZones_AreReachedByADangerousLaneAndLeftByASafeOne()
    {
        var (lanes, waypointAreas) = ReadModuleLanes();
        foreach (var zone in StarlaneDangerRules.ZoneAreaResrefs)
        {
            lanes.Where(x => waypointAreas[x.Destination] == zone).Should().NotBeEmpty($"{zone} has a lane in");
            foreach (var lane in lanes.Where(x => waypointAreas[x.Destination] == zone))
                StarlaneDangerRules.Classify(lane.Area, zone).Should().HaveFlag(StarlaneDanger.Zone);

            foreach (var lane in lanes.Where(x => x.Area == zone))
                StarlaneDangerRules.Classify(zone, waypointAreas[lane.Destination]).Should().NotHaveFlag(StarlaneDanger.Zone);
        }
    }

    // Wiring

    [Test]
    public void TheJump_WarnsThePilotAndPartyAndTheAreaTagsItsLanes()
    {
        var script = ReadSource("Feature", "PlaceableScripts.cs");
        script.Should().Contain("Space.WarnOfDangerousStarlane(user, fromArea, destinationArea)");
        script.Should().Contain("Space.WarnOfDangerousStarlane(partyMember, fromArea, destinationArea)");
        // The warning comes before the jump.
        script.IndexOf("WarnOfDangerousStarlane(user", StringComparison.Ordinal)
            .Should().BeLessThan(script.IndexOf("TeleportCreature(user, location, waypoint)", StringComparison.Ordinal));

        var lanes = ReadSource("Service", "SpaceStarlanes.cs");
        lanes.Should().Contain("[NWNEventHandler(ScriptName.OnAreaEnter)]").And.Contain("StarlaneDangerRules.Classify(")
            .And.Contain("SetName(lane").And.Contain("SetDescription(lane");
    }
}
