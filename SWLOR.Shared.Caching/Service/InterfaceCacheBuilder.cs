using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Shared.Caching.Service
{
    /// <summary>
    /// Builder for configuring interface-based type discovery and caching
    /// </summary>
    /// <typeparam name="TInterface">The interface type to discover implementations of</typeparam>
    /// <typeparam name="TKey">The key type for the cache dictionary</typeparam>
    /// <typeparam name="TValue">The value type for the cache dictionary</typeparam>
    public class InterfaceCacheBuilder<TInterface, TKey, TValue> : IInterfaceCacheBuilder<TInterface, TKey, TValue>
        where TInterface : class
    {
        private readonly Dictionary<TKey, TValue> _allItems = new();
        private readonly Dictionary<string, Dictionary<TKey, TValue>> _filteredCaches = new();
        private readonly Dictionary<string, object> _groupedCaches = new();
        private readonly Dictionary<string, object> _filteredGroupedCaches = new();
        private readonly IServiceProvider _serviceProvider;
        private Func<TInterface, Dictionary<TKey, TValue>> _dataExtractor;

        public InterfaceCacheBuilder(IServiceProvider serviceProvider = null)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Sets the function to extract data from discovered interface implementations
        /// </summary>
        /// <param name="dataExtractor">Function that takes an interface instance and returns key-value pairs</param>
        /// <returns>The builder instance for method chaining</returns>
        public IInterfaceCacheBuilder<TInterface, TKey, TValue> WithDataExtractor(Func<TInterface, Dictionary<TKey, TValue>> dataExtractor)
        {
            _dataExtractor = dataExtractor;
            
            // Discover and cache all implementations
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(TInterface).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                TInterface instance;
                
                // Try to resolve from DI container first, fallback to Activator
                if (_serviceProvider != null)
                {
                    try
                    {
                        instance = (TInterface)_serviceProvider.GetRequiredService(type);
                    }
                    catch (Exception ex)
                    {
                        // Log the exception for debugging
                        Console.WriteLine($"Failed to resolve {type.Name} from DI container: {ex.Message}");
                        // Fallback to Activator if not registered in DI
                        instance = (TInterface)Activator.CreateInstance(type);
                    }
                }
                else
                {
                    Console.WriteLine($"ServiceProvider is null, using Activator for {type.Name}");
                    instance = (TInterface)Activator.CreateInstance(type);
                }
                
                var data = dataExtractor(instance);
                
                foreach (var kvp in data)
                {
                    _allItems[kvp.Key] = kvp.Value;
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
        public IInterfaceCacheBuilder<TInterface, TKey, TValue> WithFilteredCache(string name, Func<TValue, bool> predicate)
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
        public IInterfaceCacheBuilder<TInterface, TKey, TValue> WithGroupedCache<TGroupKey>(string name, Func<TValue, TGroupKey> keySelector)
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
        public IInterfaceCacheBuilder<TInterface, TKey, TValue> WithFilteredGroupedCache<TGroupKey>(string name, Func<TValue, bool> predicate, Func<TValue, TGroupKey> keySelector)
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
        /// <returns>A configured interface cache</returns>
        public IInterfaceCache<TKey, TValue> Build()
        {
            return new InterfaceCache<TKey, TValue>(_allItems, _filteredCaches, _groupedCaches, _filteredGroupedCaches);
        }
    }
}
