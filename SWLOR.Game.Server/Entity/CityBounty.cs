using SWLOR.Game.Server.Service.CityBountyService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// A bounty a city posted on an identity that shows the PvP flag (Player City Revamp, "Conflict and PvP"). The reward
    /// sits in the city's <see cref="WorldProperty.Escrow"/> under this record's id from the moment it is posted. The
    /// record is deleted once the reward has been paid or returned; the Gazette keeps the story. A new type, so no
    /// migration.
    /// </summary>
    public class CityBounty : EntityBase
    {
        public CityBounty()
        {
            CityId = string.Empty;
            TargetPlayerId = string.Empty;
            TargetDisguiseId = string.Empty;
            TargetLabel = string.Empty;
            PostedByPlayerId = string.Empty;
            ClaimantPlayerId = string.Empty;
            ClaimantLabel = string.Empty;
        }

        /// <summary>The property id of the city that posted the bounty.</summary>
        [Indexed]
        public string CityId { get; set; }

        /// <summary>The player the bounty is on. Never shown to anyone but staff, since it could undo a disguise.</summary>
        [Indexed]
        public string TargetPlayerId { get; set; }

        /// <summary>The disguise the bounty is on, or empty when it is on the player's own character.</summary>
        public string TargetDisguiseId { get; set; }

        [Indexed]
        public BountyState State { get; set; }

        /// <summary>How strangers saw the identity when the bounty was posted. This is the only name the Gazette uses.</summary>
        public string TargetLabel { get; set; }

        /// <summary>The credits paid to whoever collects it, held out of the city's ordinary treasury.</summary>
        public long Reward { get; set; }

        public string PostedByPlayerId { get; set; }

        public DateTime PostedUtc { get; set; }

        public DateTime ExpiresUtc { get; set; }

        /// <summary>Whether the target has been told. A bounty posted while they were offline tells them at their next login.</summary>
        public bool TargetNotified { get; set; }

        /// <summary>The player who collected it. Set when a claim is decided, so a restart finishes the same payment.</summary>
        public string ClaimantPlayerId { get; set; }

        /// <summary>How strangers saw the claimant when they collected it.</summary>
        public string ClaimantLabel { get; set; }

        public DateTime? ClaimedUtc { get; set; }
    }
}
