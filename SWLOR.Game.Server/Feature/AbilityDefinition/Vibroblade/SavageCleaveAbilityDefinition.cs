using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroblade
{
    public class SavageCleaveAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SavageCleave1(builder);
            SavageCleave2(builder);

            return builder.Build();
        }

        private static void SavageCleave1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SavageCleave1, PerkType.SavageCleave)
                .Name("Savage Cleave I")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.Whirlwind)
                .HasRecastDelay(RecastGroup.SavageCleave, 24f)
                .IsAreaAbility()
                .HasImpactAction(SavageCleave1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(7);
        }

        private static void SavageCleave2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SavageCleave2, PerkType.SavageCleave)
                .Name("Savage Cleave II")
                .Level(2)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.Whirlwind)
                .HasRecastDelay(RecastGroup.SavageCleave, 45f)
                .IsAreaAbility()
                .HasImpactAction(SavageCleave1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(12);
        }

        private static void SavageCleave1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Vibroblade,
                25,
                0,
                null,
                CombatImpactAreaShape.Cone,
                0.25f,
                5f,
                5f,
                baseDamageAdjustment: hitTarget => hitTarget != target
                    ? Stat.GetStatAdjustment(activator, StatType.SavageCleaveSecondaryDamageBonus)
                    : 0,
                afterImpactAction: summary => RestoreSecondaryTargetStamina(activator, summary));
        }

        private static void RestoreSecondaryTargetStamina(uint activator, AbilityImpactSummary summary)
        {
            var restorePerTarget = Stat.GetStatAdjustment(activator, StatType.SavageCleaveSecondaryTargetStaminaRestore);
            var maximumRestore = Stat.GetStatAdjustment(activator, StatType.SavageCleaveSecondaryTargetStaminaRestoreMaximum);
            if (restorePerTarget <= 0 || maximumRestore <= 0 || summary == null)
                return;

            var secondaryTargets = Math.Max(0, summary.ImpactedTargetCount - 1);
            var amount = Math.Min(maximumRestore, secondaryTargets * restorePerTarget);
            if (amount > 0)
            {
                Stat.RestoreStamina(activator, amount);
            }
        }
    }
}
