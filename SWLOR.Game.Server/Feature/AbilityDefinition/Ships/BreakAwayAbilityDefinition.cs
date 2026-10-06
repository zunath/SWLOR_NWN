using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ships
{
    public sealed class BreakAwayAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder.Create(FeatType.ShipBreakAway1, PerkType.ShipBreakAway).Name("Break Away I").Level(1).SkillType(SkillType.Piloting).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipBreakAway, 36f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipBreakAway1));
            builder.Create(FeatType.ShipBreakAway2, PerkType.ShipBreakAway).Name("Break Away II").Level(2).SkillType(SkillType.Piloting).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipBreakAway, 36f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipBreakAway2));
            builder.Create(FeatType.ShipBreakAway3, PerkType.ShipBreakAway).Name("Break Away III").Level(3).SkillType(SkillType.Piloting).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipBreakAway, 36f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipBreakAway3));
            return builder.Build();
        }
    }
}
