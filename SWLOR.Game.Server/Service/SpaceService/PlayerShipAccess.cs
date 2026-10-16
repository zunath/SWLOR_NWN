using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Loads the complete ship lists shown in ship management.
    /// </summary>
    public static class PlayerShipAccess
    {
        /// <summary>
        /// Retrieves every ship registered to a player.
        /// </summary>
        /// <param name="playerId">The player's Id.</param>
        /// <returns>All ships owned by the player.</returns>
        public static List<PlayerShip> GetOwnedShips(string playerId)
        {
            var query = new DBQuery<PlayerShip>()
                .AddFieldSearch(nameof(PlayerShip.OwnerPlayerId), playerId, false);

            return SearchAll(query);
        }

        /// <summary>
        /// Retrieves every ship owned by someone else on which a player holds a permission record.
        /// </summary>
        /// <param name="playerId">The player's Id.</param>
        /// <returns>All ships shared with the player.</returns>
        public static List<PlayerShip> GetSharedShips(string playerId)
        {
            var permissionQuery = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false);
            var propertyIds = SearchAll(permissionQuery)
                .Select(permission => permission.PropertyId)
                .Where(propertyId => !string.IsNullOrWhiteSpace(propertyId))
                .Distinct()
                .ToList();

            if (propertyIds.Count <= 0)
                return new List<PlayerShip>();

            var shipQuery = new DBQuery<PlayerShip>()
                .AddFieldSearch(nameof(PlayerShip.PropertyId), propertyIds);

            // Owned ships are removed after the full set loads so they cannot use up the shared ships' rows.
            return SearchAll(shipQuery)
                .Where(ship => ship.OwnerPlayerId != playerId)
                .ToList();
        }

        /// <summary>
        /// Runs a query with a page sized to its full match count. Unpaged searches stop at 50 rows.
        /// </summary>
        private static List<T> SearchAll<T>(DBQuery<T> query)
            where T : EntityBase
        {
            var count = (int)DB.SearchCount(query);
            if (count <= 0)
                return new List<T>();

            return DB.Search(query.AddPaging(count, 0))
                .Where(entity => entity != null)
                .ToList();
        }
    }
}
