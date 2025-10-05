using SWLOR.Shared.Domain.Entities;

namespace SWLOR.Shared.Domain.Repositories
{
    /// <summary>
    /// Repository interface for Beast entity operations.
    /// </summary>
    public interface IBeastRepository
    {
        /// <summary>
        /// Gets a beast by its unique identifier.
        /// </summary>
        /// <param name="id">The beast's unique identifier</param>
        /// <returns>The beast if found, null otherwise</returns>
        Beast GetById(string id);

        /// <summary>
        /// Gets all beasts by owner player ID.
        /// </summary>
        /// <param name="ownerPlayerId">The owner player ID to search for</param>
        /// <returns>Collection of beasts owned by the specified player</returns>
        IEnumerable<Beast> GetByOwnerPlayerId(string ownerPlayerId);

        /// <summary>
        /// Gets all beasts by name.
        /// </summary>
        /// <param name="name">The name to search for</param>
        /// <returns>Collection of beasts with the specified name</returns>
        IEnumerable<Beast> GetByName(string name);

        /// <summary>
        /// Gets all beasts.
        /// </summary>
        /// <returns>Collection of all beasts</returns>
        IEnumerable<Beast> GetAll();

        /// <summary>
        /// Gets the count of beasts by owner player ID.
        /// </summary>
        /// <param name="ownerPlayerId">The owner player ID to count</param>
        /// <returns>The count of beasts owned by the specified player</returns>
        long GetCountByOwnerPlayerId(string ownerPlayerId);

        /// <summary>
        /// Saves a beast entity.
        /// </summary>
        /// <param name="beast">The beast to save</param>
        void Save(Beast beast);

        /// <summary>
        /// Deletes a beast by its unique identifier.
        /// </summary>
        /// <param name="id">The beast's unique identifier</param>
        void Delete(string id);

        /// <summary>
        /// Checks if a beast exists by its unique identifier.
        /// </summary>
        /// <param name="id">The beast's unique identifier</param>
        /// <returns>True if the beast exists, false otherwise</returns>
        bool Exists(string id);
    }
}
