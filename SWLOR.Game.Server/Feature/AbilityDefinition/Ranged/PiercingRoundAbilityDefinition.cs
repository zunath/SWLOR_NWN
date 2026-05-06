using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ranged
{
    public class PiercingRoundAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            PiercingRound1(builder);
            PiercingRound2(builder);
            PiercingRound3(builder);

            return builder.Build();
        }

        private static void PiercingRound1(AbilityBuilder builder)
        {
            builder.Create(FeatType.PiercingRound1, PerkType.PiercingRound)
                .Name("Piercing Round I")
                .Level(1)
                .HasActivationDelay(0f)
                .RequiresTarget()
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void PiercingRound2(AbilityBuilder builder)
        {
            builder.Create(FeatType.PiercingRound2, PerkType.PiercingRound)
                .Name("Piercing Round II")
                .Level(2)
                .HasActivationDelay(0f)
                .RequiresTarget()
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(7);
        }

        private static void PiercingRound3(AbilityBuilder builder)
        {
            builder.Create(FeatType.PiercingRound3, PerkType.PiercingRound)
                .Name("Piercing Round III")
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
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Ranged, 14, 12, 12, SavingThrow.Fortitude, StatusEffectType.Invalid, AbilityControlEffect.None, false);
                    break;
                case 2:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Ranged, 26, 12, 15, SavingThrow.Fortitude, StatusEffectType.Invalid, AbilityControlEffect.None, false);
                    break;
                case 3:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Ranged, 38, 15, 18, SavingThrow.Fortitude, StatusEffectType.Invalid, AbilityControlEffect.None, false);
                    break;
            }
        }
    }
}
