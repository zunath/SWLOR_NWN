using SWLOR.Component.Quest.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.Quest.Model
{
    public class FactionStandingReward : IQuestReward
    {
        private readonly IFactionService _factionService;
        public bool IsSelectable { get; }
        public string MenuName { get; }
        public FactionType Faction { get; }
        public int Amount { get; }

        public FactionStandingReward(IFactionService factionService, FactionType faction, int amount, bool isSelectable)
        {
            _factionService = factionService;
            IsSelectable = isSelectable;
            Faction = faction;
            Amount = amount;

            var factionDetail = _factionService.GetFactionDetail(Faction);
            MenuName = $"{factionDetail.Name} standing";
        }

        public void GiveReward(uint player)
        {
            _factionService.AdjustPlayerFactionStanding(player, Faction, Amount);
        }
    }
}
