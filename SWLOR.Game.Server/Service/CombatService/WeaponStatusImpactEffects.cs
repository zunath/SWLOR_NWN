using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Service.CombatService
{
    public static class WeaponStatusImpactEffects
    {
        internal static void ApplyKatarVenomCurrentImpactRiders(uint activator, uint target)
        {
            if (StatusEffect.HasStatusEffect(target, typeof(PoisonStatusEffect)))
            {
                WeaponStatusImpactEffects.SpreadPoisonFromTarget(activator, target);
            }
        }

        internal static void ApplyToxicRushDamageDealtEffects(
            uint attacker,
            uint defender,
            CombatDamageDeliveryType deliveryType)
        {
            if (deliveryType == CombatDamageDeliveryType.DamageOverTime)
                return;

            if (!GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                !StatusEffect.HasStatusEffect(defender, typeof(PoisonStatusEffect)))
                return;

            var haste = Stat.GetStatAdjustment(attacker, StatType.KatarToxicRushHastePercentPerStack);
            var attack = Stat.GetStatAdjustment(attacker, StatType.KatarToxicRushAttackPercentPerStack);
            var maxStacks = Stat.GetStatAdjustment(attacker, StatType.KatarToxicRushMaximumStacks);
            var duration = Stat.GetStatAdjustment(attacker, StatType.KatarToxicRushDurationSeconds);
            if (maxStacks <= 0 || duration <= 0 || (haste <= 0 && attack <= 0))
                return;

            var currentStacks = StatusEffect.GetStatusEffect<ToxicRushStatusEffect>(attacker)?.Stacks ?? 0;
            var stacks = Math.Min(maxStacks, currentStacks + 1);
            StatusEffect.ApplyStatusEffect(
                attacker,
                attacker,
                new ToxicRushStatusEffect(stacks, haste, attack),
                duration);

            if (stacks >= maxStacks)
            {
                Stat.RestoreStamina(attacker, 2);
            }
        }

        internal static void SpreadPoisonFromTarget(uint activator, uint target)
        {
            var radius = Stat.GetStatAdjustment(activator, StatType.KatarVenomCurrentPoisonSpreadRadiusMeters);
            var duration = Stat.GetStatAdjustment(activator, StatType.KatarVenomCurrentPoisonSpreadDurationSeconds);
            if (radius <= 0 || duration <= 0)
                return;

            foreach (var nearby in AbilityTargeting.GetHostileTargetsNearLocation(activator, GetLocation(target), radius, 0, target))
            {
                StatusEffect.ApplyStatusEffect(activator, nearby, typeof(PoisonStatusEffect), duration, CombatDamageType.Poison);
            }
        }

        internal static void ApplyLeadershipVanguardImpactRiders(uint activator, uint target)
        {
            var rank = Stat.GetStatAdjustment(activator, StatType.LeadershipVanguardMarkTargetRank);
            if (rank <= 0)
                return;

            StatusEffect.ApplyStatusEffect(
                activator,
                target,
                rank >= 2 ? typeof(MarkTarget2StatusEffect) : typeof(MarkTarget1StatusEffect),
                18f,
                ResistanceType.Mind);
        }

        internal static void ApplyPistolSkirmisherImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!ability.IsHostileAbility)
                return;

            var disorientedDuration = Stat.GetStatAdjustment(activator, StatType.PistolSkirmisherDisorientedDurationSeconds);
            if (disorientedDuration > 0 && (ability.IsAreaAbility || ability.MaxRange <= 5f))
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(DisorientedStatusEffect), disorientedDuration, ResistanceType.Mind);
            }
        }

        internal static void ApplyRiflePacificationImpactRiders(uint activator, uint target)
        {
            if (Stat.GetStatAdjustment(activator, StatType.RiflePacificationNeutralizingShot) > 0)
            {
                StatusEffect.RemoveFirstBeneficialCombatStatusEffect(target, false);
                StatusEffect.ApplyStatusEffect(activator, target, typeof(DisorientedStatusEffect), 30f, ResistanceType.Mind);
            }

            if (Stat.GetStatAdjustment(activator, StatType.RiflePacificationOverwatch) > 0)
            {
                AssignCommand(target, () => ClearAllActions());
                StatusEffect.ApplyStatusEffect(activator, target, new FoggyMindStatusEffect(2), 30f, ResistanceType.Mind);
            }

            var pinningRank = Stat.GetStatAdjustment(activator, StatType.RiflePacificationPinningFireRank);
            if (pinningRank >= 2)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(KnockdownStatusEffect), 30f, ResistanceType.Trauma);
            }
            else if (pinningRank == 1)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(DazedStatusEffect), 30f, ResistanceType.Mind);
            }
        }

        internal static void ApplySaberstaffConduitImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!ability.IsHostileAbility ||
                Stat.GetStatAdjustment(activator, StatType.SaberstaffConduitAreaConduitFlare) <= 0)
            {
                return;
            }

            var duration = Stat.GetStatAdjustment(activator, StatType.SaberstaffConduitFlareForceDisruptionDurationSeconds);
            if (duration > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(ForceDisruptionStatusEffect), duration, CombatDamageType.Force);
            }
        }

        internal static void ApplySaberstaffTempestImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!ability.IsAreaAbility ||
                Stat.GetStatAdjustment(activator, StatType.SaberstaffTempestForceGyre) <= 0)
            {
                return;
            }

            var duration = Stat.GetStatAdjustment(activator, StatType.SaberstaffTempestForceGyreDurationSeconds);
            if (duration > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(ForceErosionStatusEffect), duration, CombatDamageType.Force);
            }
        }

        internal static void ApplySpearDamageImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (Stat.GetStatAdjustment(activator, StatType.SpearDamageBreachStrike) > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(BreachStatusEffect), 30f, CombatDamageType.Physical);
            }

            if (ability.IsAreaAbility && Stat.GetStatAdjustment(activator, StatType.SpearDamageCripplingDefense) > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(CrippledDefenseStatusEffect), 45f, CombatDamageType.Physical);
            }
        }

        internal static void ApplySpearDisablerImpactRiders(
            uint activator,
            uint target,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects)
        {
            var appliesDisruption = WeaponStatusImpactEffects.AbilityAppliedAnyStatus(
                primaryStatusEffect,
                additionalStatusEffects,
                typeof(ForceDisruptionStatusEffect));
            var appliesSuppression = WeaponStatusImpactEffects.AbilityAppliedAnyStatus(
                primaryStatusEffect,
                additionalStatusEffects,
                typeof(ForceSuppressionStatusEffect),
                typeof(DisruptionFieldStatusEffect),
                typeof(ForceDisruptionStatusEffect));

            if (Stat.GetStatAdjustment(activator, StatType.SpearDisablerForceNullification) > 0 && appliesDisruption)
            {
                StatusEffect.ApplyStatusEffect(activator, target, new ForceDisruptionStatusEffect(true), 30f, CombatDamageType.Force);
            }

            if (Stat.GetStatAdjustment(activator, StatType.SpearDisablerForcebane) > 0 && appliesSuppression)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(ForcebaneStatusEffect), 45f, CombatDamageType.Force);
            }

            if (Stat.GetStatAdjustment(activator, StatType.SpearDisablerFractureStrike) > 0 && appliesDisruption)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(FracturedFocusStatusEffect), 30f, CombatDamageType.Force);
            }
        }

        public static void ApplySpearDisablerSuppressionRiders(uint activator, uint target)
        {
            if (!GetIsObjectValid(activator) || !GetIsObjectValid(target))
                return;

            if (Stat.GetStatAdjustment(activator, StatType.SpearDisablerFractureStrike) > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(FracturedFocusStatusEffect), 30f, CombatDamageType.Force);
            }

            if (Stat.GetStatAdjustment(activator, StatType.SpearDisablerForcebane) > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(ForcebaneStatusEffect), 45f, CombatDamageType.Force);
            }
        }

        internal static void ApplyStaffCrusherImpactRiders(uint activator, uint target)
        {
            var duration = Stat.GetStatAdjustment(activator, StatType.StaffCrusherFinisherDazedDurationSeconds);
            if (duration > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(DazedStatusEffect), duration, ResistanceType.Mind);
            }
        }

        internal static void ApplyThrowingDeadeyeImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!ability.IsSingleTargetAbility ||
                Stat.GetStatAdjustment(activator, StatType.ThrowingDeadeyeMarkingToss) <= 0)
            {
                return;
            }

            StatusEffect.ApplyStatusEffect(activator, target, typeof(MarkingTossStatusEffect), 30f, CombatDamageType.Physical);
        }

        internal static void ApplyTwinBladeDuelistImpactRiders(uint activator, uint target, AbilityDetail ability)
        {
            if (!AbilityImpactEffects.AbilityMatchesReversalCutTrigger(activator, ability))
            {
                return;
            }

            var duration = TemporaryStatModifier.Consume(
                activator,
                StatType.TwinBladeDuelistReversalCutDazedDurationSeconds,
                StatType.TwinBladeDuelistReversalCut);
            if (duration > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(DazedStatusEffect), duration, ResistanceType.Mind);
            }
        }

        internal static void ApplyVibroknifeShadowImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!ability.IsSingleTargetAbility ||
                Stat.GetStatAdjustment(activator, StatType.VibroknifeShadowMarkedForDeath) <= 0)
            {
                return;
            }

            StatusEffect.ApplyStatusEffect(activator, target, typeof(MarkedForDeathStatusEffect), 30f, CombatDamageType.Physical);
        }

        internal static void ApplyVibroknifeSaboteurImpactRiders(
            uint activator,
            uint target,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects)
        {
            var toxicCoatingRank = Stat.GetStatAdjustment(activator, StatType.VibroknifeSaboteurToxicCoatingRank);
            if (toxicCoatingRank > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(ToxinStatusEffect), 30f, CombatDamageType.Poison);
            }

            var sapRank = Stat.GetStatAdjustment(activator, StatType.VibroknifeSaboteurSapVitalityRank);
            if (sapRank <= 0 ||
                !WeaponStatusImpactEffects.AbilityAppliedAnyStatusCategory(primaryStatusEffect, additionalStatusEffects, StatusEffectCategory.Debuff) ||
                !CombatStatTriggers.TryUseStatTrigger(target, StatType.VibroknifeSaboteurSapVitalityRank, 6))
            {
                return;
            }

            Stat.ReduceStamina(target, sapRank);
        }

        internal static bool AbilityAppliedAnyStatus(Type primaryStatusEffect, IEnumerable<Type> additionalStatusEffects, params Type[] matches)
        {
            if (primaryStatusEffect != null && matches.Contains(primaryStatusEffect))
                return true;

            return additionalStatusEffects?.Any(matches.Contains) ?? false;
        }

        internal static bool AbilityAppliedAnyStatusCategory(Type primaryStatusEffect, IEnumerable<Type> additionalStatusEffects, StatusEffectCategory category)
        {
            if (WeaponStatusImpactEffects.StatusEffectTypeHasCategory(primaryStatusEffect, category))
                return true;

            return additionalStatusEffects?.Any(statusEffect => WeaponStatusImpactEffects.StatusEffectTypeHasCategory(statusEffect, category)) ?? false;
        }

        internal static bool StatusEffectTypeHasCategory(Type statusEffectType, StatusEffectCategory category)
        {
            if (statusEffectType == null || !typeof(IStatusEffect).IsAssignableFrom(statusEffectType))
                return false;

            var statusEffect = (IStatusEffect)Activator.CreateInstance(statusEffectType);
            return (statusEffect.Categories & category) != 0;
        }

    }
}
