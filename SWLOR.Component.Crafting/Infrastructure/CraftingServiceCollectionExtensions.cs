using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Crafting.Contracts;
using SWLOR.Component.Crafting.EventHandlers;
using SWLOR.Component.Crafting.Feature;
using SWLOR.Component.Crafting.Repository;
using SWLOR.Component.Crafting.Service;
using SWLOR.Shared.Domain.Crafting.Contracts;
using SWLOR.Shared.Domain.Repositories;
using SWLOR.Shared.Core.Infrastructure;

namespace SWLOR.Component.Crafting.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Crafting-related services in the dependency injection container.
    /// </summary>
    public static class CraftingServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Crafting services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddCraftingServices(this IServiceCollection services)
        {
            // Register repositories
            services.AddSingleton<IResearchJobRepository, ResearchJobRepository>();
            
            // Register RecipeBuilder as transient since each recipe definition needs its own instance
            services.AddSingleton<IRecipeBuilder, RecipeBuilder>();
            services.AddSingleton<IFishingLocationBuilder, FishingLocationBuilder>();
            
            // Register Crafting services
            services.AddSingleton<ICraftService, CraftService>();
            services.AddSingleton<IFishingService, FishingService>();

            // Register feature classes
            services.AddSingleton<ScavengePoint>();
            services.AddSingleton<Resource>();
            
            // Automatically register all IRecipeListDefinition implementations
            services.RegisterInterfaceImplementations<IRecipeListDefinition>();
            
            // Automatically register all IFishingLocationDefinition implementations
            services.RegisterInterfaceImplementations<IFishingLocationDefinition>();

            // Register event handlers as singletons
            services.AddSingleton<CraftingServiceEventHandlers>();
            
            return services;
        }
    }
}
