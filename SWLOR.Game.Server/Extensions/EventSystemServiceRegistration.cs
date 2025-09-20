using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Events.Service;

namespace SWLOR.Game.Server.Extensions
{
    /// <summary>
    /// Extension methods for registering the event system services.
    /// </summary>
    public static class EventSystemServiceRegistration
    {
        /// <summary>
        /// Adds the event system services to the service collection.
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddEventSystem(this IServiceCollection services)
        {
            services.AddSingleton<IEventService, EventService>();
            return services;
        }
    }
}
