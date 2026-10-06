using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ships
{
    public sealed class CargoHandlingAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder.Create(FeatType.ShipCargoHandling1, PerkType.ShipCargoHandling).Name("Cargo Handling I").Level(1).SkillType(SkillType.SpaceIndustry).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipCargoHandling, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipCargoHandling1));
            builder.Create(FeatType.ShipCargoHandling2, PerkType.ShipCargoHandling).Name("Cargo Handling II").Level(2).SkillType(SkillType.SpaceIndustry).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipCargoHandling, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipCargoHandling2));
            builder.Create(FeatType.ShipCargoHandling3, PerkType.ShipCargoHandling).Name("Cargo Handling III").Level(3).SkillType(SkillType.SpaceIndustry).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipCargoHandling, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipCargoHandling3));
            return builder.Build();
        }
    }
}
