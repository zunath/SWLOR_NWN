using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Admin.Contracts;
using SWLOR.Component.Admin.EventHandlers;
using SWLOR.Component.Admin.Feature;
using SWLOR.Component.Admin.Service;
using SWLOR.Component.Admin.UI.ViewModel;
using SWLOR.Shared.Domain.Common.Contracts;

namespace SWLOR.Component.Admin.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Admin-related services in the dependency injection container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Admin services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddAdminServices(this IServiceCollection services)
        {
            // Register services as singletons
            services.AddSingleton<IAuditingService, AuditingService>();
            services.AddSingleton<IAuthorizationService, AuthorizationService>();

            // Register features as singletons
            services.AddSingleton<TlkOverrides>();
            services.AddSingleton<DMActions>();
            services.AddSingleton<ServerTasks>();
            services.AddSingleton<DMAuthorization>();

            // Register view models as singletons
            services.AddSingleton<DMPlayerExamineViewModel>();
            services.AddSingleton<DebugEnmityViewModel>();
            services.AddSingleton<CreatureManagerViewModel>();

            // Register event handlers as singletons
            services.AddSingleton<AdminEventHandlers>();

            return services;
        }
    }
}
