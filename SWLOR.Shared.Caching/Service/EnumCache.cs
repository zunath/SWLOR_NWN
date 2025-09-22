using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Contracts;

namespace SWLOR.Shared.Caching.Service
{
    /// <summary>
    /// Implementation of a configured enum cache
    /// </summary>
    /// <typeparam name="TEnum">The enum type</typeparam>
    /// <typeparam name="TAttribute">The attribute type</typeparam>
    public class EnumCache<TEnum, TAttribute> : IEnumCache<TEnum, TAttribute>
        where TEnum : Enum
        where TAttribute : Attribute
    {
        private readonly Dictionary<TEnum, TAttribute> _allItems;
        private readonly Dictionary<string, Dictionary<TEnum, TAttribute>> _filteredCaches;
        private readonly Dictionary<string, object> _groupedCaches;
        private readonly Dictionary<string, object> _filteredGroupedCaches;

        /// <summary>
        /// Initializes a new instance of the EnumCache class
        /// </summary>
        /// <param name="allItems">All enum values with their attributes</param>
        /// <param name="filteredCaches">Filtered caches by name</param>
        /// <param name="groupedCaches">Grouped caches by name</param>
        /// <param name="filteredGroupedCaches">Filtered and grouped caches by name</param>
        public EnumCache(
            Dictionary<TEnum, TAttribute> allItems,
            Dictionary<string, Dictionary<TEnum, TAttribute>> filteredCaches,
            Dictionary<string, object> groupedCaches,
            Dictionary<string, object> filteredGroupedCaches)
        {
            _allItems = allItems;
            _filteredCaches = filteredCaches;
            _groupedCaches = groupedCaches;
            _filteredGroupedCaches = filteredGroupedCaches;
        }

        /// <summary>
        /// Gets all enum values with their attributes
        /// </summary>
        public Dictionary<TEnum, TAttribute> AllItems => _allItems;

        /// <summary>
        /// Gets a filtered cache by name
        /// </summary>
        /// <param name="name">The name of the filtered cache</param>
        /// <returns>The filtered cache, or null if not found</returns>
        public Dictionary<TEnum, TAttribute>? GetFilteredCache(string name)
        {
            return _filteredCaches.TryGetValue(name, out var cache) ? cache : null;
        }

        /// <summary>
        /// Gets a grouped cache by name
        /// </summary>
        /// <typeparam name="TGroupKey">The type of the grouping key</typeparam>
        /// <param name="name">The name of the grouped cache</param>
        /// <returns>The grouped cache, or null if not found</returns>
        public Dictionary<TGroupKey, Dictionary<TEnum, TAttribute>>? GetGroupedCache<TGroupKey>(string name)
        {
            return _groupedCaches.TryGetValue(name, out var cache) ? (Dictionary<TGroupKey, Dictionary<TEnum, TAttribute>>)cache : null;
        }

        /// <summary>
        /// Gets a filtered and grouped cache by name
        /// </summary>
        /// <typeparam name="TGroupKey">The type of the grouping key</typeparam>
        /// <param name="name">The name of the cache</param>
        /// <returns>The filtered and grouped cache, or null if not found</returns>
        public Dictionary<TGroupKey, Dictionary<TEnum, TAttribute>>? GetFilteredGroupedCache<TGroupKey>(string name)
        {
            return _filteredGroupedCaches.TryGetValue(name, out var cache) ? (Dictionary<TGroupKey, Dictionary<TEnum, TAttribute>>)cache : null;
        }
    }
}
