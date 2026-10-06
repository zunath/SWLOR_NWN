using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ships
{
    public sealed class SpecialistRecoveryAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder.Create(FeatType.ShipSpecialistRecovery1, PerkType.ShipSpecialistRecovery).Name("Specialist Recovery").Level(1).SkillType(SkillType.SpaceIndustry).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipCapstone, 150f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipSpecialistRecovery1));
            return builder.Build();
        }
    }
}
