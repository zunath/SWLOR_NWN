using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ships
{
    public sealed class HazardRunAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder.Create(FeatType.ShipHazardRun1, PerkType.ShipHazardRun).Name("Hazard Run I").Level(1).SkillType(SkillType.Piloting).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipHazardRun, 36f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipHazardRun1));
            builder.Create(FeatType.ShipHazardRun2, PerkType.ShipHazardRun).Name("Hazard Run II").Level(2).SkillType(SkillType.Piloting).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipHazardRun, 36f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipHazardRun2));
            builder.Create(FeatType.ShipHazardRun3, PerkType.ShipHazardRun).Name("Hazard Run III").Level(3).SkillType(SkillType.Piloting).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipHazardRun, 36f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipHazardRun3));
            return builder.Build();
        }
    }
}
