using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Crafting.Contracts;
using SWLOR.Component.Crafting.Feature;
using SWLOR.Component.Crafting.Service;
using System.Reflection;
using SWLOR.Shared.Domain.Crafting.Contracts;
using SWLOR.Shared.Domain.Fishing.Contracts;

namespace SWLOR.Component.Crafting.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Crafting-related services in the dependency injection container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Crafting services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddCraftingServices(this IServiceCollection services)
        {
            // Register RecipeBuilder as transient since each recipe definition needs its own instance
            services.AddTransient<IRecipeBuilder, RecipeBuilder>();
            services.AddTransient<IFishingLocationBuilder, FishingLocationBuilder>();
            
            // Register Crafting services
            services.AddSingleton<ICraftService, Craft>();
            services.AddSingleton<IFishingService, Fishing>();

            // Register feature classes
            services.AddTransient<ScavengePoint>();
            
            // Automatically register all IRecipeListDefinition implementations
            var assembly = Assembly.GetExecutingAssembly();
            var recipeDefinitionTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IRecipeListDefinition).IsAssignableFrom(t));
            
            foreach (var type in recipeDefinitionTypes)
            {
                services.AddTransient(type);
            }
            
            // Automatically register all IFishingLocationDefinition implementations
            var fishingLocationDefinitionTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IFishingLocationDefinition).IsAssignableFrom(t));
            
            foreach (var type in fishingLocationDefinitionTypes)
            {
                services.AddTransient(type);
            }
            
            return services;
        }
    }
}
