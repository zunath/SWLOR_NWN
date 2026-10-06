using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ships
{
    public sealed class SelectiveRecoveryAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder.Create(FeatType.ShipSelectiveRecovery1, PerkType.ShipSelectiveRecovery).Name("Selective Recovery I").Level(1).SkillType(SkillType.SpaceIndustry).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipSelectiveRecovery, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipSelectiveRecovery1));
            builder.Create(FeatType.ShipSelectiveRecovery2, PerkType.ShipSelectiveRecovery).Name("Selective Recovery II").Level(2).SkillType(SkillType.SpaceIndustry).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipSelectiveRecovery, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipSelectiveRecovery2));
            builder.Create(FeatType.ShipSelectiveRecovery3, PerkType.ShipSelectiveRecovery).Name("Selective Recovery III").Level(3).SkillType(SkillType.SpaceIndustry).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipSelectiveRecovery, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipSelectiveRecovery3));
            return builder.Build();
        }
    }
}
