using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ships
{
    public sealed class EfficientTransitAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder.Create(FeatType.ShipEfficientTransit1, PerkType.ShipEfficientTransit).Name("Efficient Transit I").Level(1).SkillType(SkillType.Piloting).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipEfficientTransit, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipEfficientTransit1));
            builder.Create(FeatType.ShipEfficientTransit2, PerkType.ShipEfficientTransit).Name("Efficient Transit II").Level(2).SkillType(SkillType.Piloting).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipEfficientTransit, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipEfficientTransit2));
            builder.Create(FeatType.ShipEfficientTransit3, PerkType.ShipEfficientTransit).Name("Efficient Transit III").Level(3).SkillType(SkillType.Piloting).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipEfficientTransit, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipEfficientTransit3));
            return builder.Build();
        }
    }
}
