using System;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>The zone hazards' decisions (W11o): a site's tier, the mine a field fires as, and the refill timers.</summary>
    public static class SpaceHazardRules
    {
        /// <summary>A waypoint's HAZARD_TIER, kept to the faction tiers 1-6 (unset reads as tier 1).</summary>
        public static int SiteTier(int local) => Math.Clamp(local, 1, 6);

        /// <summary>The Rift Raider Minelayer, whose preset a Wild Space field mine hits as (tier 5.5).</summary>
        public const string WildSpaceMinelayerTag = "rrminelayer";

        /// <summary>The Minelayer whose preset a field mine hits as: the tier's, or the Rift Raiders' in Wild Space.</summary>
        public static string MinelayerTag(int tier, bool wildSpace = false) =>
            wildSpace ? WildSpaceMinelayerTag : $"t{SiteTier(tier)}minelayer";

        public static DateTime MineRearmAt(DateTime now) => now.AddSeconds(SpaceHazardTuning.MineRearmSeconds);

        public static DateTime WreckRespawnAt(DateTime now) => now.AddMinutes(SpaceHazardTuning.WreckRespawnMinutes);
    }
}
