using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ships
{
    public sealed class InterferenceAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder.Create(FeatType.ShipInterference1, PerkType.ShipInterference).Name("Interference").Level(1).SkillType(SkillType.Astrometrics).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipMode, 5f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipInterference1));
            return builder.Build();
        }
    }
}
