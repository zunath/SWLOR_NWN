using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// Boarding: boarding calls, putting the ticket holders at a flights terminal aboard a departing flight, following them
    /// until they stand in the shuttle, and the messages to those who missed it. The logic moved here unchanged from
    /// <see cref="ShuttleService"/> (P5-9c).
    /// </summary>
    public class ShuttleBoardingService : IShuttleBoardingService
    {
        internal const float BoardingRangeMeters = 15f;
        internal const int BoardingCallSeconds = 60;

        private readonly IDBService _db;
        private readonly IPlanetService _planet;
        private readonly IShuttleStateService _state;
        private readonly IShuttleClockService _clock;
        private readonly IShuttleTicketService _tickets;
        private readonly IShuttleFlightService _flights;
        private readonly IShuttlePassengerService _passengers;

        public ShuttleBoardingService(IDBService db, IPlanetService planet, IShuttleStateService state, IShuttleClockService clock,
            IShuttleTicketService tickets, IShuttleFlightService flights, IShuttlePassengerService passengers)
        {
            _db = db;
            _planet = planet;
            _state = state;
            _clock = clock;
            _tickets = tickets;
            _flights = flights;
            _passengers = passengers;
        }

        /// <summary>
        /// Boards passengers for every scheduled departure that occurred since the last tick.
        /// </summary>
        public void ProcessDepartures(DateTime lastTickUtc, DateTime now)
        {
            var routes = _state.TicketHolders.Values.Distinct().ToList();
            foreach (var route in routes)
            {
                var departures = ShuttleSchedule.GetDeparturesBetween(route.Origin, route.Destination, lastTickUtc, now);
                foreach (var departure in departures)
                {
                    BoardPassengers(route.Origin, route.Destination, departure, now);
                }
            }
        }

        /// <summary>
        /// Boards eligible ticket holders onto a departing flight and messages those who missed it.
        /// </summary>
        private void BoardPassengers(PlanetType origin, PlanetType destination, DateTime departureUtc, DateTime now)
        {
            var ticketHolders = _state.TicketHolders
                .Where(x => x.Value.Origin == origin && x.Value.Destination == destination)
                .Select(x => x.Key)
                .ToList();

            var flightId = ShuttleSchedule.BuildFlightId(origin, destination, departureUtc);
            var transitSeconds = GalaxyMap.GetTransitSeconds(origin, destination);
            var boardedAnyone = false;

            foreach (var playerId in ticketHolders)
            {
                // A saved ticket is not permission to board an unannounced flight immediately
                // after login. Only this session's purchase or ticket reminder enables boarding.
                if (!_state.BoardingNotBeforeUtc.TryGetValue(playerId, out var notBefore) || departureUtc < notBefore)
                    continue;

                var player = _passengers.GetOnlinePlayerById(playerId);
                if (!GetIsObjectValid(player) ||
                    GetIsDead(player) ||
                    GetIsDMPossessed(player) ||
                    !IsAtBoardingTerminal(player, origin))
                {
                    // Tell the player once; later departures stay silent until they return to the terminal.
                    if (_state.MissedBoardings.RecordMiss(playerId))
                        NotifyMissedBoarding(player, origin, destination, now);
                    continue;
                }

                var ride = _tickets.GetRide(playerId);
                if (ride == null || ride.Status != ShuttleRideStatus.Ticketed)
                {
                    _state.TicketHolders.Remove(playerId);
                    _state.BoardingNotBeforeUtc.Remove(playerId);
                    _state.MissedBoardings.Clear(playerId);
                    continue;
                }

                var flight = _flights.GetOrCreateFlight(flightId, origin, destination, now, now.AddSeconds(transitSeconds));

                ride.Status = ShuttleRideStatus.InTransit;
                ride.FlightId = flightId;
                ride.DepartureUtc = flight.DepartureUtc;
                ride.ArrivalUtc = flight.ArrivalUtc;
                _db.Set(ride);
                _state.TicketHolders.Remove(playerId);
                _state.BoardingNotBeforeUtc.Remove(playerId);
                _state.MissedBoardings.Clear(playerId);
                boardedAnyone = true;

                SendBoardingJump(player, flight.EntranceLocation);
                _state.Boardings.Start(playerId, flightId, now);

                var destinationName = _planet.GetPlanetByType(destination).Name;
                var transit = Time.GetTimeLongIntervals(TimeSpan.FromSeconds(transitSeconds), false);
                SendMessageToPC(player, $"You board the shuttle to {destinationName}. Estimated flight time: {transit}.");
            }

            if (boardedAnyone)
            {
                Log.Write(LogGroup.Server, $"Shuttle flight {flightId} departed {origin} for {destination}.");
            }
        }

        /// <summary>
        /// Moves a passenger to the shuttle's boarding point.
        /// </summary>
        private static void SendBoardingJump(uint player, Location entrance)
        {
            AssignCommand(player, () =>
            {
                ClearAllActions();
                ActionJumpToLocation(entrance);
            });
        }

        /// <summary>
        /// Checks that every passenger booked onto a departed flight is actually aboard. The boarding
        /// jump is a queued action the engine can drop, which used to leave the passenger standing at
        /// the terminal with an in-transit ride until they relogged. Such passengers are sent again.
        /// </summary>
        public void ConfirmBoardings(DateTime now)
        {
            foreach (var (playerId, flightId, delivering) in _state.Boardings.GetPending())
            {
                if (delivering)
                {
                    ConfirmDelivery(playerId, flightId, now);
                    continue;
                }

                if (!_state.ActiveFlights.TryGetValue(flightId, out var flight))
                {
                    _state.Boardings.Clear(playerId);
                    continue;
                }

                // Arrival resolves the ride from here, including a passenger who never made it aboard.
                if (flight.ArrivalUtc <= now || !GetIsObjectValid(flight.Area))
                    continue;

                var player = _passengers.GetOnlinePlayerById(playerId);
                if (!GetIsObjectValid(player))
                {
                    // Logging back in places an in-transit passenger aboard.
                    _state.Boardings.Clear(playerId);
                    continue;
                }

                var area = GetArea(player);
                if (area == flight.Area)
                {
                    _state.Boardings.Clear(playerId);
                    continue;
                }

                // No area means the passenger is still loading into the shuttle.
                if (!GetIsObjectValid(area) || GetIsDead(player))
                    continue;

                var ride = _tickets.GetRide(playerId);
                if (ride == null || ride.Status != ShuttleRideStatus.InTransit || ride.FlightId != flightId)
                {
                    _state.Boardings.Clear(playerId);
                    continue;
                }

                if (!_state.Boardings.TryStartRetry(playerId, now, out var attempt))
                    continue;

                if (ShouldLogRetry(attempt))
                    Log.Write(LogGroup.Server, $"Shuttle passenger {playerId} was not aboard flight {flightId}; boarding attempt {attempt}. " +
                                               DescribePassenger(player, area));
                SendBoardingJump(player, flight.EntranceLocation);
            }
        }

        /// <summary>
        /// Follows a passenger whose flight landed without them until they stand at the destination's
        /// landing point. The ride is kept until then, so a dropped delivery jump is sent again and a
        /// passenger who logs out is still placed at the destination when they log back in.
        /// </summary>
        private void ConfirmDelivery(string playerId, string flightId, DateTime now)
        {
            var player = _passengers.GetOnlinePlayerById(playerId);
            var ride = _tickets.GetRide(playerId);
            if (!GetIsObjectValid(player) || ride == null || ride.Status != ShuttleRideStatus.InTransit || ride.FlightId != flightId)
            {
                // Logging back in resolves the landed ride at the destination.
                _state.Boardings.Clear(playerId);
                return;
            }

            var landingLocation = _passengers.GetLandingLocation(ride.Destination);
            var area = GetArea(player);
            if (area == GetAreaFromLocation(landingLocation))
            {
                _state.Boardings.Clear(playerId);
                _db.Delete<ShuttleRide>(ride.Id);
                Log.Write(LogGroup.Server, $"Shuttle passenger {playerId} reached {ride.Destination} after flight {flightId} landed without them.");
                return;
            }

            // No area means the passenger is still loading into the destination.
            if (!GetIsObjectValid(area) || !_state.Boardings.TryStartRetry(playerId, now, out var attempt))
                return;

            if (ShouldLogRetry(attempt))
                Log.Write(LogGroup.Server, $"Shuttle passenger {playerId} was not at {ride.Destination} after flight {flightId} landed; delivery attempt {attempt}. " +
                                           DescribePassenger(player, area));
            _passengers.DeliverPassenger(player, landingLocation);
        }

        /// <summary>
        /// Logs the first retry and every tenth after it, so a passenger stuck for a whole flight
        /// does not write a line every few seconds.
        /// </summary>
        private static bool ShouldLogRetry(int attempt)
        {
            return attempt == 2 || attempt % 10 == 0;
        }

        /// <summary>
        /// Describes a passenger's state for the boarding and delivery retry logs.
        /// </summary>
        private static string DescribePassenger(uint player, uint area)
        {
            var position = GetPosition(player);
            return $"Area={GetResRef(area)} Position=({position.X:0.00}, {position.Y:0.00}) Action={GetCurrentAction(player)} " +
                   $"Commandable={GetCommandable(player)} InCombat={GetIsInCombat(player)} Dead={GetIsDead(player)}.";
        }

        /// <summary>
        /// Re-enables boarding calls and missed-shuttle messages for players who missed a departure
        /// and have since come back within range of a flights terminal for their route.
        /// </summary>
        public void RearmReturnedPassengers()
        {
            foreach (var playerId in _state.MissedBoardings.GetMissed())
            {
                if (!_state.TicketHolders.TryGetValue(playerId, out var route))
                {
                    _state.MissedBoardings.Clear(playerId);
                    continue;
                }

                var player = _passengers.GetOnlinePlayerById(playerId);
                if (GetIsObjectValid(player) && !GetIsDead(player) && !GetIsDMPossessed(player) &&
                    IsAtBoardingTerminal(player, route.Origin))
                {
                    _state.MissedBoardings.Clear(playerId);
                    SendLateBoardingCall(player, playerId, route);
                }
            }
        }

        /// <summary>
        /// Sends the boarding call to a re-armed ticket holder when the route's call for the
        /// upcoming departure was already announced before they came back.
        /// </summary>
        private void SendLateBoardingCall(uint player, string playerId, (PlanetType Origin, PlanetType Destination) route)
        {
            var now = _clock.UtcNow();
            var next = ShuttleSchedule.GetNextDepartureUtc(route.Origin, route.Destination, now);
            if ((next - now).TotalSeconds > BoardingCallSeconds ||
                !_state.AnnouncedBoardings.TryGetValue(route, out var announced) || announced != next ||
                !_state.BoardingNotBeforeUtc.TryGetValue(playerId, out var notBefore) || next < notBefore)
                return;

            SendBoardingCall(player, route.Destination, next - now);
        }

        /// <summary>
        /// Sends the boarding call message to one player.
        /// </summary>
        private void SendBoardingCall(uint player, PlanetType destination, TimeSpan untilDeparture)
        {
            var destinationName = _planet.GetPlanetByType(destination).Name;
            var wait = Time.GetTimeLongIntervals(untilDeparture, false);
            SendMessageToPC(player, ColorToken.Cyan($"Now boarding: shuttle to {destinationName} departs in {wait}. Be within {(int)BoardingRangeMeters} meters of the flights terminal!"));
        }

        /// <summary>
        /// Tells a ticket holder they missed the shuttle and when the next one departs.
        /// </summary>
        private void NotifyMissedBoarding(uint player, PlanetType origin, PlanetType destination, DateTime now)
        {
            if (!GetIsObjectValid(player))
                return;

            var next = ShuttleSchedule.GetNextDepartureUtc(origin, destination, now);
            var wait = Time.GetTimeLongIntervals(next - now, false);
            var destinationName = _planet.GetPlanetByType(destination).Name;
            SendMessageToPC(player, ColorToken.Yellow($"You missed your shuttle to {destinationName}! Your ticket remains valid. The next shuttle departs in {wait} - be within {(int)BoardingRangeMeters} meters of the flights terminal. You can refund your ticket at any flights terminal before boarding."));
        }

        /// <summary>
        /// Announces the boarding call to ticket holders shortly before each departure.
        /// </summary>
        public void ProcessBoardingCalls(DateTime now)
        {
            var routes = _state.TicketHolders.Values.Distinct().ToList();
            foreach (var route in routes)
            {
                var next = ShuttleSchedule.GetNextDepartureUtc(route.Origin, route.Destination, now);
                if ((next - now).TotalSeconds > BoardingCallSeconds)
                    continue;

                if (_state.AnnouncedBoardings.TryGetValue(route, out var announced) && announced == next)
                    continue;

                _state.AnnouncedBoardings[route] = next;

                foreach (var (playerId, ticketRoute) in _state.TicketHolders)
                {
                    if (ticketRoute != route)
                        continue;

                    if (!_state.BoardingNotBeforeUtc.TryGetValue(playerId, out var notBefore) || next < notBefore)
                        continue;

                    if (_state.MissedBoardings.HasMissed(playerId))
                        continue;

                    var player = _passengers.GetOnlinePlayerById(playerId);
                    if (!GetIsObjectValid(player))
                        continue;

                    SendBoardingCall(player, route.Destination, next - now);
                }
            }
        }

        /// <summary>
        /// Determines whether the player is within boarding range of a flights terminal for the given origin planet.
        /// </summary>
        public bool IsAtBoardingTerminal(uint player, PlanetType origin)
        {
            var nth = 1;
            var terminal = GetNearestObjectByTag(ShuttleFlightService.TerminalTag, player, nth);
            while (GetIsObjectValid(terminal))
            {
                // Results are ordered by distance - once we pass the boarding range, stop looking.
                if (GetDistanceBetween(player, terminal) > BoardingRangeMeters)
                    return false;

                if ((PlanetType)GetLocalInt(terminal, ShuttleFlightService.TerminalPlanetVariable) == origin)
                    return true;

                nth++;
                terminal = GetNearestObjectByTag(ShuttleFlightService.TerminalTag, player, nth);
            }

            return false;
        }
    }
}
