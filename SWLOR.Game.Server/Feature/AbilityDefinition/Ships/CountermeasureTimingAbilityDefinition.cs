using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ships
{
    public sealed class CountermeasureTimingAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder.Create(FeatType.ShipCountermeasureTiming1, PerkType.ShipCountermeasureTiming).Name("Countermeasure Timing I").Level(1).SkillType(SkillType.Astrometrics).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipCountermeasureTiming, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipCountermeasureTiming1));
            builder.Create(FeatType.ShipCountermeasureTiming2, PerkType.ShipCountermeasureTiming).Name("Countermeasure Timing II").Level(2).SkillType(SkillType.Astrometrics).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipCountermeasureTiming, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipCountermeasureTiming2));
            builder.Create(FeatType.ShipCountermeasureTiming3, PerkType.ShipCountermeasureTiming).Name("Countermeasure Timing III").Level(3).SkillType(SkillType.Astrometrics).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipCountermeasureTiming, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipCountermeasureTiming3));
            return builder.Build();
        }
    }
}
