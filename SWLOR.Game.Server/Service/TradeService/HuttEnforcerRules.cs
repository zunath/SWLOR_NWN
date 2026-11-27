using System.Linq;
using SWLOR.Game.Server.Service.FactionService;

namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>
    /// The Hutt Cartel enforcers on the Smuggler's Moon lanes (plan section 11): tier 4 ships on the Neutral faction in
    /// Smuggler's Moon and the orbits its starlanes reach. Every enforcer in an orbit is one group: a pilot Hostile to
    /// the Hutt Cartel (the W7c standing tiers) is attacked on sight, and firing on one turns the orbit's enforcers on
    /// the attacker and costs Hutt Cartel standing. The numbers are in <see cref="TradeTuning"/> (trade.py).
    /// </summary>
    public static class HuttEnforcerRules
    {
        /// <summary>The faction the enforcers fly for, whose standing decides whom they hunt.</summary>
        public const FactionType Faction = FactionType.HuttCartel;

        private const string GroupPrefix = "HUTT_ENFORCERS:";

        /// <summary>Whether a creature tag is a Hutt Cartel enforcer.</summary>
        public static bool IsEnforcer(string tag) =>
            !string.IsNullOrEmpty(tag) && TradeTuning.HuttEnforcerTags.Contains(tag);

        /// <summary>Whether the enforcers patrol a space area (by resref).</summary>
        public static bool PatrolsOrbit(string areaResref) =>
            !string.IsNullOrEmpty(areaResref) && TradeTuning.HuttEnforcerOrbits.Contains(areaResref);

        /// <summary>The lawful group every enforcer of one area belongs to.</summary>
        public static string GetGroup(string areaId) => GroupPrefix + areaId;

        /// <summary>Whether the enforcers attack a pilot of this standing tier on sight: only one Hostile to the Cartel.</summary>
        public static bool AttacksOnSight(TradeStandingTier tier) => tier == TradeStandingTier.Hostile;
    }
}
