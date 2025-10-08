using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.World.Contracts;
using SWLOR.Component.World.EventHandlers;
using SWLOR.Component.World.Feature;
using SWLOR.Component.World.Repository;
using SWLOR.Component.World.Service;
using SWLOR.Shared.Abstractions.Extensions;
using SWLOR.Shared.Domain.Repositories;
using SWLOR.Shared.Domain.World.Contracts;
using SWLOR.Shared.Core.Infrastructure;

namespace SWLOR.Component.World.Infrastructure
{
    /// <summary>
    /// Extension methods for registering World-related services in the dependency injection container.
    /// </summary>
    public static class WorldServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all World services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddWorldServices(this IServiceCollection services)
        {
            // Register repositories
            services.AddSingleton<IAreaNoteRepository, AreaNoteRepository>();
            
            services.AddSingleton<ITileMagicService, TileMagicService>();
            services.AddSingleton<ITaxiService, TaxiService>();
            services.AddSingleton<ISpawnTableBuilder, SpawnTableBuilder>();
            
            // Spawn-related services
            services.AddSingleton<ISpawnCalculationService, SpawnCalculationService>();
            services.AddSingleton<ISpawnCreationService, SpawnCreationService>();
            services.AddSingleton<ISpawnManagementService, SpawnManagementService>();
            services.AddSingleton<ISpawnProcessingService, SpawnProcessingService>();
            services.AddSingleton<ISpawnService, SpawnService>();
            
            services.AddSingleton<IVisibilityObjectCacheService, VisibilityObjectCacheService>();
            services.AddSingleton<IPlayerVisibilityService, PlayerVisibilityService>();
            services.AddSingleton<IObjectVisibilityService, ObjectVisibilityService>();
            services.AddSingleton<IMusicService, MusicService>();
            services.AddSingleton<IAreaService, AreaService>();
            services.AddSingleton<IAreaNoteService, AreaNoteService>();
            
            // Weather-related services
            services.AddSingleton<IWeatherClimateService, WeatherClimateService>();
            services.AddSingleton<IWeatherCalculationService, WeatherCalculationService>();
            services.AddSingleton<IWeatherEffectsService, WeatherEffectsService>();
            services.AddSingleton<IWeatherVisualService, WeatherVisualService>();
            services.AddSingleton<IWeatherService, WeatherService>();
            
            services.AddSingleton<IPlanetAreaService, PlanetAreaService>();
            services.AddSingleton<IPlanetService, PlanetService>();
            services.AddSingleton<IWalkmeshService, WalkmeshService>();
            services.AddSingleton<WorldEventHandlers>();

            // Register event handlers as singletons
            services.AddSingleton<WorldServiceEventHandlers>();

            // Register feature classes
            services.AddSingleton<PlaceableScripts>();
            services.AddSingleton<HoloNetTerminal>();
            services.AddSingleton<MiniMaps>();
            services.AddSingleton<GameWorldEntry>();

            // Automatically register all spawn definition implementations
            services.RegisterInterfaceImplementations<ISpawnListDefinition>();

            return services;
        }
    }
}
