using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Quest.Contracts;
using SWLOR.Component.Quest.Model;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Domain.Common.Enums;
using SWLOR.Shared.Domain.Communication.Contracts;
using SWLOR.Shared.Domain.Communication.Enums;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Inventory.Enums;
using SWLOR.Shared.Domain.Quest.Contracts;

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
            return new GoldReward(amount, isSelectable, isGuildQuest, _serviceProvider);
        }

        public IQuestReward CreateXPReward(int amount, bool isSelectable = true)
        {
            var db = _serviceProvider.GetRequiredService<IDatabaseService>();
            var itemCache = _serviceProvider.GetRequiredService<IItemCacheService>();
            return new XPReward(db, _serviceProvider, amount, isSelectable);
        }

        public IQuestReward CreateItemReward(string itemResref, int quantity, bool isSelectable = true)
        {
            var itemCache = _serviceProvider.GetRequiredService<IItemCacheService>();
            return new ItemReward(itemCache, itemResref, quantity, isSelectable);
        }

        public IQuestReward CreateKeyItemReward(KeyItemType keyItemType, bool isSelectable = true)
        {
            var keyItemService = _serviceProvider.GetRequiredService<IKeyItemService>();
            return new KeyItemReward(keyItemType, isSelectable, _serviceProvider);
        }

        public IQuestReward CreateGPReward(GuildType guild, int amount, bool isSelectable = true)
        {
            var guildService = _serviceProvider.GetRequiredService<IGuildService>();
            return new GPReward(guildService, guild, amount, isSelectable);
        }

        public IQuestReward CreateFactionStandingReward(FactionType faction, int amount, bool isSelectable = true)
        {
            var factionService = _serviceProvider.GetRequiredService<IFactionService>();
            return new FactionStandingReward(_serviceProvider, faction, amount, isSelectable);
        }

        public IQuestReward CreateFactionPointsReward(FactionType faction, int amount, bool isSelectable = true)
        {
            var factionService = _serviceProvider.GetRequiredService<IFactionService>();
            return new FactionPointsReward(_serviceProvider, faction, amount, isSelectable);
        }
    }
}
