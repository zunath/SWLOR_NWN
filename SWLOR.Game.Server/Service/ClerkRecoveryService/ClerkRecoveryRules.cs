using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.FarmingService;
using SWLOR.Game.Server.Service.LivestockService;

namespace SWLOR.Game.Server.Service.ClerkRecoveryService
{
    /// <summary>
    /// The City Hall clerk's recovery rules (Player City Revamp, "Farming", "Livestock" and "Recovery"), with no database or engine
    /// calls: who cannot reach their crop or animal, which actions each allows, when an owner counts as absent for the mayor's
    /// send-away, and the marks and overflow record ids that make a clerk action repeatable after a restart. The
    /// <c>ClerkRecovery</c> service reads and writes the records and applies them.
    /// </summary>
    public static class ClerkRecoveryRules
    {
        /// <summary>An owner who left the city more than this many days ago is absent: the mayor can send their animals away.</summary>
        public const int AbsentDays = 7;

        /// <summary>
        /// The city power that sends an absent owner's animals away. Only the mayor holds it: the Plan names the mayor, no permission
        /// grants Grant City Permissions, and an Acting Mayor does not hold it.
        /// </summary>
        public const CityPower SendAwayPower = CityPower.GrantPermissions;

        #region Who cannot reach

        /// <summary>
        /// Whether a planter can reach their crop themselves. They cannot once their garden permission on the plot's house or city is
        /// gone (removed, the house changed hands, or they left the city, which removes it) or they are no longer a citizen of the
        /// city the plot stands in.
        /// </summary>
        public static bool CanReachCrop(bool holdsGardenPermission, bool isCitizenOfCity) => holdsGardenPermission && isCitizenOfCity;

        /// <summary>Whether an owner can reach their corral: a citizen of its city. One who left the city or was deleted cannot.</summary>
        public static bool CanReachAnimal(bool isCitizenOfCity) => isCitizenOfCity;

        #endregion

        #region Offered actions

        /// <summary>The actions the clerk offers for the planter's crop: harvest when it is ripe, and clear at any stage short of withered.</summary>
        public static IReadOnlyList<ClerkRecoveryAction> GetCropActions(GardenPlot record, DateTime now)
        {
            var actions = new List<ClerkRecoveryAction>();
            if (record == null || FarmingRules.GetStage(record, now) is GardenPlotStage.Empty or GardenPlotStage.Withered)
                return actions;

            if (FarmingRules.CheckHarvest(true, record, now, true) == FarmingRefusal.None)
                actions.Add(ClerkRecoveryAction.Harvest);
            if (FarmingRules.CheckClear(true, record, now, true) == FarmingRefusal.None)
                actions.Add(ClerkRecoveryAction.Clear);
            return actions;
        }

        /// <summary>
        /// The actions the clerk offers for the owner's animal: the ones the corral allows its owner right now. Crating is always
        /// possible; collecting and butchering need an adult, and collecting also a recent meal, no penalty and 20 hours.
        /// </summary>
        public static IReadOnlyList<ClerkRecoveryAction> GetAnimalActions(Livestock animal, DateTime now)
        {
            var actions = new List<ClerkRecoveryAction>();
            if (animal == null)
                return actions;

            if (LivestockRules.CheckCollect(true, animal, now, true) == LivestockRefusal.None)
                actions.Add(ClerkRecoveryAction.Collect);
            if (LivestockRules.CheckButcher(true, animal, now, true) == LivestockRefusal.None)
                actions.Add(ClerkRecoveryAction.Butcher);
            if (LivestockRules.CheckCrate(true, animal, now, true) == LivestockRefusal.None)
                actions.Add(ClerkRecoveryAction.Crate);
            return actions;
        }

        #endregion

        #region Absent owners

        /// <summary>
        /// Whether an animal's owner is absent, so the mayor can send the animal away: the owner is not a citizen of the corral's city
        /// and left it more than <see cref="AbsentDays"/> days ago. A deleted character cannot come back, so it counts at once; one
        /// whose departure was never recorded cannot be shown to have left, so it does not.
        /// </summary>
        /// <param name="isCitizenOfCity">Whether the owner is a citizen of the corral's city now.</param>
        /// <param name="isDeleted">Whether the owner's character is deleted or its record is gone.</param>
        /// <param name="leftCityUtc">When the owner stopped being a citizen of that city, or null when it was never recorded.</param>
        /// <param name="now">The time now.</param>
        public static bool IsAbsent(bool isCitizenOfCity, bool isDeleted, DateTime? leftCityUtc, DateTime now)
        {
            if (isCitizenOfCity)
                return false;
            if (isDeleted)
                return true;

            return leftCityUtc.HasValue && now - leftCityUtc.Value > TimeSpan.FromDays(AbsentDays);
        }

        /// <summary>Notes the time a character stopped being a citizen of the city. A character who left twice counts from the latest.</summary>
        /// <param name="departures">The character's departures by city id, or null for a record that has none.</param>
        public static Dictionary<string, DateTime> RecordDeparture(Dictionary<string, DateTime> departures, string cityId, DateTime now)
        {
            departures ??= new Dictionary<string, DateTime>();
            if (!string.IsNullOrWhiteSpace(cityId))
                departures[cityId] = now;
            return departures;
        }

        /// <summary>Ends a character's absence from a city: they are a citizen of it again.</summary>
        public static Dictionary<string, DateTime> ClearDeparture(Dictionary<string, DateTime> departures, string cityId)
        {
            departures ??= new Dictionary<string, DateTime>();
            if (!string.IsNullOrWhiteSpace(cityId))
                departures.Remove(cityId);
            return departures;
        }

        #endregion

        #region Marks and overflow records

        /// <summary>The mark of a harvest: the crop's items and, when the Seed Saver roll returned it, one seed.</summary>
        public static ClerkRecoveryMark BuildHarvestMark(
            string markId,
            string planterId,
            DateTime now,
            string harvestResref,
            int yield,
            string seedResref,
            bool seedReturned)
        {
            var mark = NewMark(markId, ClerkRecoveryAction.Harvest, planterId, now);
            Add(mark, harvestResref, yield);
            if (seedReturned)
                Add(mark, seedResref, 1);
            return mark;
        }

        /// <summary>The mark of a collection or a butchering: the products it gives.</summary>
        public static ClerkRecoveryMark BuildProductMark(
            string markId,
            ClerkRecoveryAction action,
            string ownerId,
            DateTime now,
            IEnumerable<(string Resref, int Count)> items)
        {
            var mark = NewMark(markId, action, ownerId, now);
            foreach (var (resref, count) in items)
                Add(mark, resref, count);
            return mark;
        }

        /// <summary>The mark of a crating: the animal as it stood, packed.</summary>
        public static ClerkRecoveryMark BuildCrateMark(string markId, string ownerId, DateTime now, LivestockCrate crate)
        {
            var mark = NewMark(markId, ClerkRecoveryAction.Crate, ownerId, now);
            mark.Crate = crate;
            return mark;
        }

        private static ClerkRecoveryMark NewMark(string markId, ClerkRecoveryAction action, string playerId, DateTime now) => new()
        {
            Id = markId,
            Action = action,
            PlayerId = playerId,
            AtUtc = now
        };

        private static void Add(ClerkRecoveryMark mark, string resref, int count)
        {
            if (string.IsNullOrWhiteSpace(resref) || count <= 0)
                return;

            mark.Items[resref] = mark.Items.GetValueOrDefault(resref) + count;
        }

        /// <summary>
        /// The items a mark gives in the order their overflow records are numbered: by resref, so the numbering never depends on the
        /// order they were added in or read back in.
        /// </summary>
        public static IReadOnlyList<KeyValuePair<string, int>> GetItems(ClerkRecoveryMark mark)
        {
            return mark.Items
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && pair.Value > 0)
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        /// The prefix of the overflow record ids of one delivery of a mark. It never changes, so an action finished again after a
        /// restart writes the same records instead of a second set.
        /// </summary>
        /// <param name="markId">The mark's id.</param>
        /// <param name="index">The delivery's number among the mark's items, then its crate.</param>
        public static string GetOverflowRecordPrefix(string markId, int index) => $"clerk-recovery:{markId}:{index}";

        /// <summary>The id of one overflow record of a delivery: a stack of the item, or the crate.</summary>
        public static string GetOverflowRecordId(string recordPrefix, int chunk) => $"{recordPrefix}:{chunk}";

        #endregion

        #region Text

        public const string RecoverResponse = "Recover my crops and animals";

        public const string ListHeader = "These are your crops and animals in this city that you can no longer reach. What I recover for you goes to your overflow storage; use /overflow to claim it.";

        public const string NothingHeader = "You have no crops or animals in this city that you cannot reach. Crops and animals you can reach are tended at their plot or corral.";

        public const string NotOffered = "That is not one of your crops or animals that you cannot reach in this city.";

        public const string CanReachCropText = "You can reach that crop yourself. Harvest it at its plot.";

        public const string CanReachAnimalText = "You can reach that animal yourself. Talk to it at its corral.";

        public const string NoCratesText = "Crates are not available yet, so the animal stays where it is.";

        public const string SendAwayNoPower = "Only the mayor can send an absent owner's animals away.";

        public const string SendAwayNone = "No animal here belongs to an owner who left the city more than 7 days ago.";

        public static string DescribeCropTitle(string cropName, GardenPlotStage stage) =>
            $"{cropName} ({FarmingRules.DescribeStage(stage).ToLowerInvariant()})";

        public static string DescribeCrop(string cropName, string placeName, GardenPlot record, DateTime now)
        {
            var stage = FarmingRules.GetStage(record, now);
            var where = string.IsNullOrWhiteSpace(placeName) ? string.Empty : $" in {placeName}";
            var state = stage == GardenPlotStage.Ripe
                ? $"It is ripe and withers in {FarmingRules.FormatDuration(FarmingRules.GetTimeLeft(record, now))}."
                : $"It ripens in {FarmingRules.FormatDuration(FarmingRules.GetTimeLeft(record, now))}.";
            return $"{cropName}{where}. {state}";
        }

        public static string DescribeAnimalTitle(string animalName, Livestock animal, DateTime now) =>
            $"{animalName} ({LivestockRules.DescribeStage(LivestockRules.GetStage(animal, now)).ToLowerInvariant()})";

        public static string DescribeAnimal(string animalName, Livestock animal, DateTime now)
        {
            var lines = new List<string>
            {
                $"{animalName}: {LivestockRules.DescribeStage(LivestockRules.GetStage(animal, now)).ToLowerInvariant()}, fed {animal.Feedings} {(animal.Feedings == 1 ? "time" : "times")}.",
                LivestockRules.DescribeGrowth(animal, now),
                LivestockRules.DescribeHunger(animal, now)
            };

            if (LivestockRules.IsAdult(animal, now))
            {
                var collect = LivestockRules.CheckCollect(true, animal, now, true);
                lines.Add(collect == LivestockRefusal.None
                    ? "It is ready to be collected from."
                    : LivestockRules.Describe(collect));
            }

            return string.Join("\n", lines.Where(line => !string.IsNullOrWhiteSpace(line)));
        }

        /// <summary>The message for a harvest the clerk made: all of it went to overflow storage.</summary>
        public static string DescribeRemoteHarvest(string cropName, int items, bool seedReturned)
        {
            var text = $"The clerk harvested the {cropName} for you: {items} {(items == 1 ? "item" : "items")}";
            text += seedReturned ? " and its seed" : string.Empty;
            return text + ". They are in your overflow storage. Use /overflow to claim them.";
        }

        /// <summary>The message for a collection or butchering the clerk made.</summary>
        public static string DescribeRemoteProducts(string verb, string animalName, IEnumerable<(string Name, int Count)> items)
        {
            var list = string.Join(", ", items.Select(item => $"{item.Count} {item.Name}"));
            return $"The clerk {verb} the {animalName} for you: {list}. They are in your overflow storage. Use /overflow to claim them.";
        }

        /// <summary>The message for an animal the clerk crated: the crate is in overflow storage.</summary>
        public static string DescribeRemoteCrated(string animalName) =>
            $"The clerk crated the {animalName}. The crate is in your overflow storage. Use /overflow to claim it.";

        /// <summary>The notice an absent owner reads about an animal the mayor sent away.</summary>
        public static string DescribeSentAway(string animalName, string cityName) =>
            $"The mayor of {cityName} sent your {animalName} away from the corrals, crated, because you left the city more than {AbsentDays} days ago. The crate is in your overflow storage. Use /overflow to claim it.";

        public static string DescribeSentAwayResult(int animals, string cityName) =>
            $"You sent {animals} {(animals == 1 ? "animal" : "animals")} of owners who left {cityName} away. Each is crated in its owner's overflow storage.";

        #endregion
    }
}
