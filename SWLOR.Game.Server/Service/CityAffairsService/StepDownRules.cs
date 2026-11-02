using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service.CityAffairsService
{
    /// <summary>
    /// A mayor stepping down (Player City Revamp, "Acting mayor and stepping down"): who may be named interim holder, the 7
    /// days to accept, and who counts as the incumbent in the snap election. Pure, so each rule can be tested.
    /// </summary>
    public static class StepDownRules
    {
        /// <summary>Days the named player has to accept before the step-down is cancelled.</summary>
        public const int AcceptDays = 7;

        public static DateTime GetAcceptDeadlineUtc(DateTime namedUtc)
        {
            return namedUtc.AddDays(AcceptDays);
        }

        /// <summary>A step-down the interim holder has not yet accepted. The mayor keeps every power meanwhile.</summary>
        public static bool IsPending(CityStepDown stepDown)
        {
            return stepDown != null && stepDown.Stage == CityStepDownStage.Pending;
        }

        /// <summary>
        /// The mayor has stepped down: the interim holder accepted and the snap election is open or about to open. The
        /// mayor of record has no powers until it ends.
        /// </summary>
        public static bool HasSteppedDown(CityStepDown stepDown)
        {
            return stepDown != null && stepDown.Stage == CityStepDownStage.Accepted;
        }

        /// <summary>Whether the player is the named holder of a step-down that waits for their answer.</summary>
        public static bool IsWaitingForAnswerFrom(CityStepDown stepDown, string playerId, DateTime nowUtc)
        {
            return IsPending(stepDown) &&
                   !string.IsNullOrWhiteSpace(playerId) &&
                   stepDown.InterimHolderPlayerId == playerId &&
                   nowUtc < stepDown.AcceptDeadlineUtc;
        }

        /// <summary>Whether a pending step-down ran out of time.</summary>
        public static bool IsExpired(CityStepDown stepDown, DateTime nowUtc)
        {
            return IsPending(stepDown) && nowUtc >= stepDown.AcceptDeadlineUtc;
        }

        /// <summary>
        /// The players the mayor may name interim holder: their Acting Mayor, if one is running, and their officers. Someone
        /// who is both appears once, and the mayor never appears.
        /// </summary>
        public static List<string> GetEligibleHolders(
            string mayorId,
            string actingMayorId,
            DateTime? actingMayorEndsUtc,
            IEnumerable<string> officerIds,
            DateTime nowUtc)
        {
            var holders = new List<string>();
            if (ActingMayorRules.IsActive(actingMayorId, actingMayorEndsUtc, nowUtc))
                holders.Add(actingMayorId);

            foreach (var officerId in officerIds ?? Enumerable.Empty<string>())
            {
                if (!holders.Contains(officerId))
                    holders.Add(officerId);
            }

            holders.Remove(mayorId);
            return holders;
        }

        /// <summary>
        /// Why the mayor may not step down naming this player, or an empty string when they may. A mayor with no Acting Mayor
        /// and no officers must name an Acting Mayor first, and cannot step down while an election is open, since the snap
        /// election would run beside it.
        /// </summary>
        /// <param name="mayorRole">The role of the player stepping down.</param>
        /// <param name="holderId">The player named interim holder.</param>
        /// <param name="eligibleHolderIds">The players who may be named, from <see cref="GetEligibleHolders"/>.</param>
        /// <param name="holderIsCitizen">The named player is a citizen of this city and not deleted.</param>
        /// <param name="isElectionOpen">The city has an election in registration or voting.</param>
        /// <param name="existingStepDown">The step-down already under way, or null.</param>
        public static string CheckNaming(
            CityRole mayorRole,
            string holderId,
            IReadOnlyCollection<string> eligibleHolderIds,
            bool holderIsCitizen,
            bool isElectionOpen,
            CityStepDown existingStepDown)
        {
            if (mayorRole != CityRole.Mayor)
                return "Only the mayor can step down.";

            if (existingStepDown != null)
                return "A step-down is already under way.";

            if (isElectionOpen)
                return "You cannot step down while an election is open.";

            if (eligibleHolderIds == null || eligibleHolderIds.Count == 0)
                return "You have no Acting Mayor or officers to take over. Name an Acting Mayor first, then step down.";

            if (string.IsNullOrWhiteSpace(holderId) || !eligibleHolderIds.Contains(holderId))
                return "The interim holder must be your Acting Mayor or one of your officers.";

            if (!holderIsCitizen)
                return "That citizen is no longer a citizen of this city.";

            return string.Empty;
        }

        /// <summary>Why the player may not accept, or an empty string when they may.</summary>
        public static string CheckAcceptance(
            CityStepDown stepDown,
            string playerId,
            bool holderIsCitizen,
            bool isElectionOpen,
            DateTime nowUtc)
        {
            if (!IsPending(stepDown) || stepDown.InterimHolderPlayerId != playerId)
                return "No step-down is waiting for you.";

            if (nowUtc >= stepDown.AcceptDeadlineUtc)
                return $"The {AcceptDays} days to accept ran out, so the step-down is cancelled.";

            if (!holderIsCitizen)
                return "You are no longer a citizen of this city.";

            if (isElectionOpen)
                return "An election is open. You can accept when it ends, if the time has not run out.";

            return string.Empty;
        }

        /// <summary>Why the player may not cancel the step-down, or an empty string when they may. The mayor or the named player cancels one still pending.</summary>
        public static string CheckCancel(CityStepDown stepDown, string playerId, string mayorId)
        {
            if (!IsPending(stepDown))
                return "There is no step-down to cancel.";

            return playerId == mayorId || playerId == stepDown.InterimHolderPlayerId
                ? string.Empty
                : "Only the mayor or the named player can cancel the step-down.";
        }

        /// <summary>
        /// Who counts as the incumbent in the step-down snap election: the interim holder, who becomes mayor wherever the
        /// incumbent would stay. If the holder is no longer a citizen the sitting mayor of record stays instead, since
        /// nobody else was named to take the seat.
        /// </summary>
        public static string GetIncumbentId(string mayorId, CityStepDown stepDown, bool holderIsCitizen)
        {
            if (stepDown == null || string.IsNullOrWhiteSpace(stepDown.InterimHolderPlayerId) || !holderIsCitizen)
                return mayorId;

            return stepDown.InterimHolderPlayerId;
        }

        /// <summary>
        /// Why the player may not register in an election, or an empty string when they may. The mayor who stepped down
        /// cannot run in the snap election that follows. They are the mayor of record for as long as it is open.
        /// </summary>
        public static string CheckCandidate(ElectionKind kind, string candidateId, string mayorOfRecordId)
        {
            if (kind == ElectionKind.SnapStepDown &&
                !string.IsNullOrWhiteSpace(candidateId) &&
                candidateId == mayorOfRecordId)
            {
                return "You stepped down as mayor, so you cannot run in this election.";
            }

            return string.Empty;
        }

        /// <summary>The id of the snap election, from the step-down that called for it, so opening it twice replaces one record.</summary>
        public static string GetElectionId(string cityId, CityStepDown stepDown)
        {
            return $"{cityId}:stepdown:{stepDown.NamedUtc.Ticks}";
        }

        /// <summary>The Gazette text for the snap election the step-down opened.</summary>
        public static string BuildSteppedDownText()
        {
            return "The mayor stepped down. The interim holder has the Acting Mayor's powers until the snap election ends, " +
                   "and the mayor has none.";
        }

        /// <summary>The Gazette text for an interim holder who left the city during the snap election.</summary>
        public static string BuildInterimLeftText()
        {
            return "The interim holder left the city, so nobody has the Acting Mayor's powers until the snap election ends. " +
                   "The mayor who stepped down stays mayor of record without powers.";
        }

        /// <summary>The Gazette text for a step-down that did not happen.</summary>
        public static string BuildCancelledText(string reason)
        {
            return $"The mayor's step-down was cancelled: {reason}.";
        }

        /// <summary>The line the Succession tab shows for a step-down.</summary>
        public static string BuildStatusText(CityStepDown stepDown, string holderName, DateTime nowUtc)
        {
            if (IsPending(stepDown))
            {
                return $"Step-down: waiting for {holderName} to accept, {ElectionRules.GetDaysLeft(nowUtc, stepDown.AcceptDeadlineUtc)} days left. " +
                       "The mayor keeps every power until they accept.";
            }

            return HasSteppedDown(stepDown)
                ? $"The mayor has stepped down. {holderName} holds the Acting Mayor's powers until the snap election ends."
                : string.Empty;
        }
    }
}
