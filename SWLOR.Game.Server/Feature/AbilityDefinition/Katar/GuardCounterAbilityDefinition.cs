using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Katar
{
    public class GuardCounterAbilityDefinition : IAbilityListDefinition
    {
        private const float GuardedHitWindowSeconds = 8f;

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
            builder
                .Create(FeatType.GuardCounter1, PerkType.GuardCounter)
                .Name("Guard Counter I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.GuardCounter, 30f)
                .HasImpactAction(GuardCounter1ImpactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(3);
        }

        private static void GuardCounter2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.GuardCounter2, PerkType.GuardCounter)
                .Name("Guard Counter II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.GuardCounter, 30f)
                .HasImpactAction(GuardCounter2ImpactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void GuardCounter3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.GuardCounter3, PerkType.GuardCounter)
                .Name("Guard Counter III")
                .Level(3)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.GuardCounter, 45f)
                .HasImpactAction(GuardCounter3ImpactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void GuardCounter1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyGuardCounter(activator, target, targetLocation, 8, 16, false);
        }

        private static void GuardCounter2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyGuardCounter(activator, target, targetLocation, 18, 30, false);
        }

        private static void GuardCounter3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyGuardCounter(activator, target, targetLocation, 28, 45, true);
        }

        private static void ApplyGuardCounter(
            uint activator,
            uint target,
            Location targetLocation,
            int baseDamage,
            int guardedDamage,
            bool dazesAfterGuard)
        {
            var hasRecentGuard = Combat.HasRecentGuardedHit(activator, GuardedHitWindowSeconds);
            var statusEffect = hasRecentGuard && dazesAfterGuard
                ? typeof(DazedStatusEffect)
                : null;
            var duration = statusEffect == null ? 0 : 3;

            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Katar,
                hasRecentGuard ? guardedDamage : baseDamage,
                duration,
                statusEffect,
                false);
        }
    }
}
