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
    public class FlashTossAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            FlashToss1(builder);
            FlashToss2(builder);

            return builder.Build();
        }

        private static void FlashToss1(AbilityBuilder builder)
        {
            builder.Create(FeatType.FlashToss1, PerkType.FlashToss)
                .Name("Flash Toss I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }

        private static void FlashToss2(AbilityBuilder builder)
        {
            builder.Create(FeatType.FlashToss2, PerkType.FlashToss)
                .Name("Flash Toss II")
                .Level(2)
                .HasActivationDelay(0f)
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
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Ranged, 6, 6, 12, SavingThrow.Fortitude, StatusEffectType.Invalid, AbilityControlEffect.Blind, true);
                    break;
                case 2:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Ranged, 22, 10, 16, SavingThrow.Fortitude, StatusEffectType.Invalid, AbilityControlEffect.Blind, true);
                    break;
            }
        }
    }
}
