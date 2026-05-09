using System.Collections.Generic;
using SWLOR.Game.Server.Service;
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

            ConfigureQueuedArea(builder.Create(FeatType.ExplosiveToss1, PerkType.ExplosiveToss).Name("Explosive Toss I").Level(1), 8, 0, 0, null, 4);
            ConfigureToggle(builder.Create(FeatType.BombardierStance1, PerkType.BombardierStance).Name("Bombardier Stance").Level(1), typeof(BombardierStanceStatusEffect));
            ConfigureQueuedArea(builder.Create(FeatType.ExplosiveToss2, PerkType.ExplosiveToss).Name("Explosive Toss II").Level(2), 16, 0, 0, null, 5);
            ConfigureQueuedArea(builder.Create(FeatType.ExplosiveToss3, PerkType.ExplosiveToss).Name("Explosive Toss III").Level(3), 26, 0, 0, null, 7);
            ConfigureQueuedArea(builder.Create(FeatType.ExplosiveToss4, PerkType.ExplosiveToss).Name("Explosive Toss IV").Level(4), 38, 15, 16, typeof(ExposedStatusEffect), 9);
            ConfigureWeapon(builder.Create(FeatType.PiercingToss1, PerkType.PiercingToss).Name("Piercing Toss I").Level(1), SkillType.Throwing, 12, 30, 12, SavingThrow.Reflex, typeof(BleedStatusEffect), 4);
            ConfigureToggle(builder.Create(FeatType.DeadeyeStance1, PerkType.DeadeyeStance).Name("Deadeye Stance").Level(1), typeof(DeadeyeStanceStatusEffect));
            ConfigureWeapon(builder.Create(FeatType.PiercingToss2, PerkType.PiercingToss).Name("Piercing Toss II").Level(2), SkillType.Throwing, 21, 60, 15, SavingThrow.Reflex, typeof(BleedStatusEffect), 5);
            ConfigureWeapon(builder.Create(FeatType.PiercingToss3, PerkType.PiercingToss).Name("Piercing Toss III").Level(3), SkillType.Throwing, 34, 60, 18, SavingThrow.Reflex, typeof(BleedStatusEffect), 7);


            return builder.Build();
        }

        private static void ConfigureQueuedArea(
            AbilityBuilder ability,
            int baseDamage,
            int duration,
            int savingThrowDc,
            Type statusEffect,
            int stamina)
        {
            ability.HasActivationDelay(0f)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    Ability.ApplyTelegraphedCombatImpact(
                        activator,
                        target,
                        targetLocation,
                        SkillType.Throwing,
                        baseDamage,
                        duration,
                        savingThrowDc,
                        SavingThrow.Fortitude,
                        statusEffect,
                        CombatImpactAreaShape.Sphere,
                        0f,
                        3f);
                })
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth();

            if (stamina > 0)
                ability.RequirementStamina(stamina);
        }
    }
}
