using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ships
{
    public sealed class RecoverySweepAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder.Create(FeatType.ShipRecoverySweep1, PerkType.ShipRecoverySweep).Name("Recovery Sweep I").Level(1).SkillType(SkillType.SpaceIndustry).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipRecoverySweep, 45f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipRecoverySweep1));
            builder.Create(FeatType.ShipRecoverySweep2, PerkType.ShipRecoverySweep).Name("Recovery Sweep II").Level(2).SkillType(SkillType.SpaceIndustry).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipRecoverySweep, 45f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipRecoverySweep2));
            builder.Create(FeatType.ShipRecoverySweep3, PerkType.ShipRecoverySweep).Name("Recovery Sweep III").Level(3).SkillType(SkillType.SpaceIndustry).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipRecoverySweep, 45f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipRecoverySweep3));
            return builder.Build();
        }
    }
}
