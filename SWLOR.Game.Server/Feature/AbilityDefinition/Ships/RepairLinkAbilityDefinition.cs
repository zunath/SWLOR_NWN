using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ships
{
    public sealed class RepairLinkAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder.Create(FeatType.ShipRepairLink1, PerkType.ShipRepairLink).Name("Repair Link I").Level(1).SkillType(SkillType.ShipSystems).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipRepairLink, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipRepairLink1));
            builder.Create(FeatType.ShipRepairLink2, PerkType.ShipRepairLink).Name("Repair Link II").Level(2).SkillType(SkillType.ShipSystems).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipRepairLink, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipRepairLink2));
            builder.Create(FeatType.ShipRepairLink3, PerkType.ShipRepairLink).Name("Repair Link III").Level(3).SkillType(SkillType.ShipSystems).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipRepairLink, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipRepairLink3));
            return builder.Build();
        }
    }
}
