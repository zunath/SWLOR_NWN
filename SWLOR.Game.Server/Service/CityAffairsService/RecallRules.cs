using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service.CityAffairsService
{
    /// <summary>
    /// The rules of neglect and recall (Player City Revamp, "Neglect and recall"): when ignored situations open a recall,
    /// and what an empty recall does. Pure, so the dated example in the Plan can be tested.
    /// </summary>
    public static class RecallRules
    {
        /// <summary>Situations in a row that expire unhandled before a recall opens.</summary>
        public const int NeglectThreshold = 5;

        /// <summary>Days before the second recall ended that a citizen's last login must fall within.</summary>
        public const int ActivityWindowDays = 14;

        /// <summary>The level 1 minimum. Fewer citizens than this in the activity window and the city is removed.</summary>
        public const int MinimumActiveCitizens = 5;

        /// <summary>The recall stage once a first recall ended empty. The second recall is open until it ends.</summary>
        public const int SecondRecallStage = 1;

        /// <summary>The count after one more situation expired unhandled.</summary>
        public static int AddExpired(int neglectCount, int expiredCount)
        {
            return Math.Max(0, neglectCount) + Math.Max(0, expiredCount);
        }

        /// <summary>
        /// Whether a recall opens: the neglect count is at the threshold, no election is open, and the city is not already
        /// paused for neglect. While an election is open the count waits, so a second election never opens beside it.
        /// </summary>
        public static bool ShouldOpenRecall(int neglectCount, bool isElectionOpen, bool isNeglected)
        {
            return neglectCount >= NeglectThreshold && !isElectionOpen && !isNeglected;
        }

        /// <summary>The city is marked Abandoned from the first empty recall until the second recall ends.</summary>
        public static bool IsMarkedAbandoned(int recallStage)
        {
            return recallStage >= SecondRecallStage;
        }

        /// <summary>The earliest last-login time that counts a citizen as using the city, for a recall that ended at the given time.</summary>
        public static DateTime GetActivityCutoffUtc(DateTime recallEndedUtc)
        {
            return recallEndedUtc.AddDays(-ActivityWindowDays);
        }

        /// <summary>The citizens whose last login is on or after the cutoff.</summary>
        public static int CountActiveCitizens(IEnumerable<DateTime?> lastLogins, DateTime recallEndedUtc)
        {
            var cutoff = GetActivityCutoffUtc(recallEndedUtc);
            return (lastLogins ?? Enumerable.Empty<DateTime?>()).Count(login => login.HasValue && login.Value >= cutoff);
        }

        /// <summary>
        /// What a recall election's end does. Only an empty recall does anything: the first opens the second, and the second
        /// removes a city nobody logs in to or keeps one that citizens use but not its politics.
        /// </summary>
        /// <param name="kind">The kind of election that ended.</param>
        /// <param name="isEmpty">No candidates, no votes, or every candidate left.</param>
        /// <param name="recallStage">The city's recall stage when the election ended.</param>
        /// <param name="activeCitizens">The citizens who logged in during the activity window. Only read for the second recall.</param>
        public static RecallStep Decide(ElectionKind kind, bool isEmpty, int recallStage, int activeCitizens)
        {
            if (kind != ElectionKind.Recall || !isEmpty)
                return RecallStep.None;

            if (recallStage < SecondRecallStage)
                return RecallStep.OpenSecondRecall;

            return activeCitizens < MinimumActiveCitizens ? RecallStep.RemoveCity : RecallStep.KeepWithNeglect;
        }

        /// <summary>
        /// The recall stage after an election ended: the first empty recall moves to the second, and anything else that ends
        /// a recall clears it. A regular or snap election leaves it alone.
        /// </summary>
        public static int GetNextRecallStage(ElectionKind kind, RecallStep step, int recallStage)
        {
            if (kind != ElectionKind.Recall)
                return recallStage;

            return step == RecallStep.OpenSecondRecall ? SecondRecallStage : 0;
        }

        /// <summary>
        /// The neglect count after an election ended. An election that ends with a candidate and a vote means citizens took
        /// part, so the count starts over; so does a recall that ends empty, since the recall path takes over from there.
        /// </summary>
        public static int GetNextNeglectCount(int neglectCount, bool citizensTookPart, ElectionKind kind, RecallStep step)
        {
            if (citizensTookPart)
                return 0;

            return kind == ElectionKind.Recall && step != RecallStep.None ? 0 : neglectCount;
        }
    }
}
