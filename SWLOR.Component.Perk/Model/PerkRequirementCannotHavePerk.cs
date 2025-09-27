using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Perk.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;

namespace SWLOR.Component.Perk.Model
{
    public class PerkRequirementCannotHavePerk: IPerkRequirement
    {
        private readonly IDatabaseService _db;
        private readonly PerkType _cannotHavePerkType;
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded services to break circular dependencies
        private IPerkService PerkService => _serviceProvider.GetRequiredService<IPerkService>();

        public PerkRequirementCannotHavePerk(
            IDatabaseService db, 
            IServiceProvider serviceProvider,
            PerkType cannotHavePerkType)
        {
            _db = db;
            // Services are now lazy-loaded via IServiceProvider
            _cannotHavePerkType = cannotHavePerkType;
        }

        public string CheckRequirements(uint player)
        {
            if (_cannotHavePerkType == PerkType.Invalid)
                return string.Empty;

            var perkDetail = PerkService.GetPerkDetails(_cannotHavePerkType);
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            if (dbPlayer.Perks.ContainsKey(_cannotHavePerkType) &&
                dbPlayer.Perks[_cannotHavePerkType] > 0)
                return $"You cannot have perk: {perkDetail.Name}";

            return string.Empty;
        }

        public string RequirementText
        {
            get
            {
                var perkDetail = PerkService.GetPerkDetails(_cannotHavePerkType);
                return $"Cannot have perk: {perkDetail.Name}";
            }
        }
    }
}
