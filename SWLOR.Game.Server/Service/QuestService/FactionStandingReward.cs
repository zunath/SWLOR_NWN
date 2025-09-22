using System;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Data.Entity;
using SWLOR.Shared.Core.Enums;

namespace SWLOR.Game.Server.Service.QuestService
{
    public class FactionStandingReward : IQuestReward
    {
        public bool IsSelectable { get; }
        public string MenuName { get; }
        public FactionType Faction { get; }
        public int Amount { get; }

        public FactionStandingReward(FactionType faction, int amount, bool isSelectable)
        {
            IsSelectable = isSelectable;
            Faction = faction;
            Amount = amount;

            var factionDetail = Service.Faction.GetFactionDetail(Faction);
            MenuName = $"{factionDetail.Name} standing";
        }

        public void GiveReward(uint player)
        {
            Service.Faction.AdjustPlayerFactionStanding(player, Faction, Amount);
        }
    }
}
