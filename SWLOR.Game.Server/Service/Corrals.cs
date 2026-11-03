using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Service.ClerkRecoveryService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.FarmingService;
using SWLOR.Game.Server.Service.LivestockService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Corrals and the animals in them (Player City Revamp, "Farming: Livestock"): placing a young or crated animal, feeding,
    /// collecting from, butchering and crating it, the creature that stands for it in the corral, and what happens to an animal
    /// when its corral goes away. The rules (tiers, caps, timers, yields, crating) are <see cref="LivestockRules"/>; this class
    /// reads and writes the records and the inventory and applies them. Every action first brings the corral's farmhand up to
    /// date (<see cref="Farmhands.CatchUp"/>) and removes an animal that wandered off, so what it acts on is current.
    /// </summary>
    public static class Corrals
    {
        /// <summary>The creature local that holds the id of the animal's <see cref="Livestock"/> record.</summary>
        public const string AnimalIdVariable = "LIVESTOCK_ID";

        /// <summary>The corral slot (0 to 2) an animal's creature stands in.</summary>
        private const string SlotVariable = "LIVESTOCK_SLOT";

        /// <summary>The local the conversation router reads to open a code-driven conversation on an object.</summary>
        private const string ConversationVariable = "CONVERSATION";

        private const float SlotRadius = 1.2f;

        private static readonly Dictionary<LivestockType, LivestockDefinition> _livestock = new();
        private static readonly Dictionary<string, LivestockDefinition> _livestockByYoungResref = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, uint> _creatures = new();

        /// <summary>When the module loads, reads the livestock definitions.</summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheLivestock()
        {
            _livestock.Clear();
            _livestockByYoungResref.Clear();

            var definitions = typeof(ILivestockListDefinition).Assembly
                .GetTypes()
                .Where(type => typeof(ILivestockListDefinition).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);

            foreach (var type in definitions)
            {
                var list = (ILivestockListDefinition)Activator.CreateInstance(type);
                foreach (var (livestockType, definition) in list.BuildLivestock())
                {
                    _livestock[livestockType] = definition;
                    _livestockByYoungResref[definition.YoungResref] = definition;
                }
            }

            Console.WriteLine($"Loaded {_livestock.Count} livestock.");
        }

        public static LivestockDefinition GetDefinition(LivestockType type) => _livestock.GetValueOrDefault(type);

        public static IReadOnlyCollection<LivestockDefinition> GetAllDefinitions() => _livestock.Values;

        public static LivestockDefinition GetDefinitionByYoungResref(string resref) =>
            string.IsNullOrWhiteSpace(resref) ? null : _livestockByYoungResref.GetValueOrDefault(resref);

        // Records

        public static Livestock GetAnimal(string animalId) =>
            string.IsNullOrWhiteSpace(animalId) ? null : DB.Get<Livestock>(animalId);

        /// <summary>The animals standing in a corral, oldest first.</summary>
        public static List<Livestock> GetAnimalsInCorral(string corralId)
        {
            if (string.IsNullOrWhiteSpace(corralId))
                return new List<Livestock>();

            return Search(new DBQuery<Livestock>().AddFieldSearch(nameof(Livestock.CorralId), corralId, false))
                .OrderBy(animal => animal.PlacedUtc)
                .ThenBy(animal => animal.Id, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>Every animal of a city's corrals.</summary>
        public static List<Livestock> GetAnimalsInCity(string cityId)
        {
            return GetCorralsOfCity(cityId).SelectMany(corral => GetAnimalsInCorral(corral.Id)).ToList();
        }

        /// <summary>The animals a CD key has placed, across every character on it. Without a CD key, the character's own.</summary>
        public static int CountPlaced(string cdKey, string playerId)
        {
            var query = string.IsNullOrWhiteSpace(cdKey)
                ? new DBQuery<Livestock>().AddFieldSearch(nameof(Livestock.OwnerPlayerId), playerId, false)
                : new DBQuery<Livestock>().AddFieldSearch(nameof(Livestock.OwnerCdKey), cdKey, false);
            return checked((int)DB.SearchCount(query));
        }

        /// <summary>The corral structure with the id, or null when it is not a corral.</summary>
        public static WorldProperty GetCorral(string corralId)
        {
            var property = string.IsNullOrWhiteSpace(corralId) ? null : DB.Get<WorldProperty>(corralId);
            return property is { PropertyType: PropertyType.Structure, StructureType: StructureType.Corral } ? property : null;
        }

        /// <summary>A city's corrals in the order they were placed. The first 2 per city level take new animals.</summary>
        public static List<WorldProperty> GetCorralsOfCity(string cityId)
        {
            if (string.IsNullOrWhiteSpace(cityId))
                return new List<WorldProperty>();

            return Search(new DBQuery<WorldProperty>()
                    .AddFieldSearch(nameof(WorldProperty.ParentPropertyId), cityId, false)
                    .AddFieldSearch(nameof(WorldProperty.StructureType), (int)StructureType.Corral))
                .OrderBy(corral => corral.DateCreated)
                .ThenBy(corral => corral.Id, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>Where the corral stands among its city's corrals, from 0, or -1 when it is not one of them.</summary>
        public static int GetCorralIndex(WorldProperty corral)
        {
            if (corral == null)
                return -1;

            return GetCorralsOfCity(corral.ParentPropertyId).FindIndex(other => other.Id == corral.Id);
        }

        private static List<T> Search<T>(DBQuery<T> query) where T : EntityBase
        {
            var count = checked((int)DB.SearchCount(query));
            return count <= 0 ? new List<T>() : DB.Search(query.AddPaging(count, 0)).ToList();
        }

        // Access

        /// <summary>
        /// Whether the character may add animals to the corral: a citizen of its city, or staff. There is no separate permission:
        /// a corral is part of the city. An owner who has lost access keeps their animals and can still collect from, butcher or
        /// crate them.
        /// </summary>
        public static bool HasAccess(uint player, WorldProperty corral)
        {
            if (GetIsDM(player) || GetIsDMPossessed(player))
                return true;

            if (!GetIsPC(player) || corral == null)
                return false;

            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            return dbPlayer != null && ElectionRules.IsCitizenOf(dbPlayer.CitizenPropertyId, corral.ParentPropertyId, dbPlayer.IsDeleted);
        }

        // Structure events

        /// <summary>A corral is placed or moved, or its city loads: make it usable, and stand its animals in it.</summary>
        public static void OnCorralSpawned(WorldProperty corral, uint placeable)
        {
            SetName(placeable, "Corral");
            SetUseableFlag(placeable, true);
            SetEventScript(placeable, EventScript.Placeable_OnUsed, ScriptName.OnCorralUsed);

            foreach (var animal in GetAnimalsInCorral(corral.Id))
            {
                DestroyCreature(animal.Id);
                SpawnCreature(animal, placeable);
            }

            LivestockHunts.OnCorralSpawned(corral, placeable);
        }

        /// <summary>A corral is picked up: its animals' creatures go. Their records are handled when the structure is deleted.</summary>
        public static void OnCorralRetrieved(WorldProperty corral, uint placeable)
        {
            foreach (var animal in GetAnimalsInCorral(corral.Id))
                DestroyCreature(animal.Id);
        }

        /// <summary>
        /// The reason the character cannot pick the corral up, or an empty string. It stays while it holds another player's
        /// animals. The character's own are crated and sent to their overflow store when it goes.
        /// </summary>
        public static string GetRetrieveError(WorldProperty corral, string playerId)
        {
            return corral == null
                ? string.Empty
                : LivestockRules.GetRetrieveError(GetAnimalsInCorral(corral.Id).Select(animal => animal.OwnerPlayerId), playerId);
        }

        /// <summary>A corral was used: open its menu.</summary>
        [NWNEventHandler(ScriptName.OnCorralUsed)]
        public static void UseCorral()
        {
            var player = GetLastUsedBy();
            if (!IsFarmer(player))
                return;

            var corral = OBJECT_SELF;
            if (GetCorral(Property.GetPropertyId(corral)) == null)
                return;

            ConversationMenu.Start(player, corral, nameof(CorralDialog));
        }

        /// <summary>The corral closest to the player within <paramref name="meters"/>, or invalid.</summary>
        public static uint GetNearestCorral(uint player, float meters)
        {
            var area = GetArea(player);
            var best = OBJECT_INVALID;
            var bestDistance = meters;
            foreach (var corral in GetCorralsOfCity(Property.GetPropertyId(area)))
            {
                var placeable = Property.GetPlaceableByPropertyId(corral.Id);
                if (!GetIsObjectValid(placeable) || GetArea(placeable) != area)
                    continue;

                var distance = GetDistanceBetween(player, placeable);
                if (distance <= bestDistance)
                {
                    best = placeable;
                    bestDistance = distance;
                }
            }

            return best;
        }

        // Actions

        /// <summary>
        /// Brings the corral's farmhand up to date, then returns the animal, or null when it is gone. An animal that wandered off
        /// is removed on the spot and its owner told.
        /// </summary>
        public static Livestock LoadAnimal(string animalId, DateTime now)
        {
            // An action the City Hall clerk was finishing is finished first.
            ClerkRecovery.RecoverAnimal(animalId);

            var animal = GetAnimal(animalId);
            if (animal == null)
                return null;

            Farmhands.CatchUp(animal.CorralId);
            animal = GetAnimal(animalId);
            if (animal == null)
                return null;

            if (LivestockRules.HasWanderedOff(animal, now))
            {
                RemoveWanderedOff(animal);
                return null;
            }

            return animal;
        }

        /// <summary>Places a young animal item or a crate in the corral. The item is used up.</summary>
        public static LivestockResult Place(uint player, string corralId, uint item)
        {
            var now = DateTime.UtcNow;
            var isPlayer = IsFarmer(player);
            Farmhands.CatchUp(corralId);

            var corral = GetCorral(corralId);
            if (!GetIsObjectValid(item) || GetItemPossessor(item) != player)
                return LivestockResult.Refused(LivestockRefusal.NotAnAnimal);

            LivestockCrate crate = null;
            LivestockDefinition definition;
            if (GetResRef(item) == LivestockRules.CrateResref)
            {
                if (!LivestockCrate.TryFromLocals(name => GetLocalInt(item, name), out crate))
                    return LivestockResult.Refused(LivestockRefusal.NotAnAnimal);

                definition = GetDefinition(crate.Species);
            }
            else
            {
                definition = GetDefinitionByYoungResref(GetResRef(item));
            }

            if (definition == null)
                return LivestockResult.Refused(LivestockRefusal.NotAnAnimal);

            var playerId = isPlayer ? GetObjectUUID(player) : string.Empty;
            var cdKey = isPlayer ? GetPCPublicCDKey(player) : string.Empty;
            var city = corral == null ? null : DB.Get<WorldProperty>(corral.ParentPropertyId);
            var refusal = LivestockRules.CheckPlace(
                isPlayer,
                GetCorralIndex(corral),
                isPlayer && HasAccess(player, corral),
                isPlayer ? Perk.GetPlayerEffectivePerkLevel(player, PerkType.CropManagement) : 0,
                definition.Tier,
                isPlayer ? CountPlaced(cdKey, playerId) : 0,
                GetAnimalsInCorral(corralId).Count,
                city == null ? 1 : CityOfficers.GetCityLevel(city));
            if (refusal != LivestockRefusal.None)
                return LivestockResult.Refused(refusal);

            // The item goes first, so a stop between the two steps can lose an animal but never duplicate one.
            UseUpOne(item);
            var animal = crate != null
                ? LivestockRules.Unpack(crate, now, playerId, cdKey, corralId)
                : LivestockRules.PlaceYoung(definition.Type, now, playerId, cdKey, corralId);
            DB.Set(animal);

            var placeable = Property.GetPlaceableByPropertyId(corralId);
            if (GetIsObjectValid(placeable))
                SpawnCreature(animal, placeable);

            Log.Write(LogGroup.Property, $"{PlayerName.GetAuditName(player)} placed {(crate != null ? "a crated" : "a young")} {definition.Type} ({animal.Id}) in corral {corralId}.");
            return LivestockResult.Success(LivestockRules.DescribePlaced(definition.Name, crate != null));
        }

        /// <summary>Feeds the animal 2 crops of the picked type, which must be of its tier or lower.</summary>
        public static LivestockResult Feed(uint player, string animalId, CropType cropType)
        {
            var now = DateTime.UtcNow;
            var isPlayer = IsFarmer(player);
            var animal = LoadAnimal(animalId, now);
            var crop = Farming.GetCrop(cropType);
            if (animal == null)
                return LivestockResult.Refused(LivestockRefusal.NoAnimal);

            var definition = GetDefinition(animal.Species);
            if (definition == null || crop == null)
                return LivestockResult.Refused(LivestockRefusal.NotAnAnimal);

            var refusal = LivestockRules.CheckFeed(
                isPlayer,
                animal,
                now,
                isPlayer && animal.OwnerPlayerId == GetObjectUUID(player),
                definition.Tier,
                crop.Tier,
                isPlayer ? FarmItems.Count(player, crop.HarvestResref) : 0);
            if (refusal != LivestockRefusal.None)
                return LivestockResult.Refused(refusal);

            // The crops go first, so a stop between the two steps loses a meal but never gives a free one.
            FarmItems.Take(player, crop.HarvestResref, LivestockRules.FeedCrops);
            LivestockRules.ApplyFeeding(animal, now);
            animal.LastProcessedUtc = now;
            DB.Set(animal);
            RefreshName(animal);

            Log.Write(LogGroup.Property, $"{PlayerName.GetAuditName(player)} fed {definition.Type} {animal.Id} {crop.Type} (feeding {animal.Feedings}).");
            return LivestockResult.Success((LivestockRules.DescribeFed(definition.Name, LivestockRules.FeedCrops) + " " + LivestockRules.DescribeGrowth(animal, now)).Trim());
        }

        /// <summary>
        /// Collects from an adult fed in the last 24 hours, once every 20: 1 manure, plus 1 milk from a Thune. The owner gets
        /// Agriculture XP. With <paramref name="toOverflow"/> everything goes to their overflow store, as it does when the City
        /// Hall clerk collects for an owner who cannot reach their corral: the collection is marked on the animal, the overflow
        /// records are written, and then the animal records the collection (<see cref="ClerkRecovery"/>).
        /// </summary>
        public static LivestockResult Collect(uint player, string animalId, bool toOverflow = false)
        {
            var now = DateTime.UtcNow;
            var isPlayer = IsFarmer(player);
            var animal = LoadAnimal(animalId, now);
            var refusal = LivestockRules.CheckCollect(isPlayer, animal, now, isPlayer && animal != null && animal.OwnerPlayerId == GetObjectUUID(player));
            if (refusal != LivestockRefusal.None)
                return RefusedFor(refusal, animal, now);

            var definition = GetDefinition(animal.Species);
            if (definition == null)
                return LivestockResult.Refused(LivestockRefusal.NotAnAnimal);

            var items = LivestockRules.GetCollectItems(definition);
            if (toOverflow)
                return GiveProductsThroughClerk(player, animal, definition, items, ClerkRecoveryAction.Collect, "collected from", now);

            // The record goes first, so a stop between the two steps can lose a collection but never repeat it.
            LivestockRules.ApplyCollection(animal, now);
            animal.LastProcessedUtc = now;
            DB.Set(animal);

            return GiveProducts(player, animal, definition, items, "collected from", now);
        }

        /// <summary>
        /// Butchers an adult: 4 of its meat plus Bountiful Harvest, and 1 of its second product where it has one. The animal is
        /// gone. The owner gets Agriculture XP.
        /// </summary>
        public static LivestockResult Butcher(uint player, string animalId, bool toOverflow = false)
        {
            var now = DateTime.UtcNow;
            var isPlayer = IsFarmer(player);
            var animal = LoadAnimal(animalId, now);
            var refusal = LivestockRules.CheckButcher(isPlayer, animal, now, isPlayer && animal != null && animal.OwnerPlayerId == GetObjectUUID(player));
            if (refusal != LivestockRefusal.None)
                return RefusedFor(refusal, animal, now);

            var definition = GetDefinition(animal.Species);
            if (definition == null)
                return LivestockResult.Refused(LivestockRefusal.NotAnAnimal);

            var items = LivestockRules.GetButcherItems(definition, Perk.GetPlayerEffectivePerkLevel(player, PerkType.BountifulHarvest));
            if (toOverflow)
                return GiveProductsThroughClerk(player, animal, definition, items, ClerkRecoveryAction.Butcher, "butchered", now);

            // The record goes first, so a stop between the two steps can lose the meat but never butcher the animal twice.
            DB.Delete<Livestock>(animal.Id);
            DestroyCreature(animal.Id);

            return GiveProducts(player, animal, definition, items, "butchered", now);
        }

        /// <summary>
        /// Packs the animal into a crate item to move, sell or trade. Every timer and any penalty it carries resume where they
        /// stopped when it is placed again. With <paramref name="toOverflow"/> the crate goes to the owner's overflow store, as it
        /// does when the City Hall clerk crates for an owner who cannot reach their corral: the crating is marked on the animal, the
        /// overflow record is written, and then the animal is deleted (<see cref="ClerkRecovery"/>).
        /// </summary>
        public static LivestockResult Crate(uint player, string animalId, bool toOverflow = false)
        {
            var now = DateTime.UtcNow;
            var isPlayer = IsFarmer(player);
            var animal = LoadAnimal(animalId, now);
            var refusal = LivestockRules.CheckCrate(isPlayer, animal, now, isPlayer && animal != null && animal.OwnerPlayerId == GetObjectUUID(player));
            if (refusal != LivestockRefusal.None)
                return RefusedFor(refusal, animal, now);

            var definition = GetDefinition(animal.Species);
            if (definition == null)
                return LivestockResult.Refused(LivestockRefusal.NotAnAnimal);

            var crate = LivestockRules.Pack(animal, now);
            if (toOverflow)
                return CrateThroughClerk(player, animal, definition, crate, now);

            var item = CreateCrateItem(crate, definition, GetLocation(player));
            if (!GetIsObjectValid(item))
                return LivestockResult.Refused(LivestockRefusal.NotAnAnimal, "Crates are not available yet, so the animal stays where it is.");

            // The record goes first, so a stop between the two steps can lose the animal but never copy it.
            DB.Delete<Livestock>(animal.Id);
            DestroyCreature(animal.Id);

            // An item that does not fit is destroyed by TryAcquireItem, so the crate is made again for the overflow store.
            var inOverflow = false;
            if (!Item.TryAcquireItem(player, item))
            {
                inOverflow = true;
                item = CreateCrateItem(crate, definition, GetLocation(player));
            }

            if (inOverflow && GetIsObjectValid(item))
                FarmItems.StoreInOverflow(animal.OwnerPlayerId, item, string.Empty);

            Log.Write(LogGroup.Property, $"{PlayerName.GetAuditName(player)} crated {definition.Type} {animal.Id}{(inOverflow ? " into overflow storage" : string.Empty)}.");
            return LivestockResult.Success(LivestockRules.DescribeCrated(definition.Name, inOverflow));
        }

        private static LivestockResult RefusedFor(LivestockRefusal refusal, Livestock animal, DateTime now)
        {
            if (animal == null)
                return LivestockResult.Refused(refusal);

            // The refusals that wait on a clock say for how long.
            switch (refusal)
            {
                case LivestockRefusal.CollectTooSoon when LivestockRules.GetNextCollectUtc(animal) is { } next:
                    return LivestockResult.Refused(refusal, $"The animal can be collected from again in {FarmingRules.FormatDuration(next - now)}.");
                case LivestockRefusal.PredatorPenalty when animal.NoProductsUntilUtc is { } until:
                    return LivestockResult.Refused(refusal, $"Predators raided the corrals. The animal gives no products for {FarmingRules.FormatDuration(until - now)}.");
                case LivestockRefusal.StillYoung:
                    return LivestockResult.Refused(refusal, "The animal is still young. " + LivestockRules.DescribeGrowth(animal, now));
                default:
                    return LivestockResult.Refused(refusal);
            }
        }

        /// <summary>Gives the owner what a collection or a butchering yielded and the Agriculture XP for it.</summary>
        private static LivestockResult GiveProducts(
            uint player,
            Livestock animal,
            LivestockDefinition definition,
            List<(string Resref, int Count)> items,
            string verb,
            DateTime now)
        {
            var itemsInOverflow = 0;
            var named = new List<(string Name, int Count)>();
            foreach (var (resref, count) in items)
            {
                itemsInOverflow += FarmItems.Give(player, animal.OwnerPlayerId, resref, count, GetLocation(player));
                named.Add((Cache.GetItemNameByResref(resref) ?? resref, count));
            }

            GiveProductXp(player, definition, items);

            Log.Write(LogGroup.Property, $"{PlayerName.GetAuditName(player)} {verb} {definition.Type} {animal.Id}: {string.Join(", ", items.Select(item => $"{item.Count} {item.Resref}"))} ({itemsInOverflow} to overflow).");
            return LivestockResult.Success(LivestockRules.DescribeProducts(verb, definition.Name, named, itemsInOverflow));
        }

        private static void GiveProductXp(uint player, LivestockDefinition definition, List<(string Resref, int Count)> items)
        {
            var rank = Skill.GetCreatureSkillRank(player, SkillType.Agriculture);
            Skill.GiveSkillXP(
                player,
                SkillType.Agriculture,
                LivestockRules.GetProductXp(definition.Level, rank, LivestockRules.CountItems(items)),
                false,
                false);
        }

        /// <summary>
        /// The City Hall clerk's collection or butchering for an owner who cannot reach their corral: the products are marked on the
        /// animal first, then written to the owner's overflow store, and then the animal records the collection or is deleted
        /// (<see cref="ClerkRecovery.RunAnimal"/>). A restart in between is finished at the next processing.
        /// </summary>
        private static LivestockResult GiveProductsThroughClerk(
            uint player,
            Livestock animal,
            LivestockDefinition definition,
            List<(string Resref, int Count)> items,
            ClerkRecoveryAction action,
            string verb,
            DateTime now)
        {
            ClerkRecovery.RunAnimal(animal, ClerkRecoveryRules.BuildProductMark(Guid.NewGuid().ToString(), action, animal.OwnerPlayerId, now, items));
            GiveProductXp(player, definition, items);

            Log.Write(LogGroup.Property, $"{PlayerName.GetAuditName(player)} had the clerk {verb} {definition.Type} {animal.Id}: {string.Join(", ", items.Select(item => $"{item.Count} {item.Resref}"))} into overflow storage.");
            return LivestockResult.Success(ClerkRecoveryRules.DescribeRemoteProducts(
                verb,
                definition.Name,
                items.Select(item => (Cache.GetItemNameByResref(item.Resref) ?? item.Resref, item.Count))));
        }

        /// <summary>
        /// The City Hall clerk's crating for an owner who cannot reach their corral, or the mayor's send-away of an absent owner's
        /// animal: the crated animal is marked first, then written to the owner's overflow store as a crate, and then the animal is
        /// deleted. Nothing is done when the crate item does not exist yet.
        /// </summary>
        private static LivestockResult CrateThroughClerk(uint player, Livestock animal, LivestockDefinition definition, LivestockCrate crate, DateTime now)
        {
            if (!CanCreateCrate(definition))
                return LivestockResult.Refused(LivestockRefusal.NotAnAnimal, ClerkRecoveryRules.NoCratesText);

            ClerkRecovery.RunAnimal(animal, ClerkRecoveryRules.BuildCrateMark(Guid.NewGuid().ToString(), animal.OwnerPlayerId, now, crate));

            Log.Write(LogGroup.Property, $"{PlayerName.GetAuditName(player)} had the clerk crate {definition.Type} {animal.Id} into overflow storage.");
            return LivestockResult.Success(ClerkRecoveryRules.DescribeRemoteCrated(definition.Name));
        }

        /// <summary>Whether the crate item exists, so an animal is never marked for a crate that cannot be made.</summary>
        internal static bool CanCreateCrate(LivestockDefinition definition)
        {
            var probe = CreateCrateItem(new LivestockCrate { Species = definition.Type }, definition, GetStartingLocation());
            if (!GetIsObjectValid(probe))
                return false;

            DestroyObject(probe);
            return true;
        }

        // Animals leaving

        /// <summary>An animal went unfed for 72 hours: it is gone, and its owner is told.</summary>
        public static void RemoveWanderedOff(Livestock animal)
        {
            // An action the City Hall clerk was finishing is finished first; the animal wanders off at the next processing.
            if (ClerkRecovery.RecoverAnimal(animal.Id))
                return;

            DB.Delete<Livestock>(animal.Id);
            DestroyCreature(animal.Id);

            var name = GetDefinition(animal.Species)?.Name ?? "animal";
            Log.Write(LogGroup.Property, $"{animal.Species} {animal.Id} of player {animal.OwnerPlayerId} wandered off from corral {animal.CorralId}.");
            Notify(animal.OwnerPlayerId, $"Your {name} went unfed for {LivestockRules.WanderOffHours} hours and wandered off.");
        }

        /// <summary>
        /// Crates an animal whose corral is gone (its city or station was lost, or the corral was picked up) and sends the crate
        /// to its owner's overflow store. The record's id makes the crate's overflow record repeatable after a restart.
        /// </summary>
        private static void SendToOverflow(Livestock animal, DateTime now)
        {
            // An action the City Hall clerk was finishing is finished first. A butchered or crated animal is gone; one only
            // collected from is crated below.
            if (ClerkRecovery.RecoverAnimal(animal.Id))
            {
                animal = GetAnimal(animal.Id);
                if (animal == null)
                    return;
            }

            var definition = GetDefinition(animal.Species);
            if (definition != null && LivestockRules.HasWanderedOff(animal, now))
            {
                RemoveWanderedOff(animal);
                return;
            }

            if (definition != null)
            {
                var item = CreateCrateItem(LivestockRules.Pack(animal, now), definition, GetStartingLocation());
                if (GetIsObjectValid(item))
                {
                    FarmItems.StoreInOverflow(animal.OwnerPlayerId, item, "livestock-crate:" + animal.Id);
                    Notify(animal.OwnerPlayerId, $"Your {definition.Name} lost its corral. It is crated in your overflow storage. Use /overflow to claim it.");
                }
                else
                {
                    Log.Write(LogGroup.Property, $"The crate for {animal.Species} {animal.Id} could not be created, so the animal is lost.");
                }
            }

            DB.Delete<Livestock>(animal.Id);
            DestroyCreature(animal.Id);
            Log.Write(LogGroup.Property, $"{animal.Species} {animal.Id} lost corral {animal.CorralId} and went to player {animal.OwnerPlayerId}'s overflow storage, crated.");
        }

        /// <summary>
        /// A property was deleted. When it was a corral (picked up, or lost with its city or station), its animals go to their
        /// owners' overflow stores, crated.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSwlorDeleteProperty)]
        public static void OnPropertyDeleted()
        {
            var propertyId = EventsPlugin.GetEventData("PROPERTY_ID");
            if (string.IsNullOrWhiteSpace(propertyId))
                return;

            var now = DateTime.UtcNow;
            foreach (var animal in GetAnimalsInCorral(propertyId))
                SendToOverflow(animal, now);

            foreach (var hunt in Search(new DBQuery<LivestockHunt>().AddFieldSearch(nameof(LivestockHunt.CityId), propertyId, false)))
                LivestockHunts.Cancel(hunt);
        }

        /// <summary>
        /// At server start, removes the animals that wandered off while the server was down, and crates the animals of corrals
        /// that no longer stand. Farmhands are brought up to date first, so the feed they had on hand counts.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void ProcessAtServerStart()
        {
            // The City Hall clerk's actions a restart interrupted are finished before anything replays or removes an animal.
            ClerkRecovery.RecoverAnimalsAtServerStart();

            Farmhands.CatchUpAll();

            var now = DateTime.UtcNow;
            var wandered = 0;
            var orphaned = 0;
            foreach (var animal in Search(new DBQuery<Livestock>()))
            {
                if (GetCorral(animal.CorralId) == null)
                {
                    SendToOverflow(animal, now);
                    orphaned++;
                }
                else if (LivestockRules.HasWanderedOff(animal, now))
                {
                    RemoveWanderedOff(animal);
                    wandered++;
                }
            }

            Log.Write(LogGroup.Property, $"Livestock processed at server start: {wandered} wandered off and {orphaned} animals of missing corrals crated.");
        }

        // The predators' penalty

        /// <summary>
        /// Predators raided a city's corrals (a hunt lost or a situation ignored): every animal in the city gives no products for
        /// 72 hours, which crating keeps. Their owners are told. Returns how many animals were penalized.
        /// </summary>
        public static int ApplyPredatorPenalty(WorldProperty city, DateTime now)
        {
            // Each corral's farmhand finishes the work it owed before the raid first, so the penalty only stops products from now on.
            foreach (var corralId in GetAnimalsInCity(city.Id).Select(animal => animal.CorralId).Distinct().ToList())
                Farmhands.CatchUp(corralId);

            var animals = GetAnimalsInCity(city.Id);
            foreach (var animal in animals)
            {
                LivestockRules.ApplyPenalty(animal, now);
                DB.Set(animal);
            }

            foreach (var owner in animals.GroupBy(animal => animal.OwnerPlayerId))
            {
                Notify(owner.Key, $"Predators raided the corrals of {city.CustomName}. Your animals there give no products for {LivestockRules.PenaltyHours} hours.");
            }

            Log.Write(LogGroup.Property, $"Predators raided the corrals of city {city.Id}: {animals.Count} animals give no products until {LivestockRules.GetPenaltyEndUtc(now):u}.");
            return animals.Count;
        }

        /// <summary>The city's animals are safe: told to their owners, who read it when they next log in.</summary>
        public static void NotifyOwnersSafe(WorldProperty city)
        {
            foreach (var owner in GetAnimalsInCity(city.Id).GroupBy(animal => animal.OwnerPlayerId))
                Notify(owner.Key, $"A hunt drove the predators from the corrals of {city.CustomName}. Your animals there are safe.");
        }

        // Notices

        /// <summary>Tells a character now if they are on, or at their next login if not.</summary>
        public static void Notify(string playerId, string text)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                return;

            var player = GetObjectByUUID(playerId);
            if (GetIsObjectValid(player) && GetIsPC(player))
            {
                SendMessageToPC(player, ColorToken.Green(text));
                return;
            }

            DB.Set(new LivestockNotice { PlayerId = playerId, Text = text });
        }

        /// <summary>At login, a character reads what happened to their animals while they were away.</summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void DeliverNotices()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var notices = Search(new DBQuery<LivestockNotice>().AddFieldSearch(nameof(LivestockNotice.PlayerId), GetObjectUUID(player), false))
                .OrderBy(notice => notice.DateCreated)
                .ToList();
            foreach (var notice in notices)
            {
                SendMessageToPC(player, ColorToken.Green(notice.Text));
                DB.Delete<LivestockNotice>(notice.Id);
            }
        }

        // The creatures

        /// <summary>
        /// Stands the animal's creature in the corral: raised from its source creature, harmless (the standard commoner faction),
        /// unkillable (plot) and still. Talking to it opens <see cref="LivestockDialog"/>.
        /// </summary>
        public static uint SpawnCreature(Livestock animal, uint corralPlaceable)
        {
            DestroyCreature(animal.Id);

            var definition = GetDefinition(animal.Species);
            if (definition == null || !GetIsObjectValid(corralPlaceable))
                return OBJECT_INVALID;

            var slot = GetFreeSlot(corralPlaceable);
            var creature = CreateObject(ObjectType.Creature, definition.SourceCreatureResref, GetSlotLocation(corralPlaceable, slot));
            if (!GetIsObjectValid(creature))
            {
                Log.Write(LogGroup.Property, $"The creature '{definition.SourceCreatureResref}' for {animal.Species} {animal.Id} could not be created.");
                return OBJECT_INVALID;
            }

            ChangeToStandardFaction(creature, StandardFaction.Commoner);
            SetPlotFlag(creature, true);
            SetLocalString(creature, AnimalIdVariable, animal.Id);
            SetLocalInt(creature, SlotVariable, slot + 1);
            SetLocalString(creature, ConversationVariable, nameof(LivestockDialog));
            SetEventScript(creature, EventScript.Creature_OnDialogue, ScriptName.OnDialogStart);
            foreach (var script in StillScripts)
                SetEventScript(creature, script, string.Empty);

            _creatures[animal.Id] = creature;
            RefreshName(animal);
            return creature;
        }

        // A placed animal stands in its pen: no heartbeat, perception or combat scripts of the creature it was raised from.
        private static readonly EventScript[] StillScripts =
        {
            EventScript.Creature_OnHeartbeat,
            EventScript.Creature_OnNotice,
            EventScript.Creature_OnSpellCastAt,
            EventScript.Creature_OnMeleeAttacked,
            EventScript.Creature_OnDamaged,
            EventScript.Creature_OnDisturbed,
            EventScript.Creature_OnEndCombatRound,
            EventScript.Creature_OnBlockedByDoor,
        };

        public static void DestroyCreature(string animalId)
        {
            if (_creatures.Remove(animalId, out var creature) && GetIsObjectValid(creature))
                DestroyObject(creature);
        }

        /// <summary>Names the creature for its kind and stage.</summary>
        public static void RefreshName(Livestock animal)
        {
            if (!_creatures.TryGetValue(animal.Id, out var creature) || !GetIsObjectValid(creature))
                return;

            var definition = GetDefinition(animal.Species);
            if (definition != null)
                SetName(creature, $"{definition.Name} ({LivestockRules.DescribeStage(LivestockRules.GetStage(animal, DateTime.UtcNow)).ToLowerInvariant()})");
        }

        private static int GetFreeSlot(uint corralPlaceable)
        {
            var used = new HashSet<int>();
            foreach (var creature in _creatures.Values)
            {
                if (GetIsObjectValid(creature) && GetArea(creature) == GetArea(corralPlaceable) &&
                    GetDistanceBetween(creature, corralPlaceable) < SlotRadius * 3)
                    used.Add(GetLocalInt(creature, SlotVariable) - 1);
            }

            for (var slot = 0; slot < LivestockRules.AnimalsPerCorral; slot++)
            {
                if (!used.Contains(slot))
                    return slot;
            }

            return 0;
        }

        private static Location GetSlotLocation(uint corralPlaceable, int slot)
        {
            var center = GetPosition(corralPlaceable);
            var angle = slot * (2f * MathF.PI / LivestockRules.AnimalsPerCorral);
            var position = Vector3(
                center.X + MathF.Cos(angle) * SlotRadius,
                center.Y + MathF.Sin(angle) * SlotRadius,
                center.Z);
            return Location(GetArea(corralPlaceable), position, slot * 120f + 180f);
        }

        /// <summary>Creates a crate item holding the animal's timers, or an invalid item when the crate blueprint does not exist yet.</summary>
        internal static uint CreateCrateItem(LivestockCrate crate, LivestockDefinition definition, Location where)
        {
            var item = CreateObject(ObjectType.Item, LivestockRules.CrateResref, where);
            if (!GetIsObjectValid(item))
            {
                Log.Write(LogGroup.Property, $"The crate item '{LivestockRules.CrateResref}' does not exist, so a {definition.Name} cannot be crated.");
                return OBJECT_INVALID;
            }

            foreach (var (name, value) in crate.ToLocals())
                SetLocalInt(item, name, value);
            SetName(item, $"Crated {definition.Name}");
            return item;
        }

        // Helpers

        private static bool IsFarmer(uint player) =>
            GetIsObjectValid(player) && GetIsPC(player) && !GetIsDM(player) && !GetIsDMPossessed(player);

        private static void UseUpOne(uint item)
        {
            var size = GetItemStackSize(item);
            if (size > 1)
                SetItemStackSize(item, size - 1);
            else
                DestroyObject(item);
        }
    }
}
