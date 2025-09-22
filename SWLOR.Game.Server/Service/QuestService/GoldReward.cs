using System;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Data.Entity;
using SWLOR.Shared.Core.Enums;

namespace SWLOR.Game.Server.Service.QuestService
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
