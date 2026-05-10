using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Throwing
{
    public class PinningTossAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            PinningToss1(builder);
            PinningToss2(builder);
            PinningToss3(builder);

            return builder.Build();
        }

        private static void PinningToss1(AbilityBuilder builder)
        {
            builder.Create(FeatType.PinningToss1, PerkType.PinningToss)
                .Name("Pinning Toss I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.PinningToss, 30f)
                .HasImpactAction(ImpactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }

        private static void PinningToss2(AbilityBuilder builder)
        {
            builder.Create(FeatType.PinningToss2, PerkType.PinningToss)
                .Name("Pinning Toss II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.PinningToss, 30f)
                .HasImpactAction(ImpactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(6);
        }

        private static void PinningToss3(AbilityBuilder builder)
        {
            builder.Create(FeatType.PinningToss3, PerkType.PinningToss)
                .Name("Pinning Toss III")
                .Level(3)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.PinningToss, 30f)
                .HasImpactAction(ImpactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            switch (level)
            {
                case 1:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Throwing, 8, 12, typeof(DisorientedStatusEffect), false);
                    break;
                case 2:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Throwing, 18, 15, typeof(DisorientedStatusEffect), false);
                    break;
                case 3:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Throwing, 30, 20, typeof(DisorientedStatusEffect), false);
                    break;
            }
        }
    }
}
