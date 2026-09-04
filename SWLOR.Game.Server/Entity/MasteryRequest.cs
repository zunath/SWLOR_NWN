using System.Collections.Generic;
using SWLOR.Game.Server.Service.MasteryService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// A player-submitted (or staff-initiated) request to gain, rank up, retrain, or
    /// remove a mastery. Reviewed by DM/Admin staff via the Masteries review window.
    /// </summary>
    public class MasteryRequest: EntityBase
    {
        public MasteryRequest()
        {
            PlayerId = string.Empty;
            CharacterName = string.Empty;
            MasteryId = string.Empty;
            CustomName = string.Empty;
            CustomDescription = string.Empty;
            Justification = string.Empty;
            Comments = new List<MasteryRequestComment>();
            ReviewerName = string.Empty;
            ReviewerCDKey = string.Empty;
            ReviewFeedback = string.Empty;
            OverrideReason = string.Empty;
            Status = MasteryRequestStatus.Pending;
        }

        [Indexed]
        public string PlayerId { get; set; }

        /// <summary>
        /// Canonical character name at time of submission, for staff display.
        /// </summary>
        public string CharacterName { get; set; }

        [Indexed]
        public MasteryRequestStatus Status { get; set; }
        [Indexed]
        public MasteryRequestType Type { get; set; }

        /// <summary>
        /// The catalog entry being requested. Null/empty for Custom (unlisted) requests.
        /// </summary>
        public string MasteryId { get; set; }

        /// <summary>
        /// Player-entered name/description for an unlisted mastery request. Only used
        /// when Type is Custom.
        /// </summary>
        public string CustomName { get; set; }
        public string CustomDescription { get; set; }

        public int TargetTier { get; set; }
        public string Justification { get; set; }

        public List<MasteryRequestComment> Comments { get; set; }

        public string ReviewerName { get; set; }
        public string ReviewerCDKey { get; set; }
        public DateTime? DateReviewed { get; set; }
        public string ReviewFeedback { get; set; }

        /// <summary>
        /// Required whenever a staff member approves a request despite a non-blocking
        /// rule violation warning (e.g. a second Rare mastery, the 17-level cap).
        /// </summary>
        public string OverrideReason { get; set; }
    }
}
