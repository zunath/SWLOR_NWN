using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition
{
    public static class TemporaryHitPointEffects
    {
        private const string BarrierVisualEffectTag = "TEMPORARY_HP_BARRIER_VFX";

        public static void ApplyFlatPlusPercent(
            uint target,
            int flatAmount,
            int percent,
            float durationSeconds)
        {
            var amount = Math.Max(1, flatAmount + GameMath.PercentOf(GetMaxHitPoints(target), percent));
            ApplyFlat(target, amount, durationSeconds);
        }

        public static void ApplyFlat(uint target, int amount, float durationSeconds)
        {
            if (amount <= 0)
                return;

            ApplyEffectToObject(
                DurationType.Temporary,
                EffectTemporaryHitpoints(amount),
                target,
                durationSeconds);
        }

        public static void ApplyFlatWithBarrierVisual(
            uint target,
            int amount,
            float durationSeconds,
            VisualEffect barrierVisualEffect = VisualEffect.Vfx_Dur_Aura_Pulse_Cyan_Blue)
        {
            ApplyFlat(target, amount, durationSeconds);

            if (amount <= 0 || durationSeconds <= 0f || barrierVisualEffect == VisualEffect.None)
                return;

            RemoveEffectByTag(target, BarrierVisualEffectTag);

            var visualEffect = TagEffect(EffectVisualEffect(barrierVisualEffect), BarrierVisualEffectTag);
            ApplyEffectToObject(DurationType.Temporary, visualEffect, target, durationSeconds);
        }
    }
}
