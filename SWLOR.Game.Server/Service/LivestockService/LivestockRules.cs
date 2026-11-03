using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.FarmingService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.TransferService;

namespace SWLOR.Game.Server.Service.LivestockService
{
    /// <summary>
    /// The livestock rules of the Player City Revamp ("Farming: Livestock"), with no database or engine calls: what an animal
    /// eats and when, when it grows up, wanders off or can be collected from, what collecting and butchering yield, how many
    /// animals and corrals a CD key and a city can have, how an animal packs into a crate, and the predators' penalty. The
    /// <c>Corrals</c> service reads and writes the records and the inventory and asks these rules what is allowed;
    /// <see cref="FarmhandService.FarmhandRules"/> replays a droid's work with the same rules.
    /// </summary>
    public static class LivestockRules
    {
        /// <summary>The crops one feeding takes, of the animal's tier or lower.</summary>
        public const int FeedCrops = 2;

        /// <summary>An animal can be fed once this often.</summary>
        public const int FeedIntervalHours = 20;

        /// <summary>An animal unfed this long wanders off. It counts from the last meal, or from placing an animal never fed.</summary>
        public const int WanderOffHours = 72;

        /// <summary>A young animal is old enough after this long, once it has had <see cref="MaturityFeedings"/> feedings.</summary>
        public const int MaturityHours = 72;

        public const int MaturityFeedings = 3;

        /// <summary>An animal can be collected from once this often.</summary>
        public const int CollectIntervalHours = 20;

        /// <summary>An animal gives products only if it was fed within this many hours.</summary>
        public const int FedWindowHours = 24;

        /// <summary>The meat butchering yields before Bountiful Harvest.</summary>
        public const int ButcherMeat = 4;

        public const int MaxButcherBonus = FarmingRules.MaxBountifulHarvestRank;

        /// <summary>The animals one CD key can have placed at once, across all its characters. Crated animals do not count.</summary>
        public const int MaxAnimalsPerCdKey = 6;

        public const int AnimalsPerCorral = 3;

        public const int CorralsPerCityLevel = 2;

        /// <summary>How long the predators' penalty lasts: the animals give no products.</summary>
        public const int PenaltyHours = 72;

        /// <summary>How long a hunt a city official called lasts before it is judged.</summary>
        public const int HuntMinutes = 30;

        /// <summary>The predators that stand at each corral during a hunt.</summary>
        public const int HuntWavePerCorral = 3;

        /// <summary>The most predators a hunt spawns in all, however many corrals the city has.</summary>
        public const int HuntWaveMax = 12;

        /// <summary>The item a crated animal is. It holds the animal's species and timers in its local variables.</summary>
        public const string CrateResref = "livestock_crate";

        /// <summary>The prefix of the young animal items and of the crate: every item a corral accepts starts with it.</summary>
        public const string ItemResrefPrefix = "livestock_";

        /// <summary>The creatures that stand at the corrals in a hunt, by city level (1 to 5): wild kin of each tier's animals.</summary>
        private static readonly string[] PredatorResrefs = { "warocas", "valley_cairnmog", "korr_klorslug", "womprat", "pthune" };

        // Tiers

        /// <summary>The highest animal tier a Crop Management rank (0 to 5) opens. The same perk gates crops and animals.</summary>
        public static int GetHighestTier(int cropManagementRank) => FarmingRules.GetHighestTier(cropManagementRank);

        public static bool CanRaiseTier(int cropManagementRank, int tier) => FarmingRules.CanPlantTier(cropManagementRank, tier);

        /// <summary>Whether an animal of <paramref name="animalTier"/> eats a crop of <paramref name="cropTier"/>: its own tier or lower.</summary>
        public static bool CanEat(int animalTier, int cropTier) => cropTier >= 1 && cropTier <= animalTier;

        /// <summary>The manure item of a tier, the alternative Fertilizer component of that tier.</summary>
        public static string GetManureResref(int tier) => $"manure_t{tier}";

        // Corrals

        /// <summary>The corrals a city of this level can have: 2 per level, so 30 at most.</summary>
        public static int GetMaxCorrals(int cityLevel) => Math.Max(1, cityLevel) * CorralsPerCityLevel;

        /// <summary>
        /// Whether the corral takes new animals. The corrals are counted in the order they were placed, so a city that drops a
        /// level stops taking animals in its newest corrals, and the animals already there stay.
        /// </summary>
        public static bool IsCorralOpen(int corralIndex, int cityLevel) => corralIndex >= 0 && corralIndex < GetMaxCorrals(cityLevel);

        /// <summary>
        /// Why a Corral cannot be placed, or an empty string. It stands on a planet city's open ground only, never on a station
        /// deck, and a city has no more than 2 per level.
        /// </summary>
        public static string GetPlacementError(StructureType structureType, bool isStation, int corralsInCity, int cityLevel)
        {
            if (structureType != StructureType.Corral)
                return string.Empty;
            if (isStation)
                return "A station deck has no open ground. Corrals stand in planet cities only.";
            if (corralsInCity >= GetMaxCorrals(cityLevel))
                return $"This city can have {GetMaxCorrals(cityLevel)} corrals at its level.";

            return string.Empty;
        }

        /// <summary>
        /// The reason a character cannot pick a corral up, or an empty string. A corral that holds another player's animals
        /// stays until they crate them; the character's own animals are crated and sent to their overflow store when it goes.
        /// </summary>
        public static string GetRetrieveError(IEnumerable<string> animalOwnerIds, string playerId)
        {
            return animalOwnerIds.Any(owner => owner != playerId)
                ? "Another player's animals are still in this corral. They must crate them first."
                : string.Empty;
        }

        // Growing up, hunger and products

        public static LivestockStage GetStage(Livestock animal, DateTime now) =>
            IsAdult(animal, now) ? LivestockStage.Adult : LivestockStage.Young;

        /// <summary>
        /// An animal is an adult once 72 hours have passed since it was placed and it has had at least 3 feedings: 72 hours
        /// after placing if it has been fed 3 times by then, or at its third feeding if later.
        /// </summary>
        public static bool IsAdult(Livestock animal, DateTime now) =>
            animal.Feedings >= MaturityFeedings && now >= animal.PlacedUtc.AddHours(MaturityHours);

        /// <summary>When the animal's 72 hours are up: the earliest it can be an adult.</summary>
        public static DateTime GetMaturityUtc(Livestock animal) => animal.PlacedUtc.AddHours(MaturityHours);

        /// <summary>When the animal wanders off unless it is fed: 72 hours after its last meal, or after placing if it never ate.</summary>
        public static DateTime GetWanderOffUtc(Livestock animal) => (animal.LastFedUtc ?? animal.PlacedUtc).AddHours(WanderOffHours);

        public static bool HasWanderedOff(Livestock animal, DateTime now) => now >= GetWanderOffUtc(animal);

        /// <summary>The first moment the animal can be fed again, or null for an animal never fed.</summary>
        public static DateTime? GetNextFeedUtc(Livestock animal) => animal.LastFedUtc?.AddHours(FeedIntervalHours);

        public static bool CanFeedAt(Livestock animal, DateTime now) => GetNextFeedUtc(animal) is not { } next || now >= next;

        /// <summary>Whether the animal was fed within the last 24 hours, which an adult needs to give products.</summary>
        public static bool IsFedRecently(Livestock animal, DateTime now) =>
            animal.LastFedUtc is { } fed && now <= fed.AddHours(FedWindowHours);

        /// <summary>The first moment the animal can be collected from again, or null for an animal never collected from.</summary>
        public static DateTime? GetNextCollectUtc(Livestock animal) => animal.LastCollectedUtc?.AddHours(CollectIntervalHours);

        public static bool HasPenalty(Livestock animal, DateTime now) =>
            animal.NoProductsUntilUtc is { } until && until > now;

        /// <summary>Records a feeding: the count and the time of the meal.</summary>
        public static void ApplyFeeding(Livestock animal, DateTime now)
        {
            animal.Feedings++;
            animal.LastFedUtc = now;
        }

        /// <summary>Records a collection.</summary>
        public static void ApplyCollection(Livestock animal, DateTime now) => animal.LastCollectedUtc = now;

        // The predators' penalty

        /// <summary>When a penalty applied at <paramref name="now"/> ends. A repeat restarts the 72 hours instead of stacking.</summary>
        public static DateTime GetPenaltyEndUtc(DateTime now) => now.AddHours(PenaltyHours);

        public static void ApplyPenalty(Livestock animal, DateTime now) => animal.NoProductsUntilUtc = GetPenaltyEndUtc(now);

        // Checks

        /// <summary>Why a character cannot feed the animal, or <see cref="LivestockRefusal.None"/>. Checked in this order.</summary>
        /// <param name="cropTier">The tier of the crop picked.</param>
        /// <param name="cropsCarried">How many of that crop the character carries.</param>
        public static LivestockRefusal CheckFeed(bool isPlayer, Livestock animal, DateTime now, bool isOwner, int animalTier, int cropTier, int cropsCarried)
        {
            if (!isPlayer)
                return LivestockRefusal.NotAPlayer;
            if (animal == null || HasWanderedOff(animal, now))
                return LivestockRefusal.NoAnimal;
            if (!isOwner)
                return LivestockRefusal.NotTheOwner;
            if (!CanFeedAt(animal, now))
                return LivestockRefusal.FeedTooSoon;
            if (!CanEat(animalTier, cropTier))
                return LivestockRefusal.CropTierTooHigh;
            if (cropsCarried < FeedCrops)
                return LivestockRefusal.NotEnoughCrops;
            return LivestockRefusal.None;
        }

        /// <summary>
        /// Why a character cannot collect from the animal, or <see cref="LivestockRefusal.None"/>. Collecting needs an adult fed
        /// in the last 24 hours, once every 20 hours, and is blocked while the predators' penalty lasts.
        /// </summary>
        public static LivestockRefusal CheckCollect(bool isPlayer, Livestock animal, DateTime now, bool isOwner)
        {
            if (!isPlayer)
                return LivestockRefusal.NotAPlayer;
            if (animal == null || HasWanderedOff(animal, now))
                return LivestockRefusal.NoAnimal;
            if (!isOwner)
                return LivestockRefusal.NotTheOwner;
            if (!IsAdult(animal, now))
                return LivestockRefusal.StillYoung;
            if (!IsFedRecently(animal, now))
                return LivestockRefusal.NotFedRecently;
            if (HasPenalty(animal, now))
                return LivestockRefusal.PredatorPenalty;
            if (GetNextCollectUtc(animal) is { } next && now < next)
                return LivestockRefusal.CollectTooSoon;
            return LivestockRefusal.None;
        }

        /// <summary>Why a character cannot butcher the animal, or <see cref="LivestockRefusal.None"/>. Only an adult.</summary>
        public static LivestockRefusal CheckButcher(bool isPlayer, Livestock animal, DateTime now, bool isOwner)
        {
            if (!isPlayer)
                return LivestockRefusal.NotAPlayer;
            if (animal == null || HasWanderedOff(animal, now))
                return LivestockRefusal.NoAnimal;
            if (!isOwner)
                return LivestockRefusal.NotTheOwner;
            if (!IsAdult(animal, now))
                return LivestockRefusal.StillYoung;
            return LivestockRefusal.None;
        }

        /// <summary>Why a character cannot crate the animal, or <see cref="LivestockRefusal.None"/>. Any animal, young or adult.</summary>
        public static LivestockRefusal CheckCrate(bool isPlayer, Livestock animal, DateTime now, bool isOwner)
        {
            if (!isPlayer)
                return LivestockRefusal.NotAPlayer;
            if (animal == null || HasWanderedOff(animal, now))
                return LivestockRefusal.NoAnimal;
            if (!isOwner)
                return LivestockRefusal.NotTheOwner;
            return LivestockRefusal.None;
        }

        /// <summary>
        /// Why a character cannot place an animal in a corral, or <see cref="LivestockRefusal.None"/>. Checked in this order. A
        /// crated animal does not count toward the CD key's six while crated, so placing it needs room under the six.
        /// </summary>
        /// <param name="corralIndex">Where the corral stands among its city's corrals, in the order they were placed. -1 when it is gone.</param>
        /// <param name="animalsPlacedByCdKey">The animals the character's CD key already has placed.</param>
        /// <param name="animalsInCorral">The animals already in the corral.</param>
        public static LivestockRefusal CheckPlace(
            bool isPlayer,
            int corralIndex,
            bool hasAccess,
            int cropManagementRank,
            int animalTier,
            int animalsPlacedByCdKey,
            int animalsInCorral,
            int cityLevel)
        {
            if (!isPlayer)
                return LivestockRefusal.NotAPlayer;
            if (corralIndex < 0)
                return LivestockRefusal.NoCorral;
            if (!hasAccess)
                return LivestockRefusal.NoAccess;
            if (!CanRaiseTier(cropManagementRank, animalTier))
                return LivestockRefusal.TierLocked;
            if (animalsPlacedByCdKey >= MaxAnimalsPerCdKey)
                return LivestockRefusal.CdKeyCap;
            if (!IsCorralOpen(corralIndex, cityLevel))
                return LivestockRefusal.CorralOverLimit;
            if (animalsInCorral >= AnimalsPerCorral)
                return LivestockRefusal.CorralFull;
            return LivestockRefusal.None;
        }

        // Products

        /// <summary>What one collection yields: 1 manure of the animal's tier, plus 1 milk from a Thune.</summary>
        public static List<(string Resref, int Count)> GetCollectItems(LivestockDefinition definition)
        {
            var items = new List<(string, int)> { (definition.ManureResref, 1) };
            if (!string.IsNullOrWhiteSpace(definition.CollectExtraResref))
                items.Add((definition.CollectExtraResref, 1));
            return items;
        }

        /// <summary>
        /// What butchering yields: 4 of the animal's meat plus one per Bountiful Harvest rank (up to 3), and 1 of its second
        /// product where it has one.
        /// </summary>
        public static List<(string Resref, int Count)> GetButcherItems(LivestockDefinition definition, int bountifulHarvestRank)
        {
            var items = new List<(string, int)>
            {
                (definition.MeatResref, ButcherMeat + Math.Clamp(bountifulHarvestRank, 0, MaxButcherBonus))
            };
            if (!string.IsNullOrWhiteSpace(definition.SecondProductResref))
                items.Add((definition.SecondProductResref, 1));
            return items;
        }

        public static int CountItems(IEnumerable<(string Resref, int Count)> items) => items.Sum(item => item.Count);

        /// <summary>The Agriculture XP for products: one item at a time, by the same level-difference formula as crops, at the animal's tier level.</summary>
        public static int GetProductXp(int animalLevel, int agricultureRank, int items) =>
            FarmingRules.GetHarvestXp(animalLevel, agricultureRank, items);

        // Crating

        /// <summary>
        /// Packs an animal: every timer becomes the time left on it, and the predators' penalty with them, so crating pauses an
        /// animal without clearing anything.
        /// </summary>
        public static LivestockCrate Pack(Livestock animal, DateTime now) => new()
        {
            Species = animal.Species,
            Feedings = animal.Feedings,
            MaturitySeconds = SecondsLeft(GetMaturityUtc(animal), now),
            HungerSeconds = SecondsLeft(GetWanderOffUtc(animal), now),
            CollectSeconds = SecondsLeft(GetNextCollectUtc(animal), now),
            PenaltySeconds = SecondsLeft(animal.NoProductsUntilUtc, now),
        };

        /// <summary>
        /// Places a crated animal: a record whose timers have the time left the crate recorded, counted from <paramref name="now"/>.
        /// The owner, the CD key and the corral are the placer's and the corral's.
        /// </summary>
        public static Livestock Unpack(LivestockCrate crate, DateTime now, string ownerPlayerId, string ownerCdKey, string corralId)
        {
            var animal = new Livestock
            {
                OwnerPlayerId = ownerPlayerId,
                OwnerCdKey = ownerCdKey,
                CorralId = corralId,
                Species = crate.Species,
                Feedings = crate.Feedings,
                PlacedUtc = now.AddSeconds(crate.MaturitySeconds).AddHours(-MaturityHours),
                LastFedUtc = crate.Feedings > 0 ? now.AddSeconds(crate.HungerSeconds).AddHours(-WanderOffHours) : null,
                LastCollectedUtc = crate.CollectSeconds > 0 ? now.AddSeconds(crate.CollectSeconds).AddHours(-CollectIntervalHours) : null,
                NoProductsUntilUtc = crate.PenaltySeconds > 0 ? now.AddSeconds(crate.PenaltySeconds) : null,
                LastProcessedUtc = now,
            };

            return animal;
        }

        /// <summary>A young animal item placed: a record with nothing fed, collected or penalized.</summary>
        public static Livestock PlaceYoung(LivestockType species, DateTime now, string ownerPlayerId, string ownerCdKey, string corralId) => new()
        {
            OwnerPlayerId = ownerPlayerId,
            OwnerCdKey = ownerCdKey,
            CorralId = corralId,
            Species = species,
            PlacedUtc = now,
            LastProcessedUtc = now,
        };

        private static int SecondsLeft(DateTime? deadline, DateTime now) =>
            deadline is { } at ? (int)Math.Ceiling(Math.Max(0.0, (at - now).TotalSeconds)) : 0;

        // Hunts

        /// <summary>When a hunt called at <paramref name="now"/> is judged.</summary>
        public static DateTime GetHuntEndUtc(DateTime now) => now.AddMinutes(HuntMinutes);

        /// <summary>The predators a hunt spawns for a city with this many corrals: 3 at each, 12 at most.</summary>
        public static int GetHuntWaveSize(int corralCount) => Math.Min(HuntWaveMax, Math.Max(0, corralCount) * HuntWavePerCorral);

        /// <summary>The creature a hunt's wave is made of, for a city of this level: wild kin of the tier's animals.</summary>
        public static string GetPredatorResref(int cityLevel) => PredatorResrefs[Math.Clamp(cityLevel, 1, PredatorResrefs.Length) - 1];

        /// <summary>A hunt is won when every predator it spawned is down, and lost if any still stands when it ends.</summary>
        public static bool IsHuntWon(int predatorsSpawned, int predatorsAlive) => predatorsSpawned > 0 && predatorsAlive <= 0;

        // Text

        public static string Describe(LivestockRefusal refusal)
        {
            return refusal switch
            {
                LivestockRefusal.None => string.Empty,
                LivestockRefusal.NotAPlayer => "Only characters can raise animals.",
                LivestockRefusal.NoAnimal => "That animal is no longer here.",
                LivestockRefusal.NotTheOwner => "Only the animal's owner can do that.",
                LivestockRefusal.FeedTooSoon => $"The animal was fed less than {FeedIntervalHours} hours ago.",
                LivestockRefusal.CropTierTooHigh => "The animal does not eat crops of a higher tier than its own.",
                LivestockRefusal.NotEnoughCrops => $"You need {FeedCrops} of that crop to feed the animal.",
                LivestockRefusal.StillYoung => "The animal is still young.",
                LivestockRefusal.NotFedRecently => $"The animal gives nothing unless it was fed in the last {FedWindowHours} hours.",
                LivestockRefusal.CollectTooSoon => $"The animal was collected from less than {CollectIntervalHours} hours ago.",
                LivestockRefusal.PredatorPenalty => "Predators raided the corrals, and the animal gives no products for now.",
                LivestockRefusal.TierLocked => "Your Crop Management is too low to raise that animal.",
                LivestockRefusal.CdKeyCap => $"You already have {MaxAnimalsPerCdKey} animals placed. Butcher or crate one first.",
                LivestockRefusal.CorralFull => $"This corral already holds {AnimalsPerCorral} animals.",
                LivestockRefusal.NoCorral => "This corral is no longer here.",
                LivestockRefusal.CorralOverLimit => "The city's level no longer allows animals in this corral.",
                LivestockRefusal.NoAccess => "Only the city's citizens can add animals to its corrals.",
                LivestockRefusal.NotAnAnimal => "That is not an animal.",
                LivestockRefusal.NotSaved => TransferJournalRules.NotSavedText,
                _ => string.Empty
            };
        }

        public static string DescribeStage(LivestockStage stage) => stage == LivestockStage.Adult ? "Adult" : "Young";

        /// <summary>The line that says how far a young animal is from growing up, or empty for an adult.</summary>
        public static string DescribeGrowth(Livestock animal, DateTime now)
        {
            if (IsAdult(animal, now))
                return string.Empty;

            var feedingsLeft = Math.Max(0, MaturityFeedings - animal.Feedings);
            var timeLeft = GetMaturityUtc(animal) - now;
            if (feedingsLeft > 0 && timeLeft > TimeSpan.Zero)
                return $"Grows up in {FarmingRules.FormatDuration(timeLeft)} if it has {feedingsLeft} more {Plural(feedingsLeft, "feeding")} by then.";
            if (feedingsLeft > 0)
                return $"Grows up at its next {feedingsLeft} {Plural(feedingsLeft, "feeding")}.";

            return $"Grows up in {FarmingRules.FormatDuration(timeLeft)}.";
        }

        public static string DescribeHunger(Livestock animal, DateTime now) =>
            $"Wanders off in {FarmingRules.FormatDuration(GetWanderOffUtc(animal) - now)} unless fed.";

        public static string DescribeFed(string animalName, int crops) =>
            $"You fed {crops} crops to the {animalName}.";

        public static string DescribePlaced(string animalName, bool crated) =>
            crated ? $"You placed the crated {animalName} in the corral." : $"You placed the young {animalName} in the corral.";

        public static string DescribeCrated(string animalName, bool inOverflow) =>
            inOverflow
                ? $"You crated the {animalName}. The crate is in your overflow storage. Use /overflow to claim it."
                : $"You crated the {animalName}.";

        /// <summary>The message for a collection or a butchering: what it yielded, and any that went to overflow storage.</summary>
        public static string DescribeProducts(string verb, string animalName, IEnumerable<(string Name, int Count)> items, int itemsInOverflow)
        {
            var list = string.Join(", ", items.Select(item => $"{item.Count} {item.Name}"));
            var text = $"You {verb} the {animalName}: {list}.";
            if (itemsInOverflow > 0)
                text += $" {itemsInOverflow} did not fit in your inventory and are in your overflow storage. Use /overflow to claim them.";
            return text;
        }

        private static string Plural(int count, string word) => count == 1 ? word : word + "s";
    }
}
