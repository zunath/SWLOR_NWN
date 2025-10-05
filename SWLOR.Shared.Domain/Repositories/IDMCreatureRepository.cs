using SWLOR.Component.Admin.Entity;

namespace SWLOR.Shared.Domain.Repositories
{
    /// <summary>
    /// Repository interface for DMCreature entity operations.
    /// </summary>
    public interface IDMCreatureRepository
    {
        /// <summary>
        /// Gets a DM creature by its unique identifier.
        /// </summary>
        /// <param name="id">The DM creature's unique identifier</param>
        /// <returns>The DM creature if found, null otherwise</returns>
        DMCreature GetById(string id);

        /// <summary>
        /// Gets all DM creatures by name.
        /// </summary>
        /// <param name="name">The name to search for</param>
        /// <returns>Collection of DM creatures with the specified name</returns>
        IEnumerable<DMCreature> GetByName(string name);

        /// <summary>
        /// Gets all DM creatures by tag.
        /// </summary>
        /// <param name="tag">The tag to search for</param>
        /// <returns>Collection of DM creatures with the specified tag</returns>
        IEnumerable<DMCreature> GetByTag(string tag);

        /// <summary>
        /// Gets all DM creatures ordered by name.
        /// </summary>
        /// <returns>Collection of all DM creatures ordered by name</returns>
        IEnumerable<DMCreature> GetAllOrderedByName();

        /// <summary>
        /// Gets all DM creatures.
        /// </summary>
        /// <returns>Collection of all DM creatures</returns>
        IEnumerable<DMCreature> GetAll();

        /// <summary>
        /// Saves a DM creature entity.
        /// </summary>
        /// <param name="dmCreature">The DM creature to save</param>
        void Save(DMCreature dmCreature);

        /// <summary>
        /// Deletes a DM creature by its unique identifier.
        /// </summary>
        /// <param name="id">The DM creature's unique identifier</param>
        void Delete(string id);

        /// <summary>
        /// Checks if a DM creature exists by its unique identifier.
        /// </summary>
        /// <param name="id">The DM creature's unique identifier</param>
        /// <returns>True if the DM creature exists, false otherwise</returns>
        bool Exists(string id);
    }
}
