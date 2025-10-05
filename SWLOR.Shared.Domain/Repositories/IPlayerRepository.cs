using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Character.Enums;

namespace SWLOR.Shared.Domain.Repositories
{
    /// <summary>
    /// Repository interface for Player entity operations.
    /// </summary>
    public interface IPlayerRepository
    {
        /// <summary>
        /// Gets a player by their unique identifier.
        /// </summary>
        /// <param name="id">The player's unique identifier</param>
        /// <returns>The player if found, null otherwise</returns>
        Player GetById(string id);

        /// <summary>
        /// Gets all players by area resref.
        /// </summary>
        /// <param name="areaResref">The area resref to search for</param>
        /// <returns>Collection of players in the specified area</returns>
        IEnumerable<Player> GetByAreaResref(string areaResref);

        /// <summary>
        /// Gets all players by character type.
        /// </summary>
        /// <param name="characterType">The character type to search for</param>
        /// <returns>Collection of players with the specified character type</returns>
        IEnumerable<Player> GetByCharacterType(CharacterType characterType);

        /// <summary>
        /// Gets all players by citizen property ID.
        /// </summary>
        /// <param name="citizenPropertyId">The citizen property ID to search for</param>
        /// <returns>Collection of players with the specified citizen property</returns>
        IEnumerable<Player> GetByCitizenPropertyId(string citizenPropertyId);

        /// <summary>
        /// Gets all active (non-deleted) players by citizen property ID.
        /// </summary>
        /// <param name="citizenPropertyId">The citizen property ID to search for</param>
        /// <returns>Collection of active players with the specified citizen property</returns>
        IEnumerable<Player> GetActiveCitizensByPropertyId(string citizenPropertyId);

        /// <summary>
        /// Gets all players that are not deleted.
        /// </summary>
        /// <returns>Collection of active players</returns>
        IEnumerable<Player> GetActivePlayers();

        /// <summary>
        /// Gets all players.
        /// </summary>
        /// <returns>Collection of all players</returns>
        IEnumerable<Player> GetAll();

        /// <summary>
        /// Gets the count of all players.
        /// </summary>
        /// <returns>The count of all players</returns>
        long GetCount();

        /// <summary>
        /// Gets players with optional search text.
        /// </summary>
        /// <param name="searchText">Optional search text to filter by name</param>
        /// <returns>Collection of players matching the search criteria</returns>
        IEnumerable<Player> GetPlayersWithSearch(string searchText = null);

        /// <summary>
        /// Saves a player entity.
        /// </summary>
        /// <param name="player">The player to save</param>
        void Save(Player player);

        /// <summary>
        /// Deletes a player by their unique identifier.
        /// </summary>
        /// <param name="id">The player's unique identifier</param>
        void Delete(string id);

        /// <summary>
        /// Checks if a player exists by their unique identifier.
        /// </summary>
        /// <param name="id">The player's unique identifier</param>
        /// <returns>True if the player exists, false otherwise</returns>
        bool Exists(string id);

        /// <summary>
        /// Gets the count of players matching the specified criteria.
        /// </summary>
        /// <param name="citizenPropertyId">The citizen property ID to count</param>
        /// <returns>The count of players</returns>
        long GetCountByCitizenPropertyId(string citizenPropertyId);
    }
}
