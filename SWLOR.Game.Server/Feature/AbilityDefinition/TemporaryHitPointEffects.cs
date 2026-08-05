using System;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition
{
    public static class TemporaryHitPointEffects
    {
        private const string EffectTagPrefix = "TEMPORARY_HP_";
        private const string BarrierVisualEffectTag = "TEMPORARY_HP_BARRIER_VFX";

        public static void ApplyFlatPlusPercent(
            uint target,
            string effectKey,
            int flatAmount,
            int percent,
            float durationSeconds)
        {
            var amount = Math.Max(1, flatAmount + GameMath.PercentOf(GetMaxHitPoints(target), percent));
            ApplyFlat(target, effectKey, amount, durationSeconds);
        }

        // Temporary HP pools from different abilities stack, but an ability never stacks with
        // itself: each grant is tagged with its ability's key and the most recent cast replaces
        // any prior pool carrying the same key, regardless of caster.
        public static void ApplyFlat(uint target, string effectKey, int amount, float durationSeconds)
        {
            if (string.IsNullOrWhiteSpace(effectKey))
                throw new ArgumentException("Temporary HP effects must declare the ability key they stack under.", nameof(effectKey));

            if (amount <= 0)
                return;

            var effectTag = EffectTagPrefix + effectKey;
            RemoveEffectByTag(target, effectTag);

            ApplyEffectToObject(
                DurationType.Temporary,
                TagEffect(EffectTemporaryHitpoints(amount), effectTag),
                target,
                durationSeconds);
        }

        public static void ApplyFlatWithBarrierVisual(
            uint target,
            string effectKey,
            int amount,
            float durationSeconds,
            VisualEffect barrierVisualEffect = VisualEffect.Vfx_Dur_Aura_Pulse_Cyan_Blue)
        {
            ApplyFlat(target, effectKey, amount, durationSeconds);

            if (amount <= 0 || durationSeconds <= 0f || barrierVisualEffect == VisualEffect.None)
                return;

            RemoveEffectByTag(target, BarrierVisualEffectTag);

            var visualEffect = TagEffect(EffectVisualEffect(barrierVisualEffect), BarrierVisualEffectTag);
            ApplyEffectToObject(DurationType.Temporary, visualEffect, target, durationSeconds);
        }
    }
}
