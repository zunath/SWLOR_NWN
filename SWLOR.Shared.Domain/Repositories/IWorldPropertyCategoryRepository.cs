using SWLOR.Shared.Domain.Properties.Entities;

namespace SWLOR.Shared.Domain.Repositories
{
    /// <summary>
    /// Repository interface for WorldPropertyCategory entity operations.
    /// </summary>
    public interface IWorldPropertyCategoryRepository
    {
        /// <summary>
        /// Gets a world property category by its unique identifier.
        /// </summary>
        /// <param name="id">The world property category's unique identifier</param>
        /// <returns>The world property category if found, null otherwise</returns>
        WorldPropertyCategory GetById(string id);

        /// <summary>
        /// Gets world property categories by property ID.
        /// </summary>
        /// <param name="propertyId">The property ID to search for</param>
        /// <returns>Collection of world property categories for the specified property</returns>
        IEnumerable<WorldPropertyCategory> GetByPropertyId(string propertyId);

        /// <summary>
        /// Gets world property categories by parent category ID.
        /// </summary>
        /// <param name="parentCategoryId">The parent category ID to search for</param>
        /// <returns>Collection of world property categories with the specified parent</returns>
        IEnumerable<WorldPropertyCategory> GetByParentCategoryId(string parentCategoryId);

        /// <summary>
        /// Saves a world property category entity.
        /// </summary>
        /// <param name="worldPropertyCategory">The world property category to save</param>
        void Save(WorldPropertyCategory worldPropertyCategory);

        /// <summary>
        /// Deletes a world property category by its unique identifier.
        /// </summary>
        /// <param name="id">The world property category's unique identifier</param>
        void Delete(string id);

        /// <summary>
        /// Checks if a world property category exists by its unique identifier.
        /// </summary>
        /// <param name="id">The world property category's unique identifier</param>
        /// <returns>True if the world property category exists, false otherwise</returns>
        bool Exists(string id);
    }
}
