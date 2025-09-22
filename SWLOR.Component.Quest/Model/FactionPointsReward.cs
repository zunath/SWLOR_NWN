using SWLOR.Component.Quest.Contracts;
using SWLOR.Shared.Core.Contracts;

namespace SWLOR.Component.Quest.Model
{
    public class FactionPointsReward : IQuestReward
    {
        private readonly IFactionService _factionService;
        public bool IsSelectable { get; }
        public string MenuName { get; }
        public FactionType Faction { get; }
        public int Amount { get; }

        public FactionPointsReward(IFactionService factionService, FactionType faction, int amount, bool isSelectable)
        {
            _factionService = factionService;
            IsSelectable = isSelectable;
            Faction = faction;
            Amount = Math.Abs(amount);

            var factionDetail = _factionService.GetFactionDetail(Faction);
            MenuName = $"{factionDetail.Name} points";
        }

        public void GiveReward(uint player)
        {
            _factionService.AdjustPlayerFactionPoints(player, Faction, Amount);
        }
    }
}
