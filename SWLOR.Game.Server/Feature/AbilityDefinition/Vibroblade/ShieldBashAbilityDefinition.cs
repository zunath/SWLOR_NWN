using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroblade
{
    public class ShieldBashAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureWeapon(builder.Create(FeatType.ShieldBash1, PerkType.ShieldBash).Name("Shield Bash I").Level(1), SkillType.Vibroblade, 12, 3, 12, SavingThrow.Will, typeof(DazedStatusEffect), 4);
            ConfigureWeapon(builder.Create(FeatType.ShieldBash2, PerkType.ShieldBash).Name("Shield Bash II").Level(2), SkillType.Vibroblade, 24, 6, 14, SavingThrow.Will, typeof(DazedStatusEffect), 6);
            ConfigureWeapon(builder.Create(FeatType.ShieldBash3, PerkType.ShieldBash).Name("Shield Bash III").Level(3), SkillType.Vibroblade, 36, 3, 16, SavingThrow.Will, typeof(StunnedStatusEffect), 8);

            return builder.Build();
        }
    }
}
