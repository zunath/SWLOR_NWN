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
    /// The shuttle tickets: a player's ride record, buying and refunding a ticket, and the next departure of a route. The
    /// logic moved here unchanged from <see cref="ShuttleService"/>.
    /// </summary>
    public class ShuttleTicketService : IShuttleTicketService
    {
        private readonly IDBService _db;
        private readonly IShuttleStateService _state;
        private readonly IShuttleClockService _clock;

        public ShuttleTicketService(IDBService db, IShuttleStateService state, IShuttleClockService clock)
        {
            _db = db;
            _state = state;
            _clock = clock;
        }

        /// <summary>
        /// Retrieves a player's active shuttle ride, or null if they have none.
        /// </summary>
        /// <param name="playerId">The player Id to look up.</param>
        /// <returns>The player's shuttle ride, or null.</returns>
        public ShuttleRide GetRide(string playerId)
        {
            var query = new DBQuery<ShuttleRide>()
                .AddFieldSearch(nameof(ShuttleRide.PlayerId), playerId, false);
            return _db.Search(query).FirstOrDefault();
        }

        /// <summary>
        /// Returns all shuttle rides currently in the given status.
        /// </summary>
        public List<ShuttleRide> SearchRidesByStatus(ShuttleRideStatus status)
        {
            var query = new DBQuery<ShuttleRide>()
                .AddFieldSearch(nameof(ShuttleRide.Status), (int)status);
            var count = (int)_db.SearchCount(query);
            return _db.Search(query.AddPaging(count, 0)).ToList();
        }

        /// <summary>
        /// Returns all shuttle rides booked on the given flight.
        /// </summary>
        public List<ShuttleRide> SearchRidesByFlightId(string flightId)
        {
            var query = new DBQuery<ShuttleRide>()
                .AddFieldSearch(nameof(ShuttleRide.FlightId), flightId, false);
            var count = (int)_db.SearchCount(query);
            return _db.Search(query.AddPaging(count, 0)).ToList();
        }

        /// <summary>
        /// Attempts to purchase a shuttle ticket for the given route.
        /// Charges the fare plus city transportation tax and deposits the tax into the city treasury.
        /// </summary>
        /// <param name="player">The player purchasing the ticket.</param>
        /// <param name="origin">The origin planet.</param>
        /// <param name="destination">The destination planet.</param>
        /// <param name="cityPropertyId">The city property Id receiving the transportation tax, if any.</param>
        /// <param name="taxRate">The transportation tax rate (0.0 - 0.25).</param>
        /// <returns>true if the ticket was purchased, false otherwise.</returns>
        public bool TryPurchaseTicket(uint player, PlanetType origin, PlanetType destination, string cityPropertyId, float taxRate)
        {
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return false;

            if (origin == PlanetType.Invalid ||
                destination == PlanetType.Invalid ||
                origin == destination)
                return false;

            var playerId = GetObjectUUID(player);
            var existingRide = GetRide(playerId);
            if (existingRide != null)
            {
                SendMessageToPC(player, ColorToken.Red("You already have a shuttle ticket."));
                return false;
            }

            var fare = GalaxyMap.GetFare(origin, destination);
            var tax = (int)(taxRate * fare);
            var price = fare + tax;

            if (GetGold(player) < price)
            {
                SendMessageToPC(player, ColorToken.Red("You do not have enough credits to purchase this flight!"));
                return false;
            }

            TakeGoldFromCreature(price, player, true);

            var ride = new ShuttleRide(playerId)
            {
                Status = ShuttleRideStatus.Ticketed,
                Origin = origin,
                Destination = destination,
                FarePaid = fare,
                TaxPaid = tax
            };
            _db.Set(ride);
            _state.TicketHolders[playerId] = (origin, destination);
            _state.BoardingNotBeforeUtc[playerId] = _clock.UtcNow();
            _state.MissedBoardings.Clear(playerId);

            if (tax > 0 && !string.IsNullOrWhiteSpace(cityPropertyId))
            {
                var dbCity = _db.Get<WorldProperty>(cityPropertyId);
                if (dbCity != null)
                {
                    dbCity.Treasury += tax;
                    _db.Set(dbCity);
                    Log.Write(LogGroup.Property, $"{GetName(player)} paid {tax} credits in tax for their trip to {destination}.");
                }
            }

            return true;
        }

        /// <summary>
        /// Refunds a player's unboarded shuttle ticket. Only the fare is returned;
        /// taxes stay with the city treasury.
        /// </summary>
        /// <param name="player">The player refunding their ticket.</param>
        /// <returns>true if the ticket was refunded, false otherwise.</returns>
        public bool RefundTicket(uint player)
        {
            var playerId = GetObjectUUID(player);
            var ride = GetRide(playerId);
            if (ride == null || ride.Status != ShuttleRideStatus.Ticketed)
                return false;

            GiveGoldToCreature(player, ride.FarePaid);
            _db.Delete<ShuttleRide>(ride.Id);
            _state.TicketHolders.Remove(playerId);
            _state.BoardingNotBeforeUtc.Remove(playerId);
            _state.MissedBoardings.Clear(playerId);

            SendMessageToPC(player, $"Your shuttle ticket has been refunded for {ride.FarePaid} credits. Taxes are non-refundable.");
            return true;
        }

        /// <summary>
        /// Retrieves the next scheduled departure for a route.
        /// </summary>
        /// <param name="origin">The origin planet.</param>
        /// <param name="destination">The destination planet.</param>
        /// <param name="playerId">Optional passenger whose login boarding grace period applies.</param>
        /// <returns>The UTC time of the next departure.</returns>
        public DateTime GetNextDepartureUtc(PlanetType origin, PlanetType destination, string playerId = null)
        {
            var notBefore = default(DateTime);
            if (!string.IsNullOrWhiteSpace(playerId))
                _state.BoardingNotBeforeUtc.TryGetValue(playerId, out notBefore);

            return ShuttleSchedule.GetNextDepartureUtc(origin, destination, _clock.UtcNow(), notBefore);
        }
    }
}
