using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Core.Extension;

namespace SWLOR.Shared.Caching.Service
{
    /// <summary>
    /// Builder for configuring enum-based caching
    /// </summary>
    /// <typeparam name="TEnum">The enum type to cache</typeparam>
    /// <typeparam name="TAttribute">The attribute type to extract from enum values</typeparam>
    public class EnumCacheBuilder<TEnum, TAttribute> : IEnumCacheBuilder<TEnum, TAttribute>
        where TEnum : Enum
        where TAttribute : Attribute
    {
        private readonly Dictionary<TEnum, TAttribute> _allItems = new();
        private readonly Dictionary<string, Dictionary<TEnum, TAttribute>> _filteredCaches = new();
        private readonly Dictionary<string, object> _groupedCaches = new();
        private readonly Dictionary<string, object> _filteredGroupedCaches = new();

        /// <summary>
        /// Includes all enum values in the cache
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        public IEnumCacheBuilder<TEnum, TAttribute> WithAllItems()
        {
            var enumValues = Enum.GetValues(typeof(TEnum)).Cast<TEnum>();
            foreach (var enumValue in enumValues)
            {
                var attribute = enumValue.GetAttribute<TEnum, TAttribute>();
                if (attribute != null)
                {
                    _allItems[enumValue] = attribute;
                }
            }
            return this;
        }

        /// <summary>
        /// Adds a filtered cache with a predicate
        /// </summary>
        /// <param name="name">The name of the filtered cache</param>
        /// <param name="predicate">The predicate to filter by</param>
        /// <returns>The builder instance for method chaining</returns>
        public IEnumCacheBuilder<TEnum, TAttribute> WithFilteredCache(string name, Func<TAttribute, bool> predicate)
        {
            var filtered = _allItems.Where(kvp => predicate(kvp.Value)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            _filteredCaches[name] = filtered;
            return this;
        }

        /// <summary>
        /// Adds a grouped cache with a key selector
        /// </summary>
        /// <typeparam name="TGroupKey">The type of the grouping key</typeparam>
        /// <param name="name">The name of the grouped cache</param>
        /// <param name="keySelector">The function to select the grouping key</param>
        /// <returns>The builder instance for method chaining</returns>
        public IEnumCacheBuilder<TEnum, TAttribute> WithGroupedCache<TGroupKey>(string name, Func<TAttribute, TGroupKey> keySelector)
        {
            var grouped = _allItems.GroupBy(kvp => keySelector(kvp.Value))
                .ToDictionary(g => g.Key, g => g.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
            _groupedCaches[name] = grouped;
            return this;
        }

        /// <summary>
        /// Adds a filtered and grouped cache
        /// </summary>
        /// <typeparam name="TGroupKey">The type of the grouping key</typeparam>
        /// <param name="name">The name of the cache</param>
        /// <param name="predicate">The predicate to filter by</param>
        /// <param name="keySelector">The function to select the grouping key</param>
        /// <returns>The builder instance for method chaining</returns>
        public IEnumCacheBuilder<TEnum, TAttribute> WithFilteredGroupedCache<TGroupKey>(string name, Func<TAttribute, bool> predicate, Func<TAttribute, TGroupKey> keySelector)
        {
            var filtered = _allItems.Where(kvp => predicate(kvp.Value));
            var grouped = filtered.GroupBy(kvp => keySelector(kvp.Value))
                .ToDictionary(g => g.Key, g => g.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
            _filteredGroupedCaches[name] = grouped;
            return this;
        }

        /// <summary>
        /// Builds the configured cache
        /// </summary>
        /// <returns>A configured enum cache</returns>
        public IEnumCache<TEnum, TAttribute> Build()
        {
            return new EnumCache<TEnum, TAttribute>(_allItems, _filteredCaches, _groupedCaches, _filteredGroupedCaches);
        }
    }
}
