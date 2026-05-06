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
    public class RicochetTossAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            RicochetToss1(builder);
            RicochetToss2(builder);

            return builder.Build();
        }

        private static void RicochetToss1(AbilityBuilder builder)
        {
            builder.Create(FeatType.RicochetToss1, PerkType.RicochetToss)
                .Name("Ricochet Toss I")
                .Level(1)
                .HasActivationDelay(0f)
                .RequiresTarget()
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void RicochetToss2(AbilityBuilder builder)
        {
            builder.Create(FeatType.RicochetToss2, PerkType.RicochetToss)
                .Name("Ricochet Toss II")
                .Level(2)
                .HasActivationDelay(0f)
                .RequiresTarget()
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            switch (level)
            {
                case 1:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Ranged, 15, 0, 0, SavingThrow.Will, StatusEffectType.Invalid, AbilityControlEffect.None, false);
                    break;
                case 2:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Ranged, 24, 0, 0, SavingThrow.Will, StatusEffectType.Invalid, AbilityControlEffect.None, false);
                    break;
            }
        }
    }
}
