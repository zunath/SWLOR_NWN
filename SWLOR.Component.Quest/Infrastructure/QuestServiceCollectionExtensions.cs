using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Quest.Contracts;
using SWLOR.Component.Quest.EventHandlers;
using SWLOR.Component.Quest.Service;
using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWNX;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Quest.Contracts;

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
            // Dynamically register all quest definition classes
            RegisterQuestDefinitionClasses(services);

            // Quest Builder Service
            services.AddSingleton<IQuestBuilder, QuestBuilder>(provider => new QuestBuilder(
                provider.GetRequiredService<IServiceProvider>()));
            services.AddSingleton<IQuestBuilderFactory, QuestBuilderFactory>();
            
            // Quest Component Factories
            services.AddSingleton<IQuestDetailFactory, QuestDetailFactory>();
            services.AddSingleton<IQuestRewardFactory, QuestRewardFactory>();
            services.AddSingleton<IQuestPrerequisiteFactory, QuestPrerequisiteFactory>();
            services.AddSingleton<IQuestObjectiveFactory, QuestObjectiveFactory>();

            // Quest Service
            services.AddSingleton<IQuestService, QuestService>(provider => new QuestService(
                provider.GetRequiredService<IDatabaseService>(),
                provider.GetRequiredService<IServiceProvider>(),
                provider.GetRequiredService<IEventAggregator>(),
                provider.GetRequiredService<IPlayerPluginService>()));

            // Register Guild service
            services.AddSingleton<IGuildService, GuildService>();
            
            // Register NPCGroup service
            services.AddSingleton<INPCGroupService, NPCGroupService>();

            // Register feature classes
            services.AddSingleton<Feature.ExplorationTrigger>();

            // Register event handlers as singletons
            services.AddSingleton<QuestServiceEventHandlers>();

            // Snippet definitions are automatically registered by the Inventory component

            // Dialog classes are automatically registered by the Inventory component

            return services;
        }

        private static void RegisterQuestDefinitionClasses(IServiceCollection services)
        {
            // Find all types that implement IQuestListDefinition
            var questDefinitionTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IQuestListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in questDefinitionTypes)
            {
                // Register each quest definition as transient
                services.AddSingleton(type);
            }
        }
    }
}
