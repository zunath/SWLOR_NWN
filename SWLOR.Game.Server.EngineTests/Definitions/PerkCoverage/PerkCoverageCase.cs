using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    /// <summary>
    /// Declares the expected structure of one perk: its level count, per-level SP prices,
    /// and the feats granted across its levels in order. The NUnit coverage ratchet
    /// (PerkCoverageTests) compares every case against the perk actually built by its
    /// definition, so an unintended change to a perk's progression fails the suite until
    /// the case is deliberately updated alongside it.
    /// </summary>
    public class PerkCoverageCase
    {
        public PerkType Perk { get; set; }

        /// <summary>
        /// Expected number of purchasable levels.
        /// </summary>
        public int MaxLevel { get; set; }

        /// <summary>
        /// Expected SP price of each level, in level order (length must equal MaxLevel).
        /// </summary>
        public int[] Prices { get; set; } = Array.Empty<int>();

        /// <summary>
        /// Every feat granted by the perk, concatenated in level order. Empty for purely
        /// passive perks that grant no feats.
        /// </summary>
        public FeatType[] GrantedFeats { get; set; } = Array.Empty<FeatType>();

        /// <summary>
        /// Free-text context for a reviewer. Not used by the ratchet.
        /// </summary>
        public string Notes { get; set; }
    }
}
