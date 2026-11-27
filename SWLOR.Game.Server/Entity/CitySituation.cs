using SWLOR.Game.Server.Service.CityAffairsService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// One City Affairs situation of a city: a follow-up waiting for its due date, an open situation, or a finished one.
    /// Finished situations are kept for a while so the weekly gain cap and the daily draw limit can see them.
    /// A new type, so no migration.
    /// </summary>
    public class CitySituation : EntityBase
    {
        public CitySituation()
        {
            CityId = string.Empty;
            ChosenOption = -1;
            DecidedByPlayerId = string.Empty;
            SecretEntryId = string.Empty;
            SubjectId = string.Empty;
            SubjectNote = string.Empty;
        }

        /// <summary>The property id of the city (or station).</summary>
        [Indexed]
        public string CityId { get; set; }

        public SituationType Type { get; set; }

        public CitySituationState State { get; set; }

        /// <summary>When a queued follow-up arrives.</summary>
        public DateTime DueUtc { get; set; }

        /// <summary>When the situation opened.</summary>
        public DateTime DrawnUtc { get; set; }

        /// <summary>When an undecided situation takes its ignored outcome.</summary>
        public DateTime ExpiresUtc { get; set; }

        /// <summary>The index of the choice picked, or -1.</summary>
        public int ChosenOption { get; set; }

        /// <summary>The player id of the official who decided, or empty.</summary>
        public string DecidedByPlayerId { get; set; }

        /// <summary>When the situation was decided or expired.</summary>
        public DateTime? ResolvedUtc { get; set; }

        /// <summary>The credits this situation added to the treasury, which count toward the weekly gain cap.</summary>
        public int TreasuryGain { get; set; }

        /// <summary>The secret Gazette entry a Scandal follow-up exposes when it arrives, or empty.</summary>
        public string SecretEntryId { get; set; }

        /// <summary>The id of what an event-driven situation is about, such as a holding, or empty.</summary>
        public string SubjectId { get; set; }

        /// <summary>What the officials are told about the subject, shown beside the situation's text. Names no player.</summary>
        public string SubjectNote { get; set; }
    }
}
