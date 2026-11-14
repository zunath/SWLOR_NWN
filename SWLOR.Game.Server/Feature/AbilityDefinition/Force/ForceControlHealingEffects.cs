using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public static class ForceControlHealingEffects
    {
        private const float SereneFocusDurationSeconds = 30f;
        private const float ForceMendCooldownSeconds = 24f;
        private const float HarmonicRestorationCooldownSeconds = 20f;
        private const float HarmonicRestorationRadius = 5f;

        public static bool IsBelowHalfHP(uint target)
        {
            return GetIsObjectValid(target) &&
                   GetMaxHitPoints(target) > 0 &&
                   GetCurrentHitPoints(target) < GetMaxHitPoints(target) * 0.5f;
        }

        /// <returns>The additional allies healed by Harmonic Restoration.</returns>
        public static IReadOnlyList<uint> ApplyRestorativeControlPower(
            uint activator,
            uint target,
            bool targetWasBelowHalfHP)
        {
            if (!GetIsObjectValid(activator) ||
                !GetIsObjectValid(target) ||
                GetIsDead(target) ||
                GetCurrentHitPoints(target) <= 0)
            {
                return Array.Empty<uint>();
            }

            ApplySereneFocus(activator, target);
            ApplyForceMend(activator, target);
            return ApplyHarmonicRestoration(activator, target, targetWasBelowHalfHP);
        }

        private static void ApplySereneFocus(uint activator, uint target)
        {
            if (Stat.GetStatAdjustment(activator, StatType.ControlHealingSereneFocus) <= 0)
                return;

            // Force Sanctuary heals every three seconds while this effect ticks every six, and a
            // fresh application restarts that cadence, so re-applying on each pulse would leave it
            // never ticking at all. Refresh the running instance instead: that extends its duration
            // without resetting its clock.
            if (StatusEffect.RefreshStatusEffectDuration(
                    target, typeof(SereneFocusStatusEffect), activator, SereneFocusDurationSeconds))
            {
                return;
            }

            // Healing someone else still pays better. A caster healing themselves gets the FP
            // half of the trait rather than nothing, so Light has an FP engine while solo.
            var restoresStamina = target != activator;
            StatusEffect.ApplyStatusEffect(
                activator, target, new SereneFocusStatusEffect(restoresStamina), SereneFocusDurationSeconds);
        }

        private static void ApplyForceMend(uint activator, uint target)
        {
            if (Stat.GetStatAdjustment(activator, StatType.ControlHealingForceMend) <= 0 ||
                !TryUseForceMend(activator, target))
            {
                return;
            }

            StatusEffect.RemoveFirstCleanseableStatusEffect(target, StatusEffectCleanseType.Purify, false);
            AbilityEffectScaling.ApplyActivatedScaledHeal(activator, target, ForceMendHeal);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Remove_Condition), target);
        }

        private static readonly HealAmount ForceMendHeal = new(27, 1.5f);
        private static readonly HealAmount HarmonicRestorationHeal = new(27, 1.5f);

        private static List<uint> ApplyHarmonicRestoration(uint activator, uint target, bool targetWasBelowHalfHP)
        {
            var healed = new List<uint>();
            if (!targetWasBelowHalfHP ||
                Stat.GetStatAdjustment(activator, StatType.HarmonicRestoration) <= 0 ||
                !TryUseHarmonicRestoration(activator))
            {
                return healed;
            }

            foreach (var ally in AbilityTargeting
                         .GetFriendlyTargetsNearLocation(activator, GetLocation(target), HarmonicRestorationRadius)
                         .Where(ally => ally != target)
                         .Take(2))
            {
                AbilityEffectScaling.ApplyActivatedScaledHeal(activator, ally, HarmonicRestorationHeal);
                StatusEffect.ApplyStatusEffect(activator, ally, typeof(HarmonicRestorationStatusEffect), 30f);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_M), ally);
                healed.Add(ally);
            }

            return healed;
        }

        private static bool TryUseForceMend(uint activator, uint target)
        {
            return ForceControlCooldowns.TryUseForceMend(activator, target, DateTime.UtcNow, ForceMendCooldownSeconds);
        }

        private static bool TryUseHarmonicRestoration(uint activator)
        {
            return ForceControlCooldowns.TryUseHarmonicRestoration(activator, DateTime.UtcNow, HarmonicRestorationCooldownSeconds);
        }
    }
}
