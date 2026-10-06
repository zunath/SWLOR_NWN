using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ships
{
    public sealed class PerfectSolutionAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder.Create(FeatType.ShipPerfectSolution1, PerkType.ShipPerfectSolution).Name("Perfect Solution").Level(1).SkillType(SkillType.Gunnery).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipCapstone, 120f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipPerfectSolution1));
            return builder.Build();
        }
    }
}
