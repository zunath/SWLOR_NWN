using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Quest.Contracts;
using SWLOR.Component.Quest.Model;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Domain.Quest.Contracts;
using SWLOR.Shared.Domain.Quest.Enums;

namespace SWLOR.Component.Quest.Service
{
    /// <summary>
    /// Factory implementation for creating quest objective instances.
    /// This ensures proper DI management and dependency injection.
    /// </summary>
    public class QuestObjectiveFactory : IQuestObjectiveFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public QuestObjectiveFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IQuestObjective CreateCollectItemObjective(string resref, int amount)
        {
            var db = _serviceProvider.GetRequiredService<IDatabaseService>();
            var itemCache = _serviceProvider.GetRequiredService<IItemCacheService>();
            var questService = _serviceProvider.GetRequiredService<IQuestService>();
            return new CollectItemObjective(db, itemCache, _serviceProvider, resref, amount);
        }

        public IQuestObjective CreateKillTargetObjective(NPCGroupType group, int amount)
        {
            var db = _serviceProvider.GetRequiredService<IDatabaseService>();
            var questService = _serviceProvider.GetRequiredService<IQuestService>();
            var npcGroupService = _serviceProvider.GetRequiredService<INPCGroupService>();
            return new KillTargetObjective(db, _serviceProvider, group, amount);
        }
    }
}
