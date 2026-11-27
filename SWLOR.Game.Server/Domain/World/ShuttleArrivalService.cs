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
    /// Arrival: delivers the passengers of each flight whose arrival time has passed, tears down its shuttle instance, and has
    /// the status consoles announce the time left. The logic moved here unchanged from <see cref="ShuttleService"/>.
    /// </summary>
    public class ShuttleArrivalService : IShuttleArrivalService
    {
        private readonly IDBService _db;
        private readonly IPlanetService _planet;
        private readonly IShuttleStateService _state;
        private readonly IShuttleClockService _clock;
        private readonly IShuttleTicketService _tickets;
        private readonly IShuttleFlightService _flights;
        private readonly IShuttlePassengerService _passengers;

        public ShuttleArrivalService(IDBService db, IPlanetService planet, IShuttleStateService state, IShuttleClockService clock,
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
        /// Processes every flight whose arrival time has passed.
        /// </summary>
        public void ProcessArrivals(DateTime now)
        {
            var arrived = _state.ActiveFlights.Values
                .Where(x => x.ArrivalUtc <= now)
                .ToList();

            foreach (var flight in arrived)
            {
                ProcessArrival(flight, now);
            }
        }

        /// <summary>
        /// Delivers a flight's passengers to the destination and tears down the shuttle instance.
        /// </summary>
        private void ProcessArrival(ShuttleActiveFlight flight, DateTime now)
        {
            var landingLocation = _passengers.GetLandingLocation(flight.Destination);
            var destinationName = _planet.GetPlanetByType(flight.Destination).Name;

            // Resolve every ride record on this flight. Delivery is decided from the passenger's
            // actual current area (engine truth), not the area's cached player list, which is not
            // reliably populated for runtime instances.
            foreach (var ride in _tickets.SearchRidesByFlightId(flight.FlightId))
            {
                var player = _passengers.GetOnlinePlayerById(ride.PlayerId);
                if (!GetIsObjectValid(player))
                {
                    _passengers.DeliverOfflinePassenger(ride);
                    continue;
                }

                // A passenger already being delivered: this flight landed before, and a hot reload rebuilt it from the
                // ride kept for them. The delivery retries own the ride until they stand at the destination.
                if (_state.Boardings.IsDelivering(ride.PlayerId))
                    continue;

                if (GetIsObjectValid(flight.Area) &&
                    GetArea(player) != flight.Area &&
                    _state.Boardings.IsBoarding(ride.PlayerId, flight.FlightId))
                {
                    // The passenger never made it aboard despite their ticket. Deliver them, and keep
                    // the ride until they are actually at the destination in case this jump is dropped too.
                    _passengers.DeliverPassenger(player, landingLocation);
                    SendMessageToPC(player, $"The shuttle touches down at {destinationName}.");
                    _state.Boardings.StartDelivery(ride.PlayerId, flight.FlightId, now);
                    continue;
                }

                if (!GetIsObjectValid(flight.Area) || GetArea(player) == flight.Area)
                {
                    // Still aboard the shuttle (or the instance is gone after a restart) - deliver them.
                    _passengers.DeliverPassenger(player, landingLocation);
                    SendMessageToPC(player, $"The shuttle touches down at {destinationName}.");
                }
                else
                {
                    // The player left the flight some other way (death respawn, DM port).
                    // They forfeit the trip - do not teleport them.
                    Log.Write(LogGroup.Server, $"Shuttle passenger {ride.PlayerId} abandoned flight {flight.FlightId} before arrival.");
                }

                _state.Boardings.Clear(ride.PlayerId);
                _db.Delete<ShuttleRide>(ride.Id);
            }

            // Safety net: deliver anyone physically aboard without a ride record, so no player is
            // trapped when the instance is torn down.
            _passengers.DeliverPlayersInArea(flight.Area, landingLocation);

            _state.ActiveFlights.Remove(flight.FlightId);

            if (GetIsObjectValid(flight.Area))
            {
                var area = flight.Area;
                _clock.Schedule(() => _flights.DestroyFlightInstance(area, landingLocation), TimeSpan.FromSeconds(30));
            }

            Log.Write(LogGroup.Server, $"Shuttle flight {flight.FlightId} arrived at {flight.Destination}.");
        }

        /// <summary>
        /// Has each in-flight shuttle console announce the remaining time to its passengers.
        /// </summary>
        public void ProcessTransitBroadcasts(DateTime now)
        {
            foreach (var flight in _state.ActiveFlights.Values)
            {
                if (!GetIsObjectValid(flight.Console))
                    continue;

                if ((now - flight.LastBroadcastUtc).TotalSeconds < 60)
                    continue;

                flight.LastBroadcastUtc = now;

                var destinationName = _planet.GetPlanetByType(flight.Destination).Name;
                var remaining = Time.GetTimeLongIntervals(flight.ArrivalUtc - now, false);

                // The status console announces the ETA to nearby passengers at talk volume.
                var console = flight.Console;
                AssignCommand(console, () => SpeakString($"Now arriving at {destinationName} in {remaining}."));
            }
        }
    }
}
