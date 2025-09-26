using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Entities;

namespace SWLOR.Component.Perk.Model
{
    public class PerkRequirementCharacterType: IPerkRequirement
    {
        private readonly IDatabaseService _db;
        private readonly IServiceProvider _serviceProvider;
        private readonly CharacterType _requiredCharacterType;
        
        // Lazy-loaded services to break circular dependencies
        private IPerkService PerkService => _serviceProvider.GetRequiredService<IPerkService>();

        public PerkRequirementCharacterType(IDatabaseService db, IServiceProvider serviceProvider, CharacterType type)
        {
            _db = db;
            // Services are now lazy-loaded via IServiceProvider
            _requiredCharacterType = type;
        }

        public string CheckRequirements(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            if (dbPlayer.CharacterType != _requiredCharacterType)
            {
                return $"Only {_requiredCharacterType} character types may access this perk.";
            }

            return string.Empty;
        }

        public string RequirementText
        {
            get
            {
                return $"Character Type: {PerkService.GetCharacterType(_requiredCharacterType).Name}";
            }
        }
    }
}
