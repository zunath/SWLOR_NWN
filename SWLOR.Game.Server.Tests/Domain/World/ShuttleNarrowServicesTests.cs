using System.Numerics;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Domain.World;

/// <summary>
/// Tests of the narrow services the shuttle was split into, each built with its own constructor-injected
/// seams on the <see cref="ShuttleBench"/>. <see cref="ShuttleServiceTests"/> runs whole flights through all of them.
/// </summary>
[TestFixture]
public class ShuttleNarrowServicesTests
{
    private const PlanetType Origin = PlanetType.Viscara;
    private const PlanetType Destination = PlanetType.Tatooine;
    private static readonly DateTime Start = new(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    private ShuttleBench _bench = null!;
    private uint _starport;

    [SetUp]
    public void SetUp()
    {
        _bench = new ShuttleBench(Start);
        _starport = _bench.AddArea("viscara_starport", "Viscara - Starport");
    }

    [TearDown]
    public void TearDown() => _bench.Dispose();

    private static ShuttleRide Ride(string playerId, ShuttleRideStatus status, string flightId = "") => new(playerId)
    {
        Status = status, Origin = Origin, Destination = Destination, FlightId = flightId
    };

    [Test]
    public void The_state_service_starts_empty_and_each_collection_is_separate()
    {
        var state = new ShuttleStateService();

        state.ActiveFlights.Should().BeEmpty();
        state.TicketHolders.Should().BeEmpty();
        state.BoardingNotBeforeUtc.Should().BeEmpty();
        state.AnnouncedBoardings.Should().BeEmpty();
        state.MissedBoardings.GetMissed().Should().BeEmpty();
        state.Boardings.GetPending().Should().BeEmpty();
        state.TicketHolders["a"] = (Origin, Destination);
        state.BoardingNotBeforeUtc.Should().BeEmpty();
    }

    [Test]
    public void The_carried_fields_read_back_from_the_shuttle_service_that_carried_them_before_the_split()
    {
        var carried = typeof(ShuttleStateService).GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(field => field.GetCustomAttribute<ReloadStateAttribute>()?.Kind == ReloadStateKind.Carried)
            .ToList();

        carried.Select(field => field.Name).Should().BeEquivalentTo("_boardingNotBeforeUtc", "_announcedBoardings", "_missedBoardings", "_boardings");
        carried.Should().OnlyContain(field => field.GetCustomAttributes<ReloadCarriedFromAttribute>()
            .Any(from => from.FormerOwner == "SWLOR.Game.Server.Domain.World.ShuttleService"));
    }

    [Test]
    public void The_clock_service_reads_the_system_clock_and_hands_back_scheduler_items()
    {
        var clock = new ShuttleClockService();

        clock.UtcNow().Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        using (clock.Schedule(() => { }, TimeSpan.FromHours(1)))
        using (clock.ScheduleRepeating(() => { }, TimeSpan.FromHours(1)))
        {
        }
    }

    [Test]
    public void The_ticket_service_finds_rides_by_player_status_and_flight()
    {
        _bench.Db.Store.With(Ride("p1", ShuttleRideStatus.Ticketed));
        _bench.Db.Store.With(Ride("p2", ShuttleRideStatus.InTransit, "flight-a"));
        _bench.Db.Store.With(Ride("p3", ShuttleRideStatus.InTransit, "flight-b"));
        var tickets = _bench.Service.Tickets;

        tickets.GetRide("p1")!.Status.Should().Be(ShuttleRideStatus.Ticketed);
        tickets.GetRide("nobody").Should().BeNull();
        tickets.SearchRidesByStatus(ShuttleRideStatus.InTransit).Select(ride => ride.PlayerId).Should().BeEquivalentTo("p2", "p3");
        tickets.SearchRidesByFlightId("flight-a").Select(ride => ride.PlayerId).Should().Equal("p2");
    }

    [Test]
    public void The_ticket_service_refuses_a_second_ticket_and_a_refund_of_a_flight_in_progress()
    {
        var player = _bench.AddPlayer("p1", _starport, gold: 100000);
        var tickets = _bench.Service.Tickets;

        tickets.TryPurchaseTicket(player.Id, Origin, Destination, string.Empty, 0f).Should().BeTrue();
        tickets.TryPurchaseTicket(player.Id, Origin, Destination, string.Empty, 0f).Should().BeFalse("the player already has a ticket");
        _bench.Service.State.TicketHolders["p1"].Should().Be((Origin, Destination));

        var ride = tickets.GetRide("p1")!;
        ride.Status = ShuttleRideStatus.InTransit;
        _bench.Db.Set(ride);
        tickets.RefundTicket(player.Id).Should().BeFalse("only an unboarded ticket is refundable");
    }

    [Test]
    public void The_passenger_service_finds_online_players_by_id_and_the_landing_point_of_a_planet()
    {
        var landingArea = _bench.AddArea("tatooine_landing_zone");
        _bench.AddWaypoint("TATOOINE_LANDING", landingArea, new Vector3(50f, 60f, 1f), 180f);
        var player = _bench.AddPlayer("p1", _starport);
        var passengers = _bench.Service.Passengers;

        passengers.GetOnlinePlayerById("p1").Should().Be(player.Id);
        passengers.GetOnlinePlayerById("p2").Should().Be(ShuttleBench.Invalid);
        _bench.Describe(passengers.GetLandingLocation(Destination)).Should().Be((landingArea, new Vector3(50f, 60f, 1f), 180f));
    }

    [Test]
    public void The_passenger_service_delivers_a_player_but_not_a_dm()
    {
        var landingArea = _bench.AddArea("tatooine_landing_zone");
        _bench.AddWaypoint("TATOOINE_LANDING", landingArea);
        var player = _bench.AddPlayer("p1", _starport);
        var dm = _bench.AddPlayer("dm", _starport);
        dm.IsDM = true;
        var passengers = _bench.Service.Passengers;
        var landing = passengers.GetLandingLocation(Destination);

        passengers.DeliverPassenger(player.Id, landing);
        passengers.DeliverPassenger(dm.Id, landing);

        _bench.AssignedTo(player.Id).Should().ContainSingle();
        _bench.AssignedTo(dm.Id).Should().BeEmpty();
    }

    [Test]
    public void The_flight_service_reports_no_flight_for_an_object_without_one_and_makes_a_flight_only_once()
    {
        var template = _bench.AddArea("shuttle", "[Prefab] Starship Shuttle - Interior");
        _bench.AddObject("PROPERTY_ENTRANCE", ObjectType.Waypoint, template);
        _bench.AddObject("pilot_chair", ObjectType.Placeable, template);
        var flights = _bench.Service.Flights;
        var departure = Start.AddMinutes(5);
        var flightId = ShuttleSchedule.BuildFlightId(Origin, Destination, departure);

        flights.TryGetFlightInfo(_starport, out _, out _).Should().BeFalse();

        var first = flights.GetOrCreateFlight(flightId, Origin, Destination, departure, departure.AddMinutes(2));
        var second = flights.GetOrCreateFlight(flightId, Origin, Destination, departure, departure.AddMinutes(2));

        second.Should().BeSameAs(first);
        _bench.Areas.Created.Should().ContainSingle();
        _bench.Service.State.ActiveFlights.Should().ContainKey(flightId);
        flights.TryGetFlightInfo(first.Console, out var destination, out var arrival).Should().BeTrue();
        (destination, arrival).Should().Be((Destination, departure.AddMinutes(2)));
    }

    [Test]
    public void The_boarding_terminal_is_one_for_the_origin_planet_within_range()
    {
        _bench.AddTerminal(Origin, _starport, new Vector3(10f, 10f, 0f));
        var near = _bench.AddPlayer("near", _starport, new Vector3(10f, 14f, 0f));
        var far = _bench.AddPlayer("far", _starport, new Vector3(10f, 40f, 0f));
        var boarding = new ShuttleBoardingService(_bench.Db, _bench.Planets, _bench.Service.State, new ClockedShuttleClockService(Start),
            _bench.Service.Tickets, _bench.Service.Flights, _bench.Service.Passengers);

        boarding.IsAtBoardingTerminal(near.Id, Origin).Should().BeTrue();
        boarding.IsAtBoardingTerminal(near.Id, Destination).Should().BeFalse("the terminal serves another planet");
        boarding.IsAtBoardingTerminal(far.Id, Origin).Should().BeFalse("the player is out of range");
    }
}
