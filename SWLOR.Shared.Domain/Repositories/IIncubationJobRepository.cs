using SWLOR.Shared.Domain.Associate.ValueObjects;

namespace SWLOR.Shared.Domain.Repositories
{
    /// <summary>
    /// Repository interface for IncubationJob entity operations.
    /// </summary>
    public interface IIncubationJobRepository
    {
        /// <summary>
        /// Gets an incubation job by its unique identifier.
        /// </summary>
        /// <param name="id">The incubation job's unique identifier</param>
        /// <returns>The incubation job if found, null otherwise</returns>
        IncubationJob GetById(string id);

        /// <summary>
        /// Gets all incubation jobs by parent property ID.
        /// </summary>
        /// <param name="parentPropertyId">The parent property ID to search for</param>
        /// <returns>Collection of incubation jobs for the specified property</returns>
        IEnumerable<IncubationJob> GetByParentPropertyId(string parentPropertyId);

        /// <summary>
        /// Gets all incubation jobs by player ID.
        /// </summary>
        /// <param name="playerId">The player ID to search for</param>
        /// <returns>Collection of incubation jobs for the specified player</returns>
        IEnumerable<IncubationJob> GetByPlayerId(string playerId);

        /// <summary>
        /// Gets all incubation jobs.
        /// </summary>
        /// <returns>Collection of all incubation jobs</returns>
        IEnumerable<IncubationJob> GetAll();

        /// <summary>
        /// Saves an incubation job entity.
        /// </summary>
        /// <param name="incubationJob">The incubation job to save</param>
        void Save(IncubationJob incubationJob);

        /// <summary>
        /// Deletes an incubation job by its unique identifier.
        /// </summary>
        /// <param name="id">The incubation job's unique identifier</param>
        void Delete(string id);

        /// <summary>
        /// Checks if an incubation job exists by its unique identifier.
        /// </summary>
        /// <param name="id">The incubation job's unique identifier</param>
        /// <returns>True if the incubation job exists, false otherwise</returns>
        bool Exists(string id);
    }
}
