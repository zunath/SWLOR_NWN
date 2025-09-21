namespace SWLOR.Shared.Caching.Contracts
{
    /// <summary>
    /// Builder interface for configuring enum-based caching
    /// </summary>
    /// <typeparam name="TEnum">The enum type to cache</typeparam>
    /// <typeparam name="TAttribute">The attribute type to extract from enum values</typeparam>
    public interface IEnumCacheBuilder<TEnum, TAttribute>
        where TEnum : Enum
        where TAttribute : Attribute
    {
        /// <summary>
        /// Includes all enum values in the cache
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        IEnumCacheBuilder<TEnum, TAttribute> WithAllItems();

        /// <summary>
        /// Adds a filtered cache with a predicate
        /// </summary>
        /// <param name="name">The name of the filtered cache</param>
        /// <param name="predicate">The predicate to filter by</param>
        /// <returns>The builder instance for method chaining</returns>
        IEnumCacheBuilder<TEnum, TAttribute> WithFilteredCache(string name, Func<TAttribute, bool> predicate);

        /// <summary>
        /// Adds a grouped cache with a key selector
        /// </summary>
        /// <typeparam name="TGroupKey">The type of the grouping key</typeparam>
        /// <param name="name">The name of the grouped cache</param>
        /// <param name="keySelector">The function to select the grouping key</param>
        /// <returns>The builder instance for method chaining</returns>
        IEnumCacheBuilder<TEnum, TAttribute> WithGroupedCache<TGroupKey>(string name, Func<TAttribute, TGroupKey> keySelector);

        /// <summary>
        /// Adds a filtered and grouped cache
        /// </summary>
        /// <typeparam name="TGroupKey">The type of the grouping key</typeparam>
        /// <param name="name">The name of the cache</param>
        /// <param name="predicate">The predicate to filter by</param>
        /// <param name="keySelector">The function to select the grouping key</param>
        /// <returns>The builder instance for method chaining</returns>
        IEnumCacheBuilder<TEnum, TAttribute> WithFilteredGroupedCache<TGroupKey>(string name, Func<TAttribute, bool> predicate, Func<TAttribute, TGroupKey> keySelector);

        /// <summary>
        /// Builds the configured cache
        /// </summary>
        /// <returns>A configured enum cache</returns>
        IEnumCache<TEnum, TAttribute> Build();
    }
}
