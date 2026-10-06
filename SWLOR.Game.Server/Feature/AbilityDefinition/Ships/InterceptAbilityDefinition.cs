using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ships
{
    public sealed class InterceptAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder.Create(FeatType.ShipIntercept1, PerkType.ShipIntercept).Name("Intercept I").Level(1).SkillType(SkillType.Piloting).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipIntercept, 24f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipIntercept1));
            builder.Create(FeatType.ShipIntercept2, PerkType.ShipIntercept).Name("Intercept II").Level(2).SkillType(SkillType.Piloting).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipIntercept, 24f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipIntercept2));
            builder.Create(FeatType.ShipIntercept3, PerkType.ShipIntercept).Name("Intercept III").Level(3).SkillType(SkillType.Piloting).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipIntercept, 24f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipIntercept3));
            return builder.Build();
        }
    }
}
