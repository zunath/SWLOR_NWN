using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ships
{
    public sealed class ExtractionSurgeAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder.Create(FeatType.ShipExtractionSurge1, PerkType.ShipExtractionSurge).Name("Extraction Surge I").Level(1).SkillType(SkillType.SpaceIndustry).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipExtractionSurge, 45f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipExtractionSurge1));
            builder.Create(FeatType.ShipExtractionSurge2, PerkType.ShipExtractionSurge).Name("Extraction Surge II").Level(2).SkillType(SkillType.SpaceIndustry).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipExtractionSurge, 45f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipExtractionSurge2));
            builder.Create(FeatType.ShipExtractionSurge3, PerkType.ShipExtractionSurge).Name("Extraction Surge III").Level(3).SkillType(SkillType.SpaceIndustry).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipExtractionSurge, 45f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipExtractionSurge3));
            return builder.Build();
        }
    }
}
