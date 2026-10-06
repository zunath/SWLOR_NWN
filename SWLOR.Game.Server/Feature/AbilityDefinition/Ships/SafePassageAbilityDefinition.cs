using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ships
{
    public sealed class SafePassageAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder.Create(FeatType.ShipSafePassage1, PerkType.ShipSafePassage).Name("Safe Passage").Level(1).SkillType(SkillType.Piloting).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipCapstone, 180f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipSafePassage1));
            return builder.Build();
        }
    }
}
