using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Game.Server.Service.CraftService
{
    public class RecipePerkRequirement: IRecipeRequirement
    {
        private readonly PerkType _perk;
        private readonly int _requiredLevel;
        private readonly PerkDetail _perkDetail;
        private readonly IPerkService _perkService;

        public RecipePerkRequirement(PerkType perk, int requiredLevel, IPerkService perkService)
        {
            _perk = perk;
            _requiredLevel = requiredLevel;
            _perkService = perkService;
            _perkDetail = _perkService.GetPerkDetails(_perk);
        }

        public string CheckRequirements(uint player)
        {
            var effectiveLevel = _perkService.GetPerkLevel(player, _perk);

            if (effectiveLevel < _requiredLevel)
            {
                return $"{_perkDetail.Name} must be level {_requiredLevel}.";
            }

            return string.Empty;
        }

        public string RequirementText => $"{_perkDetail.Name} lvl {_requiredLevel}";
    }
}
