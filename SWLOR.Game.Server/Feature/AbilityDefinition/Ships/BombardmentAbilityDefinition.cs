using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ships
{
    public sealed class BombardmentAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder.Create(FeatType.ShipBombardment1, PerkType.ShipBombardment).Name("Bombardment I").Level(1).SkillType(SkillType.Gunnery).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipBombardment, 45f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipBombardment1));
            builder.Create(FeatType.ShipBombardment2, PerkType.ShipBombardment).Name("Bombardment II").Level(2).SkillType(SkillType.Gunnery).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipBombardment, 45f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipBombardment2));
            builder.Create(FeatType.ShipBombardment3, PerkType.ShipBombardment).Name("Bombardment III").Level(3).SkillType(SkillType.Gunnery).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipBombardment, 45f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipBombardment3));
            return builder.Build();
        }
    }
}
