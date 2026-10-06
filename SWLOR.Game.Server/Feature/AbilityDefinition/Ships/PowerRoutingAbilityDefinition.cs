using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ships
{
    public sealed class PowerRoutingAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder.Create(FeatType.ShipPowerRouting1, PerkType.ShipPowerRouting).Name("Power Routing I").Level(1).SkillType(SkillType.ShipSystems).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipPowerRouting, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipPowerRouting1));
            builder.Create(FeatType.ShipPowerRouting2, PerkType.ShipPowerRouting).Name("Power Routing II").Level(2).SkillType(SkillType.ShipSystems).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipPowerRouting, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipPowerRouting2));
            builder.Create(FeatType.ShipPowerRouting3, PerkType.ShipPowerRouting).Name("Power Routing III").Level(3).SkillType(SkillType.ShipSystems).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipPowerRouting, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipPowerRouting3));
            return builder.Build();
        }
    }
}
