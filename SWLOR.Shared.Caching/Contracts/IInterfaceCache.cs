namespace SWLOR.Shared.Caching.Contracts
{
    /// <summary>
    /// Interface for a configured interface-based cache
    /// </summary>
    /// <typeparam name="TKey">The key type for the cache dictionary</typeparam>
    /// <typeparam name="TValue">The value type for the cache dictionary</typeparam>
    public interface IInterfaceCache<TKey, TValue>
    {
        /// <summary>
        /// Gets all cached items
        /// </summary>
        Dictionary<TKey, TValue> AllItems { get; }

        /// <summary>
        /// Gets a filtered cache by name
        /// </summary>
        /// <param name="name">The name of the filtered cache</param>
        /// <returns>The filtered cache, or null if not found</returns>
        Dictionary<TKey, TValue>? GetFilteredCache(string name);

        /// <summary>
        /// Gets a grouped cache by name
        /// </summary>
        /// <typeparam name="TGroupKey">The type of the grouping key</typeparam>
        /// <param name="name">The name of the grouped cache</param>
        /// <returns>The grouped cache, or null if not found</returns>
        Dictionary<TGroupKey, Dictionary<TKey, TValue>>? GetGroupedCache<TGroupKey>(string name);

        /// <summary>
        /// Gets a filtered and grouped cache by name
        /// </summary>
        /// <typeparam name="TGroupKey">The type of the grouping key</typeparam>
        /// <param name="name">The name of the cache</param>
        /// <returns>The filtered and grouped cache, or null if not found</returns>
        Dictionary<TGroupKey, Dictionary<TKey, TValue>>? GetFilteredGroupedCache<TGroupKey>(string name);
    }
}
