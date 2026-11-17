using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// Finding a passenger among the connected players and placing passengers where a landed flight sends them.
    /// </summary>
    public interface IShuttlePassengerService
    {
        /// <summary>Returns the online player with the given id, or OBJECT_INVALID if none is connected.</summary>
        uint GetOnlinePlayerById(string playerId);

        /// <summary>Returns the landing waypoint location for a destination planet.</summary>
        Location GetLandingLocation(PlanetType destination);

        /// <summary>
        /// Delivers every player physically present in the given area to the landing location.
        /// Iterates the area's objects directly rather than relying on the cached player list,
        /// which is not reliably populated for runtime instances.
        /// </summary>
        void DeliverPlayersInArea(uint area, Location landingLocation);

        /// <summary>Jumps a passenger to the landing location (a dead one is jumped without clearing actions).</summary>
        void DeliverPassenger(uint player, Location landingLocation);

        /// <summary>
        /// Records an offline passenger's landing location. Retains the elapsed ride until
        /// login so the passenger receives their destination notice before it is cleared.
        /// </summary>
        void DeliverOfflinePassenger(ShuttleRide ride);
    }
}
