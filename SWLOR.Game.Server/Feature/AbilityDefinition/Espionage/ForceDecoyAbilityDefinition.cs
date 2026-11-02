using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.AbilityDefinition.Force;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Espionage
{
    /// <summary>
    /// Force Sensitive counterpart to Tactical Escape: drops the caster's enmity and clouds the
    /// minds of organic enemies attacking them, lowering their Accuracy against the caster.
    /// </summary>
    public class ForceDecoyAbilityDefinition : IAbilityListDefinition
    {
        public const float DecoyDurationSeconds = 30f;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ForceDecoy(builder, FeatType.ForceDecoy1, "Force Decoy I", 1, 4, 35, 8, false);
            ForceDecoy(builder, FeatType.ForceDecoy2, "Force Decoy II", 2, 6, 60, 12, true);

            return builder.Build();
        }

        private static void ForceDecoy(
            AbilityBuilder builder,
            FeatType feat,
            string name,
            int level,
            int fp,
            int enmityReductionPercent,
            int accuracyPenaltyPercent,
            bool removesMovementSlows)
        {
            builder
                .Create(feat, PerkType.ForceDecoy)
                .DisplaysVisualEffectOnSuccessfulImpact(VisualEffect.Vfx_Ability_TacticalEscape)
                .Name(name)
                .Level(level)
                .HasActivationDelay(0f)
                .UsesImmediateAuthoredAnimation()
                .UsesAnimation(Animation.CastOutAnimation)
                .HasRecastDelay(RecastGroup.ForceDecoy, 45f)
                .SkillType(SkillType.Espionage)
                .IsForcePower()
                .PlaysSoundOnImpact("ksfx_frc_mind")
                .HasImpactAction((activator, _, _, _) =>
                    ApplyForceDecoy(activator, enmityReductionPercent, accuracyPenaltyPercent, removesMovementSlows))
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(fp);
        }

        private static void ApplyForceDecoy(
            uint activator,
            int enmityReductionPercent,
            int accuracyPenaltyPercent,
            bool removesMovementSlows)
        {
            // Read who is attacking the caster before the enmity drop sends them elsewhere.
            var attackers = GetOrganicEnemiesTargeting(activator).ToList();

            Enmity.ReduceEnmityOnAll(activator, enmityReductionPercent);

            foreach (var enemy in attackers)
            {
                var applied = StatusEffect.ApplyStatusEffect(
                    activator,
                    enemy,
                    new DecoyStatusEffect(accuracyPenaltyPercent),
                    DecoyDurationSeconds);
                if (applied)
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Confusion_S), enemy);
            }

            if (removesMovementSlows)
            {
                StatusEffect.RemoveStatusEffectsWithNegativeStat(activator, StatType.MovementSpeedPercentAdjustment);
            }

            Ability.PlaySuccessfulImpactVisualEffect(activator, activator);
        }

        /// <summary>
        /// Living, organic enemies holding the caster on their enmity table whose current or
        /// queued attack target is the caster. Droids still lose enmity but are never decoyed.
        /// </summary>
        public static IEnumerable<uint> GetOrganicEnemiesTargeting(uint activator)
        {
            foreach (var enemy in Enmity.GetEnmityTowardsAllEnemies(activator).Keys)
            {
                if (!GetIsObjectValid(enemy) ||
                    GetIsDead(enemy) ||
                    !MindTrickAbilityDefinition.IsNonMechanical(enemy))
                    continue;

                if (IsTargeting(enemy, activator))
                    yield return enemy;
            }
        }

        private static bool IsTargeting(uint enemy, uint activator)
        {
            return GetAttackTarget(enemy) == activator ||
                   ActionInterruption.GetQueuedAttackTarget(enemy) == activator ||
                   Enmity.GetHighestEnmityAttackTarget(enemy) == activator;
        }
    }
}
