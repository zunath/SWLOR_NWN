using SWLOR.Shared.Domain.Entities;

namespace SWLOR.Shared.Domain.Repositories
{
    /// <summary>
    /// Repository interface for PlayerNote entity operations.
    /// </summary>
    public interface IPlayerNoteRepository
    {
        /// <summary>
        /// Gets a player note by its unique identifier.
        /// </summary>
        /// <param name="id">The player note's unique identifier</param>
        /// <returns>The player note if found, null otherwise</returns>
        PlayerNote GetById(string id);

        /// <summary>
        /// Gets all player notes by player ID.
        /// </summary>
        /// <param name="playerId">The player ID to search for</param>
        /// <returns>Collection of player notes for the specified player</returns>
        IEnumerable<PlayerNote> GetByPlayerId(string playerId);

        /// <summary>
        /// Gets all player notes by player ID that are not DM notes.
        /// </summary>
        /// <param name="playerId">The player ID to search for</param>
        /// <returns>Collection of player notes for the specified player that are not DM notes</returns>
        IEnumerable<PlayerNote> GetPlayerNotesByPlayerId(string playerId);

        /// <summary>
        /// Gets all player notes by name.
        /// </summary>
        /// <param name="name">The name to search for</param>
        /// <returns>Collection of player notes with the specified name</returns>
        IEnumerable<PlayerNote> GetByName(string name);

        /// <summary>
        /// Gets all DM notes.
        /// </summary>
        /// <returns>Collection of DM notes</returns>
        IEnumerable<PlayerNote> GetDMNotes();

        /// <summary>
        /// Gets all DM notes by creator name.
        /// </summary>
        /// <param name="creatorName">The DM creator name to search for</param>
        /// <returns>Collection of DM notes created by the specified DM</returns>
        IEnumerable<PlayerNote> GetDMNotesByCreatorName(string creatorName);

        /// <summary>
        /// Gets all DM notes by creator CD key.
        /// </summary>
        /// <param name="creatorCDKey">The DM creator CD key to search for</param>
        /// <returns>Collection of DM notes created by the specified DM</returns>
        IEnumerable<PlayerNote> GetDMNotesByCreatorCDKey(string creatorCDKey);

        /// <summary>
        /// Gets all DM notes by player ID.
        /// </summary>
        /// <param name="playerId">The player ID to search for</param>
        /// <returns>Collection of DM notes for the specified player</returns>
        IEnumerable<PlayerNote> GetDMNotesByPlayerId(string playerId);

        /// <summary>
        /// Saves a player note entity.
        /// </summary>
        /// <param name="playerNote">The player note to save</param>
        void Save(PlayerNote playerNote);

        /// <summary>
        /// Deletes a player note by its unique identifier.
        /// </summary>
        /// <param name="id">The player note's unique identifier</param>
        void Delete(string id);

        /// <summary>
        /// Gets all player notes.
        /// </summary>
        /// <returns>Collection of all player notes</returns>
        IEnumerable<PlayerNote> GetAll();

        /// <summary>
        /// Gets the count of all player notes.
        /// </summary>
        /// <returns>The count of all player notes</returns>
        long GetCount();

        /// <summary>
        /// Checks if a player note exists by its unique identifier.
        /// </summary>
        /// <param name="id">The player note's unique identifier</param>
        /// <returns>True if the player note exists, false otherwise</returns>
        bool Exists(string id);
    }
}
