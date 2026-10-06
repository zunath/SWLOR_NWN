using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ships
{
    public sealed class AnomalyScanAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder.Create(FeatType.ShipAnomalyScan1, PerkType.ShipAnomalyScan).Name("Anomaly Scan I").Level(1).SkillType(SkillType.Astrometrics).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipAnomalyScan, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipAnomalyScan1));
            builder.Create(FeatType.ShipAnomalyScan2, PerkType.ShipAnomalyScan).Name("Anomaly Scan II").Level(2).SkillType(SkillType.Astrometrics).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipAnomalyScan, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipAnomalyScan2));
            builder.Create(FeatType.ShipAnomalyScan3, PerkType.ShipAnomalyScan).Name("Anomaly Scan III").Level(3).SkillType(SkillType.Astrometrics).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipAnomalyScan, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipAnomalyScan3));
            return builder.Build();
        }
    }
}
