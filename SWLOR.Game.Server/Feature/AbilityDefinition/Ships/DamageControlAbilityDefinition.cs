using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ships
{
    public sealed class DamageControlAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder.Create(FeatType.ShipDamageControl1, PerkType.ShipDamageControl).Name("Damage Control").Level(1).SkillType(SkillType.ShipSystems).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipCapstone, 150f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipDamageControl1));
            return builder.Build();
        }
    }
}
