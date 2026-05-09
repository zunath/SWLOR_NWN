using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Throwing
{
    public class ThrowingActiveAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureTelegraphedArea(builder.Create(FeatType.ExplosiveToss1, PerkType.ExplosiveToss).Name("Explosive Toss I").Level(1), SkillType.Throwing, CombatImpactAreaShape.Sphere, 8, 0, 0, SavingThrow.Fortitude, null, 3f, 0f, 4);
            ConfigureToggle(builder.Create(FeatType.BombardierStance1, PerkType.BombardierStance).Name("Bombardier Stance").Level(1), typeof(BombardierStanceStatusEffect));
            ConfigureTelegraphedArea(builder.Create(FeatType.ExplosiveToss2, PerkType.ExplosiveToss).Name("Explosive Toss II").Level(2), SkillType.Throwing, CombatImpactAreaShape.Sphere, 16, 0, 0, SavingThrow.Fortitude, null, 3f, 0f, 5);
            ConfigureTelegraphedArea(builder.Create(FeatType.ExplosiveToss3, PerkType.ExplosiveToss).Name("Explosive Toss III").Level(3), SkillType.Throwing, CombatImpactAreaShape.Sphere, 26, 0, 0, SavingThrow.Fortitude, null, 3f, 0f, 7);
            ConfigureTelegraphedArea(builder.Create(FeatType.ExplosiveToss4, PerkType.ExplosiveToss).Name("Explosive Toss IV").Level(4), SkillType.Throwing, CombatImpactAreaShape.Sphere, 38, 15, 16, SavingThrow.Fortitude, typeof(ExposedStatusEffect), 3f, 0f, 9);
            ConfigureWeapon(builder.Create(FeatType.PiercingToss1, PerkType.PiercingToss).Name("Piercing Toss I").Level(1), SkillType.Throwing, 12, 30, 12, SavingThrow.Reflex, typeof(BleedStatusEffect), 4);
            ConfigureToggle(builder.Create(FeatType.DeadeyeStance1, PerkType.DeadeyeStance).Name("Deadeye Stance").Level(1), typeof(DeadeyeStanceStatusEffect));
            ConfigureWeapon(builder.Create(FeatType.PiercingToss2, PerkType.PiercingToss).Name("Piercing Toss II").Level(2), SkillType.Throwing, 21, 60, 15, SavingThrow.Reflex, typeof(BleedStatusEffect), 5);
            ConfigureWeapon(builder.Create(FeatType.PiercingToss3, PerkType.PiercingToss).Name("Piercing Toss III").Level(3), SkillType.Throwing, 34, 60, 18, SavingThrow.Reflex, typeof(BleedStatusEffect), 7);


            return builder.Build();
        }
    }
}
