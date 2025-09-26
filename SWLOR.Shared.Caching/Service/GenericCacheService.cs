using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Shared.Caching.Service
{
    /// <summary>
    /// Generic caching service that can handle both enum attribute caching and interface-based type discovery
    /// </summary>
    public class GenericCacheService : IGenericCacheService
    {
        private readonly IServiceProvider? _serviceProvider;

        public GenericCacheService(IServiceProvider? serviceProvider = null)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Creates a builder for enum-based caching
        /// </summary>
        /// <typeparam name="TEnum">The enum type to cache</typeparam>
        /// <typeparam name="TAttribute">The attribute type to extract from enum values</typeparam>
        /// <returns>A builder for configuring enum caching</returns>
        public IEnumCacheBuilder<TEnum, TAttribute> BuildEnumCache<TEnum, TAttribute>()
            where TEnum : Enum
            where TAttribute : Attribute
        {
            return new EnumCacheBuilder<TEnum, TAttribute>();
        }

        /// <summary>
        /// Creates a builder for interface-based type discovery and caching
        /// </summary>
        /// <typeparam name="TInterface">The interface type to discover implementations of</typeparam>
        /// <typeparam name="TKey">The key type for the cache dictionary</typeparam>
        /// <typeparam name="TValue">The value type for the cache dictionary</typeparam>
        /// <returns>A builder for configuring interface-based caching</returns>
        public IInterfaceCacheBuilder<TInterface, TKey, TValue> BuildInterfaceCache<TInterface, TKey, TValue>()
            where TInterface : class
        {
            return new InterfaceCacheBuilder<TInterface, TKey, TValue>(_serviceProvider);
        }
    }
}
