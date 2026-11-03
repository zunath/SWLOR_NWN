namespace SWLOR.Game.Server.Service.CityBountyService
{
    /// <summary>
    /// Why a fall did not pay a bounty. <see cref="None"/> means it did.
    /// </summary>
    public enum BountyClaimRefusal
    {
        None = 0,

        /// <summary>The bounty was already claimed, or is being paid.</summary>
        NotActive = 1,

        /// <summary>The bounty ran out before the fall.</summary>
        Expired = 2,

        /// <summary>The fall happened in a duel, which never pays a bounty.</summary>
        Duel = 3,

        /// <summary>No player is credited with the fall.</summary>
        NoClaimant = 4,

        /// <summary>The player credited is the target's own player.</summary>
        OwnFall = 5,

        /// <summary>The target showed a different identity when they fell.</summary>
        OtherIdentity = 6,

        /// <summary>The identity's PvP flag was off and no PvP combat window was open.</summary>
        FlagOff = 7,

        /// <summary>The claimant shares the target's CD key.</summary>
        SharedCdKey = 8,

        /// <summary>The claimant is in the target's party.</summary>
        SameParty = 9,

        /// <summary>A bounty on the identity was already claimed in the last 7 days.</summary>
        ClaimedThisWeek = 10
    }
}
