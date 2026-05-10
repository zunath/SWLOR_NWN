using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Staff
{
    public class SlamAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureWeapon(builder.Create(FeatType.Slam1, PerkType.Slam).Name("Slam I").Level(1), SkillType.Staff, 8, 8, 12, SavingThrow.Fortitude, typeof(BlindStatusEffect), 4);
            ConfigureWeapon(builder.Create(FeatType.Slam2, PerkType.Slam).Name("Slam II").Level(2), SkillType.Staff, 20, 10, 15, SavingThrow.Fortitude, typeof(BlindStatusEffect), 5);
            ConfigureWeapon(builder.Create(FeatType.Slam3, PerkType.Slam).Name("Slam III").Level(3), SkillType.Staff, 32, 12, 18, SavingThrow.Fortitude, typeof(BlindStatusEffect), 7);

            return builder.Build();
        }
    }
}
