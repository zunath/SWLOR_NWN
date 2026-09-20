using System;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade
{
    /// <summary>
    /// Shared scaling rule for the HP a Berserker ability takes out of its user, covering both
    /// up-front sacrifices and Soul Devourer's per-attack recoil. Might buys the cost down by a
    /// percentage point per point of MGT.
    ///
    /// The rate is tuned to the Design Bible's progression cap of 26 purchased attribute points
    /// plus a one-time racial point, so 27 is the most a character reaches. Across that range the
    /// costs run from their full value down to 13% for Soul Burst and Soul Storm, with only Soul
    /// Devourer's recoil reaching its floor, and then only at the very top of the stat. Do not
    /// reason about this formula using MGT values above 27; they do not occur.
    /// </summary>
    public static class HeavyVibrobladeMightCostRules
    {
        public static int Percent(int basePercent, int minimumPercent, int might)
        {
            return Math.Max(minimumPercent, basePercent - Math.Max(0, might));
        }
    }
}
