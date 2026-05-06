using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwoHanded
{
    public class SplitGuardStrikeAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SplitGuardStrike1(builder);
            SplitGuardStrike2(builder);
            SplitGuardStrike3(builder);

            return builder.Build();
        }

        private static void SplitGuardStrike1(AbilityBuilder builder)
        {
            builder.Create(FeatType.SplitGuardStrike1, PerkType.SplitGuardStrike)
                .Name("Split Guard Strike I")
                .Level(1)
                .HasActivationDelay(0f)
                .RequiresTarget()
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(3);
        }

        private static void SplitGuardStrike2(AbilityBuilder builder)
        {
            builder.Create(FeatType.SplitGuardStrike2, PerkType.SplitGuardStrike)
                .Name("Split Guard Strike II")
                .Level(2)
                .HasActivationDelay(0f)
                .RequiresTarget()
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void SplitGuardStrike3(AbilityBuilder builder)
        {
            builder.Create(FeatType.SplitGuardStrike3, PerkType.SplitGuardStrike)
                .Name("Split Guard Strike III")
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
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.TwoHanded, 10, 10, 0, SavingThrow.Will, StatusEffectType.Invalid, AbilityControlEffect.None, false);
                    break;
                case 2:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.TwoHanded, 22, 10, 0, SavingThrow.Will, StatusEffectType.Invalid, AbilityControlEffect.None, false);
                    break;
                case 3:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.TwoHanded, 34, 10, 0, SavingThrow.Will, StatusEffectType.Invalid, AbilityControlEffect.None, false);
                    break;
            }
        }
    }
}
