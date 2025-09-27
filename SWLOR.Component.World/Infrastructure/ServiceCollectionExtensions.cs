using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.World.Contracts;
using SWLOR.Component.World.EventHandlers;
using SWLOR.Component.World.Feature;
using SWLOR.Component.World.Service;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Domain.World.Contracts;

namespace SWLOR.Component.World.Infrastructure
{
    /// <summary>
    /// Extension methods for registering World-related services in the dependency injection container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all World services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddWorldServices(this IServiceCollection services)
        {
            services.AddSingleton<ITileMagicService, TileMagicService>();
            services.AddSingleton<ITaxiService, TaxiService>();
            services.AddSingleton<ISpawnTableBuilder, SpawnTableBuilder>();
            services.AddSingleton<ISpawnService, SpawnService>();
            services.AddSingleton<IObjectVisibilityService, ObjectVisibilityService>();
            services.AddSingleton<IData2DAService, Data2DAService>();
            services.AddSingleton<IMusicService, MusicService>();
            services.AddSingleton<IAreaService, AreaService>();
            services.AddSingleton<IAreaNoteService, AreaNoteService>();
            services.AddSingleton<IWeatherService, WeatherService>();
            services.AddSingleton<IPlanetService, PlanetService>();
            services.AddSingleton<IWalkmeshService, WalkmeshService>();
            services.AddSingleton<WorldEventHandlers>();

            // Register feature classes
            services.AddTransient<PlaceableScripts>();
            services.AddTransient<HoloNetTerminal>();
            services.AddTransient<MiniMaps>();
            services.AddTransient<GameWorldEntry>();

            // Snippet definitions are automatically registered by the Inventory component

            // Dialog classes are automatically registered by the Inventory component

            // Automatically register all spawn definition implementations
            var assembly = Assembly.GetExecutingAssembly();
            
            // Register ISpawnListDefinition implementations
            var spawnDefinitionTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(ISpawnListDefinition).IsAssignableFrom(t));
            
            foreach (var type in spawnDefinitionTypes)
            {
                services.AddTransient(type);
            }

            return services;
        }
    }
}
