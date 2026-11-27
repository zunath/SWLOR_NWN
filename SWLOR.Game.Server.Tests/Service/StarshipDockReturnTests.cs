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
    private const string Cz220 = "czs220_hangar";
    private const string PlayerStarportId = "player-starport";

    private static readonly string[] StationAreas = { "zomb_abanstation", "zomb_abanstatio2", "zomb_abanstatio3" };
    private static readonly HashSet<string> ShipOnlyAreas = new(StationAreas);

    private static bool IsShipOnly(string areaResref) => ShipOnlyAreas.Contains(areaResref);

    // The Abandoned Station, Veles and Viscara's orbit all belong to Viscara, as Property.GetPlanetStarport resolves
    // them (an orbit through its planet's orbit waypoint, ModuleOrbits_ResolveToTheirPlanets).
    private static (PropertyLocation Landing, PropertyLocation Orbit)? ViscaraStarport(string areaResref) =>
        areaResref.StartsWith("zomb_abanstati", StringComparison.Ordinal) || areaResref is Veles or StationOrbit
            ? (At(Veles, 1f), At(StationOrbit, 2f))
            : null;

    private static (PropertyLocation Landing, PropertyLocation Orbit)? ViscaraOrTatooineStarport(string areaResref) =>
        areaResref is Tatooine or TatooineOrbit
            ? (At(Tatooine, 7f), At(TatooineOrbit, 8f))
            : ViscaraStarport(areaResref);

    // Viscara and CZ-220 share one orbit area, as in the module (viscaraorbit holds both orbit waypoints).
    private static (PropertyLocation Landing, PropertyLocation Orbit)? SystemStarport(PlanetType planet) => planet switch
    {
        PlanetType.Viscara => (At(Veles, 1f), OrbitAt(StationOrbit, 86f, 54f)),
        PlanetType.CZ220 => (At(Cz220, 11f), OrbitAt(StationOrbit, 103f, 33f)),
        PlanetType.Tatooine => (At(Tatooine, 7f), OrbitAt(TatooineOrbit, 80f, 52f)),
        _ => null
    };

    private static (PropertyLocation Landing, PropertyLocation Orbit)? SystemAreaStarport(string areaResref) => SystemStarport(
        IsShipOnly(areaResref) || areaResref == Veles ? PlanetType.Viscara
        : areaResref == Cz220 ? PlanetType.CZ220
        : areaResref == Tatooine ? PlanetType.Tatooine
        : PlanetType.Invalid);

    private static (PropertyLocation Landing, PropertyLocation Orbit)? SystemOrbitStarport(PropertyLocation orbit) =>
        SystemStarport(StarshipDockReturn.FindOrbitPlanet(orbit, new[] { PlanetType.Viscara, PlanetType.CZ220, PlanetType.Tatooine }
            .Select(planet => (planet, SystemStarport(planet)!.Value.Orbit))));

    private static StarshipDockReturnResult RestartInSystems(
        Dictionary<PropertyLocationType, PropertyLocation> positions,
        Func<string, bool> starportExists = null) =>
        StarshipDockReturn.ReturnToSafeDock(positions, IsShipOnly, starportExists ?? (_ => true), SystemAreaStarport,
            getOrbitStarport: SystemOrbitStarport);

    private static PropertyLocation OrbitAt(string areaResref, float x, float y) =>
        new() { AreaResref = areaResref, X = x, Y = y, Z = 3f };

    private static void ShouldBeAtStarport(Dictionary<PropertyLocationType, PropertyLocation> positions, PlanetType planet)
    {
        var (landing, orbit) = SystemStarport(planet)!.Value;
        positions[PropertyLocationType.DockPosition].Should().BeEquivalentTo(landing);
        positions[PropertyLocationType.SpacePosition].Should().BeEquivalentTo(orbit);
        positions[PropertyLocationType.LastNPCDockPosition].Should().BeEquivalentTo(landing);
        positions[PropertyLocationType.LastNPCSpacePosition].Should().BeEquivalentTo(orbit);
    }

    private static void ShouldBeAtVeles(Dictionary<PropertyLocationType, PropertyLocation> positions)
    {
        positions[PropertyLocationType.DockPosition].Should().BeEquivalentTo(At(Veles, 1f));
        positions[PropertyLocationType.SpacePosition].Should().BeEquivalentTo(At(StationOrbit, 2f));
        positions[PropertyLocationType.LastNPCDockPosition].Should().BeEquivalentTo(At(Veles, 1f));
        positions[PropertyLocationType.LastNPCSpacePosition].Should().BeEquivalentTo(At(StationOrbit, 2f));
        positions[PropertyLocationType.DockPosition].InstancePropertyId.Should().BeNullOrEmpty();
    }

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
    public void ShipLeftAtShipOnlyLocation_ReturnsToItsOwnPlanetsStarport_NotItsSafeDockInAnotherSystem()
    {
        // A restart must never carry a ship (and what it holds) from Viscara to the Tatooine dock it last used.
        var positions = DockedAtStationAfterTatooine();

        StarshipDockReturn.ReturnToSafeDock(positions, IsShipOnly, _ => true, ViscaraOrTatooineStarport)
            .Should().Be(StarshipDockReturnResult.ReturnedFromShipOnlyLocation);

        ShouldBeAtVeles(positions);
    }

    [Test]
    public void ShipLeftAtShipOnlyLocation_KeepsASafeDockOnThatPlanet()
    {
        var positions = DockedAtStationAfterTatooine();
        positions[PropertyLocationType.LastNPCDockPosition] = At(Veles, 7f);
        positions[PropertyLocationType.LastNPCSpacePosition] = At(StationOrbit, 8f);
        var safeDock = positions[PropertyLocationType.LastNPCDockPosition];
        var safeOrbit = positions[PropertyLocationType.LastNPCSpacePosition];

        Restart(positions).Should().Be(StarshipDockReturnResult.ReturnedFromShipOnlyLocation);

        ShouldBeAt(positions[PropertyLocationType.DockPosition], safeDock);
        ShouldBeAt(positions[PropertyLocationType.SpacePosition], safeOrbit);
        positions[PropertyLocationType.LastNPCDockPosition].Should().BeSameAs(safeDock);
        positions[PropertyLocationType.LastNPCSpacePosition].Should().BeSameAs(safeOrbit);
    }

    [Test]
    public void ShipInSpaceFromShipOnlyLocation_ReturnsToItsPlanetsStarport()
    {
        var positions = DockedAtStationAfterTatooine();
        positions[PropertyLocationType.CurrentPosition] = At(StationOrbit, 9f);

        Restart(positions).Should().Be(StarshipDockReturnResult.ReturnedFromShipOnlyLocation);

        positions.Should().NotContainKey(PropertyLocationType.CurrentPosition);
        ShouldBeAtVeles(positions);
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
    public void ShipAtDeletedPlayerStarport_ReturnsToTheStarportOfThePlanetItDockedAt()
    {
        var positions = new Dictionary<PropertyLocationType, PropertyLocation>
        {
            [PropertyLocationType.DockPosition] = AtStarport(PlayerStarportId),
            [PropertyLocationType.SpacePosition] = At(StationOrbit, 6f),
            [PropertyLocationType.LastNPCDockPosition] = At(Tatooine, 7f),
            [PropertyLocationType.LastNPCSpacePosition] = At(TatooineOrbit, 8f),
        };

        // The player starport was in Viscara's orbit; its owner removing it must not send the ship to Tatooine.
        Restart(positions, _ => false).Should().Be(StarshipDockReturnResult.ReturnedFromMissingStarport);

        ShouldBeAtVeles(positions);
    }

    [Test]
    public void ShipAtRetrievedPlayerStarport_ReturnsWhileShipsAtOtherStarportsStay()
    {
        // Property.ReturnStarshipToSafeDock treats a starport being retrieved as gone, though it still exists.
        const string otherStarportId = "other-starport";
        Func<string, bool> starportExists = id => id != PlayerStarportId && id == otherStarportId;
        var retrieved = new Dictionary<PropertyLocationType, PropertyLocation>
        {
            [PropertyLocationType.DockPosition] = AtStarport(PlayerStarportId),
            [PropertyLocationType.SpacePosition] = At(StationOrbit, 6f),
            [PropertyLocationType.LastNPCDockPosition] = At(Tatooine, 7f),
            [PropertyLocationType.LastNPCSpacePosition] = At(TatooineOrbit, 8f),
        };
        var otherDock = AtStarport(otherStarportId);
        var other = new Dictionary<PropertyLocationType, PropertyLocation>
        {
            [PropertyLocationType.DockPosition] = otherDock,
            [PropertyLocationType.LastNPCDockPosition] = At(Tatooine, 7f),
        };

        StarshipDockReturn.ReturnToSafeDock(retrieved, IsShipOnly, starportExists, ViscaraStarport)
            .Should().Be(StarshipDockReturnResult.ReturnedFromMissingStarport);
        StarshipDockReturn.ReturnToSafeDock(other, IsShipOnly, starportExists, ViscaraStarport)
            .Should().Be(StarshipDockReturnResult.Unchanged);

        ShouldBeAtVeles(retrieved);
        other[PropertyLocationType.DockPosition].Should().BeSameAs(otherDock);
    }

    [Test]
    public void ShipAtRetrievedStarportWithoutSafeDock_ReturnsToTheCityPlanetStarport()
    {
        const string cityArea = "veles_city";
        (PropertyLocation Landing, PropertyLocation Orbit)? CityPlanetStarport(string areaResref) =>
            areaResref == cityArea ? (At(Veles, 1f), At(StationOrbit, 2f)) : null;
        var positions = new Dictionary<PropertyLocationType, PropertyLocation>
        {
            [PropertyLocationType.DockPosition] = AtStarport(PlayerStarportId),
            [PropertyLocationType.SpacePosition] = At("city_orbit", 6f),
        };

        StarshipDockReturn.ReturnToSafeDock(positions, IsShipOnly, _ => false, CityPlanetStarport, cityArea)
            .Should().Be(StarshipDockReturnResult.ReturnedToPlanetStarport);

        var (landing, orbit) = CityPlanetStarport(cityArea)!.Value;
        positions[PropertyLocationType.DockPosition].Should().BeEquivalentTo(landing);
        positions[PropertyLocationType.SpacePosition].Should().BeEquivalentTo(orbit);
        positions[PropertyLocationType.LastNPCDockPosition].Should().BeEquivalentTo(landing);
        positions[PropertyLocationType.LastNPCSpacePosition].Should().BeEquivalentTo(orbit);
    }

    [Test]
    public void ShipWithoutSafeOrbit_ReturnsToSafeDockAndItsPlanetsOrbit()
    {
        // Ships that have not docked at a regular starport since safe orbits were recorded.
        var positions = DockedAtStationAfterTatooine();
        positions[PropertyLocationType.LastNPCDockPosition] = At(Veles, 7f);
        positions.Remove(PropertyLocationType.LastNPCSpacePosition);

        Restart(positions).Should().Be(StarshipDockReturnResult.ReturnedFromShipOnlyLocation);

        positions[PropertyLocationType.DockPosition].Should().BeEquivalentTo(At(Veles, 7f));
        positions[PropertyLocationType.SpacePosition].Should().BeEquivalentTo(At(StationOrbit, 2f));
        positions[PropertyLocationType.LastNPCSpacePosition].Should().BeEquivalentTo(At(StationOrbit, 2f));
    }

    [Test]
    public void ShipAtAnUnknownPlanet_ReturnsToSafeDockAndKeepsItsOrbit()
    {
        // Where the ship is cannot be resolved, so its safe dock is the only place known to be safe.
        var orbit = At("unknown_orbit", 6f);
        var positions = new Dictionary<PropertyLocationType, PropertyLocation>
        {
            [PropertyLocationType.DockPosition] = AtStarport(PlayerStarportId),
            [PropertyLocationType.SpacePosition] = orbit,
            [PropertyLocationType.LastNPCDockPosition] = At(Tatooine, 7f),
        };

        Restart(positions, _ => false).Should().Be(StarshipDockReturnResult.ReturnedFromMissingStarport);

        positions[PropertyLocationType.DockPosition].AreaResref.Should().Be(Tatooine);
        positions[PropertyLocationType.SpacePosition].Should().BeSameAs(orbit);
        positions.Should().NotContainKey(PropertyLocationType.LastNPCSpacePosition);
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
    public void FindOrbitPlanet_PicksTheNearestOrbitWaypointInTheOrbitsArea()
    {
        var planetOrbits = new[]
        {
            (PlanetType.Viscara, OrbitAt(StationOrbit, 86f, 54f)),
            (PlanetType.CZ220, OrbitAt(StationOrbit, 103f, 33f)),
            (PlanetType.Tatooine, OrbitAt(TatooineOrbit, 80f, 52f)),
        };

        StarshipDockReturn.FindOrbitPlanet(OrbitAt(StationOrbit, 100f, 36f), planetOrbits).Should().Be(PlanetType.CZ220);
        StarshipDockReturn.FindOrbitPlanet(OrbitAt(StationOrbit, 90f, 50f), planetOrbits).Should().Be(PlanetType.Viscara);
        StarshipDockReturn.FindOrbitPlanet(OrbitAt(StationOrbit, 224f, 155f), planetOrbits)
            .Should().Be(PlanetType.Viscara, "any position in a shared orbit area resolves to the nearest planet there");
        StarshipDockReturn.FindOrbitPlanet(OrbitAt("VISCARAORBIT", 86f, 54f), planetOrbits).Should().Be(PlanetType.Viscara);
        StarshipDockReturn.FindOrbitPlanet(OrbitAt(TatooineOrbit, 103f, 33f), planetOrbits)
            .Should().Be(PlanetType.Tatooine, "a waypoint in another area never matches, however near its coordinates");
        StarshipDockReturn.FindOrbitPlanet(OrbitAt("unknown_orbit", 86f, 54f), planetOrbits).Should().Be(PlanetType.Invalid);
        StarshipDockReturn.FindOrbitPlanet(null, planetOrbits).Should().Be(PlanetType.Invalid);
    }

    [Test]
    public void ShipWithoutDockInCz220Orbit_KeepsItsCz220SafeDock()
    {
        // CZ-220's orbit is in the Viscara orbit area. Resolving the area alone sent such a ship to Veles.
        var safeDock = At(Cz220, 11f);
        var safeOrbit = OrbitAt(StationOrbit, 103f, 33f);
        var positions = new Dictionary<PropertyLocationType, PropertyLocation>
        {
            [PropertyLocationType.SpacePosition] = OrbitAt(StationOrbit, 102f, 34f),
            [PropertyLocationType.LastNPCDockPosition] = safeDock,
            [PropertyLocationType.LastNPCSpacePosition] = safeOrbit,
        };

        RestartInSystems(positions).Should().Be(StarshipDockReturnResult.RestoredMissingDock);

        ShouldBeAt(positions[PropertyLocationType.DockPosition], safeDock);
        ShouldBeAt(positions[PropertyLocationType.SpacePosition], safeOrbit);
        positions[PropertyLocationType.LastNPCDockPosition].Should().BeSameAs(safeDock);
        positions[PropertyLocationType.LastNPCSpacePosition].Should().BeSameAs(safeOrbit);
    }

    [TestCase(102f, 34f, PlanetType.CZ220)]
    [TestCase(87f, 53f, PlanetType.Viscara)]
    public void ShipInASharedOrbitWithATatooineSafeDock_GoesToTheNearestStarportInThatOrbit(float x, float y, PlanetType expected)
    {
        var positions = new Dictionary<PropertyLocationType, PropertyLocation>
        {
            [PropertyLocationType.DockPosition] = AtStarport(PlayerStarportId),
            [PropertyLocationType.SpacePosition] = OrbitAt(StationOrbit, x, y),
            [PropertyLocationType.LastNPCDockPosition] = At(Tatooine, 7f),
            [PropertyLocationType.LastNPCSpacePosition] = OrbitAt(TatooineOrbit, 80f, 52f),
        };

        RestartInSystems(positions, _ => false).Should().Be(StarshipDockReturnResult.ReturnedFromMissingStarport);

        ShouldBeAtStarport(positions, expected);
    }

    [Test]
    public void SafeDockInTheSameSystem_IsKept()
    {
        // Viscara and CZ-220 are reached from one orbit area without a jump, so either is a safe dock for the other.
        (Dictionary<PropertyLocationType, PropertyLocation> Positions, StarshipDockReturnResult Result, string Case)[] ships =
        {
            (new()
            {
                [PropertyLocationType.DockPosition] = AtStarport(PlayerStarportId),
                [PropertyLocationType.SpacePosition] = OrbitAt(StationOrbit, 87f, 53f),
                [PropertyLocationType.LastNPCDockPosition] = At(Cz220, 11f),
                [PropertyLocationType.LastNPCSpacePosition] = OrbitAt(StationOrbit, 103f, 33f),
            }, StarshipDockReturnResult.ReturnedFromMissingStarport, "a removed starport by Viscara with a CZ-220 safe dock"),
            (new()
            {
                [PropertyLocationType.DockPosition] = At(Station, 5f),
                [PropertyLocationType.SpacePosition] = OrbitAt(StationOrbit, 224f, 155f),
                [PropertyLocationType.LastNPCDockPosition] = At(Cz220, 11f),
                [PropertyLocationType.LastNPCSpacePosition] = OrbitAt(StationOrbit, 103f, 33f),
            }, StarshipDockReturnResult.ReturnedFromShipOnlyLocation, "the Abandoned Station with a CZ-220 safe dock"),
            (new()
            {
                [PropertyLocationType.DockPosition] = AtStarport(PlayerStarportId),
                [PropertyLocationType.SpacePosition] = OrbitAt(StationOrbit, 102f, 34f),
                [PropertyLocationType.LastNPCDockPosition] = At(Veles, 1f),
                [PropertyLocationType.LastNPCSpacePosition] = OrbitAt(StationOrbit, 86f, 54f),
            }, StarshipDockReturnResult.ReturnedFromMissingStarport, "a removed starport by CZ-220 with a Veles safe dock"),
        };

        foreach (var (positions, result, description) in ships)
        {
            var safeDock = positions[PropertyLocationType.LastNPCDockPosition];
            var safeOrbit = positions[PropertyLocationType.LastNPCSpacePosition];

            RestartInSystems(positions, _ => false).Should().Be(result, description);

            positions[PropertyLocationType.DockPosition].Should().BeEquivalentTo(safeDock, description);
            positions[PropertyLocationType.SpacePosition].Should().BeEquivalentTo(safeOrbit, description);
            positions[PropertyLocationType.LastNPCDockPosition].Should().BeSameAs(safeDock, description);
            positions[PropertyLocationType.LastNPCSpacePosition].Should().BeSameAs(safeOrbit, description);
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

        shipOnly.Should().Contain(StationAreas, "every area of the Abandoned Station is reachable only by ship");
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

    [Test]
    public void ModuleOrbits_ResolveToTheirPlanets()
    {
        // A ship's saved orbit is a position in the space area it docked from. Planet.GetPlanetType leaves space areas
        // unresolved, and one orbit area can hold several planets' orbits, so the dock return picks the planet whose
        // orbit waypoint is nearest.
        var root = FindRepositoryRoot();
        var areas = Directory.EnumerateFiles(Path.Combine(root, "Module", "git"), "*.git.json")
            .ToDictionary(path => Path.GetFileName(path)[..^".git.json".Length], ReadArea);
        var planetOrbits = Enum.GetValues<PlanetType>()
            .Where(planet => planet != PlanetType.Invalid && planet.GetAttribute<PlanetType, PlanetAttribute>().IsActive)
            .Select(planet => (planet, areas.Values.SelectMany(area => area.Waypoints)
                .Single(waypoint => waypoint.Tag == planet.GetAttribute<PlanetType, PlanetAttribute>().SpaceOrbitWaypointTag).Location))
            .ToList();
        var orbitAreas = planetOrbits.ToDictionary(orbit => orbit.Item1, orbit => orbit.Location.AreaResref);

        orbitAreas[PlanetType.CZ220].Should().Be(orbitAreas[PlanetType.Viscara], "CZ-220's orbit is in the Viscara orbit area");
        orbitAreas[PlanetType.SmugglersMoonStation].Should().Be(orbitAreas[PlanetType.SmugglersMoon], "both Smuggler's Moon orbits share an area");
        foreach (var (planet, orbit) in planetOrbits)
            StarshipDockReturn.FindOrbitPlanet(orbit, planetOrbits).Should().Be(planet, $"{planet}'s own orbit waypoint resolves to it");

        StarshipDockReturn.FindOrbitPlanet(At(Veles, 1f), planetOrbits).Should().Be(PlanetType.Invalid, "a landing area is no orbit");
        StarshipDockReturn.FindOrbitPlanet(new PropertyLocation(), planetOrbits).Should().Be(PlanetType.Invalid);
    }

    private static (Dictionary<string, int> Variables, HashSet<string> WaypointTags, List<(string Tag, PropertyLocation Location)> Waypoints) ReadArea(string path)
    {
        var areaResref = Path.GetFileName(path)[..^".git.json".Length];
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

        var waypointList = new List<(string Tag, PropertyLocation Location)>();
        if (root.TryGetProperty("WaypointList", out var waypoints))
        {
            foreach (var waypoint in waypoints.GetProperty("value").EnumerateArray())
            {
                float Coordinate(string name) => waypoint.GetProperty(name).GetProperty("value").GetSingle();
                waypointList.Add((waypoint.GetProperty("Tag").GetProperty("value").GetString()!, new PropertyLocation
                {
                    AreaResref = areaResref,
                    X = Coordinate("XPosition"),
                    Y = Coordinate("YPosition"),
                    Z = Coordinate("ZPosition")
                }));
            }
        }

        return (variables, waypointList.Select(waypoint => waypoint.Tag).ToHashSet(), waypointList);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module", "git")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new DirectoryNotFoundException("Module directory not found.");
    }
}
