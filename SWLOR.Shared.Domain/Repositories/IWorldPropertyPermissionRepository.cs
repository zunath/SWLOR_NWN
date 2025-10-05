using SWLOR.Shared.Domain.Properties.Entities;

namespace SWLOR.Shared.Domain.Repositories
{
    /// <summary>
    /// Repository interface for WorldPropertyPermission entity operations.
    /// </summary>
    public interface IWorldPropertyPermissionRepository
    {
        /// <summary>
        /// Gets a world property permission by its unique identifier.
        /// </summary>
        /// <param name="id">The world property permission's unique identifier</param>
        /// <returns>The world property permission if found, null otherwise</returns>
        WorldPropertyPermission GetById(string id);

        /// <summary>
        /// Gets all world property permissions by property ID.
        /// </summary>
        /// <param name="propertyId">The property ID to search for</param>
        /// <returns>Collection of world property permissions for the specified property</returns>
        IEnumerable<WorldPropertyPermission> GetByPropertyId(string propertyId);

        /// <summary>
        /// Gets all world property permissions by player ID.
        /// </summary>
        /// <param name="playerId">The player ID to search for</param>
        /// <returns>Collection of world property permissions for the specified player</returns>
        IEnumerable<WorldPropertyPermission> GetByPlayerId(string playerId);

        /// <summary>
        /// Gets all world property permissions by property ID and player ID.
        /// </summary>
        /// <param name="propertyId">The property ID to search for</param>
        /// <param name="playerId">The player ID to search for</param>
        /// <returns>Collection of world property permissions for the specified property and player</returns>
        IEnumerable<WorldPropertyPermission> GetByPropertyIdAndPlayerId(string propertyId, string playerId);

        /// <summary>
        /// Gets a single world property permission by property ID and player ID.
        /// </summary>
        /// <param name="propertyId">The property ID to search for</param>
        /// <param name="playerId">The player ID to search for</param>
        /// <returns>The world property permission if found, null otherwise</returns>
        WorldPropertyPermission GetSingleByPropertyIdAndPlayerId(string propertyId, string playerId);

        /// <summary>
        /// Gets all world property permissions by multiple property IDs.
        /// </summary>
        /// <param name="propertyIds">The property IDs to search for</param>
        /// <returns>Collection of world property permissions for the specified properties</returns>
        IEnumerable<WorldPropertyPermission> GetByPropertyIds(IEnumerable<string> propertyIds);

        /// <summary>
        /// Gets all world property permissions by multiple player IDs.
        /// </summary>
        /// <param name="playerIds">The player IDs to search for</param>
        /// <returns>Collection of world property permissions for the specified players</returns>
        IEnumerable<WorldPropertyPermission> GetByPlayerIds(IEnumerable<string> playerIds);

        /// <summary>
        /// Gets all world property permissions by player ID and multiple property IDs.
        /// </summary>
        /// <param name="playerId">The player ID to search for</param>
        /// <param name="propertyIds">The property IDs to search for</param>
        /// <returns>Collection of world property permissions for the specified player and properties</returns>
        IEnumerable<WorldPropertyPermission> GetByPlayerIdAndPropertyIds(string playerId, IEnumerable<string> propertyIds);

        /// <summary>
        /// Saves a world property permission entity.
        /// </summary>
        /// <param name="worldPropertyPermission">The world property permission to save</param>
        void Save(WorldPropertyPermission worldPropertyPermission);

        /// <summary>
        /// Deletes a world property permission by its unique identifier.
        /// </summary>
        /// <param name="id">The world property permission's unique identifier</param>
        void Delete(string id);

        /// <summary>
        /// Checks if a world property permission exists by its unique identifier.
        /// </summary>
        /// <param name="id">The world property permission's unique identifier</param>
        /// <returns>True if the world property permission exists, false otherwise</returns>
        bool Exists(string id);
    }
}
