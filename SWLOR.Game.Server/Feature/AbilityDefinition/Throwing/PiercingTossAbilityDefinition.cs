using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Throwing
{
    public class PiercingTossAbilityDefinition : IAbilityListDefinition
    {
        private const SkillType Skill = SkillType.Throwing;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            PiercingToss1(builder);
            PiercingToss2(builder);
            PiercingToss3(builder);

            return builder.Build();
        }

        private static void PiercingToss1(AbilityBuilder builder)
        {
            PiercingToss(builder, FeatType.PiercingToss1, "Piercing Toss I", level: 1, stamina: 4, PiercingToss1ImpactAction);
        }

        private static void PiercingToss2(AbilityBuilder builder)
        {
            PiercingToss(builder, FeatType.PiercingToss2, "Piercing Toss II", level: 2, stamina: 5, PiercingToss2ImpactAction);
        }

        private static void PiercingToss3(AbilityBuilder builder)
        {
            PiercingToss(builder, FeatType.PiercingToss3, "Piercing Toss III", level: 3, stamina: 7, PiercingToss3ImpactAction);
        }

        private static void PiercingToss(
            AbilityBuilder builder,
            FeatType feat,
            string name,
            int level,
            int stamina,
            AbilityImpactAction impactAction)
        {
            builder
                .Create(feat, PerkType.PiercingToss)
                .Name(name)
                .Level(level)
                .HasRecastDelay(RecastGroup.PiercingToss, 30f)
                .HasActivationDelay(0f)
                .SkillType(Skill)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.ThrowGrenade)
                .HasMaxRange(ThrowingAbilityRange.Standard)
                .IsSingleTargetAbility()
                .HasImpactAction(impactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(stamina);
        }

        private static void PiercingToss1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyPiercingToss(activator, target, targetLocation, 12, 30);
        }

        private static void PiercingToss2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyPiercingToss(activator, target, targetLocation, 21, 60);
        }

        private static void PiercingToss3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyPiercingToss(activator, target, targetLocation, 34, 60);
        }

        private static void ApplyPiercingToss(uint activator, uint target, Location targetLocation, int baseDamage, int duration)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                Skill,
                baseDamage,
                duration,
                typeof(BleedStatusEffect),
                false);
        }
    }
}
