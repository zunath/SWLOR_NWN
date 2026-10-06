using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ships
{
    public sealed class SupportSurgeAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder.Create(FeatType.ShipSupportSurge1, PerkType.ShipSupportSurge).Name("Support Surge I").Level(1).SkillType(SkillType.ShipSystems).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipSupportSurge, 36f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipSupportSurge1));
            builder.Create(FeatType.ShipSupportSurge2, PerkType.ShipSupportSurge).Name("Support Surge II").Level(2).SkillType(SkillType.ShipSystems).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipSupportSurge, 36f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipSupportSurge2));
            builder.Create(FeatType.ShipSupportSurge3, PerkType.ShipSupportSurge).Name("Support Surge III").Level(3).SkillType(SkillType.ShipSystems).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipSupportSurge, 36f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipSupportSurge3));
            return builder.Build();
        }
    }
}
