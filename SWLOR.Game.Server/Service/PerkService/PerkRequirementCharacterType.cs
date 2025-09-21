using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data.Entity;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Infrastructure;

namespace SWLOR.Game.Server.Service.PerkService
{
    public class PerkRequirementCharacterType: IPerkRequirement
    {
        private static readonly IDatabaseService _db = ServiceContainer.GetService<IDatabaseService>();
        private readonly CharacterType _requiredCharacterType;

        public PerkRequirementCharacterType(CharacterType type)
        {
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
                var perkService = ServiceContainer.GetService<IPerkService>();
                return $"Character Type: {perkService.GetCharacterType(_requiredCharacterType).Name}";
            }
        }
    }
}
