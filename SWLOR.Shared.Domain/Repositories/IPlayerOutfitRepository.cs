using SWLOR.Shared.Domain.Entities;

namespace SWLOR.Shared.Domain.Repositories
{
    /// <summary>
    /// Repository interface for PlayerOutfit entity operations.
    /// </summary>
    public interface IPlayerOutfitRepository
    {
        /// <summary>
        /// Gets a player outfit by its unique identifier.
        /// </summary>
        /// <param name="id">The player outfit's unique identifier</param>
        /// <returns>The player outfit if found, null otherwise</returns>
        PlayerOutfit GetById(string id);

        /// <summary>
        /// Gets all player outfits by player ID.
        /// </summary>
        /// <param name="playerId">The player ID to search for</param>
        /// <returns>Collection of player outfits for the specified player</returns>
        IEnumerable<PlayerOutfit> GetByPlayerId(string playerId);

        /// <summary>
        /// Gets all player outfits by name.
        /// </summary>
        /// <param name="name">The name to search for</param>
        /// <returns>Collection of player outfits with the specified name</returns>
        IEnumerable<PlayerOutfit> GetByName(string name);

        /// <summary>
        /// Gets all player outfits.
        /// </summary>
        /// <returns>Collection of all player outfits</returns>
        IEnumerable<PlayerOutfit> GetAll();

        /// <summary>
        /// Gets the count of player outfits by player ID.
        /// </summary>
        /// <param name="playerId">The player ID to count</param>
        /// <returns>The count of player outfits for the specified player</returns>
        long GetCountByPlayerId(string playerId);

        /// <summary>
        /// Saves a player outfit entity.
        /// </summary>
        /// <param name="playerOutfit">The player outfit to save</param>
        void Save(PlayerOutfit playerOutfit);

        /// <summary>
        /// Deletes a player outfit by its unique identifier.
        /// </summary>
        /// <param name="id">The player outfit's unique identifier</param>
        void Delete(string id);

        /// <summary>
        /// Checks if a player outfit exists by its unique identifier.
        /// </summary>
        /// <param name="id">The player outfit's unique identifier</param>
        /// <returns>True if the player outfit exists, false otherwise</returns>
        bool Exists(string id);
    }
}
