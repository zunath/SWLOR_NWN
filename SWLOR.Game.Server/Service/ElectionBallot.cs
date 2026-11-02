using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.EdictService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Changes to a city election's ballot: candidates registering, editing their statement, withdrawing or leaving,
    /// and citizens voting. The rules live in <see cref="ElectionRules"/>; this class reads and writes the records.
    /// </summary>
    public static class ElectionBallot
    {
        /// <summary>The city's open election, or null when none is open.</summary>
        public static Election GetCityElection(string cityId)
        {
            if (string.IsNullOrWhiteSpace(cityId))
                return null;

            return DB.Search(new DBQuery<Election>()
                    .AddFieldSearch(nameof(Election.PropertyId), cityId, false))
                .OrderByDescending(election => election.RegistrationOpenedUtc)
                .FirstOrDefault();
        }

        /// <summary>Deletes every election record of a city that is being deleted.</summary>
        public static void DeleteCityElections(string cityId)
        {
            if (string.IsNullOrWhiteSpace(cityId))
                return;

            foreach (var election in DB.Search(new DBQuery<Election>()
                         .AddFieldSearch(nameof(Election.PropertyId), cityId, false))
                     .ToList())
            {
                DB.Delete<Election>(election.Id);
                Log.Write(LogGroup.Property, $"Deleted election '{election.Id}' of deleted city '{cityId}'.");
            }
        }

        /// <summary>Whether the character may vote in the election: a citizen since registration opened or earlier.</summary>
        public static bool IsEligibleVoter(Player dbPlayer, Election election)
        {
            return dbPlayer != null &&
                   election != null &&
                   ElectionRules.IsEligibleVoter(
                       dbPlayer.CitizenPropertyId,
                       election.PropertyId,
                       dbPlayer.IsDeleted,
                       dbPlayer.CitizenSinceUtc,
                       election.RegistrationOpenedUtc);
        }

        /// <summary>The CD key's vote in the election, or null when it has not voted.</summary>
        public static ElectionVoter GetVote(Election election, string cdKey)
        {
            return election != null && !string.IsNullOrWhiteSpace(cdKey) && election.VoterSelections.TryGetValue(cdKey, out var vote)
                ? vote
                : null;
        }

        #region Candidates

        /// <summary>
        /// Enters the character in the race with a platform: the Edicts they promise and a statement. Returns the reason it
        /// was refused, or empty on success.
        /// </summary>
        public static string RegisterCandidate(string electionId, string playerId, string statement, IEnumerable<ElectionPlatformEdict> edicts)
        {
            var election = DB.Get<Election>(electionId);
            var error = CheckRegistrationChange(election, playerId);
            if (string.IsNullOrEmpty(error))
                error = CheckPlatform(election, edicts);
            if (!string.IsNullOrEmpty(error))
                return error;

            error = CheckCanRun(election, playerId);
            if (!string.IsNullOrEmpty(error))
                return error;

            if (!election.CandidatePlayerIds.Contains(playerId))
                election.CandidatePlayerIds.Add(playerId);

            SetPlatform(election, playerId, statement, edicts);
            DB.Set(election);
            return string.Empty;
        }

        /// <summary>
        /// Replaces a candidate's platform. Returns the reason it was refused, or empty on success.
        /// </summary>
        public static string UpdatePlatform(string electionId, string playerId, string statement, IEnumerable<ElectionPlatformEdict> edicts)
        {
            var election = DB.Get<Election>(electionId);
            var error = CheckRegistrationChange(election, playerId);
            if (string.IsNullOrEmpty(error))
                error = CheckPlatform(election, edicts);
            if (!string.IsNullOrEmpty(error))
                return error;

            if (!election.CandidatePlayerIds.Contains(playerId))
                return "You are not in the race.";

            SetPlatform(election, playerId, statement, edicts);
            DB.Set(election);
            return string.Empty;
        }

        /// <summary>
        /// The reason the character cannot enter the race or edit a platform right now (registration closed, not a citizen),
        /// or empty.
        /// </summary>
        public static string GetRegistrationAccessError(Election election, string playerId)
        {
            return CheckRegistrationChange(election, playerId);
        }

        /// <summary>
        /// Checks the promised Edicts against the city as it is now. Returns the reason they cannot be a platform, or empty.
        /// </summary>
        public static string GetPlatformError(Election election, IEnumerable<ElectionPlatformEdict> edicts)
        {
            return CheckPlatform(election, edicts);
        }

        private static string CheckPlatform(Election election, IEnumerable<ElectionPlatformEdict> edicts)
        {
            var city = DB.Get<WorldProperty>(election.PropertyId);
            if (city == null)
                return "This city is no longer available.";

            var (refusal, type) = ElectionPlatformRules.GetRefusal(
                edicts,
                city.Id,
                EdictChanges.GetCityLevel(city),
                Edict.IsWildSpaceStation(city),
                targetId => Edict.GetLiveCity(targetId) != null);
            return refusal == EdictRefusal.None
                ? string.Empty
                : ElectionPlatformRules.DescribeRefusal(refusal, type, EdictChanges.GetCityLevel(city));
        }

        /// <summary>
        /// Takes the character out of the race during registration. Returns the reason it was refused, or empty on success.
        /// </summary>
        public static string WithdrawCandidate(string electionId, string playerId)
        {
            var election = DB.Get<Election>(electionId);
            var error = CheckRegistrationChange(election, playerId);
            if (!string.IsNullOrEmpty(error))
                return error;

            RemoveFromBallot(election, playerId);
            DB.Set(election);
            return string.Empty;
        }

        /// <summary>
        /// Removes a candidate who left the city or was deleted, in any stage. Votes for them are dropped, so those
        /// voters can vote again. Does nothing when the city has no open election or the character is not in it.
        /// </summary>
        public static void RemoveDepartedCandidate(string cityId, string playerId)
        {
            var election = GetCityElection(cityId);
            if (election == null || !election.CandidatePlayerIds.Contains(playerId))
                return;

            RemoveFromBallot(election, playerId);
            DB.Set(election);
        }

        /// <summary>
        /// Removes every candidate who is no longer a citizen of the city or has been deleted, with the votes for them.
        /// Returns true if the ballot changed. The caller saves the election.
        /// </summary>
        public static bool RemoveDepartedCandidates(Election election)
        {
            var changed = false;
            foreach (var candidateId in election.CandidatePlayerIds.ToList())
            {
                var dbCandidate = DB.Get<Player>(candidateId);
                if (dbCandidate != null &&
                    ElectionRules.IsCitizenOf(dbCandidate.CitizenPropertyId, election.PropertyId, dbCandidate.IsDeleted))
                    continue;

                RemoveFromBallot(election, candidateId);
                changed = true;
            }

            return changed;
        }

        /// <summary>
        /// Why the character may not run in this election, beyond the registration rules: the mayor who stepped down cannot
        /// run in the snap election that follows. Empty when they may.
        /// </summary>
        private static string CheckCanRun(Election election, string playerId)
        {
            if (election.Kind != ElectionKind.SnapStepDown)
                return string.Empty;

            return StepDownRules.CheckCandidate(election.Kind, playerId, DB.Get<WorldProperty>(election.PropertyId)?.OwnerPlayerId);
        }

        private static string CheckRegistrationChange(Election election, string playerId)
        {
            if (election == null)
                return "This election has ended.";

            if (!ElectionRules.IsRegistrationOpen(election.Stage, DateTime.UtcNow, election.RegistrationClosesUtc))
                return "Registration has closed. The ballot is fixed.";

            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null ||
                !ElectionRules.IsCitizenOf(dbPlayer.CitizenPropertyId, election.PropertyId, dbPlayer.IsDeleted))
                return "You must be a registered citizen of this city to run in the election.";

            return string.Empty;
        }

        private static void SetPlatform(Election election, string playerId, string statement, IEnumerable<ElectionPlatformEdict> edicts)
        {
            election.Platforms ??= new Dictionary<string, ElectionPlatform>();
            if (!election.Platforms.TryGetValue(playerId, out var platform))
            {
                platform = new ElectionPlatform();
                election.Platforms[playerId] = platform;
            }

            platform.Statement = ElectionRules.NormalizeStatement(statement);
            platform.Edicts = ElectionPlatformRules.Normalize(edicts);
        }

        private static void RemoveFromBallot(Election election, string playerId)
        {
            election.CandidatePlayerIds.Remove(playerId);
            election.Platforms?.Remove(playerId);

            foreach (var vote in election.VoterSelections.Where(x => x.Value.CandidatePlayerId == playerId).ToList())
            {
                election.VoterSelections.Remove(vote.Key);
                Log.Write(LogGroup.Property, $"Removed vote from CD key '{vote.Key}' because candidate '{playerId}' left the race.");
            }
        }

        #endregion

        #region Votes

        /// <summary>
        /// Records a character's vote, replacing any earlier vote from the same CD key, then pays the voter reward if
        /// the CD key is due one. Returns the reason the vote was refused, or empty on success.
        /// </summary>
        /// <param name="electionId">The election.</param>
        /// <param name="playerId">The voting character.</param>
        /// <param name="cdKey">The voting character's CD key. There is one vote per CD key.</param>
        /// <param name="candidateId">The candidate voted for. In a confidence vote, the lone candidate.</param>
        /// <param name="isNo">True for a No vote in a confidence vote.</param>
        /// <param name="rewardPoints">The RP points paid for this vote, or 0.</param>
        public static string CastVote(string electionId, string playerId, string cdKey, string candidateId, bool isNo, out int rewardPoints)
        {
            rewardPoints = 0;
            var election = DB.Get<Election>(electionId);
            var error = CheckVotingChange(election, playerId, cdKey);
            if (!string.IsNullOrEmpty(error))
                return error;

            if (!election.CandidatePlayerIds.Contains(candidateId))
                return "That candidate is no longer on the ballot.";

            election.VoterSelections[cdKey] = new ElectionVoter
            {
                VoterPlayerId = playerId,
                CandidatePlayerId = candidateId,
                IsNo = election.IsConfidenceVote && isNo
            };
            DB.Set(election);

            rewardPoints = ClaimVoterReward(cdKey, playerId, DateTime.UtcNow);
            return string.Empty;
        }

        /// <summary>
        /// Withdraws the CD key's vote while voting is open. Never pays the reward and never takes it back.
        /// </summary>
        public static string ClearVote(string electionId, string playerId, string cdKey)
        {
            var election = DB.Get<Election>(electionId);
            var error = CheckVotingChange(election, playerId, cdKey);
            if (!string.IsNullOrEmpty(error))
                return error;

            if (election.VoterSelections.Remove(cdKey))
                DB.Set(election);

            return string.Empty;
        }

        private static string CheckVotingChange(Election election, string playerId, string cdKey)
        {
            if (election == null)
                return "This election has ended.";

            if (string.IsNullOrWhiteSpace(cdKey))
                return "Your vote could not be recorded.";

            if (!ElectionRules.IsVotingOpen(election.Stage, DateTime.UtcNow, election.VotingClosesUtc))
                return "Voting is not open.";

            if (!IsEligibleVoter(DB.Get<Player>(playerId), election))
                return "Only citizens who joined this city before registration opened can vote in this election.";

            return string.Empty;
        }

        /// <summary>
        /// Pays the voter reward when the CD key has not earned it in the last 28 days, and returns the RP points paid.
        /// The ledger is written first, so a failure in between loses a reward instead of paying it twice.
        /// </summary>
        public static int ClaimVoterReward(string cdKey, string playerId, DateTime nowUtc)
        {
            var ledger = VoterRewardRules.Claim(DB.Get<VoterRewardLedger>(cdKey), cdKey, nowUtc);
            if (ledger == null)
                return 0;

            DB.Set(ledger);

            var dbPlayer = DB.Get<Player>(playerId);
            dbPlayer.RoleplayProgress.RPPoints += VoterRewardRules.RewardPoints;
            DB.Set(dbPlayer);
            return VoterRewardRules.RewardPoints;
        }

        #endregion

        #region Count

        /// <summary>
        /// The candidates still on the ballot and the votes that count, for the end of voting. A candidate who is no
        /// longer a citizen or was deleted is removed along with the votes for them, and a vote from a character that
        /// is no longer an eligible citizen is dropped.
        /// </summary>
        public static (List<string> Candidates, List<ElectionVoter> Votes) GetCountedBallot(Election election)
        {
            RemoveDepartedCandidates(election);

            var votes = new List<ElectionVoter>();
            foreach (var vote in election.VoterSelections.Values)
            {
                if (!election.CandidatePlayerIds.Contains(vote.CandidatePlayerId))
                    continue;

                if (!IsEligibleVoter(DB.Get<Player>(vote.VoterPlayerId), election))
                    continue;

                votes.Add(vote);
            }

            return (election.CandidatePlayerIds.ToList(), votes);
        }

        #endregion
    }
}
