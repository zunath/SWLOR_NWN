using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Neglect and recall (Player City Revamp): ignored situations open a recall election, and a recall nobody takes part
    /// in marks the city Abandoned, opens a second, and then removes the city or keeps it paused. The rules live in
    /// <see cref="RecallRules"/>; this class reads and writes the records.
    /// </summary>
    public static class CityRecall
    {
        /// <summary>Opens a recall election when ignored situations reached the threshold and no election is open.</summary>
        public static void OpenIfNeglected(DateTime now, WorldProperty city)
        {
            var isElectionOpen = ElectionBallot.GetCityElection(city.Id) != null;
            if (!RecallRules.ShouldOpenRecall(city.NeglectCount, isElectionOpen, city.NeglectSinceUtc.HasValue))
                return;

            Property.OpenElection(now, city, ElectionKind.Recall, null);
            Log.Write(LogGroup.Property, $"City '{city.CustomName}' ({city.Id}) ignored {city.NeglectCount} situations in a row. A recall election opened.");
        }

        /// <summary>
        /// What an election's end does to the recall path. The second recall counts the citizens who logged in during the
        /// last 14 days before it ended.
        /// </summary>
        public static RecallStep Decide(WorldProperty city, ElectionKind kind, bool isEmpty, DateTime endedUtc)
        {
            var activeCitizens = 0;
            if (kind == ElectionKind.Recall && isEmpty && city.RecallStage >= RecallRules.SecondRecallStage)
                activeCitizens = CountActiveCitizens(city.Id, endedUtc);

            return RecallRules.Decide(kind, isEmpty, city.RecallStage, activeCitizens);
        }

        /// <summary>Updates the city for the end of an election. The caller saves the city.</summary>
        public static void Apply(DateTime now, WorldProperty city, ElectionKind kind, bool citizensTookPart, RecallStep step)
        {
            city.RecallStage = RecallRules.GetNextRecallStage(kind, step, city.RecallStage);
            city.NeglectCount = RecallRules.GetNextNeglectCount(city.NeglectCount, citizensTookPart, kind, step);

            if (step == RecallStep.KeepWithNeglect)
                city.NeglectSinceUtc = now;

            if (step == RecallStep.RemoveCity)
                city.IsQueuedForDeletion = true;
        }

        /// <summary>Tells the Gazette and Discord what the end of the recall path did.</summary>
        public static void Announce(WorldProperty city, RecallStep step)
        {
            switch (step)
            {
                case RecallStep.OpenSecondRecall:
                    Property.BroadcastPropertyEvent(
                        city,
                        "City Abandoned",
                        "Nobody took part in this city's recall election, so the city has been marked Abandoned. A second recall election has opened.",
                        15105570);
                    break;
                case RecallStep.RemoveCity:
                    Property.BroadcastPropertyEvent(
                        city,
                        "City Expiration",
                        "This city has been marked for removal. Nobody took part in two recall elections, and fewer than " +
                        $"{RecallRules.MinimumActiveCitizens} citizens logged in during the last {RecallRules.ActivityWindowDays} days.",
                        15158332);
                    break;
                case RecallStep.KeepWithNeglect:
                    Property.BroadcastPropertyEvent(
                        city,
                        "City Neglected",
                        "Nobody took part in two recall elections, but citizens still use the city, so it stays. The Abandoned mark is cleared. " +
                        "Its Edicts are paused and no situations arrive until an election with a candidate and a vote ends.",
                        15105570);
                    break;
            }
        }

        private static int CountActiveCitizens(string cityId, DateTime endedUtc)
        {
            var query = new DBQuery<Player>()
                .AddFieldSearch(nameof(Player.CitizenPropertyId), cityId, false)
                .AddFieldSearch(nameof(Player.IsDeleted), false);
            var count = (int)DB.SearchCount(query);
            if (count <= 0)
                return 0;

            return RecallRules.CountActiveCitizens(
                DB.Search(query.AddPaging(count, 0)).Select(player => player.LastLoginDate).ToList(),
                endedUtc);
        }
    }
}
