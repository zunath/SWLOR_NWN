using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Caching.Service;

namespace SWLOR.Shared.Caching.Extensions
{
    /// <summary>
    /// Extension methods for registering cache-related services in the dependency injection container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all cache services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddCacheServices(this IServiceCollection services)
        {
            services.AddSingleton<IGenericCacheService, GenericCacheService>();
            services.AddSingleton<IItemCacheService, ItemCacheService>();
            services.AddSingleton<IPortraitCacheService, PortraitCacheService>();
            services.AddSingleton<ISoundSetCacheService, SoundSetCacheService>();
            services.AddSingleton<IModuleCacheService, ModuleCacheService>();

            return services;
        }
    }
}
