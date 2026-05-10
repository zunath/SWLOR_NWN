using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
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

            ConfigureWeapon(builder.Create(FeatType.StrikingCobra1, PerkType.StrikingCobra).Name("Striking Cobra I").Level(1), SkillType.Katar, 8, 30, typeof(PoisonStatusEffect), 3, damageType: CombatDamageType.Poison);
            ConfigureWeapon(builder.Create(FeatType.StrikingCobra2, PerkType.StrikingCobra).Name("Striking Cobra II").Level(2), SkillType.Katar, 18, 60, typeof(PoisonStatusEffect), 5, damageType: CombatDamageType.Poison);
            ConfigureWeapon(builder.Create(FeatType.StrikingCobra3, PerkType.StrikingCobra).Name("Striking Cobra III").Level(3), SkillType.Katar, 28, 60, typeof(PoisonStatusEffect), 7, damageType: CombatDamageType.Poison);

            return builder.Build();
        }
    }
}
