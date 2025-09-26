using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Quest.Contracts;
using SWLOR.Shared.Domain.Communication.Contracts;
using SWLOR.Shared.Domain.Communication.Enums;
using SWLOR.Shared.Domain.Quest.Contracts;

namespace SWLOR.Component.Quest.Model
{
    public class FactionStandingReward : IQuestReward
    {
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded services to break circular dependencies
        private IFactionService FactionService => _serviceProvider.GetRequiredService<IFactionService>();
        public bool IsSelectable { get; }
        public string MenuName { get; }
        public FactionType Faction { get; }
        public int Amount { get; }

        public FactionStandingReward(IServiceProvider serviceProvider, FactionType faction, int amount, bool isSelectable)
        {
            // Services are now lazy-loaded via IServiceProvider
            IsSelectable = isSelectable;
            Faction = faction;
            Amount = amount;

            var factionDetail = FactionService.GetFactionDetail(Faction);
            MenuName = $"{factionDetail.Name} standing";
        }

        public void GiveReward(uint player)
        {
            FactionService.AdjustPlayerFactionStanding(player, Faction, Amount);
        }
    }
}
