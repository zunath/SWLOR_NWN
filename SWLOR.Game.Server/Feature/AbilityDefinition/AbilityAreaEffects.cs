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
            Action<uint, float> onFirstApplication = null,
            VisualEffect areaMarkerVisualEffect = VisualEffect.None,
            float areaMarkerVisualEffectScale = 1f)
        {
            if (areaMarkerVisualEffect != VisualEffect.None)
            {
                ApplyEffectAtLocation(
                    DurationType.Temporary,
                    EffectVisualEffect(areaMarkerVisualEffect, false, Math.Max(0.1f, areaMarkerVisualEffectScale)),
                    location,
                    durationSeconds);
            }

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
            float multiplier = 1f,
            Action<uint, bool> onHealed = null)
        {
            for (var elapsed = 3f; elapsed <= durationSeconds + 0.01f; elapsed += 3f)
            {
                var pulseDelay = elapsed;
                DelayCommand(pulseDelay, () =>
                {
                    foreach (var friendly in AbilityTargeting.GetFriendlyTargetsNearLocation(activator, location, radius))
                    {
                        var targetWasBelowHalfHP = GetMaxHitPoints(friendly) > 0 &&
                                                   GetCurrentHitPoints(friendly) < GetMaxHitPoints(friendly) * 0.5f;
                        AbilityEffectScaling.ApplyScaledHeal(activator, friendly, percentPerTick, multiplier: multiplier);
                        onHealed?.Invoke(friendly, targetWasBelowHalfHP);

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
