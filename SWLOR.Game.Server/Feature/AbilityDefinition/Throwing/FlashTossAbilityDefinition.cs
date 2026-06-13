using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Throwing
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
            builder
                .Create(FeatType.FlashToss1, PerkType.FlashToss)
                .Name("Flash Toss I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.FlashToss, 45f)
                .SkillType(SkillType.Throwing)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.ThrowGrenade)
                .HasMaxRange(ThrowingAbilityRange.Standard)
                .IsAreaAbility()
                .HasImpactAction(FlashToss1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }

        private static void FlashToss2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.FlashToss2, PerkType.FlashToss)
                .Name("Flash Toss II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.FlashToss, 45f)
                .SkillType(SkillType.Throwing)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.ThrowGrenade)
                .HasMaxRange(ThrowingAbilityRange.Standard)
                .IsAreaAbility()
                .HasImpactAction(FlashToss2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void FlashToss1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.Throwing, 6, 6, typeof(BlindStatusEffect), CombatImpactAreaShape.Sphere, 0.25f, 5f);
        }

        private static void FlashToss2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.Throwing, 22, 10, typeof(BlindStatusEffect), CombatImpactAreaShape.Sphere, 0.25f, 5f);
        }
    }
}
