using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition
{
    public static class AbilityAreaEffects
    {
        public static void ScheduleFriendlyZoneStatus(
            uint activator,
            Location location,
            float radius,
            float durationSeconds,
            Type statusEffect,
            VisualEffect visualEffect = VisualEffect.None,
            Action<uint, float> onFirstApplication = null)
        {
            var firstApplications = new HashSet<uint>();
            for (var elapsed = 0f; elapsed < durationSeconds - 0.01f; elapsed += 3f)
            {
                var pulseDelay = elapsed;
                DelayCommand(pulseDelay, () =>
                {
                    foreach (var friendly in AbilityTargeting.GetFriendlyTargetsNearLocation(activator, location, radius))
                    {
                        if (firstApplications.Add(friendly))
                            onFirstApplication?.Invoke(friendly, Math.Max(0.1f, durationSeconds - pulseDelay));

                        StatusEffect.ApplyStatusEffect(activator, friendly, statusEffect, 3.2f);
                        if (visualEffect != VisualEffect.None)
                            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(visualEffect), friendly);
                    }
                });
            }
        }

        public static void ScheduleFriendlyZoneHealing(
            uint activator,
            Location location,
            float radius,
            float durationSeconds,
            float percentPerTick,
            Type statusEffect = null,
            VisualEffect visualEffect = VisualEffect.None,
            float multiplier = 1f)
        {
            for (var elapsed = 3f; elapsed <= durationSeconds + 0.01f; elapsed += 3f)
            {
                var pulseDelay = elapsed;
                DelayCommand(pulseDelay, () =>
                {
                    foreach (var friendly in AbilityTargeting.GetFriendlyTargetsNearLocation(activator, location, radius))
                    {
                        AbilityEffectScaling.ApplyScaledHeal(activator, friendly, percentPerTick, multiplier: multiplier);

                        if (statusEffect != null)
                            StatusEffect.ApplyStatusEffect(activator, friendly, statusEffect, 3.2f);

                        if (visualEffect != VisualEffect.None)
                            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(visualEffect), friendly);
                    }
                });
            }
        }
    }
}
