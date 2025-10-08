using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Associate.EventHandlers;
using SWLOR.Component.Associate.Repository;
using SWLOR.Component.Associate.Service;
using SWLOR.Component.Associate.Contracts;
using SWLOR.Component.Associate.Definitions.ItemDefinition;
using SWLOR.Shared.Abstractions.Extensions;
using SWLOR.Shared.Domain.Associate.Contracts;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Repositories;
using SWLOR.Shared.Core.Infrastructure;

namespace SWLOR.Component.Associate.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Associate-related services in the dependency injection container.
    /// </summary>
    public static class AssociateServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Associate services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddAssociateServices(this IServiceCollection services)
        {
            services.AddSingleton<IBeastRepository, BeastRepository>();
            services.AddSingleton<IIncubationJobRepository, IncubationJobRepository>();
            services.AddSingleton<IBeastMasteryService, BeastMasteryService>();
            services.AddSingleton<IDroidService, DroidService>();
            services.AddSingleton<Model.DroidGeekyPersonality>();
            services.AddSingleton<Model.DroidPrissyPersonality>();
            services.AddSingleton<Model.DroidSarcasticPersonality>();
            services.AddSingleton<Model.DroidSlangPersonality>();
            services.AddSingleton<Model.DroidBlandPersonality>();
            services.AddSingleton<Model.DroidWorshipfulPersonality>();
            services.AddSingleton<AssociateEventHandlers>();
            services.AddSingleton<IItemListDefinition, BeastEggItemDefinition>();
            services.AddSingleton<BeastEggItemDefinition>();
            services.AddSingleton<DNAExtractorItemDefinition>();

            services.RegisterInterfaceImplementations<ILootTableDefinition>();
            services.RegisterInterfaceImplementations<IBeastListDefinition>();
            services.RegisterInterfaceImplementations<IItemListDefinition>();

            return services;
        }
    }
}
