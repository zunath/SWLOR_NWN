using SWLOR.Shared.Domain.Entities;

namespace SWLOR.Shared.Domain.Repositories
{
    /// <summary>
    /// Repository interface for PlayerShip entity operations.
    /// </summary>
    public interface IPlayerShipRepository
    {
        /// <summary>
        /// Gets a player ship by its unique identifier.
        /// </summary>
        /// <param name="id">The player ship's unique identifier</param>
        /// <returns>The player ship if found, null otherwise</returns>
        PlayerShip GetById(string id);

        /// <summary>
        /// Gets all player ships by owner player ID.
        /// </summary>
        /// <param name="ownerPlayerId">The owner player ID to search for</param>
        /// <returns>Collection of player ships owned by the specified player</returns>
        IEnumerable<PlayerShip> GetByOwnerPlayerId(string ownerPlayerId);

        /// <summary>
        /// Gets all player ships by property ID.
        /// </summary>
        /// <param name="propertyId">The property ID to search for</param>
        /// <returns>Collection of player ships associated with the specified property</returns>
        IEnumerable<PlayerShip> GetByPropertyId(string propertyId);

        /// <summary>
        /// Gets all player ships by multiple property IDs.
        /// </summary>
        /// <param name="propertyIds">The property IDs to search for</param>
        /// <returns>Collection of player ships associated with the specified properties</returns>
        IEnumerable<PlayerShip> GetByPropertyIds(IEnumerable<string> propertyIds);

        /// <summary>
        /// Gets all player ships excluding those owned by a specific player.
        /// </summary>
        /// <param name="propertyIds">The property IDs to search for</param>
        /// <param name="excludePlayerId">The player ID to exclude from results</param>
        /// <returns>Collection of player ships not owned by the specified player</returns>
        IEnumerable<PlayerShip> GetByPropertyIdsExcludingPlayer(IEnumerable<string> propertyIds, string excludePlayerId);

        /// <summary>
        /// Saves a player ship entity.
        /// </summary>
        /// <param name="playerShip">The player ship to save</param>
        void Save(PlayerShip playerShip);

        /// <summary>
        /// Deletes a player ship by its unique identifier.
        /// </summary>
        /// <param name="id">The player ship's unique identifier</param>
        void Delete(string id);

        /// <summary>
        /// Checks if a player ship exists by its unique identifier.
        /// </summary>
        /// <param name="id">The player ship's unique identifier</param>
        /// <returns>True if the player ship exists, false otherwise</returns>
        bool Exists(string id);
    }
}
