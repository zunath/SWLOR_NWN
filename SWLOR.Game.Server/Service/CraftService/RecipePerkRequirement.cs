using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Service.CraftService
{
    public class RecipePerkRequirement: IRecipeRequirement
    {
        private readonly PerkType _perk;
        private readonly int _requiredLevel;
        private readonly PerkDetail _perkDetail;

        public RecipePerkRequirement(PerkType perk, int requiredLevel)
        {
            _perk = perk;
            _requiredLevel = requiredLevel;
            _perkDetail = Perk.GetPerkDetails(_perk);
        }

        public string CheckRequirements(uint player)
        {
            var effectiveLevel = Perk.GetEffectivePerkLevel(player, _perk);

            if (effectiveLevel < _requiredLevel)
            {
                return $"{_perkDetail.Name} must be level {_requiredLevel}.";
            }

            return string.Empty;
        }

        public string RequirementText => $"{_perkDetail.Name} lvl {_requiredLevel}";
    }
}
