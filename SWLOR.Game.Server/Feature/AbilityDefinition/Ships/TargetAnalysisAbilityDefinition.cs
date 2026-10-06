using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ships
{
    public sealed class TargetAnalysisAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder.Create(FeatType.ShipTargetAnalysis1, PerkType.ShipTargetAnalysis).Name("Target Analysis I").Level(1).SkillType(SkillType.Astrometrics).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipTargetAnalysis, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipTargetAnalysis1)).RequiresTarget().IsHostileAbility().HasMaxRange(35f);
            builder.Create(FeatType.ShipTargetAnalysis2, PerkType.ShipTargetAnalysis).Name("Target Analysis II").Level(2).SkillType(SkillType.Astrometrics).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipTargetAnalysis, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipTargetAnalysis2)).RequiresTarget().IsHostileAbility().HasMaxRange(35f);
            builder.Create(FeatType.ShipTargetAnalysis3, PerkType.ShipTargetAnalysis).Name("Target Analysis III").Level(3).SkillType(SkillType.Astrometrics).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipTargetAnalysis, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipTargetAnalysis3)).RequiresTarget().IsHostileAbility().HasMaxRange(35f);
            return builder.Build();
        }
    }
}
