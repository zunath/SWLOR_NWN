using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.World.Contracts;
using SWLOR.Component.World.EventHandlers;
using SWLOR.Component.World.Feature;
using SWLOR.Component.World.Feature.SnippetDefinition;
using SWLOR.Component.World.Service;
using SWLOR.Shared.Domain.Common.Contracts;
using SWLOR.Shared.Domain.Dialog.Contracts;
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
            services.AddSingleton<ITaxiService, Taxi>();
            services.AddSingleton<ISpawnTableBuilder, SpawnTableBuilder>();
            services.AddSingleton<ISpawnService, SpawnService>();
            services.AddSingleton<IObjectVisibilityService, ObjectVisibilityService>();
            services.AddSingleton<IMusicService, MusicService>();
            services.AddSingleton<IAreaService, Area>();
            services.AddSingleton<IWeatherService, WeatherService>();
            services.AddSingleton<IPlanetService, PlanetService>();
            services.AddSingleton<IWalkmeshService, Walkmesh>();
            services.AddSingleton<WorldEventHandlers>();

            // Register feature classes
            services.AddTransient<PlaceableScripts>();
            services.AddTransient<HoloNetTerminal>();
            services.AddTransient<MiniMaps>();
            services.AddTransient<GameWorldEntry>();

            // Snippet definitions are automatically registered by the Inventory component

            // Dialog classes are automatically registered by the Inventory component

            return services;
        }
    }
}
