using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ships
{
    public sealed class RouteSurveyAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder.Create(FeatType.ShipRouteSurvey1, PerkType.ShipRouteSurvey).Name("Route Survey I").Level(1).SkillType(SkillType.Astrometrics).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipRouteSurvey, 45f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipRouteSurvey1));
            builder.Create(FeatType.ShipRouteSurvey2, PerkType.ShipRouteSurvey).Name("Route Survey II").Level(2).SkillType(SkillType.Astrometrics).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipRouteSurvey, 45f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipRouteSurvey2));
            builder.Create(FeatType.ShipRouteSurvey3, PerkType.ShipRouteSurvey).Name("Route Survey III").Level(3).SkillType(SkillType.Astrometrics).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipRouteSurvey, 45f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipRouteSurvey3));
            return builder.Build();
        }
    }
}
