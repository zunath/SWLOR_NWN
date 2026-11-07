using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The Bulwark Shield Generator's aura on an ally (plan section 8) as pure functions. Each strength an ally is
    /// covered by keeps its own expiry, so overlapping generators never replace each other: the strongest one still
    /// active raises max shield, and the next one takes over as it lapses.
    /// </summary>
    public static class ShipShieldAuraRules
    {
        /// <summary>The percent added to max shield right now: the strongest unexpired aura, or 0.</summary>
        public static int ActivePercent(IReadOnlyDictionary<int, DateTime> auras, DateTime now) =>
            auras == null ? 0 : auras.Where(x => now < x.Value).Select(x => x.Key).DefaultIfEmpty(0).Max();

        /// <summary>Max shield with the strongest active aura added.</summary>
        public static int EffectiveMaxShield(int maxShield, IReadOnlyDictionary<int, DateTime> auras, DateTime now) =>
            maxShield + (int)Math.Round(maxShield * ActivePercent(auras, now) / 100.0);

        /// <summary>
        /// Refreshes one aura strength on the ship until <paramref name="until"/>, never shortening it, drops the
        /// lapsed ones and returns the shield to grant: only the max shield the refresh added, so an aura grants
        /// once when it first applies or a stronger one takes over, never when it is renewed or a weaker one joins.
        /// </summary>
        public static int Refresh(ShipStatus status, int percent, DateTime until, DateTime now)
        {
            status.ShieldAuras ??= new Dictionary<int, DateTime>();
            var before = status.GetEffectiveMaxShield(now);

            foreach (var lapsed in status.ShieldAuras.Where(x => x.Value <= now).Select(x => x.Key).ToList())
                status.ShieldAuras.Remove(lapsed);

            if (!status.ShieldAuras.TryGetValue(percent, out var current) || current < until)
                status.ShieldAuras[percent] = until;

            return Math.Max(0, status.GetEffectiveMaxShield(now) - before);
        }
    }
}
