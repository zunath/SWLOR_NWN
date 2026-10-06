using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ships
{
    public sealed class EmergencyRepairAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder.Create(FeatType.ShipEmergencyRepair1, PerkType.ShipEmergencyRepair).Name("Emergency Repair I").Level(1).SkillType(SkillType.ShipSystems).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipEmergencyRepair, 36f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipEmergencyRepair1));
            builder.Create(FeatType.ShipEmergencyRepair2, PerkType.ShipEmergencyRepair).Name("Emergency Repair II").Level(2).SkillType(SkillType.ShipSystems).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipEmergencyRepair, 36f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipEmergencyRepair2));
            builder.Create(FeatType.ShipEmergencyRepair3, PerkType.ShipEmergencyRepair).Name("Emergency Repair III").Level(3).SkillType(SkillType.ShipSystems).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipEmergencyRepair, 36f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipEmergencyRepair3));
            return builder.Build();
        }
    }
}
