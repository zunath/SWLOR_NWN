using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.CityGazetteService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Acting Mayors and stepping down (Player City Revamp, "Acting mayor and stepping down"). The mayor names an Acting
    /// Mayor, or steps down by naming an interim holder who accepts within 7 days; an election seat change, a citizen
    /// leaving, and the clock end them. The rules live in <see cref="ActingMayorRules"/> and <see cref="StepDownRules"/>;
    /// this class reads and writes the city and posts the Gazette.
    /// </summary>
    public static class CitySuccession
    {
        #region Acting Mayor

        /// <summary>
        /// The mayor names a citizen in good standing Acting Mayor for up to 30 days, replacing any running term, and the
        /// Gazette announces it. Returns the reason it was refused, or empty on success.
        /// </summary>
        public static string NameActingMayor(string cityId, string mayorId, string targetId, int days)
        {
            var city = LoadCity(cityId);
            if (city == null)
                return "This city no longer exists.";

            var now = DateTime.UtcNow;
            var target = string.IsNullOrWhiteSpace(targetId) ? null : DB.Get<Player>(targetId);
            var isCitizen = target != null && ElectionRules.IsCitizenOf(target.CitizenPropertyId, city.Id, target.IsDeleted);
            var error = ActingMayorRules.CheckNaming(
                CityPowerRules.GetRole(city, mayorId, now),
                city.OwnerPlayerId,
                targetId,
                isCitizen,
                target != null && target.PropertyOwedTaxes > 0);
            if (!string.IsNullOrEmpty(error))
                return error;

            var replaced = ClearTerm(city);
            var endsUtc = ActingMayorRules.GetEndsUtc(now, days);
            city.ActingMayorPlayerId = targetId;
            city.ActingMayorEndsUtc = endsUtc;
            DB.Set(city);

            if (!string.IsNullOrWhiteSpace(replaced) && replaced != targetId)
                AnnounceTermEnded(city.Id, replaced, ActingMayorEndReason.MayorEnded);

            CityGazette.Post(city.Id, CityGazetteEntryType.Notice, ActingMayorRules.BuildNamedText(endsUtc), targetId, CityPowerRules.ActingMayorTitle);
            Log.Write(LogGroup.Property, $"Mayor '{mayorId}' named '{targetId}' Acting Mayor of city '{city.CustomName}' ({city.Id}) until {endsUtc:O}.");
            return string.Empty;
        }

        /// <summary>The mayor ends the Acting Mayor's term. Returns the reason it was refused, or empty on success.</summary>
        public static string EndActingMayor(string cityId, string requesterId)
        {
            var city = LoadCity(cityId);
            if (city == null)
                return "This city no longer exists.";

            var now = DateTime.UtcNow;
            var error = ActingMayorRules.CheckEnding(
                CityPowerRules.GetRole(city, requesterId, now),
                !string.IsNullOrWhiteSpace(city.ActingMayorPlayerId));
            if (!string.IsNullOrEmpty(error))
                return error;

            var ended = ClearTerm(city);
            DB.Set(city);
            AnnounceTermEnded(city.Id, ended, ActingMayorEndReason.MayorEnded);
            Log.Write(LogGroup.Property, $"Mayor '{requesterId}' ended the Acting Mayor term of '{ended}' in city '{city.CustomName}' ({city.Id}).");
            return string.Empty;
        }

        /// <summary>Clears the Acting Mayor record in memory and returns who held it, or empty. The caller saves the city.</summary>
        private static string ClearTerm(WorldProperty city)
        {
            var former = city.ActingMayorPlayerId ?? string.Empty;
            city.ActingMayorPlayerId = string.Empty;
            city.ActingMayorEndsUtc = null;
            return former;
        }

        private static void AnnounceTermEnded(string cityId, string formerActingMayorId, ActingMayorEndReason reason)
        {
            CityGazette.Post(
                cityId,
                CityGazetteEntryType.Notice,
                ActingMayorRules.BuildEndedText(reason),
                formerActingMayorId,
                CityPowerRules.ActingMayorTitle);
        }

        #endregion

        #region Stepping down

        /// <summary>
        /// The mayor steps down by naming an interim holder from their Acting Mayor or officers. The named player has 7 days
        /// to accept in Manage City; until they do, the mayor keeps every power. Returns the reason it was refused, or
        /// empty on success.
        /// </summary>
        public static string BeginStepDown(string cityId, string mayorId, string holderId)
        {
            var city = LoadCity(cityId);
            if (city == null)
                return "This city no longer exists.";

            var now = DateTime.UtcNow;
            var error = StepDownRules.CheckNaming(
                CityPowerRules.GetRole(city, mayorId, now),
                holderId,
                GetEligibleHolders(city, now),
                IsCitizen(city, holderId),
                IsElectionOpen(city.Id),
                city.StepDown);
            if (!string.IsNullOrEmpty(error))
                return error;

            city.StepDown = new CityStepDown
            {
                InterimHolderPlayerId = holderId,
                Stage = CityStepDownStage.Pending,
                NamedUtc = now,
                AcceptDeadlineUtc = StepDownRules.GetAcceptDeadlineUtc(now)
            };
            DB.Set(city);

            Log.Write(LogGroup.Property, $"Mayor '{mayorId}' of city '{city.CustomName}' ({city.Id}) named '{holderId}' interim holder. They have until {city.StepDown.AcceptDeadlineUtc:O} to accept.");
            return string.Empty;
        }

        /// <summary>
        /// The named player accepts. The mayor stays mayor of record with no powers, the interim holder has the Acting
        /// Mayor's powers, and a snap election opens at once. Returns the reason it was refused, or empty on success.
        /// </summary>
        public static string AcceptStepDown(string cityId, string playerId)
        {
            var city = LoadCity(cityId);
            if (city == null)
                return "This city no longer exists.";

            var now = DateTime.UtcNow;
            var error = StepDownRules.CheckAcceptance(city.StepDown, playerId, IsCitizen(city, playerId), IsElectionOpen(city.Id), now);
            if (!string.IsNullOrEmpty(error))
            {
                if (StepDownRules.IsExpired(city.StepDown, now))
                    Cancel(city, "the named player did not accept in time");

                return error;
            }

            // The holder is the only one with the Acting Mayor's powers now, so any running term ends.
            var stepDown = city.StepDown;
            stepDown.Stage = CityStepDownStage.Accepted;
            var formerActingMayor = ClearTerm(city);

            // Saves the election and then the city, so the stage and the election are never saved apart. A restart between
            // the two is repaired at the next server start (see RepairAfterRestart).
            Property.OpenElection(now, city, ElectionKind.SnapStepDown, StepDownRules.GetElectionId(city.Id, stepDown));

            CityGazette.Post(city.Id, CityGazetteEntryType.Election, StepDownRules.BuildSteppedDownText(), playerId, CityPowerRules.ActingMayorTitle);
            if (!string.IsNullOrWhiteSpace(formerActingMayor) && formerActingMayor != playerId)
                AnnounceTermEnded(city.Id, formerActingMayor, ActingMayorEndReason.MayorSteppedDown);

            Log.Write(LogGroup.Property, $"Interim holder '{playerId}' accepted the step-down of mayor '{city.OwnerPlayerId}' of city '{city.CustomName}' ({city.Id}). A snap election opened.");
            return string.Empty;
        }

        /// <summary>
        /// The mayor withdraws a step-down still waiting for an answer, or the named player declines it. Returns the reason
        /// it was refused, or empty on success.
        /// </summary>
        public static string CancelStepDown(string cityId, string playerId)
        {
            var city = LoadCity(cityId);
            if (city == null)
                return "This city no longer exists.";

            var error = StepDownRules.CheckCancel(city.StepDown, playerId, city.OwnerPlayerId);
            if (!string.IsNullOrEmpty(error))
                return error;

            var reason = playerId == city.OwnerPlayerId ? "the mayor withdrew it" : "the named player declined";
            Cancel(city, reason);
            return string.Empty;
        }

        private static void Cancel(WorldProperty city, string reason)
        {
            city.StepDown = null;
            DB.Set(city);
            CityGazette.Post(city.Id, CityGazetteEntryType.Notice, StepDownRules.BuildCancelledText(reason));
            Log.Write(LogGroup.Property, $"The step-down of mayor '{city.OwnerPlayerId}' of city '{city.CustomName}' ({city.Id}) was cancelled: {reason}.");
        }

        /// <summary>
        /// Who the snap election counts as the incumbent. In a step-down election it is the interim holder, who becomes
        /// mayor wherever the incumbent would stay; in any other it is the sitting mayor.
        /// </summary>
        public static string GetIncumbentId(WorldProperty city, ElectionKind kind)
        {
            if (kind != ElectionKind.SnapStepDown)
                return city.OwnerPlayerId;

            return StepDownRules.GetIncumbentId(
                city.OwnerPlayerId,
                city.StepDown,
                city.StepDown != null && IsCitizen(city, city.StepDown.InterimHolderPlayerId));
        }

        /// <summary>
        /// A restart between saving a step-down's snap election and saving the city leaves the election open with the
        /// step-down still pending. The election proves the holder accepted, so the stage catches up. Returns true if it changed.
        /// </summary>
        public static bool RepairAfterRestart(WorldProperty city, Election election)
        {
            if (election == null || election.Kind != ElectionKind.SnapStepDown || !StepDownRules.IsPending(city.StepDown))
                return false;

            city.StepDown.Stage = CityStepDownStage.Accepted;
            ClearTerm(city);
            DB.Set(city);
            Log.Write(LogGroup.Property, $"City '{city.CustomName}' ({city.Id}) was found with its step-down election open and the step-down still pending. It is accepted.");
            return true;
        }

        #endregion

        #region Elections

        /// <summary>
        /// Applies an election's end to the Acting Mayor and the step-down. An election that seats a mayor, or a change of who
        /// holds the seat, ends the Acting Mayor's term; the end of a step-down snap election ends the step-down, whatever
        /// its result. Changes only the record in memory: the caller saves the city, then calls
        /// <see cref="AnnounceElectionEnd"/>.
        /// </summary>
        /// <returns>The Acting Mayor whose term ended, or empty, and whether a step-down ended.</returns>
        public static (string EndedActingMayorId, bool StepDownEnded) ApplyElectionEnd(
            WorldProperty city,
            ElectionKind kind,
            bool electionHadOutrightWin,
            bool seatChangedHands)
        {
            var ended = string.Empty;
            if (ActingMayorRules.ElectionEndsTerm(electionHadOutrightWin, seatChangedHands))
                ended = ClearTerm(city);

            var stepDownEnded = false;
            if (kind == ElectionKind.SnapStepDown && city.StepDown != null)
            {
                city.StepDown = null;
                stepDownEnded = true;
            }

            return (ended, stepDownEnded);
        }

        /// <summary>Posts to the Gazette what <see cref="ApplyElectionEnd"/> changed. Call it after the city is saved.</summary>
        public static void AnnounceElectionEnd(string cityId, (string EndedActingMayorId, bool StepDownEnded) change)
        {
            if (!string.IsNullOrWhiteSpace(change.EndedActingMayorId))
                AnnounceTermEnded(cityId, change.EndedActingMayorId, ActingMayorEndReason.ElectionSeatedMayor);
        }

        #endregion

        #region Leaving and the clock

        /// <summary>
        /// A citizen left the city or was deleted: an Acting Mayor's term ends, and a step-down waiting for the leaver is
        /// cancelled. Saves the city when it changed. Returns true if it did.
        /// </summary>
        public static bool OnCitizenLeft(WorldProperty city, string playerId)
        {
            if (city == null || string.IsNullOrWhiteSpace(playerId))
                return false;

            var changed = false;
            string endedTerm = null;
            if (city.ActingMayorPlayerId == playerId)
            {
                endedTerm = ClearTerm(city);
                changed = true;
            }

            var cancelsStepDown = StepDownRules.IsPending(city.StepDown) && city.StepDown.InterimHolderPlayerId == playerId;
            if (cancelsStepDown)
            {
                city.StepDown = null;
                changed = true;
            }

            // An interim holder who leaves takes the Acting Mayor's powers with them. The mayor of record stays without powers.
            var interimLeft = ClearInterimHolderIfGone(city, playerId);
            changed |= interimLeft;

            if (!changed)
                return false;

            DB.Set(city);
            if (!string.IsNullOrWhiteSpace(endedTerm))
                AnnounceTermEnded(city.Id, endedTerm, ActingMayorEndReason.LeftCity);

            if (cancelsStepDown)
                CityGazette.Post(city.Id, CityGazetteEntryType.Notice, StepDownRules.BuildCancelledText("the named player left the city"));

            if (interimLeft)
                CityGazette.Post(city.Id, CityGazetteEntryType.Notice, StepDownRules.BuildInterimLeftText());

            return true;
        }

        /// <summary>
        /// Runs at server start for each city: ends a term whose 30 days passed or whose Acting Mayor is no longer a
        /// citizen, and cancels a step-down nobody accepted in time or whose named player left. A failure is logged and
        /// never stops the rest of city processing.
        /// </summary>
        public static void Process(DateTime now, WorldProperty city)
        {
            try
            {
                ProcessCity(now, city);
            }
            catch (Exception ex)
            {
                Log.Write(LogGroup.Error, $"Failed to process the Acting Mayor of city '{city?.CustomName}' ({city?.Id}): {ex}");
            }
        }

        private static void ProcessCity(DateTime now, WorldProperty city)
        {
            if (city.IsQueuedForDeletion)
                return;

            if (!string.IsNullOrWhiteSpace(city.ActingMayorPlayerId))
            {
                var reason = ActingMayorRules.IsElapsed(city.ActingMayorPlayerId, city.ActingMayorEndsUtc, now)
                    ? ActingMayorEndReason.TermElapsed
                    : !IsCitizen(city, city.ActingMayorPlayerId)
                        ? ActingMayorEndReason.LeftCity
                        : ActingMayorEndReason.Invalid;

                if (reason != ActingMayorEndReason.Invalid)
                {
                    var ended = ClearTerm(city);
                    DB.Set(city);
                    AnnounceTermEnded(city.Id, ended, reason);
                }
            }

            if (StepDownRules.IsExpired(city.StepDown, now))
            {
                Cancel(city, "the named player did not accept in time");
            }
            else if (StepDownRules.IsPending(city.StepDown) && !IsCitizen(city, city.StepDown.InterimHolderPlayerId))
            {
                Cancel(city, "the named player left the city");
            }
            else if (StepDownRules.HasSteppedDown(city.StepDown) && !IsCitizen(city, city.StepDown.InterimHolderPlayerId) &&
                     ClearInterimHolderIfGone(city, city.StepDown.InterimHolderPlayerId))
            {
                DB.Set(city);
                CityGazette.Post(city.Id, CityGazetteEntryType.Notice, StepDownRules.BuildInterimLeftText());
            }
        }

        /// <summary>
        /// Takes the interim holder's name off an accepted step-down when it is the player who left, so they hold no powers and
        /// the mayor of record is the incumbent in the snap election. Changes only the record in memory. Returns true if it changed.
        /// </summary>
        private static bool ClearInterimHolderIfGone(WorldProperty city, string playerId)
        {
            if (!StepDownRules.HasSteppedDown(city.StepDown) ||
                string.IsNullOrWhiteSpace(playerId) ||
                city.StepDown.InterimHolderPlayerId != playerId)
                return false;

            city.StepDown.InterimHolderPlayerId = string.Empty;
            return true;
        }

        #endregion

        #region Lookups

        /// <summary>The players the mayor may name interim holder: the running Acting Mayor and the officers.</summary>
        public static List<string> GetEligibleHolders(WorldProperty city, DateTime nowUtc)
        {
            return StepDownRules.GetEligibleHolders(
                city.OwnerPlayerId,
                city.ActingMayorPlayerId,
                city.ActingMayorEndsUtc,
                city.Officers.Keys,
                nowUtc);
        }

        /// <summary>The city's citizens who may be named Acting Mayor: in good standing, and not the mayor.</summary>
        public static List<Player> GetActingMayorCandidates(WorldProperty city)
        {
            var query = new DBQuery<Player>()
                .AddFieldSearch(nameof(Player.CitizenPropertyId), city.Id, false)
                .AddFieldSearch(nameof(Player.IsDeleted), false);
            var count = (int)DB.SearchCount(query);
            if (count <= 0)
                return new List<Player>();

            return DB.Search(query.AddPaging(count, 0))
                .Where(citizen => citizen.Id != city.OwnerPlayerId && citizen.PropertyOwedTaxes <= 0)
                .OrderBy(citizen => citizen.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool IsCitizen(WorldProperty city, string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                return false;

            var player = DB.Get<Player>(playerId);
            return player != null && ElectionRules.IsCitizenOf(player.CitizenPropertyId, city.Id, player.IsDeleted);
        }

        private static bool IsElectionOpen(string cityId)
        {
            return ElectionBallot.GetCityElection(cityId) != null;
        }

        private static WorldProperty LoadCity(string cityId)
        {
            var city = string.IsNullOrWhiteSpace(cityId) ? null : DB.Get<WorldProperty>(cityId);
            return city != null && city.PropertyType == PropertyType.City && !city.IsQueuedForDeletion
                ? city
                : null;
        }

        #endregion
    }
}
