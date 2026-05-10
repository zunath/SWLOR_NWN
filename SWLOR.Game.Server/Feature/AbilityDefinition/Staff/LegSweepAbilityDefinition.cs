using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Staff
{
    public class LegSweepAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureWeapon(builder.Create(FeatType.LegSweep1, PerkType.LegSweep).Name("Leg Sweep I").Level(1), SkillType.Staff, 6, 3, 12, SavingThrow.Reflex, typeof(KnockdownStatusEffect), 4);
            ConfigureWeapon(builder.Create(FeatType.LegSweep2, PerkType.LegSweep).Name("Leg Sweep II").Level(2), SkillType.Staff, 16, 3, 15, SavingThrow.Reflex, typeof(KnockdownStatusEffect), 5);
            ConfigureWeapon(builder.Create(FeatType.LegSweep3, PerkType.LegSweep).Name("Leg Sweep III").Level(3), SkillType.Staff, 26, 4, 18, SavingThrow.Reflex, typeof(KnockdownStatusEffect), 7);

            return builder.Build();
        }
    }
}
