using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Quest.Contracts;
using SWLOR.Component.Quest.EventHandlers;
using SWLOR.Component.Quest.Service;
using SWLOR.NWN.API.Contracts;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Extensions;
using SWLOR.Shared.Domain.Quest.Contracts;
using SWLOR.Shared.Core.Infrastructure;

namespace SWLOR.Component.Quest.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Quest-related services in the dependency injection container.
    /// </summary>
    public static class QuestServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Quest services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddQuestServices(this IServiceCollection services)
        {
            services.AddSingleton<IQuestBuilder, QuestBuilder>(provider => new QuestBuilder(
                provider.GetRequiredService<IServiceProvider>()));
            services.AddSingleton<IQuestBuilderFactory, QuestBuilderFactory>();
            services.AddSingleton<IQuestDetailFactory, QuestDetailFactory>();
            services.AddSingleton<IQuestRewardFactory, QuestRewardFactory>();
            services.AddSingleton<IQuestPrerequisiteFactory, QuestPrerequisiteFactory>();
            services.AddSingleton<IQuestObjectiveFactory, QuestObjectiveFactory>();
            services.AddSingleton<IQuestService, QuestService>(provider => new QuestService(
                provider.GetRequiredService<IDatabaseService>(),
                provider.GetRequiredService<IServiceProvider>(),
                provider.GetRequiredService<IEventAggregator>(),
                provider.GetRequiredService<IPlayerPluginService>()));
            services.AddSingleton<IGuildService, GuildService>();
            services.AddSingleton<INPCGroupService, NPCGroupService>();
            services.AddSingleton<Feature.ExplorationTrigger>();
            services.AddSingleton<QuestServiceEventHandlers>();

            services.RegisterInterfaceImplementations<IQuestListDefinition>();

            return services;
        }
    }
}
