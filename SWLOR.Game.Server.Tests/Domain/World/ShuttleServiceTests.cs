using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ShuttleService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Domain.World;

/// <summary>
/// Unit tests of the shuttle instance. The engine is a model of a small module (<see cref="ShuttleBench"/>), the
/// database keeps the rides in memory, and the clock, the scheduler and the area service are in the test's hands: a
/// test moves <see cref="ClockedShuttleService.Now"/>, runs the repeating tick the instance registered, and runs the
/// delayed tasks it scheduled, so a whole flight (ticket, boarding, transit, arrival) runs in a few lines.
/// </summary>
[TestFixture]
public class ShuttleServiceTests
{
    private const string PassengerId = "passenger-1";
    private const string OtherPassengerId = "passenger-2";
    private const PlanetType Origin = PlanetType.Viscara;
    private const PlanetType Destination = PlanetType.Tatooine;

    // The first departure after the base time is ten seconds after the test starts, so a tick a second after it boards.
    private static readonly DateTime BaseTime = new(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FirstDeparture = ShuttleSchedule.GetNextDepartureUtc(Origin, Destination, BaseTime);
    private static readonly DateTime Start = FirstDeparture.AddSeconds(-10);
    private static readonly int TransitSeconds = GalaxyMap.GetTransitSeconds(Origin, Destination);

    private ShuttleBench _bench = null!;
    private uint _starport;
    private uint _terminal;
    private uint _landingArea;
    private uint _landing;
    private SimObject _passenger = null!;

    private ClockedShuttleService Service => _bench.Service;

    [SetUp]
    public void SetUp()
    {
        _bench = new ShuttleBench(Start);
        _starport = _bench.AddArea("viscara_starport", "Viscara - Starport");
        _terminal = _bench.AddTerminal(Origin, _starport, new Vector3(10f, 10f, 0f));
        _landingArea = _bench.AddArea("tatooine_landing_zone");
        _landing = _bench.AddWaypoint("TATOOINE_LANDING", _landingArea, new Vector3(50f, 60f, 1f), 180f);
        _passenger = _bench.AddPlayer(PassengerId, _starport, new Vector3(10f, 12f, 0f), 100000);
    }

    [TearDown]
    public void TearDown() => _bench.Dispose();

    // ---- helpers

    private void AddShuttleTemplate(bool boardingContract = true)
    {
        var template = _bench.AddArea("shuttle", "[Prefab] Starship Shuttle - Interior");
        if (boardingContract)
        {
            _bench.AddObject("PROPERTY_ENTRANCE", ObjectType.Waypoint, template);
            _bench.AddObject("pilot_chair", ObjectType.Placeable, template);
        }
    }

    private ShuttleRide Ticketed(string playerId = PassengerId, int fare = 500, int tax = 50)
    {
        var ride = new ShuttleRide(playerId)
        {
            Status = ShuttleRideStatus.Ticketed, Origin = Origin, Destination = Destination, FarePaid = fare, TaxPaid = tax
        };
        _bench.Db.Store.With(ride);
        return ride;
    }

    private ShuttleRide InTransit(string playerId, DateTime arrival, string? flightId = null)
    {
        var departure = arrival.AddSeconds(-TransitSeconds);
        var ride = new ShuttleRide(playerId)
        {
            Status = ShuttleRideStatus.InTransit, Origin = Origin, Destination = Destination, FarePaid = 500,
            DepartureUtc = departure, ArrivalUtc = arrival,
            FlightId = flightId ?? ShuttleSchedule.BuildFlightId(Origin, Destination, departure)
        };
        _bench.Db.Store.With(ride);
        return ride;
    }

    private Player StoredPlayer(string playerId, bool rebuildComplete = true)
    {
        var player = new Player(playerId) { LocationAreaResref = "old_area", LocationX = 1f, RebuildComplete = rebuildComplete };
        _bench.Db.Store.With(player);
        return player;
    }

    private ShuttleRide Purchase(uint? player = null, string playerId = PassengerId)
    {
        Service.TryPurchaseTicket(player ?? _passenger.Id, Origin, Destination, string.Empty, 0f).Should().BeTrue();
        return Service.GetRide(playerId);
    }

    /// <summary>Buys a ticket, loads the module and ticks a second past the first departure, so the passenger is aboard.</summary>
    private ShuttleRide BoardThePassenger()
    {
        AddShuttleTemplate();
        Purchase();
        Service.LoadShuttleFlights();
        Service.Now = FirstDeparture.AddSeconds(1);
        Service.Tick!();
        return Service.GetRide(PassengerId);
    }

    private static string Wait(TimeSpan span) => Time.GetTimeLongIntervals(span, false);

    private uint FlightArea() => _bench.Objects.Single(sim => sim.IsArea && sim.Tag == "shuttle_flight").Id;

    // ---- loading the module

    [Test]
    public void LoadShuttleFlights_schedules_the_flight_tick_every_two_seconds()
    {
        Service.LoadShuttleFlights();

        Service.Tick.Should().NotBeNull();
        Service.TickInterval.Should().Be(TimeSpan.FromSeconds(2));
    }

    [Test]
    public void LoadShuttleFlights_keeps_saved_tickets_and_flights_in_the_database_untouched()
    {
        var ticket = Ticketed();
        var flying = InTransit(OtherPassengerId, Start.AddMinutes(2));

        Service.LoadShuttleFlights();

        _bench.Db.Store.Saved.Should().BeEmpty();
        _bench.Db.Store.Deleted.Should().BeEmpty();
        Service.GetRide(PassengerId).Should().BeSameAs(ticket);
        Service.GetRide(OtherPassengerId).Should().BeSameAs(flying);
    }

    [Test]
    public void LoadShuttleFlights_registers_a_flight_that_is_still_in_the_air_once_for_all_its_passengers()
    {
        var arrival = Start.AddMinutes(2);
        var flightId = ShuttleSchedule.BuildFlightId(Origin, Destination, arrival.AddSeconds(-TransitSeconds));
        InTransit(PassengerId, arrival, flightId);
        InTransit(OtherPassengerId, arrival, flightId);
        var console = _bench.AddPlaceable("flights_terminal", _starport);
        _bench.Engine.SetLocalString(console, "SHUTTLE_FLIGHT_ID", flightId);

        Service.LoadShuttleFlights();

        Service.TryGetFlightInfo(console, out var destination, out var arrivalUtc).Should().BeTrue();
        destination.Should().Be(Destination);
        arrivalUtc.Should().Be(arrival);
    }

    [Test]
    public void LoadShuttleFlights_records_the_landing_of_a_passenger_whose_flight_arrived_while_the_server_was_down()
    {
        var ride = InTransit(PassengerId, Start.AddSeconds(-5));
        var stored = StoredPlayer(PassengerId);

        Service.LoadShuttleFlights();

        stored.LocationAreaResref.Should().Be("tatooine_landing_zone");
        stored.LocationX.Should().Be(50f);
        stored.LocationY.Should().Be(60f);
        stored.LocationZ.Should().Be(1f);
        stored.LocationOrientation.Should().Be(180f);
        _bench.Db.Store.Saved.Should().ContainSingle().Which.Should().BeSameAs(stored);
        Service.GetRide(PassengerId).Should().BeSameAs(ride, "the elapsed ride stays until the passenger logs in");
        Service.TryGetFlightInfo(_starport, out _, out _).Should().BeFalse("an arrived flight is not tracked");
    }

    [Test]
    public void LoadShuttleFlights_leaves_an_unknown_offline_passenger_alone()
    {
        InTransit(PassengerId, Start.AddSeconds(-5));

        Service.LoadShuttleFlights();

        _bench.Db.Store.Saved.Should().BeEmpty();
    }

    [Test]
    public void LoadShuttleFlights_does_not_move_a_passenger_when_the_landing_waypoint_is_missing()
    {
        _bench.Remove(_landing);
        InTransit(PassengerId, Start.AddSeconds(-5));
        var stored = StoredPlayer(PassengerId);

        Service.LoadShuttleFlights();

        stored.LocationAreaResref.Should().Be("old_area");
        _bench.Db.Store.Saved.Should().BeEmpty();
    }

    // ---- buying a ticket

    [TestCase(false, false, false)]
    [TestCase(true, true, false)]
    [TestCase(true, false, true)]
    public void TryPurchaseTicket_refuses_anyone_who_is_not_a_playing_player(bool isPlayer, bool isDm, bool isPossessed)
    {
        var other = _bench.AddCreature("npc", _starport);
        other.IsPC = isPlayer;
        other.IsDM = isDm;
        other.IsDMPossessed = isPossessed;

        Service.TryPurchaseTicket(other.Id, Origin, Destination, string.Empty, 0f).Should().BeFalse();

        _bench.Db.Store.Saved.Should().BeEmpty();
    }

    [TestCase(PlanetType.Invalid, PlanetType.Tatooine)]
    [TestCase(PlanetType.Viscara, PlanetType.Invalid)]
    [TestCase(PlanetType.Viscara, PlanetType.Viscara)]
    public void TryPurchaseTicket_refuses_a_route_that_goes_nowhere(PlanetType origin, PlanetType destination)
    {
        Service.TryPurchaseTicket(_passenger.Id, origin, destination, string.Empty, 0f).Should().BeFalse();

        _bench.Db.Store.Saved.Should().BeEmpty();
        _bench.MessagesTo(_passenger.Id).Should().BeEmpty();
    }

    [Test]
    public void TryPurchaseTicket_refuses_a_second_ticket()
    {
        Ticketed();

        Service.TryPurchaseTicket(_passenger.Id, Origin, Destination, string.Empty, 0f).Should().BeFalse();

        _bench.MessagesTo(_passenger.Id).Should().ContainSingle().Which.Should().Contain("You already have a shuttle ticket.");
        _bench.Engine.CallsTo("TakeGoldFromCreature").Should().BeEmpty();
    }

    [Test]
    public void TryPurchaseTicket_refuses_a_passenger_who_cannot_pay_the_fare_and_the_tax()
    {
        var fare = GalaxyMap.GetFare(Origin, Destination);
        _passenger.Gold = fare;

        Service.TryPurchaseTicket(_passenger.Id, Origin, Destination, string.Empty, 0.25f).Should().BeFalse();

        _bench.MessagesTo(_passenger.Id).Should().ContainSingle().Which.Should().Contain("You do not have enough credits");
        _bench.Engine.CallsTo("TakeGoldFromCreature").Should().BeEmpty();
        _bench.Db.Store.Saved.Should().BeEmpty();
    }

    [Test]
    public void TryPurchaseTicket_charges_the_fare_and_the_tax_and_saves_a_ticket_for_the_route()
    {
        var fare = GalaxyMap.GetFare(Origin, Destination);
        var tax = (int)(0.1f * fare);

        Service.TryPurchaseTicket(_passenger.Id, Origin, Destination, string.Empty, 0.1f).Should().BeTrue();

        var take = _bench.Engine.CallsTo("TakeGoldFromCreature").Should().ContainSingle().Subject;
        take.Arguments.Should().Equal(fare + tax, _passenger.Id, true);
        var ride = _bench.Db.Store.Saved.Should().ContainSingle().Subject.Should().BeOfType<ShuttleRide>().Subject;
        ride.PlayerId.Should().Be(PassengerId);
        ride.Status.Should().Be(ShuttleRideStatus.Ticketed);
        ride.Origin.Should().Be(Origin);
        ride.Destination.Should().Be(Destination);
        ride.FarePaid.Should().Be(fare);
        ride.TaxPaid.Should().Be(tax);
    }

    [Test]
    public void TryPurchaseTicket_pays_the_tax_into_the_city_treasury()
    {
        var fare = GalaxyMap.GetFare(Origin, Destination);
        var tax = (int)(0.1f * fare);
        var city = new WorldProperty { Treasury = 1000 };
        _bench.Db.Store.With(city);

        Service.TryPurchaseTicket(_passenger.Id, Origin, Destination, city.Id, 0.1f).Should().BeTrue();

        city.Treasury.Should().Be(1000 + tax);
        _bench.Db.Store.Saved.Should().Contain(city);
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("no-such-city")]
    public void TryPurchaseTicket_keeps_the_ticket_when_there_is_no_city_to_receive_the_tax(string cityPropertyId)
    {
        Service.TryPurchaseTicket(_passenger.Id, Origin, Destination, cityPropertyId, 0.1f).Should().BeTrue();

        _bench.Db.Store.Saved.OfType<WorldProperty>().Should().BeEmpty();
        Service.GetRide(PassengerId).Should().NotBeNull();
    }

    [Test]
    public void TryPurchaseTicket_charges_no_tax_when_the_rate_is_zero()
    {
        var city = new WorldProperty { Treasury = 1000 };
        _bench.Db.Store.With(city);

        Service.TryPurchaseTicket(_passenger.Id, Origin, Destination, city.Id, 0f).Should().BeTrue();

        city.Treasury.Should().Be(1000);
        Service.GetRide(PassengerId).TaxPaid.Should().Be(0);
    }

    // ---- refunding

    [Test]
    public void RefundTicket_returns_only_the_fare_and_deletes_the_ticket()
    {
        var ride = Ticketed(fare: 500, tax: 50);

        Service.RefundTicket(_passenger.Id).Should().BeTrue();

        _bench.Engine.CallsTo("GiveGoldToCreature").Should().ContainSingle().Which.Arguments.Should().Equal(_passenger.Id, 500);
        _bench.Db.Store.Deleted.Should().ContainSingle().Which.Should().Be(ride.Id);
        Service.GetRide(PassengerId).Should().BeNull();
        _bench.MessagesTo(_passenger.Id).Should().ContainSingle()
            .Which.Should().Be("Your shuttle ticket has been refunded for 500 credits. Taxes are non-refundable.");
    }

    [Test]
    public void RefundTicket_does_nothing_without_a_ticket()
    {
        Service.RefundTicket(_passenger.Id).Should().BeFalse();

        _bench.Engine.CallsTo("GiveGoldToCreature").Should().BeEmpty();
    }

    [Test]
    public void RefundTicket_does_not_refund_a_flight_already_under_way()
    {
        InTransit(PassengerId, Start.AddMinutes(3));

        Service.RefundTicket(_passenger.Id).Should().BeFalse();

        _bench.Engine.CallsTo("GiveGoldToCreature").Should().BeEmpty();
        Service.GetRide(PassengerId).Should().NotBeNull();
    }

    [Test]
    public void RefundTicket_stops_automatic_boarding_for_the_refunded_ticket()
    {
        AddShuttleTemplate();
        Purchase();
        Service.LoadShuttleFlights();
        Service.RefundTicket(_passenger.Id).Should().BeTrue();

        Service.Now = FirstDeparture.AddSeconds(1);
        Service.Tick!();

        Service.GetRide(PassengerId).Should().BeNull();
        _bench.Areas.Created.Should().BeEmpty();
    }

    // ---- boarding

    [Test]
    public void The_tick_boards_a_ticket_holder_at_the_terminal_when_the_flight_departs()
    {
        var ride = BoardThePassenger();

        var boardedAt = FirstDeparture.AddSeconds(1);
        ride.Status.Should().Be(ShuttleRideStatus.InTransit);
        ride.FlightId.Should().Be(ShuttleSchedule.BuildFlightId(Origin, Destination, FirstDeparture));
        ride.DepartureUtc.Should().Be(boardedAt);
        ride.ArrivalUtc.Should().Be(boardedAt.AddSeconds(TransitSeconds));
        _bench.Db.Store.Saved.Should().Contain(ride);
        _bench.MessagesTo(_passenger.Id).Should().Contain(
            $"You board the shuttle to Tatooine. Estimated flight time: {Wait(TimeSpan.FromSeconds(TransitSeconds))}.");
    }

    [Test]
    public void The_tick_sends_the_boarded_passenger_to_the_shuttle_entrance()
    {
        BoardThePassenger();

        _bench.RunAssigned(_passenger.Id);

        _bench.Engine.CountOf("ClearAllActions").Should().Be(1);
        var jump = (Location)_bench.Engine.CallsTo("ActionJumpToLocation").Should().ContainSingle().Subject.Arguments[0]!;
        var destination = _bench.Describe(jump);
        destination.Area.Should().Be(FlightArea());
        destination.Position.Should().Be(new Vector3(3f, 4f, 0f));
        destination.Facing.Should().Be(90f);
    }

    [Test]
    public void The_tick_builds_the_shuttle_interior_from_the_passenger_layout()
    {
        BoardThePassenger();

        _bench.Areas.Created.Should().ContainSingle().Which.Should().Be(("shuttle", "shuttle_flight", "Passenger Shuttle - Tatooine"));
        var area = FlightArea();
        var flightId = ShuttleSchedule.BuildFlightId(Origin, Destination, FirstDeparture);
        _bench.Locals.String(area, "SHUTTLE_FLIGHT_ID").Should().Be(flightId);

        // The exit and the ship computer are destroyed; the boarding waypoint, the pilot chair and the rest stay.
        var destroyed = _bench.Engine.CallsTo("DestroyObject").Select(call => (uint)call.Arguments[0]!).ToList();
        destroyed.Should().HaveCount(2);
        destroyed.Select(id => _bench.Get(id).Tag).Should().BeEquivalentTo("building_exit", "ShipComputer");
    }

    [Test]
    public void The_tick_puts_a_status_console_at_the_entrance_that_knows_its_flight()
    {
        BoardThePassenger();

        var area = FlightArea();
        var console = _bench.Objects.Single(sim => sim.Area == area && sim.Type == ObjectType.Placeable && sim.Tag == "flights_terminal");
        _bench.Locals.String(console.Id, "SHUTTLE_FLIGHT_ID").Should().Be(ShuttleSchedule.BuildFlightId(Origin, Destination, FirstDeparture));
        _bench.Locals.String(console.Id, "CONVERSATION").Should().Be("ShuttleStatusDialog");
        _bench.Locals.Int(console.Id, "SUPPRESS_CHAT_AUDIT").Should().Be(1);
        _bench.Locals.Has(console.Id, "CURRENT_LOCATION").Should().BeFalse("the console serves no starport");
        _bench.Engine.CallsTo("SetName").Should().Contain(call => (uint)call.Arguments[0]! == console.Id && (string)call.Arguments[1]! == "Shuttle Status Console");
        _bench.Engine.CallsTo("SetPlotFlag").Should().Contain(call => (uint)call.Arguments[0]! == console.Id && (bool)call.Arguments[1]!);
        console.Position.Should().Be(new Vector3(3f, 4f, 0f));
        Service.TryGetFlightInfo(console.Id, out var destination, out _).Should().BeTrue();
        destination.Should().Be(Destination);
    }

    [Test]
    public void The_tick_seats_a_pilot_droid_in_the_pilot_chair()
    {
        BoardThePassenger();

        var area = FlightArea();
        var chair = _bench.Objects.Single(sim => sim.Area == area && sim.Tag == "pilot_chair");
        var droid = _bench.Objects.Single(sim => sim.Type == ObjectType.Creature && sim.ResRef == "maledroid");
        droid.Position.Should().Be(chair.Position);
        _bench.Engine.CallsTo("SetName").Should().Contain(call => (uint)call.Arguments[0]! == droid.Id && (string)call.Arguments[1]! == "Shuttle Pilot Droid");
        _bench.Engine.CallsTo("SetSoundset").Should().ContainSingle().Which.Arguments.Should().Equal(droid.Id, 910);
        _bench.Engine.CallsTo("SetPlotFlag").Should().Contain(call => (uint)call.Arguments[0]! == droid.Id && (bool)call.Arguments[1]!);
        _bench.Engine.CallsTo("SetImmortal").Should().ContainSingle().Which.Arguments.Should().Equal(droid.Id, true);

        _bench.RunAssigned(droid.Id);

        _bench.Engine.CallsTo("ActionSit").Should().ContainSingle().Which.Arguments.Should().Equal(chair.Id);
    }

    [Test]
    public void The_tick_uses_the_original_freighter_when_the_template_lacks_the_boarding_contract()
    {
        AddShuttleTemplate(boardingContract: false);
        Purchase();
        Service.LoadShuttleFlights();
        Service.Now = FirstDeparture.AddSeconds(1);
        _bench.InteriorHasBoardingContract = false;

        Service.Tick!();

        _bench.Areas.Created.Should().ContainSingle().Which.Source.Should().Be("starship1_int");
        _bench.Get(FlightArea()).Name.Should().Be("Passenger Shuttle - Tatooine");
        Service.GetRide(PassengerId).Status.Should().Be(ShuttleRideStatus.InTransit);
    }

    [Test]
    public void The_tick_uses_the_original_freighter_when_the_template_is_not_in_the_module()
    {
        Purchase();
        Service.LoadShuttleFlights();
        Service.Now = FirstDeparture.AddSeconds(1);

        Service.Tick!();

        _bench.Areas.Created.Should().ContainSingle().Which.Source.Should().Be("starship1_int");
    }

    [Test]
    public void The_tick_boards_everyone_who_is_ready_onto_one_flight()
    {
        AddShuttleTemplate();
        var second = _bench.AddPlayer(OtherPassengerId, _starport, new Vector3(11f, 11f, 0f), 100000);
        Purchase();
        Service.TryPurchaseTicket(second.Id, Origin, Destination, string.Empty, 0f).Should().BeTrue();
        Service.LoadShuttleFlights();
        Service.Now = FirstDeparture.AddSeconds(1);

        Service.Tick!();

        _bench.Areas.Created.Should().ContainSingle("both passengers share the instance");
        Service.GetRide(PassengerId).FlightId.Should().Be(Service.GetRide(OtherPassengerId).FlightId);
        Service.GetRide(OtherPassengerId).Status.Should().Be(ShuttleRideStatus.InTransit);
    }

    [Test]
    public void The_tick_does_not_board_a_saved_ticket_nobody_confirmed_this_session()
    {
        AddShuttleTemplate();
        var ride = Ticketed();
        Service.LoadShuttleFlights();
        Service.Now = FirstDeparture.AddSeconds(1);

        Service.Tick!();

        ride.Status.Should().Be(ShuttleRideStatus.Ticketed);
        _bench.Areas.Created.Should().BeEmpty();
        _bench.MessagesTo(_passenger.Id).Should().BeEmpty();
    }

    [Test]
    public void The_tick_tells_a_passenger_away_from_the_terminal_that_the_shuttle_left()
    {
        AddShuttleTemplate();
        Purchase();
        Service.LoadShuttleFlights();
        _passenger.Position = new Vector3(10f, 40f, 0f);
        Service.Now = FirstDeparture.AddSeconds(1);

        Service.Tick!();

        Service.GetRide(PassengerId).Status.Should().Be(ShuttleRideStatus.Ticketed, "the ticket stays valid");
        _bench.Areas.Created.Should().BeEmpty();
        var next = ShuttleSchedule.GetNextDepartureUtc(Origin, Destination, Service.Now);
        _bench.MessagesTo(_passenger.Id).Should().ContainSingle().Which.Should().Contain("You missed your shuttle to Tatooine!")
            .And.Contain($"The next shuttle departs in {Wait(next - Service.Now)}")
            .And.Contain("within 15 meters of the flights terminal");
    }

    [Test]
    public void The_tick_does_not_board_a_passenger_standing_at_a_terminal_of_another_planet()
    {
        AddShuttleTemplate();
        Purchase();
        Service.LoadShuttleFlights();
        _bench.Engine.SetLocalInt(_terminal, "CURRENT_LOCATION", (int)PlanetType.Tatooine);
        Service.Now = FirstDeparture.AddSeconds(1);

        Service.Tick!();

        Service.GetRide(PassengerId).Status.Should().Be(ShuttleRideStatus.Ticketed);
        _bench.MessagesTo(_passenger.Id).Should().ContainSingle().Which.Should().Contain("You missed your shuttle");
    }

    [Test]
    public void The_tick_skips_a_terminal_for_another_planet_and_boards_at_the_next_one_in_range()
    {
        AddShuttleTemplate();
        Purchase();
        Service.LoadShuttleFlights();
        _bench.Engine.SetLocalInt(_terminal, "CURRENT_LOCATION", (int)PlanetType.Tatooine);
        _bench.AddTerminal(Origin, _starport, new Vector3(10f, 20f, 0f));
        Service.Now = FirstDeparture.AddSeconds(1);

        Service.Tick!();

        Service.GetRide(PassengerId).Status.Should().Be(ShuttleRideStatus.InTransit);
    }

    [TestCase("dead")]
    [TestCase("possessed")]
    public void The_tick_does_not_board_a_dead_or_possessed_passenger(string condition)
    {
        AddShuttleTemplate();
        Purchase();
        Service.LoadShuttleFlights();
        _passenger.IsDead = condition == "dead";
        _passenger.IsDMPossessed = condition == "possessed";
        Service.Now = FirstDeparture.AddSeconds(1);

        Service.Tick!();

        Service.GetRide(PassengerId).Status.Should().Be(ShuttleRideStatus.Ticketed);
        _bench.MessagesTo(_passenger.Id).Should().ContainSingle().Which.Should().Contain("You missed your shuttle");
    }

    [Test]
    public void The_tick_leaves_a_passenger_who_logged_off_with_the_ticket()
    {
        AddShuttleTemplate();
        Purchase();
        Service.LoadShuttleFlights();
        _bench.Remove(_passenger.Id);
        Service.Now = FirstDeparture.AddSeconds(1);

        Service.Tick!();

        Service.GetRide(PassengerId).Status.Should().Be(ShuttleRideStatus.Ticketed);
        _bench.Engine.CallsTo("SendMessageToPC").Should().BeEmpty();
    }

    [Test]
    public void The_tick_drops_a_ticket_holder_whose_ticket_is_gone_from_the_database()
    {
        AddShuttleTemplate();
        var ride = Purchase();
        Service.LoadShuttleFlights();
        _bench.Db.Delete<ShuttleRide>(ride.Id);
        Service.Now = FirstDeparture.AddSeconds(1);

        Service.Tick!();

        _bench.Areas.Created.Should().BeEmpty();
        _bench.MessagesTo(_passenger.Id).Should().BeEmpty();
    }

    [Test]
    public void The_tick_calls_boarding_shortly_before_a_departure_once()
    {
        Purchase();
        Service.LoadShuttleFlights();

        Service.Tick!();
        Service.Tick!();

        _bench.MessagesTo(_passenger.Id).Should().ContainSingle().Which.Should().Contain("Now boarding: shuttle to Tatooine departs in 10 seconds")
            .And.Contain("Be within 15 meters of the flights terminal!");
    }

    [Test]
    public void The_tick_announces_the_next_departure_again_after_the_previous_one_has_passed()
    {
        Purchase();
        Service.LoadShuttleFlights();
        Service.Tick!();
        _passenger.Position = new Vector3(10f, 40f, 0f);
        Service.Now = FirstDeparture.AddSeconds(1);
        Service.Tick!();
        _bench.Locals.ClearCalls();

        Service.Now = FirstDeparture.AddSeconds(100);
        Service.Tick!();

        _bench.MessagesTo(_passenger.Id).Should().ContainSingle().Which.Should().Contain("Now boarding: shuttle to Tatooine departs in 20 seconds");
    }

    [Test]
    public void The_tick_does_not_call_boarding_while_the_next_departure_is_far_off()
    {
        Purchase();
        Service.LoadShuttleFlights();
        _passenger.Position = new Vector3(10f, 40f, 0f);
        Service.Now = FirstDeparture.AddSeconds(1);
        _bench.Locals.ClearCalls();

        Service.Tick!();

        _bench.MessagesTo(_passenger.Id).Should().NotContain(message => message.Contains("Now boarding"));
    }

    [Test]
    public void The_tick_does_not_call_boarding_for_a_passenger_who_is_not_online()
    {
        Purchase();
        Service.LoadShuttleFlights();
        _bench.Remove(_passenger.Id);

        Service.Tick!();

        _bench.Engine.CallsTo("SendMessageToPC").Should().BeEmpty();
    }

    [Test]
    public void The_tick_calls_boarding_only_for_the_route_of_the_ticket()
    {
        var elsewhere = _bench.AddPlayer(OtherPassengerId, _starport, new Vector3(11f, 11f, 0f), 100000);
        Purchase();
        Service.TryPurchaseTicket(elsewhere.Id, PlanetType.Tatooine, PlanetType.Viscara, string.Empty, 0f).Should().BeTrue();
        Service.LoadShuttleFlights();

        Service.Tick!();

        _bench.MessagesTo(_passenger.Id).Should().OnlyContain(message => message.Contains("shuttle to Tatooine"));
        _bench.MessagesTo(elsewhere.Id).Should().OnlyContain(message => message.Contains("shuttle to Viscara"));
    }

    // ---- in transit and arrival

    [Test]
    public void The_status_console_announces_the_remaining_time_once_a_minute()
    {
        BoardThePassenger();
        var console = _bench.Objects.Single(sim => sim.Type == ObjectType.Placeable && sim.Tag == "flights_terminal" && sim.Area == FlightArea());
        var boardedAt = Service.Now;

        Service.Now = boardedAt.AddSeconds(30);
        Service.Tick!();
        _bench.AssignedTo(console.Id).Should().BeEmpty("a minute has not passed");

        Service.Now = boardedAt.AddSeconds(61);
        Service.Tick!();
        Service.Tick!();

        _bench.AssignedTo(console.Id).Should().ContainSingle();
        _bench.RunAssigned(console.Id);
        var remaining = Wait(boardedAt.AddSeconds(TransitSeconds) - Service.Now);
        _bench.Engine.CallsTo("SpeakString").Should().ContainSingle().Which.Arguments[0].Should().Be($"Now arriving at Tatooine in {remaining}.");
    }

    [Test]
    public void The_status_console_stays_quiet_once_it_is_gone()
    {
        BoardThePassenger();
        var console = _bench.Objects.Single(sim => sim.Type == ObjectType.Placeable && sim.Tag == "flights_terminal" && sim.Area == FlightArea());
        _bench.Remove(console.Id);

        Service.Now = Service.Now.AddSeconds(61);
        Service.Tick!();

        _bench.AssignedTo(console.Id).Should().BeEmpty();
    }

    [Test]
    public void The_tick_delivers_a_passenger_aboard_when_the_flight_arrives_and_ends_the_ride()
    {
        var ride = BoardThePassenger();
        var area = FlightArea();
        _passenger.Area = area;
        var arrival = ride.ArrivalUtc;
        _bench.Locals.ClearCalls();

        Service.Now = arrival;
        Service.Tick!();

        _bench.MessagesTo(_passenger.Id).Should().Contain("The shuttle touches down at Tatooine.");
        _bench.Db.Store.Deleted.Should().Contain(ride.Id);
        Service.GetRide(PassengerId).Should().BeNull();
        _bench.RunAssigned(_passenger.Id);
        var jump = (Location)_bench.Engine.CallsTo("ActionJumpToLocation").Last().Arguments[0]!;
        _bench.Describe(jump).Area.Should().Be(_landingArea);
        _bench.Describe(jump).Position.Should().Be(new Vector3(50f, 60f, 1f));
    }

    [Test]
    public void The_tick_forgets_the_flight_when_it_arrives()
    {
        var ride = BoardThePassenger();
        var console = _bench.Objects.Single(sim => sim.Type == ObjectType.Placeable && sim.Tag == "flights_terminal" && sim.Area == FlightArea());

        Service.Now = ride.ArrivalUtc;
        Service.Tick!();

        Service.TryGetFlightInfo(console.Id, out _, out _).Should().BeFalse();
    }

    [Test]
    public void The_tick_waits_for_the_arrival_time_before_delivering_anyone()
    {
        var ride = BoardThePassenger();
        _passenger.Area = FlightArea();

        Service.Now = ride.ArrivalUtc.AddSeconds(-1);
        Service.Tick!();

        Service.GetRide(PassengerId).Status.Should().Be(ShuttleRideStatus.InTransit);
        _bench.MessagesTo(_passenger.Id).Should().NotContain("The shuttle touches down at Tatooine.");
    }

    [Test]
    public void The_tick_does_not_deliver_a_passenger_who_left_the_flight_before_it_landed()
    {
        var ride = BoardThePassenger();
        _passenger.Area = _starport;
        _bench.Locals.ClearCalls();

        Service.Now = ride.ArrivalUtc;
        Service.Tick!();

        _bench.MessagesTo(_passenger.Id).Should().BeEmpty();
        _bench.AssignedTo(_passenger.Id).Should().BeEmpty("a passenger who left forfeits the trip");
        _bench.Db.Store.Deleted.Should().Contain(ride.Id);
    }

    [Test]
    public void The_tick_records_the_landing_of_a_passenger_who_is_offline_at_arrival_and_keeps_the_ride()
    {
        var ride = BoardThePassenger();
        var stored = StoredPlayer(PassengerId);
        _bench.Remove(_passenger.Id);

        Service.Now = ride.ArrivalUtc;
        Service.Tick!();

        stored.LocationAreaResref.Should().Be("tatooine_landing_zone");
        _bench.Db.Store.Deleted.Should().NotContain(ride.Id, "the ride stays until the passenger logs in");
    }

    [Test]
    public void The_tick_delivers_a_passenger_when_the_flight_instance_is_gone_after_a_restart()
    {
        var ride = InTransit(PassengerId, Start.AddSeconds(40));
        Service.LoadShuttleFlights();

        Service.Now = ride.ArrivalUtc;
        Service.Tick!();

        _bench.MessagesTo(_passenger.Id).Should().Contain("The shuttle touches down at Tatooine.");
        _bench.Db.Store.Deleted.Should().Contain(ride.Id);
        Service.Scheduled.Should().BeEmpty("there is no instance to destroy");
    }

    [Test]
    public void The_tick_delivers_anyone_aboard_without_a_ride_record_and_destroys_the_instance_later()
    {
        var ride = BoardThePassenger();
        var area = FlightArea();
        var stowaway = _bench.AddPlayer("stowaway", area, new Vector3(4f, 4f, 0f));
        var droid = _bench.Objects.Single(sim => sim.ResRef == "maledroid");
        droid.Area = area;

        Service.Now = ride.ArrivalUtc;
        Service.Tick!();

        _bench.AssignedTo(stowaway.Id).Should().ContainSingle("the safety net delivers anyone physically aboard");
        _bench.AssignedTo(droid.Id).Should().ContainSingle("only the seat command: creatures are not delivered");
        Service.Scheduled.Should().ContainSingle().Which.Delay.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Test]
    public void The_instance_is_destroyed_after_thirty_seconds_and_stragglers_are_delivered_first()
    {
        var ride = BoardThePassenger();
        var area = FlightArea();
        Service.Now = ride.ArrivalUtc;
        Service.Tick!();
        var straggler = _bench.AddPlayer("straggler", area, new Vector3(4f, 4f, 0f));

        Service.Scheduled.Single().Task();

        _bench.AssignedTo(straggler.Id).Should().ContainSingle();
        _bench.Engine.CallsTo("DestroyArea").Should().ContainSingle().Which.Arguments.Should().Equal(area);
        _bench.Exists(area).Should().BeFalse();
        Service.Scheduled.Should().HaveCount(1, "the area was destroyed, so nothing is retried");
    }

    [Test]
    public void The_instance_destruction_is_retried_until_the_engine_destroys_the_area()
    {
        var ride = BoardThePassenger();
        Service.Now = ride.ArrivalUtc;
        Service.Tick!();
        _bench.DestroyAreaResult = 0;

        Service.Scheduled.Single().Task();

        Service.Scheduled.Should().HaveCount(2);
        Service.Scheduled[1].Delay.Should().Be(TimeSpan.FromSeconds(30));
        _bench.DestroyAreaResult = 1;
        Service.Scheduled[1].Task();
        Service.Scheduled.Should().HaveCount(2);
    }

    [Test]
    public void The_instance_destruction_does_nothing_once_the_area_is_already_gone()
    {
        var ride = BoardThePassenger();
        var area = FlightArea();
        Service.Now = ride.ArrivalUtc;
        Service.Tick!();
        _bench.Remove(area);

        Service.Scheduled.Single().Task();

        _bench.Engine.CallsTo("DestroyArea").Should().BeEmpty();
    }

    [Test]
    public void Delivering_a_dead_passenger_jumps_them_directly_and_a_living_one_after_clearing_actions()
    {
        var ride = BoardThePassenger();
        _passenger.Area = FlightArea();
        _passenger.IsDead = true;
        var dm = _bench.AddPlayer("dm-in-the-shuttle", FlightArea());
        dm.IsDM = true;
        var possessed = _bench.AddPlayer("possessed", FlightArea());
        possessed.IsDMPossessed = true;
        _bench.Locals.ClearCalls();

        Service.Now = ride.ArrivalUtc;
        Service.Tick!();

        _bench.AssignedTo(dm.Id).Should().BeEmpty("a DM is never moved");
        _bench.AssignedTo(possessed.Id).Should().BeEmpty("a possessed creature is never moved");
        _bench.RunAssigned(_passenger.Id);
        _bench.Engine.CallsTo("JumpToLocation").Should().NotBeEmpty("a dead passenger jumps directly");
        _bench.Engine.CallsTo("ActionJumpToLocation").Should().BeEmpty();
    }

    // ---- logging in

    [Test]
    public void TryGetLoginRedirect_ignores_anyone_who_is_not_a_playing_player()
    {
        var creature = _bench.AddCreature("npc", _starport);
        var dm = _bench.AddPlayer("dm", _starport);
        dm.IsDM = true;

        Service.TryGetLoginRedirect(creature.Id, out var npcLocation).Should().BeFalse();
        Service.TryGetLoginRedirect(dm.Id, out var dmLocation).Should().BeFalse();

        npcLocation.Should().BeNull();
        dmLocation.Should().BeNull();
    }

    [Test]
    public void TryGetLoginRedirect_ignores_a_player_without_a_flight()
    {
        Ticketed();

        Service.TryGetLoginRedirect(_passenger.Id, out var location).Should().BeFalse();

        location.Should().BeNull();
    }

    [Test]
    public void TryGetLoginRedirect_sends_a_passenger_whose_flight_landed_to_the_landing_pad_and_ends_the_ride()
    {
        var ride = InTransit(PassengerId, Start.AddSeconds(-1));
        StoredPlayer(PassengerId);

        Service.TryGetLoginRedirect(_passenger.Id, out var location).Should().BeTrue();

        var landing = _bench.Describe(location);
        landing.Area.Should().Be(_landingArea);
        landing.Position.Should().Be(new Vector3(50f, 60f, 1f));
        _bench.MessagesTo(_passenger.Id).Should().ContainSingle().Which.Should().Be("Your shuttle has arrived at Tatooine.");
        _bench.Db.Store.Deleted.Should().ContainSingle().Which.Should().Be(ride.Id);
    }

    [Test]
    public void TryGetLoginRedirect_announces_the_arrival_but_does_not_move_a_player_with_a_pending_rebuild()
    {
        var ride = InTransit(PassengerId, Start.AddSeconds(-1));
        StoredPlayer(PassengerId, rebuildComplete: false);

        Service.TryGetLoginRedirect(_passenger.Id, out var location).Should().BeFalse();

        location.Should().BeNull();
        _bench.MessagesTo(_passenger.Id).Should().ContainSingle().Which.Should().Be("Your shuttle has arrived at Tatooine.");
        _bench.Db.Store.Deleted.Should().ContainSingle().Which.Should().Be(ride.Id);
    }

    [Test]
    public void TryGetLoginRedirect_announces_the_arrival_but_does_not_move_a_player_with_no_saved_character()
    {
        InTransit(PassengerId, Start.AddSeconds(-1));

        Service.TryGetLoginRedirect(_passenger.Id, out var location).Should().BeFalse();

        location.Should().BeNull();
        _bench.MessagesTo(_passenger.Id).Should().ContainSingle();
    }

    [Test]
    public void TryGetLoginRedirect_keeps_the_ride_of_a_player_with_a_pending_rebuild_while_the_flight_is_in_the_air()
    {
        InTransit(PassengerId, Start.AddMinutes(3));
        StoredPlayer(PassengerId, rebuildComplete: false);

        Service.TryGetLoginRedirect(_passenger.Id, out var location).Should().BeFalse();

        location.Should().BeNull();
        _bench.Db.Store.Deleted.Should().BeEmpty();
        _bench.MessagesTo(_passenger.Id).Should().BeEmpty();
    }

    [Test]
    public void TryGetLoginRedirect_puts_a_passenger_back_aboard_a_flight_still_in_the_air()
    {
        AddShuttleTemplate();
        InTransit(PassengerId, Start.AddMinutes(3));
        StoredPlayer(PassengerId);

        Service.TryGetLoginRedirect(_passenger.Id, out var location).Should().BeTrue();

        var entrance = _bench.Describe(location);
        entrance.Area.Should().Be(FlightArea());
        entrance.Position.Should().Be(new Vector3(3f, 4f, 0f));
        _bench.Areas.Created.Should().ContainSingle("the shuttle interior is recreated lazily");
    }

    [Test]
    public void TryGetLoginRedirect_reuses_the_flight_instance_for_a_second_passenger()
    {
        AddShuttleTemplate();
        var arrival = Start.AddMinutes(3);
        InTransit(PassengerId, arrival, "shared-flight");
        InTransit(OtherPassengerId, arrival, "shared-flight");
        StoredPlayer(PassengerId);
        StoredPlayer(OtherPassengerId);
        var other = _bench.AddPlayer(OtherPassengerId, _starport);

        Service.TryGetLoginRedirect(_passenger.Id, out var first).Should().BeTrue();
        Service.TryGetLoginRedirect(other.Id, out var second).Should().BeTrue();

        _bench.Areas.Created.Should().ContainSingle();
        _bench.Describe(second).Area.Should().Be(_bench.Describe(first).Area);
    }

    // ---- entering and leaving the server

    private void Enters(SimObject player) => _bench.Engine.SetReturn("GetEnteringObject", player.Id);

    [Test]
    public void EnterServer_ignores_creatures_and_DMs()
    {
        var dm = _bench.AddPlayer("dm", _starport);
        dm.IsDM = true;

        Enters(_bench.AddCreature("npc", _starport));
        Service.EnterServer();
        Enters(dm);
        Service.EnterServer();

        Service.Scheduled.Should().BeEmpty();
    }

    [Test]
    public void EnterServer_checks_the_player_again_two_seconds_after_they_arrive()
    {
        Enters(_passenger);

        Service.EnterServer();

        Service.Scheduled.Should().ContainSingle().Which.Delay.Should().Be(TimeSpan.FromSeconds(2));
    }

    [Test]
    public void EnterServer_reminds_a_returning_ticket_holder_and_gives_a_full_boarding_call_interval()
    {
        Ticketed();
        Enters(_passenger);
        Service.EnterServer();

        Service.Scheduled.Single().Task();

        var next = ShuttleSchedule.GetNextDepartureUtc(Origin, Destination, Service.Now, Service.Now.AddSeconds(60));
        _bench.MessagesTo(_passenger.Id).Should().ContainSingle().Which.Should()
            .Contain("You still have a shuttle ticket from Viscara to Tatooine.")
            .And.Contain($"Automatic boarding resumes in {Wait(next - Service.Now)} if you are within 15 meters of a flights terminal at Viscara.");
        Service.GetNextDepartureUtc(Origin, Destination, PassengerId).Should().Be(next);
    }

    [Test]
    public void EnterServer_does_not_postpone_a_ticket_bought_while_the_login_check_was_pending()
    {
        Enters(_passenger);
        Service.EnterServer();
        Purchase();
        _bench.Locals.ClearCalls();

        Service.Scheduled.Single().Task();

        _bench.MessagesTo(_passenger.Id).Should().BeEmpty();
        Service.GetNextDepartureUtc(Origin, Destination, PassengerId)
            .Should().Be(ShuttleSchedule.GetNextDepartureUtc(Origin, Destination, Service.Now));
    }

    [Test]
    public void EnterServer_ends_the_boarding_eligibility_of_the_previous_session()
    {
        Enters(_passenger);
        Ticketed();
        Service.EnterServer();
        Service.Scheduled.Single().Task();
        Service.GetNextDepartureUtc(Origin, Destination, PassengerId).Should()
            .Be(ShuttleSchedule.GetNextDepartureUtc(Origin, Destination, Service.Now, Service.Now.AddSeconds(60)));

        Service.EnterServer();

        Service.GetNextDepartureUtc(Origin, Destination, PassengerId)
            .Should().Be(ShuttleSchedule.GetNextDepartureUtc(Origin, Destination, Service.Now));
    }

    [Test]
    public void EnterServer_does_nothing_for_a_player_who_has_gone_by_the_time_it_checks()
    {
        Ticketed();
        Enters(_passenger);
        Service.EnterServer();
        _bench.Remove(_passenger.Id);

        Service.Scheduled.Single().Task();

        _bench.Engine.CallsTo("SendMessageToPC").Should().BeEmpty();
    }

    [Test]
    public void EnterServer_does_nothing_for_a_player_without_a_ride()
    {
        Enters(_passenger);
        Service.EnterServer();

        Service.Scheduled.Single().Task();

        _bench.Engine.CallsTo("SendMessageToPC").Should().BeEmpty();
        _bench.Engine.CallsTo("AssignCommand").Should().BeEmpty();
    }

    [Test]
    public void EnterServer_ignores_a_ride_that_is_neither_a_ticket_nor_a_flight()
    {
        var ride = Ticketed();
        ride.Status = ShuttleRideStatus.Invalid;
        Enters(_passenger);
        Service.EnterServer();

        Service.Scheduled.Single().Task();

        _bench.Engine.CallsTo("SendMessageToPC").Should().BeEmpty();
    }

    [Test]
    public void EnterServer_resumes_a_flight_for_a_passenger_logging_back_in_and_sends_them_to_the_shuttle()
    {
        AddShuttleTemplate();
        var ride = InTransit(PassengerId, Start.AddMinutes(3));
        StoredPlayer(PassengerId);
        Enters(_passenger);
        Service.EnterServer();

        Service.Scheduled.Single().Task();

        _bench.MessagesTo(_passenger.Id).Should().ContainSingle().Which.Should()
            .Be($"You resume your shuttle flight to Tatooine. Estimated arrival: {Wait(ride.ArrivalUtc - Service.Now)}.");
        _bench.RunAssigned(_passenger.Id);
        var jump = (Location)_bench.Engine.CallsTo("ActionJumpToLocation").Should().ContainSingle().Subject.Arguments[0]!;
        _bench.Describe(jump).Area.Should().Be(FlightArea());
    }

    [Test]
    public void EnterServer_leaves_a_passenger_who_reconnected_directly_into_the_shuttle_where_they_are()
    {
        AddShuttleTemplate();
        InTransit(PassengerId, Start.AddMinutes(3));
        StoredPlayer(PassengerId);
        Service.TryGetLoginRedirect(_passenger.Id, out var _).Should().BeTrue();
        _passenger.Area = FlightArea();
        _bench.Locals.ClearCalls();
        Enters(_passenger);
        Service.EnterServer();

        Service.Scheduled.Single().Task();

        _bench.MessagesTo(_passenger.Id).Should().ContainSingle().Which.Should().StartWith("You resume your shuttle flight to Tatooine.");
        _bench.Engine.CallsTo("AssignCommand").Should().BeEmpty("they are already aboard");
        _bench.Areas.Created.Should().ContainSingle();
    }

    [Test]
    public void EnterServer_sends_a_passenger_whose_flight_landed_to_the_landing_pad_without_a_resume_notice()
    {
        InTransit(PassengerId, Start.AddSeconds(-1));
        StoredPlayer(PassengerId);
        Enters(_passenger);
        Service.EnterServer();

        Service.Scheduled.Single().Task();

        _bench.MessagesTo(_passenger.Id).Should().ContainSingle().Which.Should().Be("Your shuttle has arrived at Tatooine.");
        _bench.RunAssigned(_passenger.Id);
        var jump = (Location)_bench.Engine.CallsTo("ActionJumpToLocation").Should().ContainSingle().Subject.Arguments[0]!;
        _bench.Describe(jump).Area.Should().Be(_landingArea);
    }

    [Test]
    public void EnterServer_does_not_send_a_passenger_with_a_pending_rebuild_anywhere()
    {
        InTransit(PassengerId, Start.AddSeconds(-1));
        StoredPlayer(PassengerId, rebuildComplete: false);
        Enters(_passenger);
        Service.EnterServer();

        Service.Scheduled.Single().Task();

        _bench.Engine.CallsTo("AssignCommand").Should().BeEmpty();
    }

    [Test]
    public void ExitServer_ends_the_boarding_eligibility_of_the_leaving_player()
    {
        Ticketed();
        Enters(_passenger);
        Service.EnterServer();
        Service.Scheduled.Single().Task();
        _bench.Engine.SetReturn("GetExitingObject", _passenger.Id);

        Service.ExitServer();

        Service.GetNextDepartureUtc(Origin, Destination, PassengerId)
            .Should().Be(ShuttleSchedule.GetNextDepartureUtc(Origin, Destination, Service.Now));
    }

    // ---- leaving the shuttle

    private void LeavesShuttle(SimObject leaving, uint area)
    {
        _bench.Engine.SetReturn("GetExitingObject", leaving.Id);
        _bench.Engine.SetReturn("OBJECT_SELF", area);
    }

    private (uint Area, ShuttleRide Ride) PassengerInFlight(DateTime arrival, string flightId = "flight-7")
    {
        var area = _bench.AddArea("flight_instance");
        _bench.Engine.SetLocalString(area, "SHUTTLE_FLIGHT_ID", flightId);
        return (area, InTransit(PassengerId, arrival, flightId));
    }

    [Test]
    public void HandleShuttleAreaExit_forfeits_the_ticket_of_a_passenger_who_leaves_before_arrival()
    {
        var (area, ride) = PassengerInFlight(Start.AddMinutes(3));
        LeavesShuttle(_passenger, area);

        Service.HandleShuttleAreaExit();

        _bench.Db.Store.Deleted.Should().ContainSingle().Which.Should().Be(ride.Id);
    }

    [Test]
    public void HandleShuttleAreaExit_keeps_the_ride_of_a_passenger_leaving_at_arrival()
    {
        var (area, _) = PassengerInFlight(Start);
        LeavesShuttle(_passenger, area);

        Service.HandleShuttleAreaExit();

        _bench.Db.Store.Deleted.Should().BeEmpty();
    }

    [Test]
    public void HandleShuttleAreaExit_ignores_a_creature_or_a_DM_leaving()
    {
        var (area, _) = PassengerInFlight(Start.AddMinutes(3));
        var dm = _bench.AddPlayer("dm", area);
        dm.IsDM = true;

        LeavesShuttle(_bench.AddCreature("npc", area), area);
        Service.HandleShuttleAreaExit();
        LeavesShuttle(dm, area);
        Service.HandleShuttleAreaExit();

        _bench.Db.Store.Deleted.Should().BeEmpty();
    }

    [Test]
    public void HandleShuttleAreaExit_ignores_an_area_that_is_not_a_shuttle_flight()
    {
        var ride = InTransit(PassengerId, Start.AddMinutes(3));
        LeavesShuttle(_passenger, _starport);

        Service.HandleShuttleAreaExit();

        _bench.Db.Store.Deleted.Should().BeEmpty();
        Service.GetRide(PassengerId).Should().BeSameAs(ride);
    }

    [Test]
    public void HandleShuttleAreaExit_ignores_a_passenger_without_a_ride_or_on_another_flight_or_still_ticketed()
    {
        var (area, ride) = PassengerInFlight(Start.AddMinutes(3), "flight-7");
        LeavesShuttle(_passenger, area);

        // No ride at all.
        var stranger = _bench.AddPlayer("stranger", area);
        LeavesShuttle(stranger, area);
        Service.HandleShuttleAreaExit();

        // A ride on a different flight.
        ride.FlightId = "another-flight";
        LeavesShuttle(_passenger, area);
        Service.HandleShuttleAreaExit();

        // A ticket that has not boarded.
        ride.FlightId = "flight-7";
        ride.Status = ShuttleRideStatus.Ticketed;
        Service.HandleShuttleAreaExit();

        _bench.Db.Store.Deleted.Should().BeEmpty();
    }

    // ---- queries

    [Test]
    public void GetRide_finds_the_ride_of_a_player_and_nobody_elses()
    {
        var ride = Ticketed();

        Service.GetRide(PassengerId).Should().BeSameAs(ride);
        Service.GetRide(OtherPassengerId).Should().BeNull();
    }

    [Test]
    public void GetNextDepartureUtc_is_the_scheduled_departure_after_now()
    {
        Service.GetNextDepartureUtc(Origin, Destination).Should().Be(FirstDeparture);
        Service.GetNextDepartureUtc(Origin, Destination, string.Empty).Should().Be(FirstDeparture);
        Service.GetNextDepartureUtc(Origin, Destination, "   ").Should().Be(FirstDeparture);
        Service.GetNextDepartureUtc(Origin, Destination, "unknown-passenger").Should().Be(FirstDeparture);
    }

    [Test]
    public void GetNextDepartureUtc_respects_the_boarding_eligibility_of_a_passenger()
    {
        Ticketed();
        Enters(_passenger);
        Service.EnterServer();
        Service.Scheduled.Single().Task();

        var departure = Service.GetNextDepartureUtc(Origin, Destination, PassengerId);

        departure.Should().BeOnOrAfter(Service.Now.AddSeconds(60));
        departure.Should().BeAfter(FirstDeparture, "the grace period skips the departure the passenger cannot make");
    }

    [Test]
    public void TryGetFlightInfo_answers_false_for_an_object_without_a_known_flight()
    {
        var console = _bench.AddPlaceable("flights_terminal", _starport);

        Service.TryGetFlightInfo(console, out var destination, out var arrival).Should().BeFalse();
        destination.Should().Be(PlanetType.Invalid);
        arrival.Should().Be(default(DateTime));

        _bench.Engine.SetLocalString(console, "SHUTTLE_FLIGHT_ID", "unknown-flight");
        Service.TryGetFlightInfo(console, out _, out _).Should().BeFalse();
    }
}
