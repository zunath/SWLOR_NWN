using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ships
{
    public sealed class CarefulDismantlingAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder.Create(FeatType.ShipCarefulDismantling1, PerkType.ShipCarefulDismantling).Name("Careful Dismantling I").Level(1).SkillType(SkillType.SpaceIndustry).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipCarefulDismantling, 36f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipCarefulDismantling1));
            builder.Create(FeatType.ShipCarefulDismantling2, PerkType.ShipCarefulDismantling).Name("Careful Dismantling II").Level(2).SkillType(SkillType.SpaceIndustry).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipCarefulDismantling, 36f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipCarefulDismantling2));
            builder.Create(FeatType.ShipCarefulDismantling3, PerkType.ShipCarefulDismantling).Name("Careful Dismantling III").Level(3).SkillType(SkillType.SpaceIndustry).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipCarefulDismantling, 36f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipCarefulDismantling3));
            return builder.Build();
        }
    }
}
