using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.ShuttleService;
using SWLOR.Game.Server.Service.StationService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Home Port (Player City Revamp, "Home Port"): who may book a flight to a city's Starport from any shuttle terminal, what it
/// costs and how long it takes, the schedule and stop key a city's Starport gets, and a ride rebuilt after a restart.
/// </summary>
public class ShuttleHomePortTests
{
    private const string CityId = "city-1";
    private const string OtherCityId = "city-2";

    private static ShuttleStop CityStarport(PlanetType planet = PlanetType.Tatooine, string cityId = CityId) =>
        ShuttleStop.ForCityStarport(planet, cityId);

    private static ShuttleStop OrbitStation => ShuttleStop.ForStation("station-1", PlanetType.Tatooine, false);
    private static ShuttleStop WildStation => ShuttleStop.ForStation("station-2", PlanetType.Hutlar, true);

    private static ShuttleRoute HomeFlight(PlanetType from = PlanetType.Viscara, PlanetType to = PlanetType.Tatooine) =>
        new(ShuttleStop.ForPlanet(from), CityStarport(to));

    // Who may book

    [Test]
    public void ACitizenWithNoTaxesOwedAndThePassMayBook()
    {
        HomePortRules.Decide(isEmbargoed: false, isCitizenHere: true, isAlliedCitizen: false, owedTaxes: 0, hasShuttlePass: true)
            .Should().Be(HomePortAccess.Allowed);
    }

    [Test]
    public void ACitizenOfAnAlliedCityMayBookToo()
    {
        HomePortRules.Decide(false, false, true, 0, true).Should().Be(HomePortAccess.Allowed);
    }

    [Test]
    public void ACitizenWhoOwesTaxesMayNotBook_AtHomeOrFromAnAlliedCity()
    {
        HomePortRules.Decide(false, true, false, 1, true).Should().Be(HomePortAccess.OwesTaxes);
        HomePortRules.Decide(false, false, true, 250, true).Should().Be(HomePortAccess.OwesTaxes);
        HomePortRules.DescribeRefusal(HomePortAccess.OwesTaxes, 250).Should().Contain("250 credits");
    }

    [Test]
    public void AnEmbargoRefusesACitizenOfTheNamedCity_BeforeAnythingElse()
    {
        HomePortRules.Decide(true, false, false, 0, true).Should().Be(HomePortAccess.Embargoed);
        HomePortRules.Decide(true, false, true, 0, true).Should().Be(HomePortAccess.Embargoed);
        HomePortRules.Decide(true, true, false, 500, true).Should().Be(HomePortAccess.Embargoed);
    }

    [Test]
    public void FlightsNeedTheShuttlePassEveryFlightNeeds()
    {
        HomePortRules.Decide(false, true, false, 0, false).Should().Be(HomePortAccess.NoShuttlePass);
        HomePortRules.Decide(false, false, true, 0, false).Should().Be(HomePortAccess.NoShuttlePass);
        HomePortRules.DescribeRefusal(HomePortAccess.NoShuttlePass, 0).Should().Contain("CZ-220 Shuttle Pass");
    }

    [Test]
    public void AVisitorIsNotEligible_OpenGatesDoesNotChangeThat()
    {
        // Open Gates lets a visitor into the Bank and Starport, never onto a Home Port flight.
        HomePortRules.Decide(false, false, false, 0, true).Should().Be(HomePortAccess.NotEligible);
        HomePortRules.DescribeRefusal(HomePortAccess.NotEligible, 0).Should().Contain("citizens");
    }

    [Test]
    public void AllowedHasNoRefusalText()
    {
        HomePortRules.DescribeRefusal(HomePortAccess.Allowed, 0).Should().BeEmpty();
    }

    // Flight time

    [TestCase(0, 300)]
    [TestCase(1, 270)]
    [TestCase(2, 240)]
    [TestCase(3, 210)]
    [TestCase(4, 180)]
    [TestCase(5, 150)]
    public void EachStarportLevelTakesThirtySecondsOffAnInterplanetaryFlight(int level, int seconds)
    {
        HomePortRules.GetInterplanetarySeconds(PlanetType.Viscara, PlanetType.Tatooine, level).Should().Be(seconds);
        ShuttleRouteRules.GetTransitSeconds(HomeFlight(), level).Should().Be(seconds);
    }

    [Test]
    public void LevelFiveIsTwoAndAHalfMinutes_AndLevelsOutsideTheRangeCountAsTheNearest()
    {
        HomePortRules.GetInterplanetarySeconds(PlanetType.Viscara, PlanetType.Tatooine, 5).Should().Be(150);
        HomePortRules.GetInterplanetarySeconds(PlanetType.Viscara, PlanetType.Tatooine, 9).Should().Be(150);
        HomePortRules.GetInterplanetarySeconds(PlanetType.Viscara, PlanetType.Tatooine, -2).Should().Be(300);
    }

    [Test]
    public void AnOrbitalHopKeepsItsSixtySeconds()
    {
        HomePortRules.GetInterplanetarySeconds(PlanetType.CZ220, PlanetType.Viscara, 5).Should().Be(60);
        ShuttleRouteRules.GetTransitSeconds(HomeFlight(PlanetType.CZ220, PlanetType.Viscara), 5).Should().Be(60);
    }

    [Test]
    public void TheLevelShortensTheInterplanetaryLegOnce_NotAStationsOwnShuttle()
    {
        var orbit = new ShuttleRoute(ShuttleStop.ForPlanet(PlanetType.Viscara), OrbitStation);
        orbit.IsHomePort.Should().BeTrue();
        ShuttleRouteRules.GetTransitSeconds(orbit, 0).Should().Be(300 + StationTuning.OrbitHopSeconds);
        ShuttleRouteRules.GetTransitSeconds(orbit, 5).Should().Be(150 + StationTuning.OrbitHopSeconds);

        var wild = new ShuttleRoute(ShuttleStop.ForPlanet(PlanetType.Viscara), WildStation);
        ShuttleRouteRules.GetTransitSeconds(wild, 3).Should().Be(210 + StationTuning.WildSpaceSeconds);
    }

    [Test]
    public void OnlyAHomePortFlightReadsTheLevel()
    {
        var planets = new ShuttleRoute(ShuttleStop.ForPlanet(PlanetType.Viscara), ShuttleStop.ForPlanet(PlanetType.Tatooine));
        ShuttleRouteRules.GetTransitSeconds(planets, 5).Should().Be(300, "an ordinary flight does not shorten");

        var stationHop = new ShuttleRoute(OrbitStation, ShuttleStop.ForPlanet(PlanetType.Tatooine));
        ShuttleRouteRules.GetTransitSeconds(stationHop, 5).Should().Be(StationTuning.OrbitHopSeconds, "a station's 60-second hop is unchanged");
        ShuttleRouteRules.GetTransitSeconds(HomeFlight()).Should().Be(300, "no level given means none");
    }

    // Fare

    [Test]
    public void TheFareIsTheTerminalsOwnFareToThatPlanet()
    {
        ShuttleRouteRules.GetFare(HomeFlight(PlanetType.Viscara, PlanetType.Tatooine))
            .Should().Be(GalaxyMap.GetFare(PlanetType.Viscara, PlanetType.Tatooine));
        ShuttleRouteRules.GetFare(HomeFlight(PlanetType.Hutlar, PlanetType.Tatooine))
            .Should().Be(ShuttleRouteRules.GetFare(new ShuttleRoute(ShuttleStop.ForPlanet(PlanetType.Hutlar), ShuttleStop.ForPlanet(PlanetType.Tatooine))));
    }

    [Test]
    public void AStationFlightFromARemoteTerminalAddsTheStationsOwnShuttle()
    {
        var orbit = new ShuttleRoute(ShuttleStop.ForPlanet(PlanetType.Viscara), OrbitStation);
        ShuttleRouteRules.GetFare(orbit).Should().Be(GalaxyMap.GetFare(PlanetType.Viscara, PlanetType.Tatooine) + StationTuning.OrbitHopFare);

        var wild = new ShuttleRoute(ShuttleStop.ForPlanet(PlanetType.Viscara), WildStation);
        ShuttleRouteRules.GetFare(wild).Should().Be(GalaxyMap.GetFare(PlanetType.Viscara, PlanetType.Hutlar) + StationTuning.WildSpaceFare);
    }

    [Test]
    public void AHomePortFlightDepartsOnTheScheduleOfItsInterplanetaryLeg()
    {
        ShuttleRouteRules.GetPeriodSeconds(HomeFlight()).Should().Be(ShuttleSchedule.GetPeriodSeconds(PlanetType.Viscara, PlanetType.Tatooine));
        ShuttleRouteRules.GetPeriodSeconds(HomeFlight(PlanetType.CZ220, PlanetType.Viscara))
            .Should().Be(ShuttleSchedule.GetPeriodSeconds(PlanetType.CZ220, PlanetType.Viscara), "an orbital hop departs every 60 seconds");

        var orbit = new ShuttleRoute(ShuttleStop.ForPlanet(PlanetType.Viscara), OrbitStation);
        ShuttleRouteRules.GetPeriodSeconds(orbit).Should().Be(ShuttleSchedule.GetPeriodSeconds(PlanetType.Viscara, PlanetType.Tatooine));
    }

    // Which routes exist

    [Test]
    public void ACityStarportIsADestinationFromAnotherPlanetsTerminal()
    {
        ShuttleRouteRules.IsValid(HomeFlight()).Should().BeTrue();
        ShuttleRouteRules.IsValid(HomeFlight(PlanetType.Tatooine, PlanetType.Tatooine)).Should().BeFalse("a flight home from the same planet is a walk");
        ShuttleRouteRules.IsValid(new ShuttleRoute(OrbitStation, CityStarport())).Should().BeFalse("a station flies only to its home port");
        ShuttleRouteRules.IsValid(new ShuttleRoute(CityStarport(PlanetType.Viscara), CityStarport(PlanetType.Tatooine, OtherCityId)))
            .Should().BeFalse("a city's Starport is only a destination");
        ShuttleRouteRules.IsValid(new ShuttleRoute(CityStarport(), ShuttleStop.ForPlanet(PlanetType.Viscara))).Should().BeFalse();
        ShuttleRouteRules.IsValid(new ShuttleRoute(ShuttleStop.ForPlanet(PlanetType.Viscara), CityStarport(PlanetType.Invalid))).Should().BeFalse();
    }

    [Test]
    public void AStationFliesToItsHomePortAsBefore_AndNowAlsoFromARemoteTerminal()
    {
        ShuttleRouteRules.IsValid(new ShuttleRoute(ShuttleStop.ForPlanet(PlanetType.Tatooine), OrbitStation)).Should().BeTrue();
        ShuttleRouteRules.IsValid(new ShuttleRoute(ShuttleStop.ForPlanet(PlanetType.Viscara), OrbitStation)).Should().BeTrue();
        ShuttleRouteRules.IsValid(new ShuttleRoute(OrbitStation, ShuttleStop.ForPlanet(PlanetType.Viscara))).Should().BeFalse("a station never flies to another planet");
        ShuttleRouteRules.IsValid(new ShuttleRoute(OrbitStation, WildStation)).Should().BeFalse();
    }

    [Test]
    public void OnlyAFlightToACityStarportOrToAStationFromAnotherPlanetIsAHomePortFlight()
    {
        HomeFlight().IsHomePort.Should().BeTrue();
        new ShuttleRoute(ShuttleStop.ForPlanet(PlanetType.Viscara), OrbitStation).IsHomePort.Should().BeTrue();
        new ShuttleRoute(ShuttleStop.ForPlanet(PlanetType.Tatooine), OrbitStation).IsHomePort.Should().BeFalse("a station's shuttle from its home port");
        new ShuttleRoute(OrbitStation, ShuttleStop.ForPlanet(PlanetType.Tatooine)).IsHomePort.Should().BeFalse();
        new ShuttleRoute(ShuttleStop.ForPlanet(PlanetType.Viscara), ShuttleStop.ForPlanet(PlanetType.Tatooine)).IsHomePort.Should().BeFalse();
    }

    [Test]
    public void ACityStarportIsNeverReadAsAStationOrAPlanet()
    {
        var stop = CityStarport();
        stop.IsCityStarport.Should().BeTrue();
        stop.IsStation.Should().BeFalse();
        stop.HomeCityId.Should().Be(CityId);
        stop.Should().NotBe(ShuttleStop.ForPlanet(PlanetType.Tatooine), "it is not the planet's NPC port");
        stop.Should().NotBe(OrbitStation);

        ShuttleStop.ForPlanet(PlanetType.Tatooine).IsCityStarport.Should().BeFalse();
        OrbitStation.IsCityStarport.Should().BeFalse();
        OrbitStation.HomeCityId.Should().Be("station-1");

        HomeFlight().IsStationRoute.Should().BeFalse();
        HomeFlight().IsCityRoute.Should().BeTrue();
    }

    // The stop key and the schedule

    [Test]
    public void ACityStopHasItsOwnKey_ThatNeverCollidesWithAPlanetOrAStation()
    {
        var planetKey = ShuttleSchedule.GetStopKey(ShuttleStop.ForPlanet(PlanetType.Tatooine));
        var stationKey = ShuttleSchedule.GetStopKey(OrbitStation);
        var cityKey = ShuttleSchedule.GetStopKey(CityStarport());

        planetKey.Should().Be(((int)PlanetType.Tatooine).ToString());
        stationKey.Should().Be($"{(int)PlanetType.Tatooine}:station-1");
        cityKey.Should().Be($"{(int)PlanetType.Tatooine}#{CityId}");
        new[] { planetKey, stationKey, cityKey }.Distinct().Should().HaveCount(3);

        // Even a city whose id equals a station's id keeps a different key from the station's.
        ShuttleSchedule.GetStopKey(CityStarport(PlanetType.Tatooine, "station-1")).Should().NotBe(stationKey);
    }

    [Test]
    public void AHomePortFlightRunsOnItsOwnSchedule_NotThePlanetsNpcPortSchedule()
    {
        var npc = new ShuttleRoute(ShuttleStop.ForPlanet(PlanetType.Viscara), ShuttleStop.ForPlanet(PlanetType.Tatooine));
        var home = HomeFlight();

        ShuttleSchedule.GetRouteHash(home).Should().NotBe(ShuttleSchedule.GetRouteHash(npc));
        ShuttleSchedule.BuildFlightId(home, new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc))
            .Should().Contain(CityId).And.NotBe(ShuttleSchedule.BuildFlightId(npc, new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc)));

        var start = new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc);
        var departures = ShuttleSchedule.GetDeparturesBetween(home, start, start.AddMinutes(10)).ToList();
        departures.Should().HaveCount(5, "an interplanetary route departs every 120 seconds");
        departures.Zip(departures.Skip(1), (a, b) => (b - a).TotalSeconds).Should().OnlyContain(x => x == 120);
        ShuttleSchedule.GetNextDepartureUtc(home, start).Should().BeAfter(start);
    }

    [Test]
    public void TwoCitiesOnOnePlanetKeepSeparateFlights()
    {
        var first = HomeFlight();
        var second = new ShuttleRoute(ShuttleStop.ForPlanet(PlanetType.Viscara), CityStarport(PlanetType.Tatooine, OtherCityId));

        first.Should().NotBe(second);
        ShuttleSchedule.GetRouteHash(first).Should().NotBe(ShuttleSchedule.GetRouteHash(second));
        ShuttleSchedule.BuildFlightId(first, DateTime.UnixEpoch).Should().NotBe(ShuttleSchedule.BuildFlightId(second, DateTime.UnixEpoch));
    }

    [Test]
    public void PlanetAndStationRoutesKeepTheirOldHashesAndFlightIds()
    {
        var start = new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc);
        var planets = new ShuttleRoute(ShuttleStop.ForPlanet(PlanetType.Viscara), ShuttleStop.ForPlanet(PlanetType.Tatooine));
        ShuttleSchedule.GetRouteHash(planets).Should().Be(ShuttleSchedule.GetRouteHash(PlanetType.Viscara, PlanetType.Tatooine));
        ShuttleSchedule.BuildFlightId(planets, start).Should().Be(ShuttleSchedule.BuildFlightId(PlanetType.Viscara, PlanetType.Tatooine, start));

        var hop = new ShuttleRoute(OrbitStation, ShuttleStop.ForPlanet(PlanetType.Tatooine));
        ShuttleSchedule.BuildFlightId(hop, start).Should().Be($"{(int)PlanetType.Tatooine}:station-1>{(int)PlanetType.Tatooine}@{start.Ticks}");
    }

    // A ride rebuilt after a restart

    [Test]
    public void ARideToACityStarportRebuildsItsStopAfterARestart()
    {
        var route = HomeFlight();
        var ride = new ShuttleRide("passenger")
        {
            Origin = route.Origin.Planet,
            Destination = route.Destination.Planet,
            OriginStationId = route.Origin.StationId,
            DestinationStationId = route.Destination.StationId,
            OriginCityId = route.Origin.CityId,
            DestinationCityId = route.Destination.CityId
        };

        var rebuilt = ShuttleRouteRules.BuildRideRoute(
            ride.Origin, ride.OriginStationId, ride.OriginCityId,
            ride.Destination, ride.DestinationStationId, ride.DestinationCityId,
            _ => throw new InvalidOperationException("a city's Starport needs no station lookup"));

        rebuilt.Should().Be(route);
        rebuilt.Destination.IsCityStarport.Should().BeTrue();
        rebuilt.Destination.CityId.Should().Be(CityId);
        rebuilt.IsHomePort.Should().BeTrue();
        ShuttleSchedule.GetRouteHash(rebuilt).Should().Be(ShuttleSchedule.GetRouteHash(route), "the boarding group and the schedule match what was ticketed");
    }

    [Test]
    public void ARideToAStationStillRebuildsItsStation_AndAGoneStationKeepsItsHomePort()
    {
        var found = ShuttleRouteRules.BuildRideRoute(
            PlanetType.Viscara, string.Empty, string.Empty,
            PlanetType.Tatooine, "station-1", string.Empty,
            id => id == "station-1" ? OrbitStation : default);
        found.Should().Be(new ShuttleRoute(ShuttleStop.ForPlanet(PlanetType.Viscara), OrbitStation));
        found.IsHomePort.Should().BeTrue();

        var gone = ShuttleRouteRules.BuildRideRoute(
            PlanetType.Viscara, string.Empty, string.Empty,
            PlanetType.Tatooine, "station-9", string.Empty,
            _ => default);
        gone.Destination.IsStation.Should().BeTrue();
        gone.Destination.StationId.Should().Be("station-9");
        gone.Destination.Planet.Should().Be(PlanetType.Tatooine);
    }

    [Test]
    public void APlanetRideRebuildsAsAPlanetRoute_AndOlderRecordsLoadWithNoCityId()
    {
        var older = new ShuttleRide("passenger") { Origin = PlanetType.Viscara, Destination = PlanetType.Tatooine };
        older.OriginCityId.Should().BeEmpty();
        older.DestinationCityId.Should().BeEmpty();

        var route = ShuttleRouteRules.BuildRideRoute(
            older.Origin, older.OriginStationId, older.OriginCityId,
            older.Destination, older.DestinationStationId, older.DestinationCityId,
            _ => throw new InvalidOperationException("a planet needs no station lookup"));
        route.Should().Be(new ShuttleRoute(ShuttleStop.ForPlanet(PlanetType.Viscara), ShuttleStop.ForPlanet(PlanetType.Tatooine)));
        route.IsHomePort.Should().BeFalse();
        route.IsCityRoute.Should().BeFalse();
    }

    // Wiring

    [Test]
    public void HomePortIsWiredIntoTheShuttleAndTheFlightsTerminal()
    {
        var shuttle = ReadServer("Service", "Shuttle.cs");
        shuttle.Should().Contain("var homePortRefusal = HomePorts.GetRefusal(player, route);", "a purchase is refused for anyone not eligible, not only hidden in the list");
        shuttle.Should().Contain("var transitSeconds = HomePorts.GetTransitSeconds(route);", "the flight time reads the Starport level");
        shuttle.Should().Contain("OriginCityId = route.Origin.CityId ?? string.Empty,").And.Contain("DestinationCityId = route.Destination.CityId ?? string.Empty,");
        shuttle.Should().Contain("ShuttleRouteRules.BuildRideRoute(");
        shuttle.Should().Contain("HomePorts.TryGetArrivalLocation(destination.CityId, out var cityLocation)");
        shuttle.Should().Contain("HomePorts.OnArrived(ride.PlayerId, flight.Route);", "an arrival gives the departure pass");
        shuttle.Should().Contain("HomePorts.OnArrived(ride.PlayerId, route);", "so does an arrival that waited for the login");

        var homePorts = ReadServer("Service", "HomePorts.cs");
        homePorts.Should().Contain("StarportDeparture.OnArrived(playerId, string.Empty, interiors[0]);");
        homePorts.Should().Contain("HomePortRules.Decide(");
        homePorts.Should().Contain("EdictEffects.IsEmbargoedBy(host, dbPlayer)").And.Contain("EdictEffects.IsAllyOf(host, dbPlayer)");

        var dialog = ReadServer("Feature", "DialogDefinition", "StarportFlightsDialog.cs");
        dialog.Should().Contain("HomePorts.GetDestinations(player, origin)");
    }

    private static string ReadServer(params string[] path) =>
        File.ReadAllText(Path.Combine(new[] { RepositoryRoot, "SWLOR.Game.Server" }.Concat(path).ToArray()));

    private static string RepositoryRoot
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        }
    }
}
