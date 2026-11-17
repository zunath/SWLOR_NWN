using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Domain.World;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// Handles scheduled passenger shuttle flights between planets.
    /// Players purchase a ticket at a starport flights terminal, wait for the scheduled
    /// departure, ride an instanced shuttle interior, and are delivered to the destination
    /// planet's landing waypoint when the flight arrives. The logic and state that used to be a static class, moved
    /// here unchanged; the event handlers run on the container's instance and static callers reach it through
    /// <c>GameServices.Shuttle</c>.
    /// </summary>
    public class ShuttleService : IShuttleService
    {
        private const float BoardingRangeMeters = ShuttleBoardingService.BoardingRangeMeters;
        private const int BoardingCallSeconds = ShuttleBoardingService.BoardingCallSeconds;

        // The flights and tickets (adopted) and the boarding calls and permissions (carried) are in ShuttleStateService.
        [ReloadState(ReloadStateKind.Runtime)]
        private DateTime _lastTickUtc;
        // The in-transit rides LoadShuttleFlights found already landed; DeliverOfflinePassengers delivers them at boot.
        [ReloadState(ReloadStateKind.Runtime)]
        private readonly List<ShuttleRide> _ridesLandedWhileDown = new();

        private readonly IDBService _db;
        private readonly IPlanetService _planet;
        private readonly IShuttleStateService _state;
        private readonly IShuttleClockService _clock;
        private readonly IShuttleTicketService _tickets;
        private readonly IShuttleFlightService _flights;
        private readonly IShuttlePassengerService _passengers;
        private readonly IShuttleBoardingService _boarding;
        private readonly IShuttleArrivalService _arrival;

        public ShuttleService(IDBService db, IPlanetService planet, IShuttleStateService state, IShuttleClockService clock,
            IShuttleTicketService tickets, IShuttleFlightService flights, IShuttlePassengerService passengers,
            IShuttleBoardingService boarding, IShuttleArrivalService arrival)
        {
            _db = db;
            _planet = planet;
            _state = state;
            _clock = clock;
            _tickets = tickets;
            _flights = flights;
            _passengers = passengers;
            _boarding = boarding;
            _arrival = arrival;
        }

        /// <summary>
        /// When the module loads, recover in-transit flights from the database and
        /// begin processing the flight schedule. It only reads the database, so a hot reload runs it again;
        /// <see cref="DeliverOfflinePassengers"/> runs right after it at boot.
        /// </summary>
        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public void LoadShuttleFlights()
        {
            var now = _clock.UtcNow();
            RecoverRidesAfterRestart(now);

            _lastTickUtc = now;
            _clock.ScheduleRepeating(ProcessFlightSchedule, TimeSpan.FromSeconds(2));
        }

        /// <summary>
        /// After <see cref="LoadShuttleFlights"/>, delivers the passengers whose flight landed while the server was down
        /// (the rides it found arrived by the time it read them) to their destination on their offline record. Boot only:
        /// it writes the player records. A hot reload runs <see cref="AdoptFlights"/> instead.
        /// </summary>
        [BootPhase(BootReload.BootOnly, AdoptedBy = nameof(AdoptFlights))]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public void DeliverOfflinePassengers()
        {
            foreach (var ride in _ridesLandedWhileDown)
            {
                // The flight landed while the server was down. Deliver the passenger
                // to the destination on their offline record; they log in at the landing pad.
                _passengers.DeliverOfflinePassenger(ride);
            }

            _ridesLandedWhileDown.Clear();
        }

        /// <summary>
        /// On a hot reload, after the rerun <see cref="LoadShuttleFlights"/> (which read the tickets and flights from Redis and
        /// started the flight tick): stands in for <see cref="DeliverOfflinePassengers"/> without writing anything. A ride the
        /// rerun found landed belongs to a flight that landed in the old build's last moments, so its flight goes back on the
        /// schedule and the next tick delivers it as it delivers any arrival (once, online or offline). Each flight in the air
        /// gets back its live shuttle interior, console and boarding point. An interior whose flight has landed is torn down
        /// as the old build had scheduled.
        /// </summary>
        public void AdoptFlights()
        {
            var now = _clock.UtcNow();
            foreach (var ride in _ridesLandedWhileDown)
            {
                if (_state.ActiveFlights.ContainsKey(ride.FlightId))
                    continue;

                _state.ActiveFlights[ride.FlightId] = new ShuttleActiveFlight
                {
                    FlightId = ride.FlightId,
                    Origin = ride.Origin,
                    Destination = ride.Destination,
                    DepartureUtc = ride.DepartureUtc,
                    ArrivalUtc = ride.ArrivalUtc,
                    LastBroadcastUtc = now
                };
            }

            _ridesLandedWhileDown.Clear();

            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                var flightId = GetLocalString(area, ShuttleFlightService.ShuttleFlightIdVariable);
                if (string.IsNullOrEmpty(flightId))
                    continue;

                if (_state.ActiveFlights.TryGetValue(flightId, out var flight))
                {
                    _flights.AttachFlightInstance(flight, area);
                }
                else if (ShuttleSchedule.TryParseFlightId(flightId, out _, out var destination, out _))
                {
                    // A for loop's variable is shared by every iteration, so the scheduled teardown takes its own copy.
                    var landedArea = area;
                    var landingLocation = _passengers.GetLandingLocation(destination);
                    _clock.Schedule(() => _flights.DestroyFlightInstance(landedArea, landingLocation), TimeSpan.FromSeconds(30));
                }
            }
        }

        /// <summary>
        /// Restores ticketed and in-transit rides from the database after a server restart. The rides that already
        /// landed are kept for <see cref="DeliverOfflinePassengers"/>.
        /// </summary>
        private void RecoverRidesAfterRestart(DateTime now)
        {
            var rides = _tickets.SearchRidesByStatus(ShuttleRideStatus.Ticketed);
            foreach (var ride in rides)
            {
                _state.TicketHolders[ride.PlayerId] = (ride.Origin, ride.Destination);
            }

            _ridesLandedWhileDown.Clear();
            var inTransit = _tickets.SearchRidesByStatus(ShuttleRideStatus.InTransit);
            foreach (var ride in inTransit)
            {
                if (ride.ArrivalUtc <= now)
                {
                    _ridesLandedWhileDown.Add(ride);
                }
                else if (!_state.ActiveFlights.ContainsKey(ride.FlightId))
                {
                    // Flight still in the air. Re-register it without an area; the shuttle
                    // interior is recreated lazily if a passenger logs back in before arrival.
                    _state.ActiveFlights[ride.FlightId] = new ShuttleActiveFlight
                    {
                        FlightId = ride.FlightId,
                        Origin = ride.Origin,
                        Destination = ride.Destination,
                        DepartureUtc = ride.DepartureUtc,
                        ArrivalUtc = ride.ArrivalUtc,
                        LastBroadcastUtc = now
                    };
                }
            }
        }

        /// <summary>
        /// Runs one scheduler tick: departures, boarding calls, arrivals, and transit broadcasts.
        /// </summary>
        private void ProcessFlightSchedule()
        {
            var now = _clock.UtcNow();

            _boarding.RearmReturnedPassengers();
            _boarding.ProcessDepartures(_lastTickUtc, now);
            _boarding.ConfirmBoardings(now);
            _boarding.ProcessBoardingCalls(now);
            _arrival.ProcessArrivals(now);
            _arrival.ProcessTransitBroadcasts(now);

            _lastTickUtc = now;
        }

        /// <summary>
        /// Determines where a logging-in player with an in-transit shuttle ride should be placed.
        /// Returns the destination landing pad if the flight has already arrived (and resolves the ride),
        /// or the shuttle interior if the flight is still in the air.
        /// </summary>
        /// <param name="player">The player logging in.</param>
        /// <param name="location">The location the player should be moved to.</param>
        /// <returns>true if the player must be redirected, false otherwise.</returns>
        public bool TryGetLoginRedirect(uint player, out Location location)
        {
            location = default;

            if (!GetIsPC(player) || GetIsDM(player))
                return false;

            var playerId = GetObjectUUID(player);
            var ride = _tickets.GetRide(playerId);
            if (ride == null || ride.Status != ShuttleRideStatus.InTransit)
                return false;

            return TryGetRideLoginRedirect(player, ride, out location);
        }

        private bool TryGetRideLoginRedirect(uint player, ShuttleRide ride, out Location location)
        {
            location = default;
            var arrived = ride.ArrivalUtc <= _clock.UtcNow();
            var dbPlayer = _db.Get<Player>(ride.PlayerId);
            if (dbPlayer == null || !dbPlayer.RebuildComplete)
            {
                // PersistentLocation owns mandatory rebuild routing. A completed flight
                // can report arrival and clear its record without moving the player out.
                if (arrived)
                    CompleteRideOnLogin(player, ride);
                return false;
            }

            if (arrived)
            {
                location = CompleteRideOnLogin(player, ride);
                return true;
            }

            var flight = _flights.GetOrCreateFlight(ride.FlightId, ride.Origin, ride.Destination, ride.DepartureUtc, ride.ArrivalUtc);
            location = flight.EntranceLocation;
            return true;
        }

        private Location CompleteRideOnLogin(uint player, ShuttleRide ride)
        {
            var location = _passengers.GetLandingLocation(ride.Destination);
            SendMessageToPC(player, $"Your shuttle has arrived at {_planet.GetPlanetByType(ride.Destination).Name}.");
            _db.Delete<ShuttleRide>(ride.Id);
            Log.Write(LogGroup.Server, $"Shuttle delivered passenger {ride.PlayerId} to {ride.Destination} on login.");
            return location;
        }

        /// <summary>
        /// Reminds a returning ticket holder and gives them a full boarding-call interval
        /// to refund or leave the terminal before automatic boarding can resume.
        /// </summary>
        private void RestoreTicketedSession(uint player, DateTime now)
        {
            var playerId = GetObjectUUID(player);
            // A ticket purchased during the login delay already has explicit boarding
            // consent for this session. Do not postpone the flight after confirmation.
            if (_state.BoardingNotBeforeUtc.ContainsKey(playerId))
                return;

            var ride = _tickets.GetRide(playerId);
            if (ride == null || ride.Status != ShuttleRideStatus.Ticketed)
                return;

            _state.TicketHolders[playerId] = (ride.Origin, ride.Destination);
            _state.BoardingNotBeforeUtc[playerId] = now.AddSeconds(BoardingCallSeconds);
            var next = _tickets.GetNextDepartureUtc(ride.Origin, ride.Destination, playerId);
            var wait = Time.GetTimeLongIntervals(next - now, false);
            var originName = _planet.GetPlanetByType(ride.Origin).Name;
            var destinationName = _planet.GetPlanetByType(ride.Destination).Name;
            SendMessageToPC(player, ColorToken.Yellow($"You still have a shuttle ticket from {originName} to {destinationName}. Automatic boarding resumes in {wait} if you are within {(int)BoardingRangeMeters} meters of a flights terminal at {originName}. You can refund your ticket at any flights terminal before boarding."));

            // The reminder above covers this session. Stay quiet about later departures until
            // the player is back at the terminal.
            if (!_boarding.IsAtBoardingTerminal(player, ride.Origin))
                _state.MissedBoardings.RecordMiss(playerId);
        }

        [PlayerSession(PlayerReload.LogoutOnly,
            "Forgets the boarding time; the boarding times are carried.")]
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public void ExitServer()
        {
            var playerId = GetObjectUUID(GetExitingObject());
            _state.BoardingNotBeforeUtc.Remove(playerId);
            _state.Boardings.Clear(playerId);
        }

        /// <summary>
        /// When a player enters the server, verify in-transit passengers actually ended up
        /// aboard their shuttle. This is a backstop for cases where the game placed them
        /// into a stale instance or an unexpected location.
        /// </summary>
        [PlayerSession(PlayerReload.LoginOnly,
            "Resumes a ticket or a flight at login with messages and a jump; the ticket holders and flights are adopted and the boarding times carried.")]
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public void EnterServer()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var playerId = GetObjectUUID(player);
            _state.BoardingNotBeforeUtc.Remove(playerId);

            _clock.Schedule(() =>
            {
                var current = _passengers.GetOnlinePlayerById(playerId);
                if (!GetIsObjectValid(current) || current != player)
                    return;

                var ride = _tickets.GetRide(playerId);
                if (ride == null)
                    return;

                if (ride.Status == ShuttleRideStatus.Ticketed)
                {
                    RestoreTicketedSession(current, _clock.UtcNow());
                    return;
                }

                if (ride.Status != ShuttleRideStatus.InTransit)
                    return;

                // Announce every resumed flight, including a reconnect directly into its
                // existing area. Login redirects may already have placed the passenger there.
                if (ride.ArrivalUtc > _clock.UtcNow())
                {
                    var remaining = Time.GetTimeLongIntervals(ride.ArrivalUtc - _clock.UtcNow(), false);
                    SendMessageToPC(current, $"You resume your shuttle flight to {_planet.GetPlanetByType(ride.Destination).Name}. Estimated arrival: {remaining}.");
                }

                if (_state.ActiveFlights.TryGetValue(ride.FlightId, out var flight) &&
                    GetIsObjectValid(flight.Area) &&
                    GetArea(current) == flight.Area &&
                    ride.ArrivalUtc > _clock.UtcNow())
                    return;

                if (TryGetLoginRedirect(current, out var location))
                {
                    var redirect = location;

                    // Follow a passenger sent back to a flight still in the air, so a dropped jump is retried.
                    if (ride.ArrivalUtc > _clock.UtcNow())
                        _state.Boardings.Start(playerId, ride.FlightId, _clock.UtcNow());

                    AssignCommand(current, () =>
                    {
                        ClearAllActions();
                        ActionJumpToLocation(redirect);
                    });
                }
            }, TimeSpan.FromSeconds(2));
        }

        /// <summary>
        /// When a passenger leaves the shuttle interior before it lands - by any means other than
        /// arrival, such as a DM port, a death respawn, or a recall - they forfeit the trip.
        /// The ride record is deleted so they are not re-boarded on login or delivered to the
        /// destination while offline. A normal logout does not fire this event, so passengers who
        /// stay aboard and disconnect still resume or arrive as intended.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAreaExit)]
        public void HandleShuttleAreaExit()
        {
            var player = GetExitingObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var area = OBJECT_SELF;
            var flightId = GetLocalString(area, ShuttleFlightService.ShuttleFlightIdVariable);
            if (string.IsNullOrWhiteSpace(flightId))
                return;

            var ride = _tickets.GetRide(GetObjectUUID(player));
            if (ride == null || ride.Status != ShuttleRideStatus.InTransit || ride.FlightId != flightId)
                return;

            // Arrival also moves passengers out of the interior, but only once the flight has
            // landed. An exit before that is a forced removal, not a normal disembarkation.
            if (ride.ArrivalUtc <= _clock.UtcNow())
                return;

            _state.Boardings.Clear(ride.PlayerId);
            _db.Delete<ShuttleRide>(ride.Id);
            Log.Write(LogGroup.Server, $"Shuttle passenger {ride.PlayerId} left flight {flightId} before arrival; ticket forfeited.");
        }

    }
}
