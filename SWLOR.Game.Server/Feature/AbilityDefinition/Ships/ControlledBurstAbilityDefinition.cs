using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ships
{
    public sealed class ControlledBurstAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder.Create(FeatType.ShipControlledBurst1, PerkType.ShipControlledBurst).Name("Controlled Burst I").Level(1).SkillType(SkillType.Gunnery).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipControlledBurst, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipControlledBurst1));
            builder.Create(FeatType.ShipControlledBurst2, PerkType.ShipControlledBurst).Name("Controlled Burst II").Level(2).SkillType(SkillType.Gunnery).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipControlledBurst, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipControlledBurst2));
            builder.Create(FeatType.ShipControlledBurst3, PerkType.ShipControlledBurst).Name("Controlled Burst III").Level(3).SkillType(SkillType.Gunnery).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipControlledBurst, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipControlledBurst3));
            return builder.Build();
        }
    }
}
