using SWLOR.Shared.Domain.Properties.Entities;
using SWLOR.Shared.Domain.Properties.Enums;

namespace SWLOR.Shared.Domain.Repositories
{
    /// <summary>
    /// Repository interface for Election entity operations.
    /// </summary>
    public interface IElectionRepository
    {
        /// <summary>
        /// Gets an election by its unique identifier.
        /// </summary>
        /// <param name="id">The election's unique identifier</param>
        /// <returns>The election if found, null otherwise</returns>
        Election GetById(string id);

        /// <summary>
        /// Gets all elections by property ID.
        /// </summary>
        /// <param name="propertyId">The property ID to search for</param>
        /// <returns>Collection of elections for the specified property</returns>
        IEnumerable<Election> GetByPropertyId(string propertyId);

        /// <summary>
        /// Gets all elections by stage.
        /// </summary>
        /// <param name="stage">The election stage to search for</param>
        /// <returns>Collection of elections in the specified stage</returns>
        IEnumerable<Election> GetByStage(ElectionStageType stage);

        /// <summary>
        /// Gets all elections by property ID and stage.
        /// </summary>
        /// <param name="propertyId">The property ID to search for</param>
        /// <param name="stage">The election stage to search for</param>
        /// <returns>Collection of elections for the specified property and stage</returns>
        IEnumerable<Election> GetByPropertyIdAndStage(string propertyId, ElectionStageType stage);

        /// <summary>
        /// Gets a single election by property ID.
        /// </summary>
        /// <param name="propertyId">The property ID to search for</param>
        /// <returns>The election if found, null otherwise</returns>
        Election GetSingleByPropertyId(string propertyId);

        /// <summary>
        /// Saves an election entity.
        /// </summary>
        /// <param name="election">The election to save</param>
        void Save(Election election);

        /// <summary>
        /// Deletes an election by its unique identifier.
        /// </summary>
        /// <param name="id">The election's unique identifier</param>
        void Delete(string id);

        /// <summary>
        /// Checks if an election exists by its unique identifier.
        /// </summary>
        /// <param name="id">The election's unique identifier</param>
        /// <returns>True if the election exists, false otherwise</returns>
        bool Exists(string id);
    }
}
