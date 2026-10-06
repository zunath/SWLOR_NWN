using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ships
{
    public sealed class ShieldRecoveryAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder.Create(FeatType.ShipShieldRecovery1, PerkType.ShipShieldRecovery).Name("Shield Recovery I").Level(1).SkillType(SkillType.ShipSystems).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipShieldRecovery, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipShieldRecovery1));
            builder.Create(FeatType.ShipShieldRecovery2, PerkType.ShipShieldRecovery).Name("Shield Recovery II").Level(2).SkillType(SkillType.ShipSystems).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipShieldRecovery, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipShieldRecovery2));
            builder.Create(FeatType.ShipShieldRecovery3, PerkType.ShipShieldRecovery).Name("Shield Recovery III").Level(3).SkillType(SkillType.ShipSystems).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipShieldRecovery, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipShieldRecovery3));
            return builder.Build();
        }
    }
}
