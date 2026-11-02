using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.FarmingService;
using SWLOR.Game.Server.Service.LivestockService;

namespace SWLOR.Game.Server.Service.FarmhandService
{
    /// <summary>
    /// The farmhand rules of the Player City Revamp ("Farmhands"), with no database or engine calls: the hoppers, which tiers a
    /// droid handles, and the replay that brings a house or corral up to date. A farmhand keeps its schedule while nobody is
    /// around, so whenever its house or corral is next processed (a server start, someone entering, or the owner using it) the
    /// replay lists every event that fell due since the last time, sorts them by time, and applies them in order against the
    /// hoppers: crops are tended when they become ready, animals are fed as soon as they may eat while feed lasts, adults are
    /// collected from while the output hopper has room, and animals and crops that go 72 hours without care are lost. Then the
    /// caller sets the farmhand's last processed time to now, so replaying the same span again does nothing new.
    /// </summary>
    public static class FarmhandRules
    {
        /// <summary>The crops the feed hopper holds, and, separately, the items the output hopper holds.</summary>
        public const int HopperCapacity = 20;

        /// <summary>The most events one replay applies. It is a guard: a replay ends long before it, since each event needs feed, hopper room or time.</summary>
        public const int MaxReplaySteps = 20000;

        // Hoppers

        public static int CountFeed(IEnumerable<KeyValuePair<CropType, int>> hopper) => hopper.Sum(pair => Math.Max(0, pair.Value));

        public static int CountOutput(IEnumerable<KeyValuePair<string, int>> hopper) => hopper.Sum(pair => Math.Max(0, pair.Value));

        public static int GetFeedRoom(IEnumerable<KeyValuePair<CropType, int>> hopper) => Math.Max(0, HopperCapacity - CountFeed(hopper));

        public static int GetOutputRoom(IEnumerable<KeyValuePair<string, int>> hopper) => Math.Max(0, HopperCapacity - CountOutput(hopper));

        /// <summary>The highest crop or animal tier a droid of this tier handles.</summary>
        public static bool Handles(int droidTier, int tier) => tier >= 1 && tier <= droidTier;

        /// <summary>Adds crops to the feed hopper, up to its room. Returns how many went in.</summary>
        public static int LoadFeed(IDictionary<CropType, int> hopper, CropType crop, int count)
        {
            var added = Math.Clamp(count, 0, GetFeedRoom(hopper));
            if (added > 0)
                hopper[crop] = (hopper.TryGetValue(crop, out var held) ? held : 0) + added;
            return added;
        }

        /// <summary>
        /// Takes the crops of one feeding out of the feed hopper: <see cref="LivestockRules.FeedCrops"/> crops of the animal's
        /// tier or lower, the lowest tier first so the best crops last. False, taking nothing, when the hopper holds too few.
        /// </summary>
        public static bool TryTakeFeed(IDictionary<CropType, int> hopper, int animalTier, Func<CropType, CropDefinition> getCrop)
        {
            var eligible = GetEligibleFeed(hopper, animalTier, getCrop);
            if (eligible.Sum(entry => entry.Count) < LivestockRules.FeedCrops)
                return false;

            var needed = LivestockRules.FeedCrops;
            foreach (var (crop, count) in eligible)
            {
                var taken = Math.Min(needed, count);
                hopper[crop] -= taken;
                if (hopper[crop] <= 0)
                    hopper.Remove(crop);
                needed -= taken;
                if (needed == 0)
                    break;
            }

            return true;
        }

        public static bool HasFeed(IEnumerable<KeyValuePair<CropType, int>> hopper, int animalTier, Func<CropType, CropDefinition> getCrop) =>
            GetEligibleFeed(hopper, animalTier, getCrop).Sum(entry => entry.Count) >= LivestockRules.FeedCrops;

        private static List<(CropType Crop, int Count)> GetEligibleFeed(
            IEnumerable<KeyValuePair<CropType, int>> hopper,
            int animalTier,
            Func<CropType, CropDefinition> getCrop)
        {
            return hopper
                .Where(pair => pair.Value > 0)
                .Select(pair => (Crop: pair.Key, Count: pair.Value, Definition: getCrop(pair.Key)))
                .Where(entry => entry.Definition != null && LivestockRules.CanEat(animalTier, entry.Definition.Tier))
                .OrderBy(entry => entry.Definition.Tier)
                .ThenBy(entry => (int)entry.Crop)
                .Select(entry => (entry.Crop, entry.Count))
                .ToList();
        }

        /// <summary>Adds collected items to the output hopper.</summary>
        public static void AddOutput(IDictionary<string, int> hopper, IEnumerable<(string Resref, int Count)> items)
        {
            foreach (var (resref, count) in items)
                hopper[resref] = (hopper.TryGetValue(resref, out var held) ? held : 0) + count;
        }

        // Checks

        /// <summary>Why a character cannot assign a droid, or <see cref="FarmhandRefusal.None"/>. Checked in this order.</summary>
        public static FarmhandRefusal CheckAssign(bool isPlayer, bool targetExists, bool hasAccess, bool alreadyAssigned, bool droidAvailable)
        {
            if (!isPlayer)
                return FarmhandRefusal.NotAPlayer;
            if (!targetExists)
                return FarmhandRefusal.NoTarget;
            if (!hasAccess)
                return FarmhandRefusal.NoAccess;
            if (alreadyAssigned)
                return FarmhandRefusal.AlreadyAssigned;
            if (!droidAvailable)
                return FarmhandRefusal.NoDroid;
            return FarmhandRefusal.None;
        }

        public static string Describe(FarmhandRefusal refusal)
        {
            return refusal switch
            {
                FarmhandRefusal.None => string.Empty,
                FarmhandRefusal.NotAPlayer => "Only characters can assign farmhands.",
                FarmhandRefusal.NoTarget => "This house or corral is no longer here.",
                FarmhandRefusal.NoAccess => "A farmhand works one of your own houses, or a corral in your city.",
                FarmhandRefusal.AlreadyAssigned => "A droid already works here.",
                FarmhandRefusal.NoDroid => "You no longer carry that droid, or it is busy.",
                FarmhandRefusal.NoFarmhand => "That droid cannot take the post.",
                FarmhandRefusal.NotTheOwner => "Only the droid's owner can do that.",
                FarmhandRefusal.FeedHopperFull => $"The feed hopper already holds {HopperCapacity} crops.",
                FarmhandRefusal.NoCrops => "You carry none of that crop.",
                FarmhandRefusal.NothingToTake => "The output hopper is empty.",
                FarmhandRefusal.NoFarmhandHere => "No droid works here.",
                _ => string.Empty
            };
        }

        // The replay

        private const int WanderPriority = 0;
        private const int WitherPriority = 1;
        private const int TendPriority = 2;
        private const int FeedPriority = 3;
        private const int MaturePriority = 4;
        private const int CollectPriority = 5;
        private const int RipePriority = 6;

        private sealed class Step
        {
            public DateTime Time;
            public int Priority;
            public string Id;
            public Action Apply;
        }

        /// <summary>
        /// Brings the farmhand's house or corral up to date at <paramref name="now"/>: lists every event that fell due since the
        /// farmhand was last processed (a crop ready to tend, ripening and withering; an animal's next feed while feed lasts,
        /// growing up, collection, and wandering off 72 hours after its last meal), sorts them by time and applies them in order.
        /// It changes the farmhand's hoppers and last processed time, and the animals and plots it was given, in place. A span
        /// that is empty or backwards does nothing, and neither does replaying a span a second time.
        /// </summary>
        /// <param name="animals">The animals in the farmhand's corral. Only its owner's are worked.</param>
        /// <param name="plots">The crop records in the farmhand's house. Only its owner's are worked.</param>
        public static FarmhandReplayResult Replay(
            Farmhand farmhand,
            IEnumerable<Livestock> animals,
            IEnumerable<GardenPlot> plots,
            Func<LivestockType, LivestockDefinition> getAnimal,
            Func<CropType, CropDefinition> getCrop,
            DateTime now)
        {
            var result = new FarmhandReplayResult();
            var from = farmhand.LastProcessedUtc;
            if (now <= from)
                return result;

            result.Ran = true;
            var live = animals.Where(animal => animal.OwnerPlayerId == farmhand.OwnerPlayerId && getAnimal(animal.Species) != null).ToList();
            var crops = plots.Where(plot => plot.PlanterPlayerId == farmhand.OwnerPlayerId && getCrop(plot.Crop) != null).ToList();

            // Animals that were already adults at the start of the span matured before it, and crops already ripe ripened before it.
            var matured = live.Where(animal => LivestockRules.IsAdult(animal, from)).Select(animal => animal.Id).ToHashSet();
            var ripened = crops.Where(plot => plot.RipeUtc <= from).Select(plot => plot.Id).ToHashSet();
            var thirdFeeding = new Dictionary<string, DateTime>();
            var cursor = from;

            for (var guard = 0; guard < MaxReplaySteps; guard++)
            {
                Step best = null;

                void Offer(DateTime time, int priority, string id, Action apply)
                {
                    if (time > now)
                        return;
                    if (best == null || (time, priority, id).CompareTo((best.Time, best.Priority, best.Id)) < 0)
                        best = new Step { Time = time, Priority = priority, Id = id, Apply = apply };
                }

                foreach (var plot in crops)
                {
                    var thisPlot = plot;
                    var crop = getCrop(thisPlot.Crop);
                    var start = Max(from, thisPlot.LastProcessedUtc);
                    DateTime At(DateTime deadline) => Max(Max(deadline, cursor), start);

                    Offer(At(thisPlot.WitherUtc), WitherPriority, thisPlot.Id, () =>
                    {
                        crops.Remove(thisPlot);
                        result.Withered.Add(thisPlot);
                        result.Events.Add(new FarmEvent(At(thisPlot.WitherUtc), FarmEventType.Withered, thisPlot.Id));
                    });

                    if (!ripened.Contains(thisPlot.Id))
                    {
                        Offer(At(thisPlot.RipeUtc), RipePriority, thisPlot.Id, () =>
                        {
                            ripened.Add(thisPlot.Id);
                            result.Events.Add(new FarmEvent(At(thisPlot.RipeUtc), FarmEventType.Ripened, thisPlot.Id));
                        });
                    }

                    // A crop is tended when it becomes ready, if the droid handles its tier and it has not withered by then.
                    if (!thisPlot.Tended && Handles(farmhand.DroidTier, crop.Tier) && At(thisPlot.TendReadyUtc) < thisPlot.WitherUtc)
                    {
                        Offer(At(thisPlot.TendReadyUtc), TendPriority, thisPlot.Id, () =>
                        {
                            thisPlot.Tended = true;
                            result.Events.Add(new FarmEvent(At(thisPlot.TendReadyUtc), FarmEventType.Tended, thisPlot.Id));
                        });
                    }
                }

                foreach (var animal in live)
                {
                    var thisAnimal = animal;
                    var definition = getAnimal(thisAnimal.Species);
                    var start = Max(from, thisAnimal.LastProcessedUtc);
                    DateTime At(DateTime deadline) => Max(Max(deadline, cursor), start);

                    Offer(At(LivestockRules.GetWanderOffUtc(thisAnimal)), WanderPriority, thisAnimal.Id, () =>
                    {
                        live.Remove(thisAnimal);
                        result.WanderedOff.Add(thisAnimal);
                        result.Events.Add(new FarmEvent(At(LivestockRules.GetWanderOffUtc(thisAnimal)), FarmEventType.WanderedOff, thisAnimal.Id));
                    });

                    if (!Handles(farmhand.DroidTier, definition.Tier))
                        continue;

                    // Fed as soon as it may eat, while feed lasts. A never-fed animal is fed as soon as it is placed.
                    if (HasFeed(farmhand.FeedHopper, definition.Tier, getCrop))
                    {
                        var feedAt = At(Max(LivestockRules.GetNextFeedUtc(thisAnimal) ?? thisAnimal.PlacedUtc, thisAnimal.PlacedUtc));
                        Offer(feedAt, FeedPriority, thisAnimal.Id, () =>
                        {
                            TryTakeFeed(farmhand.FeedHopper, definition.Tier, getCrop);
                            LivestockRules.ApplyFeeding(thisAnimal, feedAt);
                            if (thisAnimal.Feedings == LivestockRules.MaturityFeedings)
                                thirdFeeding[thisAnimal.Id] = feedAt;
                            result.Events.Add(new FarmEvent(feedAt, FarmEventType.Fed, thisAnimal.Id));
                        });
                    }

                    if (thisAnimal.Feedings < LivestockRules.MaturityFeedings)
                        continue;

                    // Grown up: 72 hours after placing, or at the third feeding if that came later.
                    var adultAt = Max(LivestockRules.GetMaturityUtc(thisAnimal), thirdFeeding.GetValueOrDefault(thisAnimal.Id, DateTime.MinValue));
                    if (!matured.Contains(thisAnimal.Id))
                    {
                        Offer(At(adultAt), MaturePriority, thisAnimal.Id, () =>
                        {
                            matured.Add(thisAnimal.Id);
                            result.Events.Add(new FarmEvent(At(adultAt), FarmEventType.Matured, thisAnimal.Id));
                        });
                    }

                    // Collected once every 20 hours while it is fed, not under the predators' penalty, and the hopper has room.
                    var items = LivestockRules.GetCollectItems(definition);
                    if (GetOutputRoom(farmhand.OutputHopper) >= LivestockRules.CountItems(items))
                    {
                        var collectAt = At(Max(
                            adultAt,
                            Max(LivestockRules.GetNextCollectUtc(thisAnimal) ?? DateTime.MinValue, thisAnimal.NoProductsUntilUtc ?? DateTime.MinValue)));
                        if (thisAnimal.LastFedUtc is { } fed && collectAt <= fed.AddHours(LivestockRules.FedWindowHours))
                        {
                            Offer(collectAt, CollectPriority, thisAnimal.Id, () =>
                            {
                                AddOutput(farmhand.OutputHopper, items);
                                LivestockRules.ApplyCollection(thisAnimal, collectAt);
                                result.Events.Add(new FarmEvent(
                                    collectAt,
                                    FarmEventType.Collected,
                                    thisAnimal.Id,
                                    string.Join(", ", items.Select(item => $"{item.Count} {item.Resref}"))));
                            });
                        }
                    }
                }

                if (best == null)
                    break;

                cursor = Max(cursor, best.Time);
                best.Apply();
            }

            farmhand.LastProcessedUtc = now;
            foreach (var animal in live)
            {
                animal.LastProcessedUtc = now;
                result.Animals.Add(animal);
            }

            foreach (var plot in crops)
            {
                plot.LastProcessedUtc = now;
                result.Plots.Add(plot);
            }

            return result;
        }

        private static DateTime Max(DateTime a, DateTime b) => a >= b ? a : b;
    }
}
