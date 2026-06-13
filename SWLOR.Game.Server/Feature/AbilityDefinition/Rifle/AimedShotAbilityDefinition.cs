using System.Collections.Generic;
using System;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Rifle
{
    public class AimedShotAbilityDefinition : IAbilityListDefinition
    {
        private const float LongRangeThreshold = 8f;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            AimedShot1(builder);
            AimedShot2(builder);
            AimedShot3(builder);

            return builder.Build();
        }

        private static void AimedShot1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.AimedShot1, PerkType.AimedShot)
                .Name("Aimed Shot I")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.AimedShot, activator => GetAimedShotRecastDelay(activator))
                .SkillType(SkillType.Rifle)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.PointPistol)
                .HasMaxRange(RifleAbilityRange.Standard)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(AimedShot1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }

        private static void AimedShot2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.AimedShot2, PerkType.AimedShot)
                .Name("Aimed Shot II")
                .Level(2)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.AimedShot, activator => GetAimedShotRecastDelay(activator))
                .SkillType(SkillType.Rifle)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.PointPistol)
                .HasMaxRange(RifleAbilityRange.Standard)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(AimedShot2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(6);
        }

        private static void AimedShot3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.AimedShot3, PerkType.AimedShot)
                .Name("Aimed Shot III")
                .Level(3)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.AimedShot, activator => GetAimedShotRecastDelay(activator))
                .SkillType(SkillType.Rifle)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.PointPistol)
                .HasMaxRange(RifleAbilityRange.Standard)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(AimedShot3ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void AimedShot1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyAimedShot(activator, target, targetLocation, 18, 10);
        }

        private static float GetAimedShotRecastDelay(uint activator)
        {
            return Math.Max(0f, 30f + Combat.GetAbilityRecastDelayFlatAdjustment(activator, PerkType.AimedShot));
        }

        private static void AimedShot2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyAimedShot(activator, target, targetLocation, 32, 16);
        }

        private static void AimedShot3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyAimedShot(activator, target, targetLocation, 46, 24);
        }

        private static void ApplyAimedShot(
            uint activator,
            uint target,
            Location targetLocation,
            int baseDamage,
            int longRangeBonusDamage)
        {
            if (IsLongRangeShot(activator, target))
                baseDamage += longRangeBonusDamage;

            Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Rifle, baseDamage, 0, null, false);
        }

        private static bool IsLongRangeShot(uint activator, uint target)
        {
            return GetIsObjectValid(activator) &&
                   GetIsObjectValid(target) &&
                   GetArea(activator) == GetArea(target) &&
                   GetDistanceBetween(activator, target) > LongRangeThreshold;
        }
    }
}
