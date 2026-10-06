using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ships
{
    public sealed class EvasiveManeuverAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder.Create(FeatType.ShipEvasiveManeuver1, PerkType.ShipEvasiveManeuver).Name("Evasive Maneuver I").Level(1).SkillType(SkillType.Piloting).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipEvasiveManeuver, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipEvasiveManeuver1));
            builder.Create(FeatType.ShipEvasiveManeuver2, PerkType.ShipEvasiveManeuver).Name("Evasive Maneuver II").Level(2).SkillType(SkillType.Piloting).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipEvasiveManeuver, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipEvasiveManeuver2));
            builder.Create(FeatType.ShipEvasiveManeuver3, PerkType.ShipEvasiveManeuver).Name("Evasive Maneuver III").Level(3).SkillType(SkillType.Piloting).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipEvasiveManeuver, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipEvasiveManeuver3));
            return builder.Build();
        }
    }
}
