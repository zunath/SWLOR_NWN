using SWLOR.Shared.Domain.Entities;

namespace SWLOR.Shared.Domain.Repositories
{
    /// <summary>
    /// Repository interface for PlayerBan entity operations.
    /// </summary>
    public interface IPlayerBanRepository
    {
        /// <summary>
        /// Gets a player ban by its unique identifier.
        /// </summary>
        /// <param name="id">The player ban's unique identifier</param>
        /// <returns>The player ban if found, null otherwise</returns>
        PlayerBan GetById(string id);

        /// <summary>
        /// Gets a player ban by CD key.
        /// </summary>
        /// <param name="cdKey">The CD key to search for</param>
        /// <returns>The player ban if found, null otherwise</returns>
        PlayerBan GetByCDKey(string cdKey);

        /// <summary>
        /// Gets all player bans.
        /// </summary>
        /// <returns>Collection of all player bans</returns>
        IEnumerable<PlayerBan> GetAll();

        /// <summary>
        /// Gets the count of all player bans.
        /// </summary>
        /// <returns>The count of all player bans</returns>
        long GetCount();

        /// <summary>
        /// Saves a player ban entity.
        /// </summary>
        /// <param name="playerBan">The player ban to save</param>
        void Save(PlayerBan playerBan);

        /// <summary>
        /// Deletes a player ban by its unique identifier.
        /// </summary>
        /// <param name="id">The player ban's unique identifier</param>
        void Delete(string id);

        /// <summary>
        /// Checks if a player ban exists by its unique identifier.
        /// </summary>
        /// <param name="id">The player ban's unique identifier</param>
        /// <returns>True if the player ban exists, false otherwise</returns>
        bool Exists(string id);
    }
}
