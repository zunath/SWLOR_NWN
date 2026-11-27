using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.CityAffairsService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// A situation's decision or expiry, saved on the city record in the same write as the treasury, modifier and neglect
    /// changes it made (<see cref="WorldProperty.PendingSituationOutcome"/>). The situation record is saved after it, so a
    /// restart in between leaves the outcome on the city, and the next City Affairs pass (or decision) settles the
    /// situation from it once, never deciding it again.
    /// </summary>
    public class CitySituationOutcome
    {
        public CitySituationOutcome()
        {
            SituationId = string.Empty;
            ChosenOption = -1;
            DecidedByPlayerId = string.Empty;
            HoldingId = string.Empty;
        }

        /// <summary>The situation the outcome settles.</summary>
        public string SituationId { get; set; }

        /// <summary>Decided or Expired.</summary>
        public CitySituationState State { get; set; }

        /// <summary>The index of the choice picked, or -1 for an expiry.</summary>
        public int ChosenOption { get; set; }

        /// <summary>The player id of the official who decided, or empty.</summary>
        public string DecidedByPlayerId { get; set; }

        public DateTime ResolvedUtc { get; set; }

        /// <summary>The credits the outcome added to the treasury, which count toward the weekly gain cap.</summary>
        public int TreasuryGain { get; set; }

        /// <summary>The planetary holding the outcome changes, or empty (<see cref="CityOutcomeEffects"/>).</summary>
        public string HoldingId { get; set; }

        /// <summary>The city drives the raiders off <see cref="HoldingId"/>.</summary>
        public bool HoldingHelp { get; set; }

        /// <summary>The credits of <see cref="HoldingId"/>'s customs fine the treasury paid off.</summary>
        public int HoldingFineCredits { get; set; }

        /// <summary>
        /// The follow-up situations the outcome queues, created (once, by id) only after the city holds the outcome, so a
        /// restart before the decision is saved never leaves a follow-up of a decision that did not happen. Older records
        /// read as null.
        /// </summary>
        public List<CitySituation> FollowUps { get; set; }

        /// <summary>Whether the outcome changes a holding.</summary>
        public bool ChangesHolding => !string.IsNullOrWhiteSpace(HoldingId) && (HoldingHelp || HoldingFineCredits > 0);

        /// <summary>The outcome of a situation settled in memory, before either record is saved.</summary>
        public static CitySituationOutcome Capture(CitySituation situation, CityOutcomeEffects holdingEffects = null)
        {
            var changesHolding = holdingEffects != null && (holdingEffects.Help || holdingEffects.FineCredits > 0);
            return new CitySituationOutcome
            {
                FollowUps = holdingEffects?.FollowUps.ToList() ?? new List<CitySituation>(),
                HoldingId = changesHolding ? situation.SubjectId ?? string.Empty : string.Empty,
                HoldingHelp = changesHolding && holdingEffects.Help,
                HoldingFineCredits = changesHolding ? System.Math.Max(0, holdingEffects.FineCredits) : 0,
                SituationId = situation.Id,
                State = situation.State,
                ChosenOption = situation.ChosenOption,
                DecidedByPlayerId = situation.DecidedByPlayerId ?? string.Empty,
                ResolvedUtc = situation.ResolvedUtc ?? DateTime.UtcNow,
                TreasuryGain = situation.TreasuryGain
            };
        }

        /// <summary>
        /// Settles the saved situation with this outcome when a restart left it open. False, changing nothing, when it is
        /// another situation or no longer open (its own save already settled it).
        /// </summary>
        public bool ApplyTo(CitySituation situation)
        {
            if (situation == null || situation.Id != SituationId || situation.State != CitySituationState.Open)
                return false;

            situation.State = State;
            situation.ChosenOption = ChosenOption;
            situation.DecidedByPlayerId = DecidedByPlayerId ?? string.Empty;
            situation.ResolvedUtc = ResolvedUtc;
            situation.TreasuryGain = TreasuryGain;
            return true;
        }
    }
}
