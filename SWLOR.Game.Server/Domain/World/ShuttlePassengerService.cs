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
    /// Finds a passenger among the connected players and puts passengers where a landed flight sends them: the landing
    /// point of the destination, or the player record of one who is offline. The logic moved here unchanged from
    /// <see cref="ShuttleService"/>.
    /// </summary>
    public class ShuttlePassengerService : IShuttlePassengerService
    {
        private readonly IDBService _db;
        private readonly IPlanetService _planet;

        public ShuttlePassengerService(IDBService db, IPlanetService planet)
        {
            _db = db;
            _planet = planet;
        }

        /// <summary>
        /// Returns the online player with the given id, or OBJECT_INVALID if none is connected.
        /// </summary>
        public uint GetOnlinePlayerById(string playerId)
        {
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (GetObjectUUID(player) == playerId)
                    return player;
            }

            return OBJECT_INVALID;
        }

        /// <summary>
        /// Returns the landing waypoint location for a destination planet.
        /// </summary>
        public Location GetLandingLocation(PlanetType destination)
        {
            var waypoint = GetWaypointByTag(_planet.GetPlanetByType(destination).LandingWaypointTag);
            return GetLocation(waypoint);
        }

        /// <summary>
        /// Delivers every player physically present in the given area to the landing location.
        /// Iterates the area's objects directly rather than relying on the cached player list,
        /// which is not reliably populated for runtime instances.
        /// </summary>
        public void DeliverPlayersInArea(uint area, Location landingLocation)
        {
            if (!GetIsObjectValid(area))
                return;

            for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
            {
                if (GetIsPC(obj))
                    DeliverPassenger(obj, landingLocation);
            }
        }

        public void DeliverPassenger(uint player, Location landingLocation)
        {
            if (GetIsDM(player) || GetIsDMPossessed(player))
                return;

            if (GetIsDead(player))
            {
                AssignCommand(player, () => JumpToLocation(landingLocation));
            }
            else
            {
                AssignCommand(player, () =>
                {
                    ClearAllActions();
                    ActionJumpToLocation(landingLocation);
                });
            }
        }

        /// <summary>
        /// Records an offline passenger's landing location. Retains the elapsed ride until
        /// login so the passenger receives their destination notice before it is cleared.
        /// </summary>
        public void DeliverOfflinePassenger(ShuttleRide ride)
        {
            var dbPlayer = _db.Get<Player>(ride.PlayerId);
            if (dbPlayer != null)
            {
                var waypoint = GetWaypointByTag(_planet.GetPlanetByType(ride.Destination).LandingWaypointTag);
                if (GetIsObjectValid(waypoint))
                {
                    var position = GetPosition(waypoint);
                    dbPlayer.LocationAreaResref = GetResRef(GetArea(waypoint));
                    dbPlayer.LocationX = position.X;
                    dbPlayer.LocationY = position.Y;
                    dbPlayer.LocationZ = position.Z;
                    dbPlayer.LocationOrientation = GetFacing(waypoint);
                    _db.Set(dbPlayer);
                }
            }

            Log.Write(LogGroup.Server, $"Shuttle delivered offline passenger {ride.PlayerId} to {ride.Destination}.");
        }
    }
}
