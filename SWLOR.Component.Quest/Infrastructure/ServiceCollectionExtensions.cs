using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Quest.Contracts;
using SWLOR.Component.Quest.Feature.QuestDefinition;
using SWLOR.Component.Quest.Service;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Common.Contracts;
using SWLOR.Shared.Domain.Communication.Contracts;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Quest.Contracts;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.Domain.Dialog.Contracts;
using SWLOR.Shared.Domain.World.Contracts;

namespace SWLOR.Component.Quest.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Quest-related services in the dependency injection container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Quest services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddQuestServices(this IServiceCollection services)
        {
            // Quest Definition Services
            services.AddSingleton<IQuestListDefinition, DantooineQuestDefinition>();
            services.AddSingleton<IQuestListDefinition, CZ220QuestDefinition>();
            services.AddSingleton<IQuestListDefinition, ViscaraQuestDefinition>();
            services.AddSingleton<IQuestListDefinition, HiddenAccessQuestDefinition>();
            services.AddSingleton<IQuestListDefinition, HutlarQuestDefinition>();
            services.AddSingleton<IQuestListDefinition, SmitheryGuildQuestDefinition>();
            services.AddSingleton<IQuestListDefinition, FabricationGuildQuestDefinition>();
            services.AddSingleton<IQuestListDefinition, EngineeringGuildQuestDefinition>();
            services.AddSingleton<IQuestListDefinition, AgricultureGuildQuestDefinition>();
            services.AddSingleton<IQuestListDefinition, TatooineQuestDefinition>();
            services.AddSingleton<IQuestListDefinition, HuntersGuildQuestDefinition>();
            services.AddSingleton<IQuestListDefinition, KorribanQuestlineDefinition>();
            services.AddSingleton<IQuestListDefinition, MonCalaQuestDefinition>();

            // Quest Builder Service
            services.AddTransient<IQuestBuilder, QuestBuilder>(provider => new QuestBuilder(
                provider.GetRequiredService<IItemCacheService>(),
                provider.GetRequiredService<IQuestService>(),
                provider.GetRequiredService<IKeyItemService>(),
                provider.GetRequiredService<IFactionService>(),
                provider.GetRequiredService<IGuildService>(),
                provider.GetRequiredService<IDatabaseService>(),
                provider.GetRequiredService<INPCGroupService>(),
                provider.GetRequiredService<IGuiService>(),
                provider.GetRequiredService<IDialogService>()));
            services.AddSingleton<IQuestBuilderFactory, QuestBuilderFactory>();
            
            // Quest Component Factories
            services.AddSingleton<IQuestDetailFactory, QuestDetailFactory>();
            services.AddSingleton<IQuestRewardFactory, QuestRewardFactory>();
            services.AddSingleton<IQuestPrerequisiteFactory, QuestPrerequisiteFactory>();
            services.AddSingleton<IQuestObjectiveFactory, QuestObjectiveFactory>();

            // Quest Service
            services.AddSingleton<IQuestService, SWLOR.Component.Quest.Service.Quest>(provider => new SWLOR.Component.Quest.Service.Quest(
                provider.GetRequiredService<IDatabaseService>(),
                provider.GetRequiredService<IItemCacheService>(),
                provider.GetRequiredService<IGenericCacheService>(),
                provider.GetRequiredService<IItemService>(),
                provider.GetRequiredService<IPerkService>(),
                provider.GetRequiredService<IEnmityService>(),
                provider.GetRequiredService<IActivityService>(),
                provider.GetRequiredService<IRandomService>()));

            // Additional services required by quest definitions
            // Note: These services should be registered elsewhere in the application
            // but we're documenting them here for reference:
            // - IKeyItemService (for key item operations)
            // - IFactionService (for faction operations) 
            // - IGuildService (for guild operations)
            // - INPCGroupService (for NPC group lookups)
            // - IGuiService (for GUI operations)
            // - IDialogService (for dialog operations)
            // - IObjectVisibilityService (for object visibility operations)

            return services;
        }
    }
}
