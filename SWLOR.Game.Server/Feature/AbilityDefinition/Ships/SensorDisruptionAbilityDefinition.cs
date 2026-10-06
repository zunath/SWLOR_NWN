using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ships
{
    public sealed class SensorDisruptionAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder.Create(FeatType.ShipSensorDisruption1, PerkType.ShipSensorDisruption).Name("Sensor Disruption I").Level(1).SkillType(SkillType.Astrometrics).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipSensorDisruption, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipSensorDisruption1));
            builder.Create(FeatType.ShipSensorDisruption2, PerkType.ShipSensorDisruption).Name("Sensor Disruption II").Level(2).SkillType(SkillType.Astrometrics).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipSensorDisruption, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipSensorDisruption2));
            builder.Create(FeatType.ShipSensorDisruption3, PerkType.ShipSensorDisruption).Name("Sensor Disruption III").Level(3).SkillType(SkillType.Astrometrics).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipSensorDisruption, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipSensorDisruption3));
            return builder.Build();
        }
    }
}
