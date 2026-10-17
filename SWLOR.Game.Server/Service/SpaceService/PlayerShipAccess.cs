using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Loads the ship lists shown in ship management.
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
        /// Retrieves the property Ids a player holds permission records on, minus the properties of ships they own.
        /// Load this once and page through it with <see cref="GetSharedShipsPage"/>.
        /// </summary>
        /// <param name="playerId">The player's Id.</param>
        /// <returns>The candidate property Ids of ships shared with the player.</returns>
        public static List<string> GetSharedPropertyIds(string playerId)
        {
            var permissionQuery = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false);
            var propertyIds = SearchAll(permissionQuery)
                .Select(permission => permission.PropertyId)
                .Where(propertyId => !string.IsNullOrWhiteSpace(propertyId))
                .ToHashSet();

            if (propertyIds.Count <= 0)
                return new List<string>();

            // Owners hold a permission record on their own ships; those belong to My Ships.
            foreach (var ship in GetOwnedShips(playerId))
                propertyIds.Remove(ship.PropertyId);

            return propertyIds.ToList();
        }

        /// <summary>
        /// Retrieves one page of the shared ships behind <paramref name="sharedPropertyIds"/>, sorted by name.
        /// Only starships that exist and are not queued for deletion are counted.
        /// </summary>
        /// <param name="sharedPropertyIds">The Ids from <see cref="GetSharedPropertyIds"/>.</param>
        /// <param name="pageIndex">The zero-based page. A page past the end returns the last page.</param>
        /// <param name="pageSize">The number of ships per page.</param>
        /// <param name="totalCount">The number of shared ships across all pages.</param>
        /// <returns>The page's ships with their properties.</returns>
        public static List<(PlayerShip Ship, WorldProperty Property)> GetSharedShipsPage(
            IReadOnlyCollection<string> sharedPropertyIds,
            int pageIndex,
            int pageSize,
            out int totalCount)
        {
            totalCount = 0;
            var page = new List<(PlayerShip Ship, WorldProperty Property)>();
            if (sharedPropertyIds.Count <= 0)
                return page;

            var propertyQuery = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.Id), sharedPropertyIds)
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.Starship)
                .AddFieldSearch(nameof(WorldProperty.IsQueuedForDeletion), false)
                .OrderBy(nameof(WorldProperty.CustomName));
            totalCount = (int)DB.SearchCount(propertyQuery);
            if (totalCount <= 0)
                return page;

            var lastPage = (totalCount - 1) / pageSize;
            pageIndex = Math.Clamp(pageIndex, 0, lastPage);
            var properties = DB.Search(propertyQuery.AddPaging(pageSize, pageIndex * pageSize))
                .Where(property => property != null &&
                                   property.PropertyType == PropertyType.Starship &&
                                   sharedPropertyIds.Contains(property.Id))
                .ToList();
            if (properties.Count <= 0)
                return page;

            var shipQuery = new DBQuery<PlayerShip>()
                .AddFieldSearch(nameof(PlayerShip.PropertyId), properties.Select(property => property.Id));
            var shipsByPropertyId = SearchAll(shipQuery)
                .GroupBy(ship => ship.PropertyId)
                .ToDictionary(group => group.Key, group => group.First());

            foreach (var property in properties)
            {
                if (shipsByPropertyId.TryGetValue(property.Id, out var ship))
                    page.Add((ship, property));
            }

            return page;
        }

        /// <summary>
        /// Deletes a player's permission records on a ship someone else owns, including the ship's storage
        /// categories, so the ship leaves their shared list. Owners keep their permissions.
        /// </summary>
        /// <param name="playerId">The player's Id.</param>
        /// <param name="ship">The ship to leave.</param>
        /// <returns>The number of permission records deleted.</returns>
        public static int RemoveSharedAccess(string playerId, PlayerShip ship)
        {
            if (ship == null || ship.OwnerPlayerId == playerId)
                return 0;

            var categoryQuery = new DBQuery<WorldPropertyCategory>()
                .AddFieldSearch(nameof(WorldPropertyCategory.ParentPropertyId), ship.PropertyId, false);
            var propertyIds = SearchAll(categoryQuery)
                .Select(category => category.Id)
                .ToHashSet();
            propertyIds.Add(ship.PropertyId);

            var permissionQuery = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false);
            var permissions = SearchAll(permissionQuery)
                .Where(permission => permission.PlayerId == playerId && propertyIds.Contains(permission.PropertyId))
                .ToList();

            foreach (var permission in permissions)
            {
                DB.Delete<WorldPropertyPermission>(permission.Id);
            }

            Log.Write(LogGroup.Property, $"Player '{playerId}' removed their access to starship property '{ship.PropertyId}' ({permissions.Count} permission records).");
            return permissions.Count;
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
