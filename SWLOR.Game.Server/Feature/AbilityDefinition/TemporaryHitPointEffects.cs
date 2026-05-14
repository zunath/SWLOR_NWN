using System;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition
{
    public static class TemporaryHitPointEffects
    {
        public static void ApplyFlatPlusPercent(
            uint target,
            int flatAmount,
            int percent,
            float durationSeconds)
        {
            var amount = Math.Max(1, flatAmount + (int)Math.Ceiling(GetMaxHitPoints(target) * (percent / 100f)));
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
    }
}
