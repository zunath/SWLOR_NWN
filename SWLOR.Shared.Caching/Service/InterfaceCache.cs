using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Contracts;

namespace SWLOR.Shared.Caching.Service
{
    /// <summary>
    /// Implementation of a configured interface-based cache
    /// </summary>
    /// <typeparam name="TKey">The key type for the cache dictionary</typeparam>
    /// <typeparam name="TValue">The value type for the cache dictionary</typeparam>
    public class InterfaceCache<TKey, TValue> : IInterfaceCache<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> _allItems;
        private readonly Dictionary<string, Dictionary<TKey, TValue>> _filteredCaches;
        private readonly Dictionary<string, object> _groupedCaches;
        private readonly Dictionary<string, object> _filteredGroupedCaches;

        /// <summary>
        /// Initializes a new instance of the InterfaceCache class
        /// </summary>
        /// <param name="allItems">All cached items</param>
        /// <param name="filteredCaches">Filtered caches by name</param>
        /// <param name="groupedCaches">Grouped caches by name</param>
        /// <param name="filteredGroupedCaches">Filtered and grouped caches by name</param>
        public InterfaceCache(
            Dictionary<TKey, TValue> allItems,
            Dictionary<string, Dictionary<TKey, TValue>> filteredCaches,
            Dictionary<string, object> groupedCaches,
            Dictionary<string, object> filteredGroupedCaches)
        {
            _allItems = allItems;
            _filteredCaches = filteredCaches;
            _groupedCaches = groupedCaches;
            _filteredGroupedCaches = filteredGroupedCaches;
        }

        /// <summary>
        /// Gets all cached items
        /// </summary>
        public Dictionary<TKey, TValue> AllItems => _allItems;

        /// <summary>
        /// Gets a filtered cache by name
        /// </summary>
        /// <param name="name">The name of the filtered cache</param>
        /// <returns>The filtered cache, or null if not found</returns>
        public Dictionary<TKey, TValue>? GetFilteredCache(string name)
        {
            return _filteredCaches.TryGetValue(name, out var cache) ? cache : null;
        }

        /// <summary>
        /// Gets a grouped cache by name
        /// </summary>
        /// <typeparam name="TGroupKey">The type of the grouping key</typeparam>
        /// <param name="name">The name of the grouped cache</param>
        /// <returns>The grouped cache, or null if not found</returns>
        public Dictionary<TGroupKey, Dictionary<TKey, TValue>>? GetGroupedCache<TGroupKey>(string name)
        {
            return _groupedCaches.TryGetValue(name, out var cache) ? (Dictionary<TGroupKey, Dictionary<TKey, TValue>>)cache : null;
        }

        /// <summary>
        /// Gets a filtered and grouped cache by name
        /// </summary>
        /// <typeparam name="TGroupKey">The type of the grouping key</typeparam>
        /// <param name="name">The name of the cache</param>
        /// <returns>The filtered and grouped cache, or null if not found</returns>
        public Dictionary<TGroupKey, Dictionary<TKey, TValue>>? GetFilteredGroupedCache<TGroupKey>(string name)
        {
            return _filteredGroupedCaches.TryGetValue(name, out var cache) ? (Dictionary<TGroupKey, Dictionary<TKey, TValue>>)cache : null;
        }
    }
}
