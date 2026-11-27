using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// Scheduled passenger shuttle flights between planets: the module load, login, logout and area exit handlers and the
    /// login redirect. Tickets, boarding, the flight instances, passenger delivery and arrival are in the narrow shuttle
    /// services. Static callers reach the container's instance through <c>GameServices.Shuttle</c>; the event handlers
    /// are on <see cref="ShuttleService"/>.
    /// </summary>
    public interface IShuttleService
    {
        /// <summary>
        /// When the module loads, recover in-transit flights from the database and
        /// begin processing the flight schedule. Writes nothing; a hot reload runs it again.
        /// </summary>
        void LoadShuttleFlights();

        /// <summary>
        /// At boot, right after <see cref="LoadShuttleFlights"/>: delivers the passengers whose flight landed while the
        /// server was down to their destination on their offline record.
        /// </summary>
        void DeliverOfflinePassengers();

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
    }
}
