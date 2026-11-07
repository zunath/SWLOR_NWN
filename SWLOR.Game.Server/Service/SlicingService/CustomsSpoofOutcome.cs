using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Service.SlicingService
{
    /// <summary>What the end of a customs spoof costs the pilot (plan sections 13 and 24).</summary>
    public sealed class CustomsSpoofOutcome
    {
        /// <summary>Whether the scan comes back clean.</summary>
        public bool Clean { get; init; }

        /// <summary>The credits the patrol takes.</summary>
        public int Fine { get; init; }

        /// <summary>Whether the patrol confiscates the goods it bans.</summary>
        public bool ConfiscatesGoods { get; init; }

        /// <summary>The standing change it records with the patrol's authority, or null for none.</summary>
        public TradeStandingEvent? Standing { get; init; }
    }
}
