using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Events.Service;
using SWLOR.Shared.Events.EventHandlers;

namespace SWLOR.Shared.Events.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Eventing-related services in the dependency injection container.
    /// </summary>
    public static class EventsServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Eventing services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddEventingServices(this IServiceCollection services)
        {
            services.AddSingleton<IEventRegistrationService, EventRegistrationService>();
            services.AddSingleton<IEventAggregator, EventAggregator.EventAggregator>();

            // Register event handlers as singletons
            services.AddSingleton<EventRegistrationEventHandlers>();

            return services;
        }
    }
}
