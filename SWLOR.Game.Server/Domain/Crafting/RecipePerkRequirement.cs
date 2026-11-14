using SWLOR.Game.Server.Domain.Character;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Domain.Crafting
{
    public class RecipePerkRequirement: IRecipeRequirement
    {
        private readonly PerkType _perkType;
        private readonly int _requiredLevel;
        private readonly string _perkName;

        public RecipePerkRequirement(PerkType perkType, int requiredLevel, string perkName)
        {
            _perkType = perkType;
            _requiredLevel = requiredLevel;
            _perkName = perkName;
        }

        public string CheckRequirements(uint player)
        {
            if (Perk.GetPerkLevel(player, _perkType) < _requiredLevel)
                return RequirementText;

            return string.Empty;
        }

        public string RequirementText => $"Requires {_perkName} {_requiredLevel}.";
    }
}
