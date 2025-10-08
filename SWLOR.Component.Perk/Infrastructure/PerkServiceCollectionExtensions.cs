using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Perk.Contracts;
using SWLOR.Component.Perk.EventHandlers;
using SWLOR.Component.Perk.Service;
using SWLOR.Shared.Abstractions.Extensions;
using SWLOR.Shared.Domain.Perk.Contracts;
using SWLOR.Shared.Core.Infrastructure;

namespace SWLOR.Component.Perk.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Perk-related services in the dependency injection container.
    /// </summary>
    public static class PerkServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Perk services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddPerkServices(this IServiceCollection services)
        {
            services.AddSingleton<IPerkBuilder, PerkBuilder>();
            services.AddSingleton<IPerkRequirementFactory, PerkRequirementFactory>();
            services.AddSingleton<IUsePerkFeat, UsePerkFeat>();
            services.AddSingleton<IPerkEffectService, PerkEffectService>();
            services.AddSingleton<IPerkService, PerkService>();
            services.AddSingleton<PerkEventHandler>();
            services.RegisterInterfaceImplementations<IPerkListDefinition>();

            return services;
        }
    }
}
