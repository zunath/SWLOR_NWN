using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroblade
{
    public class SavageCleaveAbilityDefinition : IAbilityListDefinition
    {
        private const float Radius = 5f;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureSavageCleave(builder, FeatType.SavageCleave1, Spell.SavageCleave1, "Savage Cleave I", 1, 10, 4, false);
            ConfigureSavageCleave(builder, FeatType.SavageCleave2, Spell.SavageCleave2, "Savage Cleave II", 2, 15, 6, true);
            ConfigureSavageCleave(builder, FeatType.SavageCleave3, Spell.SavageCleave3, "Savage Cleave III", 3, 20, 8, true);

            return builder.Build();
        }

        private static void ConfigureSavageCleave(
            AbilityBuilder builder,
            FeatType featType,
            Spell spell,
            string name,
            int level,
            int baseDamage,
            int stamina,
            bool restoresSecondaryTargetStamina)
        {
            builder
                .Create(featType, PerkType.SavageCleave)
                .Name(name)
                .Level(level)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.Whirlwind)
                .HasRecastDelay(RecastGroup.SavageCleave, 24f)
                .SkillType(SkillType.Vibroblade)
                .HasImpactAction((activator, target, effectivePerkLevel, targetLocation) =>
                    ApplySavageCleave(activator, target, targetLocation, baseDamage, restoresSecondaryTargetStamina))
                .HasTargetingSphere(
                    spell,
                    Radius,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(stamina);
        }

        private static void ApplySavageCleave(
            uint activator,
            uint target,
            Location targetLocation,
            int baseDamage,
            bool restoresSecondaryTargetStamina)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Vibroblade,
                baseDamage,
                0,
                null,
                CombatImpactAreaShape.Sphere,
                0.25f,
                Radius,
                0f,
                centerOnActivator: true,
                afterImpactAction: restoresSecondaryTargetStamina
                    ? summary => RestoreSecondaryTargetStamina(activator, summary)
                    : null);
        }

        private static void RestoreSecondaryTargetStamina(uint activator, AbilityImpactSummary summary)
        {
            if (summary == null)
                return;

            var secondaryTargets = Math.Max(0, summary.ImpactedTargetCount - 1);
            var amount = secondaryTargets * 2;
            if (amount > 0)
            {
                Stat.RestoreStamina(activator, amount);
            }
        }
    }
}
