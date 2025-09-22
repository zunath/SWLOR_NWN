using SWLOR.Component.Quest.Contracts;
using SWLOR.Shared.Core.Enums;

namespace SWLOR.Component.Quest.Model
{
    public class FactionPointsReward : IQuestReward
    {
        public bool IsSelectable { get; }
        public string MenuName { get; }
        public FactionType Faction { get; }
        public int Amount { get; }

        public FactionPointsReward(FactionType faction, int amount, bool isSelectable)
        {
            IsSelectable = isSelectable;
            Faction = faction;
            Amount = Math.Abs(amount);

            var factionDetail = Service.Faction.GetFactionDetail(Faction);
            MenuName = $"{factionDetail.Name} points";
        }

        public void GiveReward(uint player)
        {
            Service.Faction.AdjustPlayerFactionPoints(player, Faction, Amount);
        }
    }
}
