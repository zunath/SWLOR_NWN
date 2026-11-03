namespace SWLOR.Game.Server.Service.CityBountyService
{
    /// <summary>
    /// What <see cref="BountyRules.CheckPost"/> needs to judge a new bounty, read by <see cref="CityBounties"/>.
    /// </summary>
    public sealed record BountyPostRequest
    {
        public DateTime NowUtc { get; init; }

        /// <summary>Whether the poster holds the power to post bounties for the city.</summary>
        public bool PosterCanPost { get; init; }

        /// <summary>Whether the target is the poster's own player.</summary>
        public bool TargetIsPoster { get; init; }

        public long Reward { get; init; }

        /// <summary>The credits in the city's ordinary treasury. Restricted funds never pay a bounty.</summary>
        public long OrdinaryTreasury { get; init; }

        /// <summary>Whether the target identity shows the PvP flag.</summary>
        public bool TargetFlagged { get; init; }

        /// <summary>Whether the target identity's player accepted the bounty notice.</summary>
        public bool TargetAccepted { get; init; }

        /// <summary>Whether the target identity already has an active bounty, in any city.</summary>
        public bool TargetHasActiveBounty { get; init; }

        /// <summary>When a bounty on the target identity was last claimed, or null when none ever was.</summary>
        public DateTime? TargetLastClaimedUtc { get; init; }
    }
}
