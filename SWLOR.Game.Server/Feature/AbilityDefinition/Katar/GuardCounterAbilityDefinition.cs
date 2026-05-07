using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Katar
{
    public class GuardCounterAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            GuardCounter1(builder);
            GuardCounter2(builder);
            GuardCounter3(builder);

            return builder.Build();
        }

        private static void GuardCounter1(AbilityBuilder builder)
        {
            builder.Create(FeatType.GuardCounter1, PerkType.GuardCounter)
                .Name("Guard Counter I")
                .Level(1)
                .HasActivationDelay(0f)
                .RequiresTarget()
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(3);
        }

        private static void GuardCounter2(AbilityBuilder builder)
        {
            builder.Create(FeatType.GuardCounter2, PerkType.GuardCounter)
                .Name("Guard Counter II")
                .Level(2)
                .HasActivationDelay(0f)
                .RequiresTarget()
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void GuardCounter3(AbilityBuilder builder)
        {
            builder.Create(FeatType.GuardCounter3, PerkType.GuardCounter)
                .Name("Guard Counter III")
                .Level(3)
                .HasActivationDelay(0f)
                .RequiresTarget()
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            switch (level)
            {
                case 1:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Katar, 8, 0, 0, SavingThrow.Will, StatusEffectType.Invalid, AbilityControlEffect.None, false);
                    break;
                case 2:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Katar, 18, 0, 0, SavingThrow.Will, StatusEffectType.Invalid, AbilityControlEffect.None, false);
                    break;
                case 3:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Katar, 28, 3, 16, SavingThrow.Reflex, StatusEffectType.Invalid, AbilityControlEffect.Dazed, false);
                    break;
            }
        }
    }
}
