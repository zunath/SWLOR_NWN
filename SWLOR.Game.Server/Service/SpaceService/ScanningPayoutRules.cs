using System.Linq;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// What exploring pays per hour against the tier's space combat (scanning.py's model): each site's own value and
    /// cache over its time plus its share of the scanning, travel and jump overhead, weighted by the type mix.
    /// </summary>
    public static class ScanningPayoutRules
    {
        /// <summary>A completed site's expected credits: its own loot, salvage or gas, and its cache's credits and items.</summary>
        /// <remarks>A cache site is the mix of plain and locked caches (<see cref="ScanningTuning.LockedCachePercent"/>).</remarks>
        public static double SiteValue(SignatureType type, int tier)
        {
            tier = Math.Clamp(tier, 1, 6);
            var own = ScanningTuning.SiteOwnCredits.TryGetValue(type, out var values) ? values[tier] : 0.0;
            var value = own + SignatureSiteRules.SiteCredits(type, tier);
            if (type == SignatureType.CacheSite)
                value = Mix(value, ScanningTuning.LockedCacheOwnCredits[tier] + SignatureSiteRules.LockedCacheCredits(tier));

            return value + ScanningTuning.CacheItemPulls * ScanningTuning.ElitePullCredits[tier];
        }

        /// <summary>The minutes a site takes, its share of the overhead included.</summary>
        public static double SiteMinutes(SignatureType type, int tier)
        {
            tier = Math.Clamp(tier, 1, 6);
            var minutes = ScanningTuning.SiteMinutes.TryGetValue(type, out var values) ? values[tier] : 0.0;
            if (type == SignatureType.CacheSite)
                minutes = Mix(minutes, ScanningTuning.LockedCacheMinutes[tier]);

            return minutes + ScanningTuning.OverheadMinutes[tier];
        }

        /// <summary>A plain cache's number blended with a locked cache's at the locked share.</summary>
        private static double Mix(double plain, double locked) =>
            plain + (locked - plain) * ScanningTuning.LockedCachePercent / 100.0;

        /// <summary>Exploring credits per hour at a tier, over the spawnable type mix.</summary>
        public static double CreditsPerHour(int tier)
        {
            var weights = SignatureSiteRules.GetWeights(tier).Where(x => x.Value > 0).ToList();
            var total = weights.Sum(x => x.Value);
            if (total == 0)
                return 0;

            var value = weights.Sum(x => x.Value / (double)total * SiteValue(x.Key, tier));
            var minutes = weights.Sum(x => x.Value / (double)total * SiteMinutes(x.Key, tier));
            return value / minutes * 60.0;
        }

        /// <summary>Exploring against the tier's combat credits per hour (1.0 = the same).</summary>
        public static double RatioToCombat(int tier) =>
            CreditsPerHour(tier) / ScanningTuning.CombatCreditsPerHour[Math.Clamp(tier, 1, 6)];
    }
}
