using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Quest.Contracts;
using SWLOR.Component.Quest.Model;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Enums;

namespace SWLOR.Component.Quest.Service
{
    /// <summary>
    /// Factory implementation for creating quest reward instances.
    /// This ensures proper DI management and dependency injection.
    /// </summary>
    public class QuestRewardFactory : IQuestRewardFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public QuestRewardFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IQuestReward CreateGoldReward(int amount, bool isSelectable = true, bool isGuildQuest = false)
        {
            var questService = _serviceProvider.GetRequiredService<IQuestService>();
            return new GoldReward(amount, isSelectable, isGuildQuest, questService);
        }

        public IQuestReward CreateXPReward(int amount, bool isSelectable = true)
        {
            var db = _serviceProvider.GetRequiredService<IDatabaseService>();
            var itemCache = _serviceProvider.GetRequiredService<IItemCacheService>();
            return new XPReward(db, itemCache, amount, isSelectable);
        }

        public IQuestReward CreateItemReward(string itemResref, int quantity, bool isSelectable = true)
        {
            var itemCache = _serviceProvider.GetRequiredService<IItemCacheService>();
            return new ItemReward(itemCache, itemResref, quantity, isSelectable);
        }

        public IQuestReward CreateKeyItemReward(KeyItemType keyItemType, bool isSelectable = true)
        {
            var keyItemService = _serviceProvider.GetRequiredService<IKeyItemService>();
            return new KeyItemReward(keyItemType, isSelectable, keyItemService);
        }

        public IQuestReward CreateGPReward(GuildType guild, int amount, bool isSelectable = true)
        {
            return new GPReward(guild, amount, isSelectable);
        }

        public IQuestReward CreateFactionStandingReward(FactionType faction, int amount, bool isSelectable = true)
        {
            return new FactionStandingReward(faction, amount, isSelectable);
        }

        public IQuestReward CreateFactionPointsReward(FactionType faction, int amount, bool isSelectable = true)
        {
            return new FactionPointsReward(faction, amount, isSelectable);
        }
    }
}
