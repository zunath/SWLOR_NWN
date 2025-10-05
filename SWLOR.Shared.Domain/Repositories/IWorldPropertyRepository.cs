using SWLOR.Shared.Domain.Properties.Entities;
using SWLOR.Shared.Domain.Properties.Enums;

namespace SWLOR.Shared.Domain.Repositories
{
    /// <summary>
    /// Repository interface for WorldProperty entity operations.
    /// </summary>
    public interface IWorldPropertyRepository
    {
        /// <summary>
        /// Gets a world property by its unique identifier.
        /// </summary>
        /// <param name="id">The world property's unique identifier</param>
        /// <returns>The world property if found, null otherwise</returns>
        WorldProperty GetById(string id);

        /// <summary>
        /// Gets all world properties by owner player ID.
        /// </summary>
        /// <param name="ownerPlayerId">The owner player ID to search for</param>
        /// <returns>Collection of world properties owned by the specified player</returns>
        IEnumerable<WorldProperty> GetByOwnerPlayerId(string ownerPlayerId);

        /// <summary>
        /// Gets all world properties by property type.
        /// </summary>
        /// <param name="propertyType">The property type to search for</param>
        /// <returns>Collection of world properties of the specified type</returns>
        IEnumerable<WorldProperty> GetByPropertyType(PropertyType propertyType);

        /// <summary>
        /// Gets all world properties by parent property ID.
        /// </summary>
        /// <param name="parentPropertyId">The parent property ID to search for</param>
        /// <returns>Collection of world properties with the specified parent</returns>
        IEnumerable<WorldProperty> GetByParentPropertyId(string parentPropertyId);

        /// <summary>
        /// Gets all world properties by structure type.
        /// </summary>
        /// <param name="structureType">The structure type to search for</param>
        /// <returns>Collection of world properties of the specified structure type</returns>
        IEnumerable<WorldProperty> GetByStructureType(StructureType structureType);

        /// <summary>
        /// Gets all world properties that are publicly accessible.
        /// </summary>
        /// <returns>Collection of publicly accessible world properties</returns>
        IEnumerable<WorldProperty> GetPubliclyAccessible();

        /// <summary>
        /// Gets all world properties that are queued for deletion.
        /// </summary>
        /// <returns>Collection of world properties queued for deletion</returns>
        IEnumerable<WorldProperty> GetQueuedForDeletion();

        /// <summary>
        /// Gets all world properties by multiple property IDs.
        /// </summary>
        /// <param name="propertyIds">The property IDs to search for</param>
        /// <returns>Collection of world properties with the specified IDs</returns>
        IEnumerable<WorldProperty> GetByPropertyIds(IEnumerable<string> propertyIds);

        /// <summary>
        /// Gets the count of world properties matching the specified criteria.
        /// </summary>
        /// <param name="propertyType">The property type to count</param>
        /// <returns>The count of world properties</returns>
        long GetCountByPropertyType(PropertyType propertyType);

        /// <summary>
        /// Gets all world properties that have leases (apartments, etc.).
        /// </summary>
        /// <returns>Collection of world properties with leases</returns>
        IEnumerable<WorldProperty> GetPropertiesWithLeases();

        /// <summary>
        /// Gets all cities that are not queued for deletion.
        /// </summary>
        /// <returns>Collection of active cities</returns>
        IEnumerable<WorldProperty> GetActiveCities();

        /// <summary>
        /// Gets all structures by parent property ID.
        /// </summary>
        /// <param name="parentPropertyId">The parent property ID to search for</param>
        /// <returns>Collection of structures with the specified parent</returns>
        IEnumerable<WorldProperty> GetStructuresByParentPropertyId(string parentPropertyId);

        /// <summary>
        /// Gets the count of active structures by parent property ID and structure type.
        /// </summary>
        /// <param name="parentPropertyId">The parent property ID to search for</param>
        /// <param name="structureType">The structure type to search for</param>
        /// <returns>The count of active structures</returns>
        long GetActiveStructureCountByParentAndType(string parentPropertyId, StructureType structureType);

        /// <summary>
        /// Gets all world properties.
        /// </summary>
        /// <returns>Collection of all world properties</returns>
        IEnumerable<WorldProperty> GetAll();

        /// <summary>
        /// Saves a world property entity.
        /// </summary>
        /// <param name="worldProperty">The world property to save</param>
        void Save(WorldProperty worldProperty);

        /// <summary>
        /// Deletes a world property by its unique identifier.
        /// </summary>
        /// <param name="id">The world property's unique identifier</param>
        void Delete(string id);

        /// <summary>
        /// Checks if a world property exists by its unique identifier.
        /// </summary>
        /// <param name="id">The world property's unique identifier</param>
        /// <returns>True if the world property exists, false otherwise</returns>
        bool Exists(string id);
    }
}
