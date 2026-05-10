using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Rifle
{
    public class TranqConeAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureTelegraphedArea(builder.Create(FeatType.TranqCone1, PerkType.TranqCone).Name("Tranq Cone I").Level(1), SkillType.Rifle, CombatImpactAreaShape.Cone, 0, 8, typeof(DazedStatusEffect), 8f, 6f, 6);
            ConfigureTelegraphedArea(builder.Create(FeatType.TranqCone2, PerkType.TranqCone).Name("Tranq Cone II").Level(2), SkillType.Rifle, CombatImpactAreaShape.Cone, 0, 10, typeof(DazedStatusEffect), 10f, 7f, 8);

            return builder.Build();
        }
    }
}
