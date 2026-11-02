using System;
using System.Globalization;
using System.Text;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.PropertyService
{
    /// <summary>
    /// The notices citizens read at login about their city's election: the open stage with its days left, and the
    /// result once after it posts. Pure, so the wording and the "show once" rule can be tested.
    /// </summary>
    public static class ElectionNoticeText
    {
        private const string ElectionLocation = "Use the citizenship terminal in City Hall and choose Election";

        /// <summary>The notice for an election that is open for registration or voting, or empty if neither.</summary>
        /// <param name="election">The city's open election.</param>
        /// <param name="nowUtc">The current time.</param>
        /// <param name="canVote">Whether the reader's character may vote in it.</param>
        public static string BuildStageNotice(Election election, DateTime nowUtc, bool canVote)
        {
            var label = DescribeKind(election.Kind);

            if (election.Stage == ElectionStageType.Registration)
            {
                var days = ElectionRules.GetDaysLeft(nowUtc, election.RegistrationClosesUtc);
                return $"The city's {label} is open for candidate registration, with {DescribeDays(days)} left. " +
                       $"{ElectionLocation} to run for mayor or read the candidates' statements.";
            }

            if (election.Stage != ElectionStageType.Voting)
                return string.Empty;

            var votingDays = ElectionRules.GetDaysLeft(nowUtc, election.VotingClosesUtc);
            var text = new StringBuilder();
            text.Append(election.IsConfidenceVote
                ? $"Citizens are voting Yes or No on the lone candidate in the city's {label}, with {DescribeDays(votingDays)} left. "
                : $"Voting in the city's {label} is open, with {DescribeDays(votingDays)} left. ");

            if (canVote)
            {
                text.Append($"{ElectionLocation} to read the candidates' statements and vote. ");
                text.Append($"Casting a vote earns {VoterRewardRules.RewardPoints} RP points, once every {VoterRewardRules.IntervalDays} days.");
            }
            else
            {
                text.Append("You joined the city after registration opened, so you cannot vote in this election.");
            }

            return text.ToString();
        }

        /// <summary>
        /// True when a citizen should be shown a result at login: they have not read it, they were a citizen when it
        /// posted, and it posted recently.
        /// </summary>
        public static bool IsResultUnread(
            ElectionResultRecord result,
            string seenResultId,
            DateTime? citizenSinceUtc,
            DateTime nowUtc)
        {
            if (result == null || string.IsNullOrWhiteSpace(result.Id))
                return false;

            if (result.Id == seenResultId)
                return false;

            if (!citizenSinceUtc.HasValue || citizenSinceUtc.Value > result.PostedUtc)
                return false;

            return nowUtc < result.PostedUtc.AddDays(ElectionRules.ResultNoticeDays);
        }

        /// <summary>The notice for a posted result.</summary>
        /// <param name="result">The result.</param>
        /// <param name="mayorName">The name the reader knows the mayor after the election by.</param>
        public static string BuildResultNotice(ElectionResultRecord result, string mayorName)
        {
            var label = DescribeKind(result.Kind);
            var text = new StringBuilder($"The city's {label} has ended. ");

            switch (result.Outcome)
            {
                case ElectionOutcomeType.NoCandidates:
                    text.Append($"Nobody ran, so {mayorName} stays mayor.");
                    break;
                case ElectionOutcomeType.NoVotes:
                    text.Append($"No votes were cast, so {mayorName} stays mayor.");
                    break;
                case ElectionOutcomeType.NewMayor:
                    text.Append($"{mayorName} won and is now mayor.");
                    break;
                case ElectionOutcomeType.IncumbentConfirmed:
                    text.Append($"{mayorName} won and stays mayor.");
                    break;
                case ElectionOutcomeType.ChallengerRejected:
                    text.Append($"Citizens voted No on the lone candidate, so {mayorName} stays mayor.");
                    break;
                case ElectionOutcomeType.NoConfidence:
                    text.Append(result.OpenedSnapElection
                        ? $"Citizens voted No on {mayorName}, so a snap election opens now."
                        : $"Citizens voted No on {mayorName}, who stays mayor until the next regular election.");
                    break;
                case ElectionOutcomeType.ConfidenceTie:
                    text.Append($"Yes and No tied, so {mayorName} stays mayor.");
                    break;
                case ElectionOutcomeType.Tie:
                    text.Append($"Candidates tied for the most votes, so {mayorName} stays mayor.");
                    break;
                default:
                    return string.Empty;
            }

            if (result.NextElectionUtc.HasValue)
            {
                text.Append(" The next regular election opens around ");
                text.Append(result.NextElectionUtc.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                text.Append(" UTC.");
            }

            return text.ToString();
        }

        private static string DescribeKind(ElectionKind kind)
        {
            switch (kind)
            {
                case ElectionKind.Recall:
                    return "recall election";
                case ElectionKind.SnapConfidence:
                case ElectionKind.SnapStepDown:
                    return "snap election";
                default:
                    return "mayoral election";
            }
        }

        private static string DescribeDays(int days)
        {
            if (days <= 0)
                return "less than a day";

            return days == 1 ? "1 day" : $"{days} days";
        }
    }
}
