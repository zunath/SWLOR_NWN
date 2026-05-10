using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroknife
{
    public class HamstringAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureWeapon(builder.Create(FeatType.Hamstring1, PerkType.Hamstring).Name("Hamstring I").Level(1), SkillType.Vibroknife, 8, 12, 10, SavingThrow.Reflex, typeof(HamstringStatusEffect), 4);
            ConfigureWeapon(builder.Create(FeatType.Hamstring2, PerkType.Hamstring).Name("Hamstring II").Level(2), SkillType.Vibroknife, 18, 12, 14, SavingThrow.Reflex, typeof(HamstringStatusEffect), 5);
            ConfigureWeapon(builder.Create(FeatType.Hamstring3, PerkType.Hamstring).Name("Hamstring III").Level(3), SkillType.Vibroknife, 28, 12, 18, SavingThrow.Reflex, typeof(HamstringStatusEffect), 7);

            return builder.Build();
        }
    }
}
