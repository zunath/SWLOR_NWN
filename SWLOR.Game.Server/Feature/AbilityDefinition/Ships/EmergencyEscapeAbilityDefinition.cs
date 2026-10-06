using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ships
{
    public sealed class EmergencyEscapeAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder.Create(FeatType.ShipEmergencyEscape1, PerkType.ShipEmergencyEscape).Name("Emergency Escape I").Level(1).SkillType(SkillType.Piloting).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipEmergencyEscape, 60f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipEmergencyEscape1));
            builder.Create(FeatType.ShipEmergencyEscape2, PerkType.ShipEmergencyEscape).Name("Emergency Escape II").Level(2).SkillType(SkillType.Piloting).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipEmergencyEscape, 60f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipEmergencyEscape2));
            builder.Create(FeatType.ShipEmergencyEscape3, PerkType.ShipEmergencyEscape).Name("Emergency Escape III").Level(3).SkillType(SkillType.Piloting).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipEmergencyEscape, 60f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipEmergencyEscape3));
            return builder.Build();
        }
    }
}
