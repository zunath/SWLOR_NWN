using Microsoft.Extensions.DependencyInjection;
using NWN.Core;
using SWLOR.Component.Admin.Infrastructure;
using SWLOR.Component.Associate.Infrastructure;
using SWLOR.Component.Combat.Infrastructure;
using SWLOR.Component.Communication.Infrastructure;
using SWLOR.Component.Crafting.Infrastructure;
using SWLOR.Component.Inventory.Infrastructure;
using SWLOR.Component.Language.Infrastructure;
using SWLOR.Component.Market.Infrastructure;
using SWLOR.Component.Player.Infrastructure;
using SWLOR.Component.Properties.Infrastructure;
using SWLOR.Component.Space.Infrastructure;
using SWLOR.Component.World.Infrastructure;
using SWLOR.Game.Server.Server;
using SWLOR.NWN.API;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Extensions;
using SWLOR.Shared.Core.Async;
using SWLOR.Shared.Core.Configuration;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Core.Log;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Events.Infrastructure;
using SWLOR.Shared.Events.Service;
using ScriptExecutionProvider = SWLOR.Game.Server.Server.ScriptExecutionProvider;
using SWLOR.Shared.UI.Infrastructure;

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
            // Shared Services
            services.AddCoreServices();
            services.AddEventingServices();
            services.AddCacheServices();
            services.AddUIServices();

            // Component Services
            services.AddAdminServices();
            services.AddAssociateServices();
            services.AddCombatServices();
            services.AddCommunicationServices();
            services.AddCraftingServices();
            services.AddInventoryServices();
            services.AddLanguageServices();
            services.AddMarketServices();
            services.AddPlayerServices();
            services.AddPropertiesServices();
            services.AddSpaceServices();
            services.AddWorldServices();

            // Game-Specific Services
            AddGameSpecificServices(services);
        }

        private static void AddGameSpecificServices(IServiceCollection services)
        {
            // Dialog Services
            services.AddSingleton<SWLOR.Game.Server.Feature.DialogDefinition.LockedDoorDialog>();
            services.AddSingleton<SWLOR.Game.Server.Feature.DialogDefinition.SliceTerminalDialog>();
            services.AddSingleton<SWLOR.Game.Server.Feature.DialogDefinition.StarportDialog>();
            services.AddSingleton<SWLOR.Component.World.Dialog.TaxiTerminalDialog>();

            // Quest Definition Services
            services.AddSingleton<SWLOR.Game.Server.Service.QuestService.IQuestListDefinition, SWLOR.Game.Server.Feature.QuestDefinition.DantooineQuestDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.QuestService.IQuestListDefinition, SWLOR.Game.Server.Feature.QuestDefinition.CZ220QuestDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.QuestService.IQuestListDefinition, SWLOR.Game.Server.Feature.QuestDefinition.ViscaraQuestDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.QuestService.IQuestListDefinition, SWLOR.Game.Server.Feature.QuestDefinition.HiddenAccessQuestDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.QuestService.IQuestListDefinition, SWLOR.Game.Server.Feature.QuestDefinition.HutlarQuestDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.QuestService.IQuestListDefinition, SWLOR.Game.Server.Feature.QuestDefinition.SmitheryGuildQuestDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.QuestService.IQuestListDefinition, SWLOR.Game.Server.Feature.QuestDefinition.FabricationGuildQuestDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.QuestService.IQuestListDefinition, SWLOR.Game.Server.Feature.QuestDefinition.EngineeringGuildQuestDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.QuestService.IQuestListDefinition, SWLOR.Game.Server.Feature.QuestDefinition.AgricultureGuildQuestDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.QuestService.IQuestListDefinition, SWLOR.Game.Server.Feature.QuestDefinition.TatooineQuestDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.QuestService.IQuestListDefinition, SWLOR.Game.Server.Feature.QuestDefinition.HuntersGuildQuestDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.QuestService.IQuestListDefinition, SWLOR.Game.Server.Feature.QuestDefinition.KorribanQuestlineDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.QuestService.IQuestListDefinition, SWLOR.Game.Server.Feature.QuestDefinition.MonCalaQuestDefinition>();

            // Feature Services
            services.AddSingleton<SWLOR.Game.Server.Feature.MiniMaps>();
            services.AddSingleton<SWLOR.Game.Server.Feature.PlaceableScripts>();
            services.AddSingleton<SWLOR.Game.Server.Feature.PersistentMapProgression>();

            // Other Services
            services.AddSingleton<SWLOR.Game.Server.Service.SnippetService.ISnippetListDefinition, SWLOR.Game.Server.Feature.SnippetDefinition.KeyItemSnippetDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.ItemService.IItemListDefinition, SWLOR.Game.Server.Feature.ItemDefinition.KeyItemDefinition>();
            
            // Core Services
            services.AddSingleton<SWLOR.Shared.Core.Contracts.IObjectVisibilityService, SWLOR.Game.Server.Service.ObjectVisibilityService>();
        }
        
    }
}
