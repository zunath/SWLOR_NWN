using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Service;

public class StarshipDockReturnTests
{
    private const string Station = "zomb_abanstation";
    private const string StationOrbit = "viscaraorbit";
    private const string Veles = "velesinterior";
    private const string Tatooine = "tat_anc_astropor";
    private const string TatooineOrbit = "tatooineorbit";
    private const string PlayerStarportId = "player-starport";

    private static readonly string[] StationAreas = { "zomb_abanstation", "zomb_abanstatio2", "zomb_abanstatio3" };
    private static readonly HashSet<string> ShipOnlyAreas = new(StationAreas);

    private static bool IsShipOnly(string areaResref) => ShipOnlyAreas.Contains(areaResref);

    private static (PropertyLocation Landing, PropertyLocation Orbit)? ViscaraStarport(string areaResref) =>
        areaResref.StartsWith("zomb_abanstati", StringComparison.Ordinal)
            ? (At(Veles, 1f), At(StationOrbit, 2f))
            : null;

    private static PropertyLocation At(string areaResref, float x) =>
        new() { AreaResref = areaResref, X = x, Y = x + 1f, Z = x + 2f, Orientation = x * 10f };

    private static PropertyLocation AtStarport(string propertyId) => new() { InstancePropertyId = propertyId, X = 3f, Y = 4f };

    private static StarshipDockReturnResult Restart(
        Dictionary<PropertyLocationType, PropertyLocation> positions,
        Func<string, bool> starportExists = null)
    {
        // Mirrors Property.CleanUpData for one starship.
        positions.Remove(PropertyLocationType.CurrentPosition);
        StarshipDockReturn.RepairSafeDock(positions, IsShipOnly, ViscaraStarport);
        return StarshipDockReturn.ReturnToSafeDock(positions, IsShipOnly, starportExists ?? (_ => true), ViscaraStarport);
    }

    private static void ShouldBeAt(PropertyLocation actual, PropertyLocation expected)
    {
        actual.Should().BeEquivalentTo(expected);
        actual.Should().NotBeSameAs(expected, "each position is stored separately");
    }

    private static Dictionary<PropertyLocationType, PropertyLocation> DockedAtStationAfterTatooine() => new()
    {
        [PropertyLocationType.DockPosition] = At(Station, 5f),
        [PropertyLocationType.SpacePosition] = At(StationOrbit, 6f),
        [PropertyLocationType.LastNPCDockPosition] = At(Tatooine, 7f),
        [PropertyLocationType.LastNPCSpacePosition] = At(TatooineOrbit, 8f),
    };

    [Test]
    public void ShipLeftAtShipOnlyLocation_ReturnsToLastSafeDockAndOrbit()
    {
        var positions = DockedAtStationAfterTatooine();
        var safeDock = positions[PropertyLocationType.LastNPCDockPosition];
        var safeOrbit = positions[PropertyLocationType.LastNPCSpacePosition];

        Restart(positions).Should().Be(StarshipDockReturnResult.ReturnedFromShipOnlyLocation);

        ShouldBeAt(positions[PropertyLocationType.DockPosition], safeDock);
        ShouldBeAt(positions[PropertyLocationType.SpacePosition], safeOrbit);
        positions[PropertyLocationType.LastNPCDockPosition].Should().BeSameAs(safeDock);
        positions[PropertyLocationType.LastNPCSpacePosition].Should().BeSameAs(safeOrbit);
    }

    [Test]
    public void ShipInSpaceFromShipOnlyLocation_ReturnsToLastSafeDock()
    {
        var positions = DockedAtStationAfterTatooine();
        positions[PropertyLocationType.CurrentPosition] = At(StationOrbit, 9f);

        Restart(positions).Should().Be(StarshipDockReturnResult.ReturnedFromShipOnlyLocation);

        positions.Should().NotContainKey(PropertyLocationType.CurrentPosition);
        positions[PropertyLocationType.DockPosition].AreaResref.Should().Be(Tatooine);
        positions[PropertyLocationType.SpacePosition].AreaResref.Should().Be(TatooineOrbit);
    }

    [Test]
    public void ShipAtRegularStarport_IsLeftUntouched()
    {
        var dock = At(Veles, 5f);
        var orbit = At(StationOrbit, 6f);
        var positions = new Dictionary<PropertyLocationType, PropertyLocation>
        {
            [PropertyLocationType.DockPosition] = dock,
            [PropertyLocationType.SpacePosition] = orbit,
            [PropertyLocationType.LastNPCDockPosition] = At(Tatooine, 7f),
            [PropertyLocationType.LastNPCSpacePosition] = At(TatooineOrbit, 8f),
        };

        Restart(positions).Should().Be(StarshipDockReturnResult.Unchanged);

        positions[PropertyLocationType.DockPosition].Should().BeSameAs(dock);
        positions[PropertyLocationType.SpacePosition].Should().BeSameAs(orbit);
    }

    [Test]
    public void ShipAtExistingPlayerStarport_IsLeftUntouched()
    {
        var dock = AtStarport(PlayerStarportId);
        var positions = new Dictionary<PropertyLocationType, PropertyLocation>
        {
            [PropertyLocationType.DockPosition] = dock,
            [PropertyLocationType.LastNPCDockPosition] = At(Tatooine, 7f),
        };

        Restart(positions, id => id == PlayerStarportId).Should().Be(StarshipDockReturnResult.Unchanged);

        positions[PropertyLocationType.DockPosition].Should().BeSameAs(dock);
    }

    [Test]
    public void ShipAtDeletedPlayerStarport_ReturnsToLastSafeDock()
    {
        var positions = new Dictionary<PropertyLocationType, PropertyLocation>
        {
            [PropertyLocationType.DockPosition] = AtStarport(PlayerStarportId),
            [PropertyLocationType.SpacePosition] = At(StationOrbit, 6f),
            [PropertyLocationType.LastNPCDockPosition] = At(Tatooine, 7f),
            [PropertyLocationType.LastNPCSpacePosition] = At(TatooineOrbit, 8f),
        };

        Restart(positions, _ => false).Should().Be(StarshipDockReturnResult.ReturnedFromMissingStarport);

        ShouldBeAt(positions[PropertyLocationType.DockPosition], positions[PropertyLocationType.LastNPCDockPosition]);
        positions[PropertyLocationType.DockPosition].InstancePropertyId.Should().BeEmpty();
        ShouldBeAt(positions[PropertyLocationType.SpacePosition], positions[PropertyLocationType.LastNPCSpacePosition]);
    }

    [Test]
    public void ShipWithoutSafeOrbit_ReturnsToSafeDockAndKeepsItsOrbit()
    {
        // Ships that have not docked at a regular starport since safe orbits were recorded.
        var positions = DockedAtStationAfterTatooine();
        positions.Remove(PropertyLocationType.LastNPCSpacePosition);
        var orbit = positions[PropertyLocationType.SpacePosition];

        Restart(positions).Should().Be(StarshipDockReturnResult.ReturnedFromShipOnlyLocation);

        positions[PropertyLocationType.DockPosition].AreaResref.Should().Be(Tatooine);
        positions[PropertyLocationType.SpacePosition].Should().BeSameAs(orbit);
    }

    [Test]
    public void ShipWithShipOnlySafeDock_ReturnsToThatLocationsPlanetStarport()
    {
        // Docking at the station recorded it as the safe dock before the station was marked ship-only.
        var positions = new Dictionary<PropertyLocationType, PropertyLocation>
        {
            [PropertyLocationType.DockPosition] = At(Station, 5f),
            [PropertyLocationType.SpacePosition] = At(StationOrbit, 6f),
            [PropertyLocationType.LastNPCDockPosition] = At(Station, 5f),
        };

        Restart(positions).Should().Be(StarshipDockReturnResult.ReturnedFromShipOnlyLocation);

        var (landing, orbit) = ViscaraStarport(Station)!.Value;
        positions[PropertyLocationType.DockPosition].Should().BeEquivalentTo(landing);
        positions[PropertyLocationType.SpacePosition].Should().BeEquivalentTo(orbit);
        positions[PropertyLocationType.LastNPCDockPosition].Should().BeEquivalentTo(landing);
        positions[PropertyLocationType.LastNPCSpacePosition].Should().BeEquivalentTo(orbit);
        positions[PropertyLocationType.DockPosition].Should().NotBeSameAs(positions[PropertyLocationType.LastNPCDockPosition]);
    }

    [Test]
    public void ShipOnlySafeDock_IsRepairedWhileTheShipStaysAtARegularStarport()
    {
        var dock = At(Tatooine, 7f);
        var positions = new Dictionary<PropertyLocationType, PropertyLocation>
        {
            [PropertyLocationType.DockPosition] = dock,
            [PropertyLocationType.LastNPCDockPosition] = At(Station, 5f),
        };

        StarshipDockReturn.RepairSafeDock(positions, IsShipOnly, ViscaraStarport).Should().BeTrue();
        StarshipDockReturn.ReturnToSafeDock(positions, IsShipOnly, _ => true, ViscaraStarport)
            .Should().Be(StarshipDockReturnResult.Unchanged);

        positions[PropertyLocationType.DockPosition].Should().BeSameAs(dock);
        positions[PropertyLocationType.LastNPCDockPosition].AreaResref.Should().Be(Veles);
        positions[PropertyLocationType.LastNPCSpacePosition].AreaResref.Should().Be(StationOrbit);
    }

    [Test]
    public void RegularSafeDock_IsNotRepaired()
    {
        var safeDock = At(Tatooine, 7f);
        var positions = new Dictionary<PropertyLocationType, PropertyLocation>
        {
            [PropertyLocationType.LastNPCDockPosition] = safeDock,
        };

        StarshipDockReturn.RepairSafeDock(positions, IsShipOnly, ViscaraStarport).Should().BeFalse();

        positions[PropertyLocationType.LastNPCDockPosition].Should().BeSameAs(safeDock);
        positions.Should().NotContainKey(PropertyLocationType.LastNPCSpacePosition);
    }

    [Test]
    public void ShipOnlyDockWithoutSafeDockOrPlanet_IsLeftInPlace()
    {
        var dock = At("unknown_station", 5f);
        var positions = new Dictionary<PropertyLocationType, PropertyLocation>
        {
            [PropertyLocationType.DockPosition] = dock,
        };

        StarshipDockReturn.ReturnToSafeDock(positions, _ => true, _ => true, _ => null)
            .Should().Be(StarshipDockReturnResult.NoSafeDock);

        positions[PropertyLocationType.DockPosition].Should().BeSameAs(dock);
        positions.Should().NotContainKey(PropertyLocationType.LastNPCDockPosition);
    }

    [Test]
    public void ShipWithoutDock_GetsItsSafeDockInsteadOfFailingTheBoot()
    {
        var positions = new Dictionary<PropertyLocationType, PropertyLocation>
        {
            [PropertyLocationType.LastNPCDockPosition] = At(Tatooine, 7f),
            [PropertyLocationType.LastNPCSpacePosition] = At(TatooineOrbit, 8f),
        };

        Restart(positions).Should().Be(StarshipDockReturnResult.RestoredMissingDock);

        ShouldBeAt(positions[PropertyLocationType.DockPosition], positions[PropertyLocationType.LastNPCDockPosition]);
        ShouldBeAt(positions[PropertyLocationType.SpacePosition], positions[PropertyLocationType.LastNPCSpacePosition]);
    }

    [Test]
    public void ShipWithoutDockOrSafeDock_IsReportedInsteadOfFailingTheBoot()
    {
        var positions = new Dictionary<PropertyLocationType, PropertyLocation>();

        Restart(positions).Should().Be(StarshipDockReturnResult.NoSafeDock);
        Restart(new Dictionary<PropertyLocationType, PropertyLocation>
        {
            [PropertyLocationType.DockPosition] = AtStarport(PlayerStarportId),
        }, _ => false).Should().Be(StarshipDockReturnResult.NoSafeDock);

        positions.Should().BeEmpty();
    }

    [Test]
    public void ReturnedShip_StaysAtItsSafeDockOnLaterRestarts()
    {
        var positions = DockedAtStationAfterTatooine();

        Restart(positions).Should().Be(StarshipDockReturnResult.ReturnedFromShipOnlyLocation);
        var dock = positions[PropertyLocationType.DockPosition];
        var orbit = positions[PropertyLocationType.SpacePosition];

        for (var restart = 0; restart < 3; restart++)
        {
            Restart(positions).Should().Be(StarshipDockReturnResult.Unchanged);
            positions[PropertyLocationType.DockPosition].Should().BeSameAs(dock);
            positions[PropertyLocationType.SpacePosition].Should().BeSameAs(orbit);
        }
    }

    [Test]
    public void ShipOnlyMarker_CoversEveryStationAreaAndNoRegularStarport()
    {
        var root = FindRepositoryRoot();
        var areas = Directory.EnumerateFiles(Path.Combine(root, "Module", "git"), "*.git.json")
            .ToDictionary(path => Path.GetFileName(path)[..^".git.json".Length], ReadArea);
        var shipOnly = areas.Where(area => area.Value.Variables.TryGetValue(StarshipDockReturn.ShipOnlyLocationVariable, out var value) && value == 1)
            .Select(area => area.Key)
            .ToList();

        shipOnly.Should().BeEquivalentTo(StationAreas, "the Abandoned Station is the only location reachable only by ship");
        foreach (var area in shipOnly)
        {
            areas[area].Variables.Should().ContainKey("PLANET_TYPE_ID",
                $"{area} names the planet whose starport receives ships that have no safe dock");
        }

        var landingTags = Enum.GetValues<PlanetType>()
            .Where(planet => planet != PlanetType.Invalid)
            .Select(planet => planet.GetAttribute<PlanetType, PlanetAttribute>().LandingWaypointTag)
            .Append("STARSHIP_DOCKPOINT")
            .ToHashSet();
        var regularStarports = areas.Where(area => area.Value.WaypointTags.Overlaps(landingTags)).Select(area => area.Key).ToList();
        regularStarports.Should().Contain("velesinterior");
        regularStarports.Should().NotIntersectWith(shipOnly, "docking at a regular starport must record it as the safe dock");
    }

    private static (Dictionary<string, int> Variables, HashSet<string> WaypointTags) ReadArea(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        var variables = new Dictionary<string, int>();
        if (root.TryGetProperty("VarTable", out var varTable))
        {
            foreach (var variable in varTable.GetProperty("value").EnumerateArray())
            {
                var value = variable.GetProperty("Value");
                if (value.GetProperty("type").GetString() == "int")
                    variables[variable.GetProperty("Name").GetProperty("value").GetString()!] = value.GetProperty("value").GetInt32();
            }
        }

        var waypointTags = new HashSet<string>();
        if (root.TryGetProperty("WaypointList", out var waypoints))
        {
            foreach (var waypoint in waypoints.GetProperty("value").EnumerateArray())
                waypointTags.Add(waypoint.GetProperty("Tag").GetProperty("value").GetString()!);
        }

        return (variables, waypointTags);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module", "git")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new DirectoryNotFoundException("Module directory not found.");
    }
}
