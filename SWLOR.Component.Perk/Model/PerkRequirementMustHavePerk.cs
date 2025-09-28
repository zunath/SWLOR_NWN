using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Perk.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;

namespace SWLOR.Component.Perk.Model
{
    public class PerkRequirementMustHavePerk: IPerkRequirement
    {
        private readonly IDatabaseService _db;
        private readonly IServiceProvider _serviceProvider;
        private readonly PerkType _mustHavePerkType;
        private readonly int _mustHavePerkLevel;
        
        // Lazy-loaded services to break circular dependencies
        private IPerkService PerkService => _serviceProvider.GetRequiredService<IPerkService>();

        public PerkRequirementMustHavePerk(
            IDatabaseService db, 
            IServiceProvider serviceProvider,
            PerkType mustHavePerkType, 
            int mustHavePerkLevel = 1)
        {
            _db = db;
            _serviceProvider = serviceProvider;
            _mustHavePerkType = mustHavePerkType;
            _mustHavePerkLevel = mustHavePerkLevel;
        }

        public string CheckRequirements(uint player)
        {
            if (_mustHavePerkType == PerkType.Invalid)
                return string.Empty;

            var perkDetail = PerkService.GetPerkDetails(_mustHavePerkType);
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            if (!dbPlayer.Perks.ContainsKey(_mustHavePerkType) || PerkService.GetPerkLevel(player, _mustHavePerkType) < _mustHavePerkLevel)
               return $"You must have perk {perkDetail.Name} at level {_mustHavePerkLevel}.";

            return string.Empty;
        }

        public string RequirementText
        {
            get
            {
                var perkDetail = PerkService.GetPerkDetails(_mustHavePerkType);
                return $"Must have perk {perkDetail.Name} at level {_mustHavePerkLevel}.";
            }
        }
    }
}
