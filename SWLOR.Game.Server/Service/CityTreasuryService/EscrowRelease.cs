using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CityTreasuryService
{
    /// <summary>
    /// What came out of an escrow entry when it was paid out or refunded. The entry is gone afterwards, so a second call
    /// finds nothing.
    /// </summary>
    public sealed record EscrowRelease(
        bool Found,
        long Restricted,
        long Unrestricted,
        IReadOnlyDictionary<string, long> PlayerFees)
    {
        /// <summary>The credits that were held for the thing itself (a prize, a bounty, a project), both portions.</summary>
        public long Credits => Restricted + Unrestricted;

        /// <summary>The entry fees that were held, which belong to the players who paid them.</summary>
        public long FeeTotal => PlayerFees.Values.Sum();

        public static EscrowRelease NotFound { get; } = new(false, 0, 0, new Dictionary<string, long>());
    }
}
