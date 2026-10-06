using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ships
{
    public sealed class TrackingSolutionAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder.Create(FeatType.ShipTrackingSolution1, PerkType.ShipTrackingSolution).Name("Tracking Solution I").Level(1).SkillType(SkillType.Gunnery).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipTrackingSolution, 24f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipTrackingSolution1));
            builder.Create(FeatType.ShipTrackingSolution2, PerkType.ShipTrackingSolution).Name("Tracking Solution II").Level(2).SkillType(SkillType.Gunnery).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipTrackingSolution, 24f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipTrackingSolution2));
            builder.Create(FeatType.ShipTrackingSolution3, PerkType.ShipTrackingSolution).Name("Tracking Solution III").Level(3).SkillType(SkillType.Gunnery).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipTrackingSolution, 24f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipTrackingSolution3));
            return builder.Build();
        }
    }
}
