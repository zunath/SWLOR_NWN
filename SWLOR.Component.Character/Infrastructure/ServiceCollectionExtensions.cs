using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Character.Contracts;
using SWLOR.Component.Character.EventHandlers;
using SWLOR.Component.Character.Feature;
using SWLOR.Component.Character.Service;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Inventory.Contracts;

namespace SWLOR.Component.Character.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Player-related services in the dependency injection container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Player services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddCharacterServices(this IServiceCollection services)
        {
            services.AddSingleton<IClientVersionCheck, ClientVersionCheckService>();
            services.AddSingleton<PlayerRestService>();
            services.AddSingleton<IRacialAppearanceService, RacialAppearanceService>();
            services.AddSingleton<IPlayerInitializationService, PlayerInitializationService>();
            services.AddSingleton<IPartyService, PartyService>();
            services.AddSingleton<IFactionService, FactionService>();
            services.AddSingleton<ICurrencyService, CurrencyService>();
            services.AddSingleton<IAnimationPlayerService, AnimationPlayerService>();
            services.AddSingleton<ITargetingService, TargetingService>();
            services.AddSingleton<IAchievementService, AchievementService>();
            services.AddSingleton<AchievementService>();
            services.AddSingleton<IRaceService, RaceService>();
            services.AddSingleton<IActivityService, ActivityService>();

            // Snippet definitions are automatically registered by the Inventory component

            // Register feature classes
            services.AddSingleton<PlayerStatusWindow>();
            services.AddSingleton<AchievementProgression>();
            services.AddSingleton<PersistentLocation>();
            services.AddSingleton<PersistentMapProgression>();
            services.AddSingleton<PersistentMapPin>();
            services.AddSingleton<PlayerTemporaryEffects>();
            services.AddSingleton<ArmorDisplay>();
            services.AddSingleton<SaveCharacters>();

            // Register event handlers as singletons
            services.AddSingleton<UIEventHandlers>();
            services.AddSingleton<CharacterServiceEventHandlers>();

            // Dialog classes are automatically registered by the Inventory component

            return services;
        }
    }
}
