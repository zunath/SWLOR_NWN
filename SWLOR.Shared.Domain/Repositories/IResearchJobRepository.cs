using SWLOR.Shared.Domain.Entities;

namespace SWLOR.Shared.Domain.Repositories
{
    /// <summary>
    /// Repository interface for ResearchJob entity operations.
    /// </summary>
    public interface IResearchJobRepository
    {
        /// <summary>
        /// Gets a research job by its unique identifier.
        /// </summary>
        /// <param name="id">The research job's unique identifier</param>
        /// <returns>The research job if found, null otherwise</returns>
        ResearchJob GetById(string id);

        /// <summary>
        /// Gets all research jobs by parent property ID.
        /// </summary>
        /// <param name="parentPropertyId">The parent property ID to search for</param>
        /// <returns>Collection of research jobs for the specified property</returns>
        IEnumerable<ResearchJob> GetByParentPropertyId(string parentPropertyId);

        /// <summary>
        /// Gets all research jobs by player ID.
        /// </summary>
        /// <param name="playerId">The player ID to search for</param>
        /// <returns>Collection of research jobs for the specified player</returns>
        IEnumerable<ResearchJob> GetByPlayerId(string playerId);

        /// <summary>
        /// Gets all research jobs.
        /// </summary>
        /// <returns>Collection of all research jobs</returns>
        IEnumerable<ResearchJob> GetAll();

        /// <summary>
        /// Saves a research job entity.
        /// </summary>
        /// <param name="researchJob">The research job to save</param>
        void Save(ResearchJob researchJob);

        /// <summary>
        /// Deletes a research job by its unique identifier.
        /// </summary>
        /// <param name="id">The research job's unique identifier</param>
        void Delete(string id);

        /// <summary>
        /// Checks if a research job exists by its unique identifier.
        /// </summary>
        /// <param name="id">The research job's unique identifier</param>
        /// <returns>True if the research job exists, false otherwise</returns>
        bool Exists(string id);
    }
}
