using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Character.Contracts;
using SWLOR.Component.Character.Service;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Common.Contracts;
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
            services.AddSingleton<IClientVersionCheck, ClientVersionCheck>();
            services.AddSingleton<PlayerRestService>();
            services.AddSingleton<IRacialAppearanceService, RacialAppearanceService>();
            services.AddSingleton<IPlayerInitializationService, PlayerInitializationService>();
            services.AddSingleton<IPartyService, PartyService>();
            services.AddSingleton<IFactionService, FactionService>();
            services.AddSingleton<ICurrencyService, CurrencyService>();
            services.AddSingleton<IAnimationPlayerService, AnimationPlayerService>();
            services.AddSingleton<ITargetingService, Targeting>();
            services.AddSingleton<IAchievementService, Achievement>();
            services.AddSingleton<IRaceService, Race>();
            services.AddSingleton<IActivityService, Activity>();

            return services;
        }
    }
}
