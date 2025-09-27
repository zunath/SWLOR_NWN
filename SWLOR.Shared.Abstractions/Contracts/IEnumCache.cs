namespace SWLOR.Shared.Abstractions.Contracts
{
    /// <summary>
    /// Interface for a configured enum cache
    /// </summary>
    /// <typeparam name="TEnum">The enum type</typeparam>
    /// <typeparam name="TAttribute">The attribute type</typeparam>
    public interface IEnumCache<TEnum, TAttribute>
        where TEnum : Enum
        where TAttribute : Attribute
    {
        /// <summary>
        /// Gets all enum values with their attributes
        /// </summary>
        Dictionary<TEnum, TAttribute> AllItems { get; }

        /// <summary>
        /// Gets a filtered cache by name
        /// </summary>
        /// <param name="name">The name of the filtered cache</param>
        /// <returns>The filtered cache, or null if not found</returns>
        Dictionary<TEnum, TAttribute> GetFilteredCache(string name);

        /// <summary>
        /// Gets a grouped cache by name
        /// </summary>
        /// <typeparam name="TGroupKey">The type of the grouping key</typeparam>
        /// <param name="name">The name of the grouped cache</param>
        /// <returns>The grouped cache, or null if not found</returns>
        Dictionary<TGroupKey, Dictionary<TEnum, TAttribute>> GetGroupedCache<TGroupKey>(string name);

        /// <summary>
        /// Gets a filtered and grouped cache by name
        /// </summary>
        /// <typeparam name="TGroupKey">The type of the grouping key</typeparam>
        /// <param name="name">The name of the cache</param>
        /// <returns>The filtered and grouped cache, or null if not found</returns>
        Dictionary<TGroupKey, Dictionary<TEnum, TAttribute>> GetFilteredGroupedCache<TGroupKey>(string name);
    }
}
