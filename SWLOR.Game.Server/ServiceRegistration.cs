using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NWN.Core;
using SWLOR.Component.Combat.Contracts;
using SWLOR.Component.Combat.Service;
using SWLOR.Component.Player.Contracts;
using SWLOR.Component.Player.Service;
using SWLOR.Game.Server.Server;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Caching.Service;
using SWLOR.Shared.Core.Async;
using SWLOR.Shared.Core.Service;
using SWLOR.Shared.Core.Configuration;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Core.Log;
using SWLOR.Shared.Events.Service;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Service;
using ScriptExecutionProvider = SWLOR.Game.Server.Server.ScriptExecutionProvider;

namespace SWLOR.Game.Server
{
    /// <summary>
    /// Handles registration of services in the dependency injection container.
    /// </summary>
    public static class ServiceRegistration
    {
        /// <summary>
        /// Registers all application services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            AddDatabaseServices(services);
            AddInfrastructureServices(services);
            AddServerServices(services);
            AddGameServices(services);

            return services;
        }

        private static void AddDatabaseServices(IServiceCollection services)
        {
            services.AddSingleton<IDatabaseService, DB>();
        }

        private static void AddInfrastructureServices(IServiceCollection services)
        {
            services.AddSingleton<ILogger, LogService>();
            services.AddSingleton<IAppSettings, AppSettings>();
        }

        private static void AddServerServices(IServiceCollection services)
        {
            // NWN.Core services
            services.AddSingleton<IScriptExecutionProvider, ScriptExecutionProvider>();
            services.AddSingleton<ClosureManager>();
            services.AddSingleton<ICoreFunctionHandler>(provider => provider.GetRequiredService<ClosureManager>());
            services.AddSingleton<IClosureManager>(provider => provider.GetRequiredService<ClosureManager>());

            // SWLOR Services
            services.AddSingleton<IMainLoopProcessor, MainLoopProcessor>();
            services.AddSingleton<INativeInteropManager, NativeInteropManager>();
            services.AddSingleton<IScriptRegistry, ScriptRegistry>();
            services.AddSingleton<IScriptExecutor, ScriptExecutor>();
            services.AddSingleton<IServerBootstrapper, ServerBootstrapper>();
            services.AddSingleton<IServerManager, ServerManager>();
            services.AddSingleton<IEventService, EventService>();
            services.AddSingleton<IEventRegistrationService, EventRegistrationService>();
            services.AddSingleton<IScheduler, Scheduler>();
        }

        private static void AddGameServices(IServiceCollection services)
        {
            services.AddSingleton<IRandomService, RandomService>();
            services.AddSingleton<ITileMagicService, TileMagicService>();
            services.AddSingleton<ITaxiService, Taxi>();
            services.AddSingleton<IAttackOfOpportunityService, AttackOfOpportunityService>();
            services.AddSingleton<IClientVersionCheck, ClientVersionCheck>();
            services.AddSingleton<IGuiService, GuiService>();
            
            // Cache Services
            services.AddSingleton<IGenericCacheService, GenericCacheService>();
            services.AddSingleton<IItemCacheService, ItemCacheService>();
            services.AddSingleton<IPortraitCacheService, PortraitCacheService>();
            services.AddSingleton<ISoundSetCacheService, SoundSetCacheService>();
            services.AddSingleton<IModuleCacheService, ModuleCacheService>();
            
            // ViewModels
            AddViewModels(services);
        }
        
        private static void AddViewModels(IServiceCollection services)
        {
            // Automatically discover and register all ViewModels that inherit from GuiViewModelBase
            var viewModelTypes = typeof(ServiceRegistration).Assembly
                .GetTypes()
                .Where(type => type.IsClass && 
                              !type.IsAbstract && 
                              IsViewModelType(type))
                .ToList();

            foreach (var viewModelType in viewModelTypes)
            {
                services.AddTransient(viewModelType);
            }
        }

        private static bool IsViewModelType(Type type)
        {
            // Check if it inherits from GuiViewModelBase<,>
            var baseType = type.BaseType;
            while (baseType != null)
            {
                if (baseType.IsGenericType && 
                    baseType.GetGenericTypeDefinition() == typeof(GuiViewModelBase<,>))
                {
                    return true;
                }
                baseType = baseType.BaseType;
            }

            return false;
        }
    }
}
