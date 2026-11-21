using SWLOR.Game.Server.Core;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.WeaponAbility
{
    /// <summary>
    /// What a profile adds to an ability's damage, percent damage, critical rate and defense ignore, and the idle-window snapshots taken at activation.
    /// </summary>
    public class WeaponAbilityDamageModifiers
    {
        private ICombatAbilityBonusService CombatAbilityBonus { get; }
        private ICombatActivityService CombatActivity { get; }
        private ICombatDefenseMemoryService CombatDefenseMemory { get; }
        private ICombatQueriesService CombatQueries { get; }
        private ICombatResourcesService CombatResources { get; }
        private IPlayerFeedbackService PlayerFeedback { get; }
        private IStatService Stat { get; }
        private IStatusEffectService StatusEffect { get; }
        private WeaponAbilityStatusRiders WeaponAbilityStatusRiders { get; }

        public WeaponAbilityDamageModifiers(ICombatAbilityBonusService combatAbilityBonus, ICombatActivityService combatActivity, ICombatDefenseMemoryService combatDefenseMemory, ICombatQueriesService combatQueries, ICombatResourcesService combatResources, IPlayerFeedbackService playerFeedback, IStatService stat, IStatusEffectService statusEffect, WeaponAbilityStatusRiders weaponAbilityStatusRiders)
        {
            CombatAbilityBonus = combatAbilityBonus;
            CombatActivity = combatActivity;
            CombatDefenseMemory = combatDefenseMemory;
            CombatQueries = combatQueries;
            CombatResources = combatResources;
            PlayerFeedback = playerFeedback;
            Stat = stat;
            StatusEffect = statusEffect;
            WeaponAbilityStatusRiders = weaponAbilityStatusRiders;
        }

        public int GetBaseDamageAdjustment(
            WeaponAbilityProfile profile,
            uint activator,
            uint target,
            WeaponAbilityProfile.ActivationIdleBonusSnapshot activationIdleBonusSnapshot = null)
        {
            var bonus = 0;
            if (profile.ExtraDamageIfRecentTarget != 0 &&
                profile.RecentTargetWindowSeconds > 0f &&
                CombatActivity.HasRecentDamageTarget(activator, target, profile.RecentTargetWindowSeconds))
            {
                bonus += profile.ExtraDamageIfRecentTarget;
            }

            if (profile.ExtraDamageIfHighResources != 0 &&
                profile.HighResourceExtraDamageThresholdPercent > 0 &&
                CombatResources.IsCurrentFPAndStaminaAbovePercent(activator, profile.HighResourceExtraDamageThresholdPercent))
            {
                bonus += profile.ExtraDamageIfHighResources;
            }

            if (profile.ExtraDamageIfBesideOrBehind != 0 &&
                (CombatQueries.IsAttackerBesideTarget(activator, target) ||
                 CombatQueries.IsTargetNotFacingAttacker(activator, target)))
            {
                bonus += profile.ExtraDamageIfBesideOrBehind;
                if (GetIsPC(activator))
                {
                    PlayerFeedback.ShowDiagnosticFloatingText(
                        ColorToken.Combat($"Flanking +{profile.ExtraDamageIfBesideOrBehind} DMG"),
                        activator,
                        false);
                }
            }

            var idleSnapshot = activationIdleBonusSnapshot?.HasSnapshot ??
                               CombatAbilityBonus.HasWeaponAbilityActivationIdleSnapshot(activator);
            var idleDamageBonus = activationIdleBonusSnapshot?.DamageBonus ??
                                  CombatAbilityBonus.GetWeaponAbilityActivationIdleDamageBonus(activator);
            if (profile.ExtraDamageIfIdle != 0 &&
                (idleSnapshot
                    ? idleDamageBonus > 0
                    : IsIdle(profile, activator)))
            {
                bonus += idleSnapshot
                    ? idleDamageBonus
                    : profile.ExtraDamageIfIdle;
                if (GetIsPC(activator) && !string.IsNullOrWhiteSpace(profile.ExtraDamageIfIdleFeedbackLabel))
                {
                    PlayerFeedback.ShowDiagnosticFloatingText(
                        ColorToken.Combat($"{profile.ExtraDamageIfIdleFeedbackLabel} +{profile.ExtraDamageIfIdle} DMG"),
                        activator,
                        false);
                }
            }

            if (profile.ExtraDamageIfTargetBleeding != 0 && WeaponAbilityStatusRiders.IsBleeding(target))
                bonus += profile.ExtraDamageIfTargetBleeding;

            if (profile.ExtraDamageIfTargetDebuffed != 0 && StatusEffect.HasStatusEffectCategory(target, StatusEffectCategory.Debuff))
                bonus += profile.ExtraDamageIfTargetDebuffed;

            if (profile.ExtraDamageIfTargetControlled != 0 && StatusEffect.HasStatusEffectCategory(target, StatusEffectCategory.Control))
                bonus += profile.ExtraDamageIfTargetControlled;

            if (profile.ExtraDamageIfTargetLowHP != 0 && IsTargetBelowThreshold(target, profile.TargetLowHPThresholdPercent))
                bonus += profile.ExtraDamageIfTargetLowHP;

            if (profile.ExtraDamageIfTargetStatusEffect != 0 &&
                profile.ExtraDamageTargetStatusEffect != null &&
                StatusEffect.HasStatusEffect(target, profile.ExtraDamageTargetStatusEffect))
            {
                bonus += profile.ExtraDamageIfTargetStatusEffect;
            }

            if (profile.ExtraDamageIfBehind != 0 && CombatQueries.IsTargetNotFacingAttacker(activator, target))
            {
                bonus += profile.ExtraDamageIfBehind;
                if (GetIsPC(activator) && !string.IsNullOrWhiteSpace(profile.ExtraDamageIfBehindFeedbackLabel))
                {
                    PlayerFeedback.ShowDiagnosticFloatingText(
                        ColorToken.Combat($"{profile.ExtraDamageIfBehindFeedbackLabel} +{profile.ExtraDamageIfBehind} DMG"),
                        activator,
                        false);
                }
            }

            if (profile.ExtraDamageIfSourceStatusEffect != 0 &&
                profile.ExtraDamageSourceStatusEffect != null &&
                StatusEffect.HasStatusEffect(target, profile.ExtraDamageSourceStatusEffect, activator))
            {
                bonus += profile.ExtraDamageIfSourceStatusEffect;
            }

            if (profile.ExtraDamagePerSourceStack != 0 &&
                profile.ExtraDamageSourceStackStatusEffect != null &&
                StatusEffect.GetStatusEffect(target, profile.ExtraDamageSourceStackStatusEffect, activator) is InfectionStatusEffect infection)
            {
                bonus += profile.ExtraDamagePerSourceStack * infection.Stacks;
            }

            if (profile.ExtraDamageIfRecentGuardedHit != 0 &&
                profile.RecentGuardedHitWindowSeconds > 0f &&
                CombatDefenseMemory.HasRecentGuardedHit(activator, profile.RecentGuardedHitWindowSeconds))
            {
                bonus += profile.ExtraDamageIfRecentGuardedHit;
            }

            return bonus;
        }

        public int GetDamagePercentAdjustment(WeaponAbilityProfile profile, uint activator, uint target)
        {
            var adjustment = profile.HPCostPercent > 0
                ? Stat.GetStatAdjustment(activator, StatType.HitPointSpendAbilityDamagePercentAdjustment)
                : 0;
            if (profile.DamagePercentIfTargetDebuffed != 0 && StatusEffect.HasStatusEffectCategory(target, StatusEffectCategory.Debuff))
                adjustment += profile.DamagePercentIfTargetDebuffed;
            if (profile.DamagePercentIfTargetControlled != 0 && StatusEffect.HasStatusEffectCategory(target, StatusEffectCategory.Control))
                adjustment += profile.DamagePercentIfTargetControlled;
            if (profile.DamagePercentIfTargetLowHP != 0 && IsTargetBelowThreshold(target, profile.TargetLowHPThresholdPercent))
                adjustment += profile.DamagePercentIfTargetLowHP;

            return adjustment;
        }

        public int GetCriticalRateAdjustment(
            WeaponAbilityProfile profile,
            uint activator,
            uint target,
            WeaponAbilityProfile.ActivationIdleBonusSnapshot activationIdleBonusSnapshot = null)
        {
            var adjustment = profile.CriticalRatePercentAdjustment;
            var idleSnapshot = activationIdleBonusSnapshot?.HasSnapshot ??
                               CombatAbilityBonus.HasWeaponAbilityActivationIdleSnapshot(activator);
            var idleCriticalRate = activationIdleBonusSnapshot?.CriticalRatePercentAdjustment ??
                                   CombatAbilityBonus.GetWeaponAbilityActivationIdleCriticalRateBonus(activator);
            if (!profile.IsQueuedWeaponAbility && profile.CriticalRateIfIdle != 0 &&
                (idleSnapshot
                    ? idleCriticalRate > 0
                    : IsIdle(profile, activator)))
            {
                adjustment += idleSnapshot
                    ? idleCriticalRate
                    : profile.CriticalRateIfIdle;
                if (GetIsPC(activator) && !string.IsNullOrWhiteSpace(profile.CriticalRateIfIdleFeedbackLabel))
                {
                    PlayerFeedback.ShowDiagnosticFloatingText(
                        ColorToken.Combat($"{profile.CriticalRateIfIdleFeedbackLabel} +{profile.CriticalRateIfIdle}% Critical Rate"),
                        activator,
                        false);
                }
            }
            if (profile.CriticalRateIfNotRecentTarget != 0 &&
                profile.NotRecentTargetWindowSeconds > 0f &&
                !CombatActivity.HasRecentDamageTarget(activator, target, profile.NotRecentTargetWindowSeconds))
            {
                adjustment += profile.CriticalRateIfNotRecentTarget;
                if (!string.IsNullOrWhiteSpace(profile.CriticalRateIfNotRecentTargetFeedbackLabel))
                {
                    PlayerFeedback.ShowDiagnosticFloatingText(
                        ColorToken.Combat($"{profile.CriticalRateIfNotRecentTargetFeedbackLabel} +{profile.CriticalRateIfNotRecentTarget}% Critical Rate"),
                        activator,
                        false);
                }
            }
            if (profile.CriticalRateIfTargetDebuffedOrControlled != 0 &&
                (StatusEffect.HasStatusEffectCategory(target, StatusEffectCategory.Debuff) ||
                 StatusEffect.HasStatusEffectCategory(target, StatusEffectCategory.Control)))
            {
                adjustment += profile.CriticalRateIfTargetDebuffedOrControlled;
            }

            return adjustment;
        }

        public int GetDefenseIgnorePercent(
            WeaponAbilityProfile profile,
            uint activator,
            WeaponAbilityProfile.ActivationIdleBonusSnapshot activationIdleBonusSnapshot = null)
        {
            var adjustment = profile.DefenseIgnorePercent;
            var idleSnapshot = activationIdleBonusSnapshot?.HasSnapshot ??
                               CombatAbilityBonus.HasWeaponAbilityActivationIdleSnapshot(activator);
            var idleDefenseIgnore = activationIdleBonusSnapshot?.DefenseIgnorePercent ??
                                    CombatAbilityBonus.GetWeaponAbilityActivationIdleDefenseIgnorePercent(activator);
            if (profile.DefenseIgnoreIfIdle != 0 &&
                (idleSnapshot
                    ? idleDefenseIgnore > 0
                    : IsIdle(profile, activator)))
            {
                adjustment += idleSnapshot
                    ? idleDefenseIgnore
                    : profile.DefenseIgnoreIfIdle;
            }

            return adjustment;
        }

        public (Type StatusEffect, int DurationSeconds) ResolveImpactStatus(
            WeaponAbilityProfile profile,
            uint activator,
            Type statusEffect,
            int durationSeconds)
        {
            if (profile.RecentGuardedHitStatusEffect == null ||
                profile.RecentGuardedHitStatusDurationSeconds <= 0 ||
                !CombatDefenseMemory.HasRecentGuardedHit(activator, profile.RecentGuardedHitWindowSeconds))
            {
                return (statusEffect, durationSeconds);
            }

            return (profile.RecentGuardedHitStatusEffect, profile.RecentGuardedHitStatusDurationSeconds);
        }

        public void ClearActivationIdleBonusSnapshots(uint activator)
        {
            CombatActivity.ClearAbilityActivationIdleBonuses(activator);
            CombatAbilityBonus.ClearWeaponAbilityActivationIdleBonuses(activator);
        }

        public WeaponAbilityProfile.ActivationIdleBonusSnapshot CaptureActivationIdleBonusSnapshot(uint activator)
        {
            return new WeaponAbilityProfile.ActivationIdleBonusSnapshot
            {
                HasSnapshot = CombatAbilityBonus.HasWeaponAbilityActivationIdleSnapshot(activator),
                DamageBonus = CombatAbilityBonus.GetWeaponAbilityActivationIdleDamageBonus(activator),
                CriticalRatePercentAdjustment = CombatAbilityBonus.GetWeaponAbilityActivationIdleCriticalRateBonus(activator),
                DefenseIgnorePercent = CombatAbilityBonus.GetWeaponAbilityActivationIdleDefenseIgnorePercent(activator)
            };
        }

        public void PrepareQueuedActivation(WeaponAbilityProfile profile, uint activator, SkillType skillType)
        {
            if (!profile.IsQueuedWeaponAbility)
                return;

            CombatAbilityBonus.StoreQueuedWeaponAbilityActivationIdleBonuses(activator, skillType);
            if (profile.CriticalRateIfIdle > 0 && IsIdle(profile, activator))
            {
                CombatAbilityBonus.StoreQueuedWeaponAbilityActivationCriticalRateBonus(
                    activator,
                    skillType,
                    profile.CriticalRateIfIdle);
                if (GetIsPC(activator) && !string.IsNullOrWhiteSpace(profile.CriticalRateIfIdleFeedbackLabel))
                {
                    PlayerFeedback.ShowDiagnosticFloatingText(
                        ColorToken.Combat($"{profile.CriticalRateIfIdleFeedbackLabel} +{profile.CriticalRateIfIdle}% Critical Rate"),
                        activator,
                        false);
                }
            }
        }

        public void PrepareActivationIdleBonuses(WeaponAbilityProfile profile, uint activator)
        {
            CombatAbilityBonus.ClearWeaponAbilityActivationIdleBonuses(activator);
            if (profile.IsQueuedWeaponAbility || profile.IdleWindowSeconds <= 0f ||
                (profile.ExtraDamageIfIdle == 0 && profile.CriticalRateIfIdle == 0 && profile.DefenseIgnoreIfIdle == 0))
                return;

            CombatAbilityBonus.StoreWeaponAbilityActivationIdleBonuses(
                activator,
                IsIdle(profile, activator),
                profile.ExtraDamageIfIdle,
                profile.CriticalRateIfIdle,
                profile.DefenseIgnoreIfIdle);
        }

        private bool IsIdle(WeaponAbilityProfile profile, uint activator)
        {
            if (profile.IdleWindowSeconds <= 0f)
                return false;

            var lastActivity = CombatActivity.GetLastCompletedOffensiveActivityAt(activator);
            return lastActivity == default ||
                   (DateTime.UtcNow - lastActivity).TotalSeconds >= profile.IdleWindowSeconds;
        }

        private static bool IsTargetBelowThreshold(uint target, int thresholdPercent)
        {
            return thresholdPercent > 0 &&
                   GetMaxHitPoints(target) > 0 &&
                   GetCurrentHitPoints(target) <= GetMaxHitPoints(target) * (thresholdPercent / 100f);
        }
    }
}
