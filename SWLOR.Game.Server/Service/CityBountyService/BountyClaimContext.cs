namespace SWLOR.Game.Server.Service.CityBountyService
{
    /// <summary>
    /// Everything <see cref="BountyRules.EvaluateClaim"/> needs to judge a fall against a bounty, read by
    /// <see cref="CityBounties"/> when the target is defeated or subdued.
    /// </summary>
    public sealed record BountyClaimContext
    {
        public DateTime NowUtc { get; init; }

        /// <summary>The identity the bounty is on.</summary>
        public BountyIdentity Bounty { get; init; }

        public BountyState State { get; init; }

        public DateTime ExpiresUtc { get; init; }

        /// <summary>The identity the target was showing when they fell.</summary>
        public BountyIdentity Shown { get; init; }

        /// <summary>Whether the shown identity's PvP flag was on.</summary>
        public bool ShownFlagOn { get; init; }

        /// <summary>Whether the target's PvP combat window was open.</summary>
        public bool WindowOpen { get; init; }

        /// <summary>Whether the fall happened in a duel, or in the protection after a third party voided one.</summary>
        public bool InDuel { get; init; }

        /// <summary>Whether a player is credited with the fall.</summary>
        public bool HasClaimant { get; init; }

        public string ClaimantPlayerId { get; init; } = string.Empty;

        public string TargetCdKey { get; init; } = string.Empty;

        public string ClaimantCdKey { get; init; } = string.Empty;

        /// <summary>Whether the claimant is in a party with the target.</summary>
        public bool SharesParty { get; init; }

        /// <summary>When a bounty on the identity was last claimed, or null when none ever was.</summary>
        public DateTime? LastClaimedUtc { get; init; }
    }
}
