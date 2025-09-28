namespace SWLOR.Shared.Domain.World.Contracts
{
    /// <summary>
    /// Service responsible for caching and managing visibility objects.
    /// </summary>
    public interface IVisibilityObjectCacheService
    {
        /// <summary>
        /// Loads all visibility objects from areas and caches them.
        /// </summary>
        void LoadVisibilityObjects();

        /// <summary>
        /// Gets a visibility object by its ID.
        /// </summary>
        /// <param name="visibilityObjectId">The visibility object ID</param>
        /// <returns>The object ID if found, null otherwise</returns>
        uint? GetVisibilityObject(string visibilityObjectId);

        /// <summary>
        /// Gets all default hidden objects.
        /// </summary>
        /// <returns>Collection of default hidden object IDs</returns>
        IEnumerable<uint> GetDefaultHiddenObjects();

        /// <summary>
        /// Checks if a visibility object ID exists in the cache.
        /// </summary>
        /// <param name="visibilityObjectId">The visibility object ID to check</param>
        /// <returns>True if the object exists, false otherwise</returns>
        bool HasVisibilityObject(string visibilityObjectId);
    }
}
