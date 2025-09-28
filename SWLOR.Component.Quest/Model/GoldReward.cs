using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Domain.Quest.Contracts;

namespace SWLOR.Component.Quest.Model
{
    public class GoldReward : IQuestReward
    {
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded services to break circular dependencies
        private IQuestService QuestService => _serviceProvider.GetRequiredService<IQuestService>();
        public int Amount { get; }
        public bool IsSelectable { get; }
        public string MenuName => Amount + " Credits";
        public bool IsGuildQuest { get; }

        public GoldReward(int amount, bool isSelectable, bool isGuildQuest, IServiceProvider serviceProvider)
        {
            Amount = amount;
            IsSelectable = isSelectable;
            IsGuildQuest = isGuildQuest;
            _serviceProvider = serviceProvider;
        }

        public void GiveReward(uint player)
        {
            var amount = QuestService.CalculateQuestGoldReward(player, IsGuildQuest, Amount);
            GiveGoldToCreature(player, amount);
        }
    }
}
