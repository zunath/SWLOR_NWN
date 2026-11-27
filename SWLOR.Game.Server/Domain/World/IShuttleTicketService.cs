using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// Shuttle tickets: a player's ride record, buying and refunding a ticket, and the next departure of a route. Static
    /// callers reach the container's instance through <c>GameServices.ShuttleTicket</c>.
    /// </summary>
    public interface IShuttleTicketService
    {
        /// <summary>
        /// Retrieves a player's active shuttle ride, or null if they have none.
        /// </summary>
        /// <param name="playerId">The player Id to look up.</param>
        /// <returns>The player's shuttle ride, or null.</returns>
        ShuttleRide GetRide(string playerId);

        /// <summary>Returns all shuttle rides currently in the given status.</summary>
        List<ShuttleRide> SearchRidesByStatus(ShuttleRideStatus status);

        /// <summary>Returns all shuttle rides booked on the given flight.</summary>
        List<ShuttleRide> SearchRidesByFlightId(string flightId);

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
        bool TryPurchaseTicket(uint player, PlanetType origin, PlanetType destination, string cityPropertyId, float taxRate);

        /// <summary>
        /// Refunds a player's unboarded shuttle ticket. Only the fare is returned;
        /// taxes stay with the city treasury.
        /// </summary>
        /// <param name="player">The player refunding their ticket.</param>
        /// <returns>true if the ticket was refunded, false otherwise.</returns>
        bool RefundTicket(uint player);

        /// <summary>
        /// Retrieves the next scheduled departure for a route.
        /// </summary>
        /// <param name="origin">The origin planet.</param>
        /// <param name="destination">The destination planet.</param>
        /// <param name="playerId">Optional passenger whose login boarding grace period applies.</param>
        /// <returns>The UTC time of the next departure.</returns>
        DateTime GetNextDepartureUtc(PlanetType origin, PlanetType destination, string playerId = null);
    }
}
