namespace SWLOR.Shared.Caching.Contracts
{
    /// <summary>
    /// Interface for a generic caching service that can handle both enum attribute caching and interface-based type discovery
    /// </summary>
    public interface IGenericCacheService
    {
        /// <summary>
        /// Creates a builder for enum-based caching
        /// </summary>
        /// <typeparam name="TEnum">The enum type to cache</typeparam>
        /// <typeparam name="TAttribute">The attribute type to extract from enum values</typeparam>
        /// <returns>A builder for configuring enum caching</returns>
        IEnumCacheBuilder<TEnum, TAttribute> BuildEnumCache<TEnum, TAttribute>() 
            where TEnum : Enum 
            where TAttribute : Attribute;

        /// <summary>
        /// Creates a builder for interface-based type discovery and caching
        /// </summary>
        /// <typeparam name="TInterface">The interface type to discover implementations of</typeparam>
        /// <typeparam name="TKey">The key type for the cache dictionary</typeparam>
        /// <typeparam name="TValue">The value type for the cache dictionary</typeparam>
        /// <returns>A builder for configuring interface-based caching</returns>
        IInterfaceCacheBuilder<TInterface, TKey, TValue> BuildInterfaceCache<TInterface, TKey, TValue>()
            where TInterface : class;
    }
}
