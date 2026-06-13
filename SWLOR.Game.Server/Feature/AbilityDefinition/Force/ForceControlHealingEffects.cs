using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public static class ForceControlHealingEffects
    {
        private const float SereneFocusDurationSeconds = 12f;
        private const float ForceMendCooldownSeconds = 24f;
        private const float HarmonicRestorationCooldownSeconds = 20f;
        private const float HarmonicRestorationRadius = 5f;

        private static readonly Dictionary<(uint, uint), DateTime> _forceMendCooldowns = new();
        private static readonly Dictionary<uint, DateTime> _harmonicRestorationCooldowns = new();

        public static bool IsBelowHalfHP(uint target)
        {
            return GetIsObjectValid(target) &&
                   GetMaxHitPoints(target) > 0 &&
                   GetCurrentHitPoints(target) < GetMaxHitPoints(target) * 0.5f;
        }

        public static void ApplyRestorativeControlPower(
            uint activator,
            uint target,
            bool targetWasBelowHalfHP)
        {
            if (!GetIsObjectValid(activator) ||
                !GetIsObjectValid(target) ||
                GetIsDead(target) ||
                GetCurrentHitPoints(target) <= 0)
            {
                return;
            }

            ApplySereneFocus(activator, target);
            ApplyForceMend(activator, target);
            ApplyHarmonicRestoration(activator, target, targetWasBelowHalfHP);
        }

        private static void ApplySereneFocus(uint activator, uint target)
        {
            if (target == activator ||
                Stat.GetStatAdjustment(activator, StatType.ControlHealingSereneFocus) <= 0)
            {
                return;
            }

            StatusEffect.ApplyStatusEffect(activator, target, typeof(SereneFocusStatusEffect), SereneFocusDurationSeconds);
        }

        private static void ApplyForceMend(uint activator, uint target)
        {
            if (Stat.GetStatAdjustment(activator, StatType.ControlHealingForceMend) <= 0 ||
                !TryUseForceMend(activator, target))
            {
                return;
            }

            StatusEffect.RemoveFirstCleanseableStatusEffect(target, StatusEffectCleanseType.Purify, false);
            AbilityEffectScaling.ApplyActivatedScaledHeal(activator, target, 10);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Remove_Condition), target);
        }

        private static void ApplyHarmonicRestoration(uint activator, uint target, bool targetWasBelowHalfHP)
        {
            if (!targetWasBelowHalfHP ||
                Stat.GetStatAdjustment(activator, StatType.HarmonicRestoration) <= 0 ||
                !TryUseHarmonicRestoration(activator))
            {
                return;
            }

            foreach (var ally in AbilityTargeting
                         .GetFriendlyTargetsNearLocation(activator, GetLocation(target), HarmonicRestorationRadius)
                         .Where(ally => ally != target)
                         .Take(2))
            {
                AbilityEffectScaling.ApplyActivatedScaledHeal(activator, ally, 6);
                StatusEffect.ApplyStatusEffect(activator, ally, typeof(HarmonicRestorationStatusEffect), 12f);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_M), ally);
            }
        }

        private static bool TryUseForceMend(uint activator, uint target)
        {
            var key = (activator, target);
            return TryUseCooldown(_forceMendCooldowns, key, ForceMendCooldownSeconds);
        }

        private static bool TryUseHarmonicRestoration(uint activator)
        {
            return TryUseCooldown(_harmonicRestorationCooldowns, activator, HarmonicRestorationCooldownSeconds);
        }

        private static bool TryUseCooldown<TKey>(
            Dictionary<TKey, DateTime> cooldowns,
            TKey key,
            float cooldownSeconds)
            where TKey : notnull
        {
            var now = DateTime.UtcNow;
            foreach (var expired in cooldowns.Where(x => x.Value <= now).Select(x => x.Key).ToList())
            {
                cooldowns.Remove(expired);
            }

            if (cooldowns.TryGetValue(key, out var nextAvailable) && nextAvailable > now)
                return false;

            cooldowns[key] = now.AddSeconds(cooldownSeconds);
            return true;
        }
    }
}
