using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ships
{
    public sealed class PrecisionExtractionAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder.Create(FeatType.ShipPrecisionExtraction1, PerkType.ShipPrecisionExtraction).Name("Precision Extraction I").Level(1).SkillType(SkillType.SpaceIndustry).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipPrecisionExtraction, 36f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipPrecisionExtraction1));
            builder.Create(FeatType.ShipPrecisionExtraction2, PerkType.ShipPrecisionExtraction).Name("Precision Extraction II").Level(2).SkillType(SkillType.SpaceIndustry).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipPrecisionExtraction, 36f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipPrecisionExtraction2));
            builder.Create(FeatType.ShipPrecisionExtraction3, PerkType.ShipPrecisionExtraction).Name("Precision Extraction III").Level(3).SkillType(SkillType.SpaceIndustry).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipPrecisionExtraction, 36f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipPrecisionExtraction3));
            return builder.Build();
        }
    }
}
