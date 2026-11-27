using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Domain.Character
{
    public class PerkRequirementCharacterType: IPerkRequirement
    {
        private readonly CharacterType _requiredCharacterType;

        public PerkRequirementCharacterType(CharacterType type)
        {
            _requiredCharacterType = type;
        }

        public PerkRequirementCategory Category => PerkRequirementCategory.CharacterType;

        public string CheckRequirements(uint player) => CheckRequirements(DB.Get<Player>(GetObjectUUID(player)), null);

        public string CheckRequirements(Player dbPlayer, Beast activeBeast)
        {
            if (dbPlayer.CharacterType != _requiredCharacterType)
            {
                return $"Only {_requiredCharacterType} character types may access this perk.";
            }

            return string.Empty;
        }

        public string RequirementText => $"Character Type: {Perk.GetCharacterType(_requiredCharacterType).Name}";
    }
}
