using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ships
{
    public sealed class TorpedoRunAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder.Create(FeatType.ShipTorpedoRun1, PerkType.ShipTorpedoRun).Name("Torpedo Run I").Level(1).SkillType(SkillType.Gunnery).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipTorpedoRun, 36f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipTorpedoRun1));
            builder.Create(FeatType.ShipTorpedoRun2, PerkType.ShipTorpedoRun).Name("Torpedo Run II").Level(2).SkillType(SkillType.Gunnery).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipTorpedoRun, 36f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipTorpedoRun2));
            builder.Create(FeatType.ShipTorpedoRun3, PerkType.ShipTorpedoRun).Name("Torpedo Run III").Level(3).SkillType(SkillType.Gunnery).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipTorpedoRun, 36f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipTorpedoRun3));
            return builder.Build();
        }
    }
}
