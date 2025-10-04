using Microsoft.Extensions.DependencyInjection;
using NWN.Core;
using SWLOR.Component.Ability.Infrastructure;
using SWLOR.Component.Admin.Infrastructure;
using SWLOR.Component.AI.Infrastructure;
using SWLOR.Component.Associate.Infrastructure;
using SWLOR.Component.Character.Infrastructure;
using SWLOR.Component.Combat.Infrastructure;
using SWLOR.Component.Communication.Infrastructure;
using SWLOR.Component.Crafting.Infrastructure;
using SWLOR.Component.Inventory.Infrastructure;
using SWLOR.Component.Market.Infrastructure;
using SWLOR.Component.Migration.Infrastructure;
using SWLOR.Component.Perk.Infrastructure;
using SWLOR.Component.Properties.Infrastructure;
using SWLOR.Component.Quest.Infrastructure;
using SWLOR.Component.Skill.Infrastructure;
using SWLOR.Component.Space.Infrastructure;
using SWLOR.Component.StatusEffect.Infrastructure;
using SWLOR.Component.World.Infrastructure;
using SWLOR.Game.Server.Server;
using SWLOR.NWN.API.Contracts;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Extensions;
using SWLOR.Shared.Core.Async;
using SWLOR.Shared.Core.Configuration;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Core.Log;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Events.Contracts;
using SWLOR.Shared.Events.Infrastructure;
using SWLOR.Shared.Events.Service;
using ScriptExecutionProvider = SWLOR.Game.Server.Server.ScriptExecutionProvider;
using SWLOR.Shared.UI.Infrastructure;
using SWLOR.NWN.API.NWNX;

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
            services.AddSingleton<IEventRegistrationService, EventRegistrationService>();
            services.AddSingleton<IEventHandlerDiscoveryService, EventHandlerDiscoveryService>();
            services.AddSingleton<IScheduler, Scheduler>();
            
            // Service Initialization
            services.AddSingleton<ServiceInitializationManager>();
        }

        private static void AddGameServices(IServiceCollection services)
        {
            // Shared Services
            services.AddCoreServices();
            services.AddEventingServices();
            services.AddCacheServices();
            services.AddUIServices();

            // Component Services
            services.AddAbilityServices();
            services.AddAdminServices();
            services.AddAIServices();
            services.AddAssociateServices();
            services.AddCharacterServices();
            services.AddCombatServices();
            services.AddCommunicationServices();
            services.AddCraftingServices();
            services.AddInventoryServices();
            services.AddMarketServices();
            services.AddMigrationServices();
            services.AddPerkServices();
            services.AddPropertiesServices();
            services.AddQuestServices();
            services.AddSkillServices();
            services.AddSpaceServices();
            services.AddStatusEffectServices();
            services.AddWorldServices();

            // Game-Specific Services
            AddGameSpecificServices(services);
        }

        private static void AddGameSpecificServices(IServiceCollection services)
        {
            // Register NWNX Plugin Services
            services.AddSingleton<IAdministrationPluginService, AdministrationPluginService>();
            services.AddSingleton<IAreaPluginService, AreaPluginService>();
            services.AddSingleton<IChatPluginService, ChatPluginService>();
            services.AddSingleton<ICreaturePluginService, CreaturePluginService>();
            services.AddSingleton<IEventsPluginService, EventsPluginService>();
            services.AddSingleton<IFeatPluginService, FeatPluginService>();
            services.AddSingleton<IFeedbackPluginService, FeedbackPluginService>();
            services.AddSingleton<IItemPluginService, ItemPluginService>();
            services.AddSingleton<IItemPropertyPluginService, ItemPropertyPluginService>();
            services.AddSingleton<IObjectPluginService, ObjectPluginService>();
            services.AddSingleton<IPlayerPluginService, PlayerPluginService>();
            services.AddSingleton<IProfilerPluginService, ProfilerPluginService>();
        }

    }
}
