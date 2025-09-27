using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Character.Contracts;
using SWLOR.Component.Character.EventHandlers;
using SWLOR.Component.Character.Feature;
using SWLOR.Component.Character.Service;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Communication.Contracts;
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
        public static IServiceCollection AddPlayerServices(this IServiceCollection services)
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
            services.AddTransient<PlayerStatusWindow>();
            services.AddTransient<AchievementProgression>();
            services.AddTransient<PersistentLocation>();
            services.AddTransient<PersistentMapProgression>();
            services.AddTransient<PersistentMapPin>();
            services.AddTransient<PlayerTemporaryEffects>();
            services.AddTransient<ArmorDisplay>();

            // Register event handlers as singletons
            services.AddSingleton<UIEventHandlers>();

            // Dialog classes are automatically registered by the Inventory component

            return services;
        }
    }
}
