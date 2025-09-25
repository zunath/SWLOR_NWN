using SWLOR.Component.Quest.Contracts;
using SWLOR.Shared.Domain.Quest.Contracts;

namespace SWLOR.Component.Quest.Model
{
    public class GoldReward : IQuestReward
    {
        private readonly IQuestService _questService;
        public int Amount { get; }
        public bool IsSelectable { get; }
        public string MenuName => Amount + " Credits";
        public bool IsGuildQuest { get; }

        public GoldReward(int amount, bool isSelectable, bool isGuildQuest, IQuestService questService)
        {
            Amount = amount;
            IsSelectable = isSelectable;
            IsGuildQuest = isGuildQuest;
            _questService = questService;
        }

        public void GiveReward(uint player)
        {
            var amount = _questService.CalculateQuestGoldReward(player, IsGuildQuest, Amount);
            GiveGoldToCreature(player, amount);
        }
    }
}
