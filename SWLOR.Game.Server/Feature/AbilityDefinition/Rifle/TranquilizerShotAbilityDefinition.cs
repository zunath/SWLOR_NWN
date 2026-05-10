using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Rifle
{
    public class TranquilizerShotAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureWeapon(builder.Create(FeatType.TranquilizerShot1, PerkType.TranquilizerShot).Name("Tranquilizer Shot I").Level(1), SkillType.Rifle, 0, 8, typeof(DazedStatusEffect), 4);
            ConfigureWeapon(builder.Create(FeatType.TranquilizerShot2, PerkType.TranquilizerShot).Name("Tranquilizer Shot II").Level(2), SkillType.Rifle, 0, 14, typeof(DazedStatusEffect), 5);

            return builder.Build();
        }
    }
}
