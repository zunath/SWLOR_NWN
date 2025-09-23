using SWLOR.Component.Perk.Contracts;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Domain.Entity;

namespace SWLOR.Component.Perk.Model
{
    public class PerkRequirementCharacterType: IPerkRequirement
    {
        private readonly IDatabaseService _db;
        private readonly IPerkService _perkService;
        private readonly CharacterType _requiredCharacterType;

        public PerkRequirementCharacterType(IDatabaseService db, IPerkService perkService, CharacterType type)
        {
            _db = db;
            _perkService = perkService;
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
                return $"Character Type: {_perkService.GetCharacterType(_requiredCharacterType).Name}";
            }
        }
    }
}
