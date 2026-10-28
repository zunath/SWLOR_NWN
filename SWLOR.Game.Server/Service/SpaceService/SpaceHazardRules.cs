using System;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>The zone hazards' decisions (W11o): a site's tier, the mine a field fires as, and the refill timers.</summary>
    public static class SpaceHazardRules
    {
        /// <summary>A waypoint's HAZARD_TIER, kept to the faction tiers 1-6 (unset reads as tier 1).</summary>
        public static int SiteTier(int local) => Math.Clamp(local, 1, 6);

        /// <summary>The tier Minelayer whose preset a field mine of this tier hits as.</summary>
        public static string MinelayerTag(int tier) => $"t{SiteTier(tier)}minelayer";

        public static DateTime MineRearmAt(DateTime now) => now.AddSeconds(SpaceHazardTuning.MineRearmSeconds);

        public static DateTime WreckRespawnAt(DateTime now) => now.AddMinutes(SpaceHazardTuning.WreckRespawnMinutes);
    }
}
