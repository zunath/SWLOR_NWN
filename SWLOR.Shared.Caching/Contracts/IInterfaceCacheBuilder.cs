namespace SWLOR.Shared.Caching.Contracts
{
    /// <summary>
    /// Builder interface for configuring interface-based type discovery and caching
    /// </summary>
    /// <typeparam name="TInterface">The interface type to discover implementations of</typeparam>
    /// <typeparam name="TKey">The key type for the cache dictionary</typeparam>
    /// <typeparam name="TValue">The value type for the cache dictionary</typeparam>
    public interface IInterfaceCacheBuilder<TInterface, TKey, TValue>
        where TInterface : class
    {
        /// <summary>
        /// Sets the function to extract data from discovered interface implementations
        /// </summary>
        /// <param name="dataExtractor">Function that takes an interface instance and returns key-value pairs</param>
        /// <returns>The builder instance for method chaining</returns>
        IInterfaceCacheBuilder<TInterface, TKey, TValue> WithDataExtractor(Func<TInterface, Dictionary<TKey, TValue>> dataExtractor);

        /// <summary>
        /// Adds a filtered cache with a predicate
        /// </summary>
        /// <param name="name">The name of the filtered cache</param>
        /// <param name="predicate">The predicate to filter by</param>
        /// <returns>The builder instance for method chaining</returns>
        IInterfaceCacheBuilder<TInterface, TKey, TValue> WithFilteredCache(string name, Func<TValue, bool> predicate);

        /// <summary>
        /// Adds a grouped cache with a key selector
        /// </summary>
        /// <typeparam name="TGroupKey">The type of the grouping key</typeparam>
        /// <param name="name">The name of the grouped cache</param>
        /// <param name="keySelector">The function to select the grouping key</param>
        /// <returns>The builder instance for method chaining</returns>
        IInterfaceCacheBuilder<TInterface, TKey, TValue> WithGroupedCache<TGroupKey>(string name, Func<TValue, TGroupKey> keySelector);

        /// <summary>
        /// Adds a filtered and grouped cache
        /// </summary>
        /// <typeparam name="TGroupKey">The type of the grouping key</typeparam>
        /// <param name="name">The name of the cache</param>
        /// <param name="predicate">The predicate to filter by</param>
        /// <param name="keySelector">The function to select the grouping key</param>
        /// <returns>The builder instance for method chaining</returns>
        IInterfaceCacheBuilder<TInterface, TKey, TValue> WithFilteredGroupedCache<TGroupKey>(string name, Func<TValue, bool> predicate, Func<TValue, TGroupKey> keySelector);

        /// <summary>
        /// Builds the configured cache
        /// </summary>
        /// <returns>A configured interface cache</returns>
        IInterfaceCache<TKey, TValue> Build();
    }
}
