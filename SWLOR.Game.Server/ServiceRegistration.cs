using Microsoft.Extensions.DependencyInjection;
using NWN.Core;
using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.Feature.AbilityDefinition.Beasts;
using SWLOR.Component.Ability.Feature.AbilityDefinition.Devices;
using SWLOR.Component.Ability.Feature.AbilityDefinition.FirstAid;
using SWLOR.Component.Ability.Feature.AbilityDefinition.Force;
using SWLOR.Component.Ability.Feature.AbilityDefinition.General;
using SWLOR.Component.Ability.Feature.AbilityDefinition.Leadership;
using SWLOR.Component.Ability.Feature.AbilityDefinition.MartialArts;
using SWLOR.Component.Ability.Feature.AbilityDefinition.NPC;
using SWLOR.Component.Ability.Feature.AbilityDefinition.OneHanded;
using SWLOR.Component.Ability.Feature.AbilityDefinition.Ranged;
using SWLOR.Component.Ability.Feature.AbilityDefinition.TwoHanded;
using SWLOR.Component.Ability.Infrastructure;
using SWLOR.Component.Ability.Service;
using SWLOR.Component.Admin.Infrastructure;
using SWLOR.Component.Admin.UI.ViewModel;
using SWLOR.Component.AI.Infrastructure;
using SWLOR.Component.Associate.Contracts;
using SWLOR.Component.Associate.Feature.ItemDefinition;
using SWLOR.Component.Associate.Infrastructure;
using SWLOR.Component.Associate.Model;
using SWLOR.Component.Associate.Service;
using SWLOR.Component.Associate.UI.ViewModel;
using SWLOR.Component.Character.Dialog;
using SWLOR.Component.Character.Feature;
using SWLOR.Component.Character.Infrastructure;
using SWLOR.Component.Character.Service;
using SWLOR.Component.Character.UI.ViewModel;
using SWLOR.Component.Combat.Contracts;
using SWLOR.Component.Combat.Feature;
using SWLOR.Component.Combat.Infrastructure;
using SWLOR.Component.Combat.Service;
using SWLOR.Component.Communication.Contracts;
using SWLOR.Component.Communication.Dialog;
using SWLOR.Component.Communication.Feature.ChatCommandDefinition;
using SWLOR.Component.Communication.Infrastructure;
using SWLOR.Component.Communication.Service;
using SWLOR.Component.Communication.UI.ViewModel;
using SWLOR.Component.Crafting.Contracts;
using SWLOR.Component.Crafting.Feature;
using SWLOR.Component.Crafting.Infrastructure;
using SWLOR.Component.Crafting.Service;
using SWLOR.Component.Crafting.UI.ViewModel;
using SWLOR.Component.Inventory.Dialog;
using SWLOR.Component.Inventory.Feature;
using SWLOR.Component.Inventory.Feature.ItemDefinition;
using SWLOR.Component.Inventory.Feature.SnippetDefinition;
using SWLOR.Component.Inventory.Infrastructure;
using SWLOR.Component.Inventory.Service;
using SWLOR.Component.Inventory.UI.ViewModel;
using SWLOR.Component.Market.Contracts;
using SWLOR.Component.Market.Dialog;
using SWLOR.Component.Market.Feature;
using SWLOR.Component.Market.Infrastructure;
using SWLOR.Component.Market.Service;
using SWLOR.Component.Market.UI.ViewModel;
using SWLOR.Component.Migration.Contracts;
using SWLOR.Component.Migration.Infrastructure;
using SWLOR.Component.Migration.Service;
using SWLOR.Component.Perk.Contracts;
using SWLOR.Component.Perk.Feature.PerkDefinition;
using SWLOR.Component.Perk.Infrastructure;
using SWLOR.Component.Perk.Service;
using SWLOR.Component.Perk.UI.ViewModel;
using SWLOR.Component.Properties.Dialog;
using SWLOR.Component.Properties.Infrastructure;
using SWLOR.Component.Properties.UI.ViewModel;
using SWLOR.Component.Quest.Contracts;
using SWLOR.Component.Quest.Dialog;
using SWLOR.Component.Quest.Infrastructure;
using SWLOR.Component.Quest.Service;
using SWLOR.Component.Skill.Infrastructure;
using SWLOR.Component.Skill.Service;
using SWLOR.Component.Skill.UI.ViewModel;
using SWLOR.Component.Space.Contracts;
using SWLOR.Component.Space.Dialog;
using SWLOR.Component.Space.Feature.ShipModuleDefinition;
using SWLOR.Component.Space.Infrastructure;
using SWLOR.Component.Space.Service;
using SWLOR.Component.Space.UI.ViewModel;
using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.Component.StatusEffect.Feature.StatusEffectDefinition;
using SWLOR.Component.StatusEffect.Infrastructure;
using SWLOR.Component.StatusEffect.Service;
using SWLOR.Component.World.Contracts;
using SWLOR.Component.World.Dialog;
using SWLOR.Component.World.Feature;
using SWLOR.Component.World.Infrastructure;
using SWLOR.Component.World.Service;
using SWLOR.Component.World.UI.ViewModel;
using SWLOR.Game.Server.Server;
using SWLOR.NWN.API;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Extensions;
using SWLOR.Shared.Caching.Service;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Async;
using SWLOR.Shared.Core.Configuration;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Core.Log;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Events.Infrastructure;
using SWLOR.Shared.Events.Service;
using SWLOR.Shared.UI.Contracts;
using ScriptExecutionProvider = SWLOR.Game.Server.Server.ScriptExecutionProvider;
using SWLOR.Shared.UI.Infrastructure;
using SWLOR.Shared.UI.Service;
using SWLOR.Shared.Domain.Beasts.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Common.Contracts;
using SWLOR.Shared.Domain.Communication.Contracts;
using SWLOR.Shared.Domain.Crafting.Contracts;
using SWLOR.Shared.Domain.Dialog.Contracts;
using SWLOR.Shared.Domain.Droids.Contracts;
using SWLOR.Shared.Domain.Fishing.Contracts;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Properties.Contracts;
using SWLOR.Shared.Domain.Space.Contracts;
using SWLOR.Shared.Domain.World.Contracts;
using Activity = System.Diagnostics.Activity;

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
            services.AddAbilityServices();
            services.AddAdminServices();
            services.AddAIServices();
            services.AddAssociateServices();
            services.AddCombatServices();
            services.AddCommunicationServices();
            services.AddCraftingServices();
            services.AddInventoryServices();
            services.AddMarketServices();
            services.AddMigrationServices();
            services.AddPerkServices();
            services.AddPlayerServices();
            services.AddQuestServices();
            services.AddSkillServices();
            services.AddPropertiesServices();
            services.AddSpaceServices();
            services.AddStatusEffectServices();
            services.AddWorldServices();

            // Game-Specific Services
            AddGameSpecificServices(services);
        }

        private static void AddGameSpecificServices(IServiceCollection services)
        {
        }

    }
}
