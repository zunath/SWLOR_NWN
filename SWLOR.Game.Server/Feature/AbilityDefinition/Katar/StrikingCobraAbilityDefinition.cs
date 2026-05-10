using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Katar
{
    public class StrikingCobraAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureWeapon(builder.Create(FeatType.StrikingCobra1, PerkType.StrikingCobra).Name("Striking Cobra I").Level(1), SkillType.Katar, 8, 30, 12, SavingThrow.Fortitude, typeof(PoisonStatusEffect), 3);
            ConfigureWeapon(builder.Create(FeatType.StrikingCobra2, PerkType.StrikingCobra).Name("Striking Cobra II").Level(2), SkillType.Katar, 18, 60, 15, SavingThrow.Fortitude, typeof(PoisonStatusEffect), 5);
            ConfigureWeapon(builder.Create(FeatType.StrikingCobra3, PerkType.StrikingCobra).Name("Striking Cobra III").Level(3), SkillType.Katar, 28, 60, 20, SavingThrow.Fortitude, typeof(PoisonStatusEffect), 7);

            return builder.Build();
        }
    }
}
