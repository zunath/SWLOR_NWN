using System;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade
{
    /// <summary>
    /// Shared scaling rule for the HP a Berserker ability takes out of its user, covering both
    /// up-front sacrifices and Soul Devourer's per-attack recoil. Might buys the cost down at half
    /// a percentage point per point of MGT so the drawback keeps scaling across the whole MGT range
    /// instead of resting on its floor as soon as the build matures.
    /// </summary>
    public static class HeavyVibrobladeMightCostRules
    {
        public static int Percent(int basePercent, int minimumPercent, int might)
        {
            return Math.Max(minimumPercent, basePercent - Math.Max(0, might) / 2);
        }
    }
}
