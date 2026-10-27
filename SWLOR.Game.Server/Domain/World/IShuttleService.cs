using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// Scheduled passenger shuttle flights between planets: tickets, boarding, the shuttle interior instance and
    /// arrival. The static facade <c>Service.Shuttle</c> forwards to the container's instance of this interface and
    /// keeps the event handlers.
    /// </summary>
    public interface IShuttleService
    {
        /// <summary>
        /// When the module loads, recover in-transit flights from the database and
        /// begin processing the flight schedule.
        /// </summary>
        void LoadShuttleFlights();

        /// <summary>
        /// Retrieves a player's active shuttle ride, or null if they have none.
        /// </summary>
        /// <param name="playerId">The player Id to look up.</param>
        /// <returns>The player's shuttle ride, or null.</returns>
        ShuttleRide GetRide(string playerId);

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
        /// Determines where a logging-in player with an in-transit shuttle ride should be placed.
        /// Returns the destination landing pad if the flight has already arrived (and resolves the ride),
        /// or the shuttle interior if the flight is still in the air.
        /// </summary>
        /// <param name="player">The player logging in.</param>
        /// <param name="location">The location the player should be moved to.</param>
        /// <returns>true if the player must be redirected, false otherwise.</returns>
        bool TryGetLoginRedirect(uint player, out Location location);

        /// <summary>
        /// When a player leaves the server, their session boarding eligibility ends.
        /// </summary>
        void ExitServer();

        /// <summary>
        /// When a player enters the server, verify in-transit passengers actually ended up
        /// aboard their shuttle. This is a backstop for cases where the game placed them
        /// into a stale instance or an unexpected location.
        /// </summary>
        void EnterServer();

        /// <summary>
        /// When a passenger leaves the shuttle interior before it lands - by any means other than
        /// arrival, such as a DM port, a death respawn, or a recall - they forfeit the trip.
        /// The ride record is deleted so they are not re-boarded on login or delivered to the
        /// destination while offline. A normal logout does not fire this event, so passengers who
        /// stay aboard and disconnect still resume or arrive as intended.
        /// </summary>
        void HandleShuttleAreaExit();

        /// <summary>
        /// Retrieves the next scheduled departure for a route.
        /// </summary>
        /// <param name="origin">The origin planet.</param>
        /// <param name="destination">The destination planet.</param>
        /// <param name="playerId">Optional passenger whose login boarding grace period applies.</param>
        /// <returns>The UTC time of the next departure.</returns>
        DateTime GetNextDepartureUtc(PlanetType origin, PlanetType destination, string playerId = null);

        /// <summary>
        /// Retrieves the flight registered to a shuttle status console or shuttle area, if any.
        /// </summary>
        /// <param name="obj">The console placeable or area to check.</param>
        /// <param name="destination">The flight's destination planet.</param>
        /// <param name="arrivalUtc">The flight's UTC arrival time.</param>
        /// <returns>true if a flight was found, false otherwise.</returns>
        bool TryGetFlightInfo(uint obj, out PlanetType destination, out DateTime arrivalUtc);
    }
}
