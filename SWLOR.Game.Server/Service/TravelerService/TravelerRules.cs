using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.CityStoreService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service.TravelerService
{
    /// <summary>
    /// The rules of the travelers (Player City Revamp, "Travelers"): who may host one this week, how the weekly rotation
    /// picks hosts, what each carries this week, and where the Traveler's Lodge may stand. Pure, with no NWN calls, so each
    /// rule can be tested. <see cref="Travelers"/> reads the cities and acts on the game.
    /// </summary>
    public static class TravelerRules
    {
        /// <summary>
        /// A city that hosted a traveler in any of the 4 weeks before this one is not asked to host it again, so a traveler
        /// is never in the same city twice within 4 weeks.
        /// </summary>
        public const int RepeatWindowWeeks = 4;

        /// <summary>How likely a city with Open Gates is to be chosen, against 1 for any other: twice as likely.</summary>
        public const int OpenGatesWeight = 2;

        public const int StandardWeight = 1;

        /// <summary>How many weeks of visits a traveler keeps, which is longer than the repeat rule needs.</summary>
        public const int HistoryWeeksKept = 8;

        /// <summary>
        /// The host of a traveler that waits at the hub. A traveler without a city is never in a city's history.
        /// </summary>
        public const string HubHostId = "";

        /// <summary>
        /// The public hub: the Viscara landing waypoint in the Veles starport area, where every shuttle to Viscara arrives.
        /// It is an existing waypoint in the busiest public area of the game, so a traveler waiting there is never out of reach.
        /// </summary>
        public const string HubWaypointTag = "VISCARA_LANDING";

        /// <summary>What players call the hub when its area is not known.</summary>
        public const string HubName = "the Veles starport";

        /// <summary>How far in front of the Lodge a traveler stands.</summary>
        public const float LodgeDistanceMeters = 3.5f;

        #region Rotation

        /// <summary>Whether a traveler chosen for one week is due to move now that this week has begun.</summary>
        /// <param name="rotatedWeekStartUtc">The week the traveler's host was chosen for. A traveler never placed has the minimum date.</param>
        /// <param name="weekStartUtc">The start of the current week.</param>
        public static bool ShouldRotate(DateTime rotatedWeekStartUtc, DateTime weekStartUtc)
        {
            return rotatedWeekStartUtc < weekStartUtc;
        }

        /// <summary>Whether the city hosted the traveler in one of the weeks that count against it.</summary>
        /// <param name="history">The weeks the traveler was hosted.</param>
        /// <param name="cityId">The city.</param>
        /// <param name="weekStartUtc">The start of the week the traveler is being placed for.</param>
        public static bool HostedWithinWindow(IEnumerable<TravelerVisit> history, string cityId, DateTime weekStartUtc)
        {
            var earliest = weekStartUtc.AddDays(-7 * RepeatWindowWeeks);
            return (history ?? Enumerable.Empty<TravelerVisit>()).Any(visit =>
                string.Equals(visit.CityId, cityId, StringComparison.Ordinal) && visit.WeekStartUtc >= earliest);
        }

        /// <summary>How much a city counts when a host is picked: twice as much with Open Gates.</summary>
        public static int GetWeight(bool hasOpenGates)
        {
            return hasOpenGates ? OpenGatesWeight : StandardWeight;
        }

        /// <summary>
        /// Picks the host of each traveler that is due to move. Each picks among the cities with a Lodge that did not host it
        /// in the last 4 weeks, with Open Gates cities twice as likely. Two travelers share a host only when no other city
        /// qualifies for the second, which is then preferred to waiting at the hub. A traveler that no city qualifies for
        /// waits at the hub (<see cref="HubHostId"/>). Travelers pick in a random order, so none always chooses first.
        /// </summary>
        /// <param name="travelers">The travelers due to move, with their history.</param>
        /// <param name="hosts">The cities with a Lodge.</param>
        /// <param name="occupiedHostIds">The hosts of travelers that are not moving, which the others avoid like any taken host.</param>
        /// <param name="weekStartUtc">The start of the week they are placed for.</param>
        /// <param name="nextInt">A random number below the given maximum.</param>
        public static IReadOnlyDictionary<TravelerType, string> PlanRotation(
            IReadOnlyList<TravelerRotationInput> travelers,
            IReadOnlyList<TravelerHostCandidate> hosts,
            IReadOnlyCollection<string> occupiedHostIds,
            DateTime weekStartUtc,
            Func<int, int> nextInt)
        {
            var plan = new Dictionary<TravelerType, string>();
            var taken = new HashSet<string>(occupiedHostIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            var candidates = (hosts ?? Array.Empty<TravelerHostCandidate>())
                .Where(host => !string.IsNullOrWhiteSpace(host.CityId))
                .GroupBy(host => host.CityId, StringComparer.Ordinal)
                .Select(group => new TravelerHostCandidate(group.Key, group.Any(host => host.HasOpenGates)))
                .ToList();

            foreach (var traveler in Shuffle(travelers ?? Array.Empty<TravelerRotationInput>(), nextInt))
            {
                var eligible = candidates
                    .Where(host => !HostedWithinWindow(traveler.History, host.CityId, weekStartUtc))
                    .ToList();
                var free = eligible.Where(host => !taken.Contains(host.CityId)).ToList();

                var chosen = PickWeighted(free.Count > 0 ? free : eligible, nextInt);
                plan[traveler.Type] = chosen ?? HubHostId;
                if (chosen != null)
                    taken.Add(chosen);
            }

            return plan;
        }

        /// <summary>
        /// Whether a traveler's host can no longer host it: its city is gone or has no Lodge. It waits at the hub until
        /// next week. A traveler already at the hub has nothing to lose.
        /// </summary>
        /// <param name="hostCityId">The traveler's host, or an empty string for the hub.</param>
        /// <param name="lodgeCityIds">The cities that have a Lodge now.</param>
        public static bool MustMoveToHub(string hostCityId, IReadOnlyCollection<string> lodgeCityIds)
        {
            if (string.IsNullOrWhiteSpace(hostCityId))
                return false;

            return lodgeCityIds == null || !lodgeCityIds.Contains(hostCityId);
        }

        /// <summary>
        /// The history after the traveler is placed for a week. A host adds a visit, and the hub adds nothing. Visits older
        /// than <see cref="HistoryWeeksKept"/> weeks are dropped, and placing the same week twice leaves one visit.
        /// </summary>
        public static List<TravelerVisit> AppendVisit(IEnumerable<TravelerVisit> history, string hostCityId, DateTime weekStartUtc)
        {
            var oldest = weekStartUtc.AddDays(-7 * HistoryWeeksKept);
            var visits = (history ?? Enumerable.Empty<TravelerVisit>())
                .Where(visit => visit.WeekStartUtc >= oldest && visit.WeekStartUtc != weekStartUtc)
                .Select(visit => new TravelerVisit(visit.CityId, visit.WeekStartUtc))
                .ToList();

            if (!string.IsNullOrWhiteSpace(hostCityId))
                visits.Add(new TravelerVisit(hostCityId, weekStartUtc));

            return visits.OrderBy(visit => visit.WeekStartUtc).ToList();
        }

        private static string PickWeighted(IReadOnlyList<TravelerHostCandidate> pool, Func<int, int> nextInt)
        {
            if (pool.Count == 0)
                return null;

            var total = pool.Sum(host => GetWeight(host.HasOpenGates));
            var roll = nextInt(total);
            foreach (var host in pool)
            {
                roll -= GetWeight(host.HasOpenGates);
                if (roll < 0)
                    return host.CityId;
            }

            return pool[^1].CityId;
        }

        private static List<TravelerRotationInput> Shuffle(IReadOnlyList<TravelerRotationInput> travelers, Func<int, int> nextInt)
        {
            var order = travelers.ToList();
            for (var i = order.Count - 1; i > 0; i--)
            {
                var j = nextInt(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            return order;
        }

        #endregion

        #region Stock

        /// <summary>
        /// What a traveler carries in the week that contains this moment: for each pool, a draw of its picks that is the same
        /// for everyone, wherever the traveler is and however often the server restarts, and a different draw the next week.
        /// </summary>
        public static IReadOnlyList<TravelerStockItem> GetWeeklyStock(TravelerDetail traveler, DateTime nowUtc)
        {
            var weekStart = CityWeek.GetWeekStart(nowUtc);
            var stock = new List<TravelerStockItem>();
            if (traveler == null)
                return stock;

            foreach (var group in traveler.Groups)
                stock.AddRange(Draw(traveler.Key, group, weekStart));

            return stock;
        }

        /// <summary>The item of this week's stock with this resref, or null when the traveler does not carry it this week.</summary>
        public static TravelerStockItem FindStockItem(TravelerDetail traveler, DateTime nowUtc, string resref)
        {
            return GetWeeklyStock(traveler, nowUtc).FirstOrDefault(item => string.Equals(item.Resref, resref, StringComparison.Ordinal));
        }

        /// <summary>
        /// What the traveler puts on the shelf right now: this week's stock, each for what is left of its weekly allowance.
        /// An item sold out for the week is left off. The allowance belongs to the traveler, so the same shelf is planned
        /// in whichever city hosts it and after any restart.
        /// </summary>
        /// <param name="traveler">The traveler.</param>
        /// <param name="nowUtc">The moment the shelf is planned for.</param>
        /// <param name="getRecord">The traveler's allowance record of an item resref, or null when none has sold.</param>
        public static IReadOnlyList<CityStoreShelfEntry> PlanShelf(
            TravelerDetail traveler,
            DateTime nowUtc,
            Func<string, IStockAllowanceRecord> getRecord)
        {
            var items = GetWeeklyStock(traveler, nowUtc)
                .Select(item => new CityStoreLimitedItem(item.Resref, 1, item.WeeklyLimit));

            return StockAllowanceRules.PlanShelf(items, 1, getRecord, CityWeek.GetWeekStart(nowUtc));
        }

        private static List<TravelerStockItem> Draw(string travelerKey, TravelerStockGroup group, DateTime weekStartUtc)
        {
            var picks = Math.Min(Math.Max(0, group.Picks), group.Items.Count);
            var candidates = group.Items.ToList();

            var random = new System.Random(GetSeed(travelerKey, group.Name, weekStartUtc));
            for (var i = 0; i < picks; i++)
            {
                var j = random.Next(i, candidates.Count);
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            }

            return candidates.Take(picks).ToList();
        }

        /// <summary>
        /// A seed that is the same on every machine and every run. A string hash code is not, since .NET picks a new one for
        /// each process.
        /// </summary>
        private static int GetSeed(string travelerKey, string groupName, DateTime weekStartUtc)
        {
            var text = $"{travelerKey}|{groupName}|{weekStartUtc:yyyyMMdd}";
            unchecked
            {
                var hash = 2166136261u;
                foreach (var c in text)
                    hash = (hash ^ c) * 16777619u;

                return (int)hash;
            }
        }

        #endregion

        #region The Lodge

        /// <summary>The refusal for placing a Traveler's Lodge, or an empty string. A city has at most one.</summary>
        /// <param name="structureType">The structure being placed. Anything but the Lodge is not this rule's business.</param>
        /// <param name="lodgesInCity">How many Lodges the city already has.</param>
        public static string GetLodgePlacementError(StructureType structureType, int lodgesInCity)
        {
            if (structureType != StructureType.TravelersLodge)
                return string.Empty;

            return lodgesInCity >= 1 ? "A city may have only 1 Traveler's Lodge." : string.Empty;
        }

        /// <summary>
        /// Where a traveler stands and which way they face: a fixed distance to the Lodge's side, looking out, as a market
        /// stall's vendor does, turned with the Lodge.
        /// </summary>
        public static (float X, float Y, float FacingDegrees) GetLodgePlacement(float lodgeX, float lodgeY, float lodgeFacingDegrees)
        {
            var facing = (lodgeFacingDegrees + 90f) % 360f;
            if (facing < 0f)
                facing += 360f;

            var radians = facing * (MathF.PI / 180f);
            return (lodgeX + LodgeDistanceMeters * MathF.Cos(radians), lodgeY + LodgeDistanceMeters * MathF.Sin(radians), facing);
        }

        #endregion
    }
}
