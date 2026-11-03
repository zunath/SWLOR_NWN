using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.ClerkRecoveryService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.FarmingService;
using SWLOR.Game.Server.Service.LivestockService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The City Hall clerk's recovery of crops and animals (Player City Revamp, "Farming", "Livestock" and "Recovery"): the crops and
    /// animals of the player that they can no longer reach, a remote harvest, clear, collection, butchering or crating whose products go
    /// to their overflow storage, and the mayor's send-away of an absent owner's animals. Every action that writes to overflow storage
    /// marks the crop or animal first and writes the overflow records before the crop or animal is deleted or updated
    /// (<see cref="ClerkRecoveryProcess"/>), and a restart that interrupts it is finished at the next processing. The rules are
    /// <see cref="ClerkRecoveryRules"/>; the harvest, collection, butchering and crating themselves are
    /// <see cref="Farming.Harvest"/> and <see cref="Corrals"/>, called with their overflow flag.
    /// </summary>
    public static class ClerkRecovery
    {
        // What the clerk offers

        /// <summary>
        /// The player's crops and animals in the city that they cannot reach, each with the actions it allows. Every crop and animal is
        /// brought up to date first (farmhand replay, a withered crop, an animal that wandered off), so the list is current.
        /// </summary>
        public static List<ClerkRecoveryOffer> GetOffers(uint player, string cityId)
        {
            var offers = new List<ClerkRecoveryOffer>();
            if (!IsPlayer(player) || string.IsNullOrWhiteSpace(cityId))
                return offers;

            var now = DateTime.UtcNow;
            var playerId = GetObjectUUID(player);
            foreach (var plot in Search(new DBQuery<GardenPlot>().AddFieldSearch(nameof(GardenPlot.PlanterPlayerId), playerId, false)))
            {
                var offer = FindCropOffer(player, cityId, plot.PlotId, now, out _);
                if (offer != null && offer.Actions.Count > 0)
                    offers.Add(offer);
            }

            foreach (var animal in Search(new DBQuery<Livestock>().AddFieldSearch(nameof(Livestock.OwnerPlayerId), playerId, false)))
            {
                var offer = FindAnimalOffer(player, cityId, animal.Id, now, out _);
                if (offer != null && offer.Actions.Count > 0)
                    offers.Add(offer);
            }

            return offers
                .OrderBy(offer => offer.Kind)
                .ThenBy(offer => offer.Name, StringComparer.Ordinal)
                .ToList();
        }

        private static ClerkRecoveryOffer FindCropOffer(uint player, string cityId, string plotId, DateTime now, out string refusal)
        {
            refusal = ClerkRecoveryRules.NotOffered;
            Farmhands.CatchUpForPlot(plotId);
            Farming.RemoveWitheredCrop(plotId, now);

            var playerId = GetObjectUUID(player);
            var record = Farming.GetPlot(plotId);
            var structure = Farming.GetPlotStructure(plotId);
            if (record == null || structure == null || record.PlanterPlayerId != playerId)
                return null;
            if (Property.GetCityIdForProperty(structure.ParentPropertyId) != cityId)
                return null;

            var crop = Farming.GetCrop(record.Crop);
            if (crop == null)
                return null;

            if (ClerkRecoveryRules.CanReachCrop(Farming.HasGardenPermission(player, structure), IsCitizenOf(playerId, cityId)))
            {
                refusal = ClerkRecoveryRules.CanReachCropText;
                return null;
            }

            var parent = DB.Get<WorldProperty>(structure.ParentPropertyId);
            var place = parent == null ? string.Empty : parent.PropertyType == PropertyType.City ? "the city's gardens" : parent.CustomName;
            return new ClerkRecoveryOffer(
                ClerkRecoverySourceKind.Crop,
                plotId,
                crop.Name,
                ClerkRecoveryRules.DescribeCropTitle(crop.Name, FarmingRules.GetStage(record, now)),
                ClerkRecoveryRules.DescribeCrop(crop.Name, place, record, now),
                ClerkRecoveryRules.GetCropActions(record, now));
        }

        private static ClerkRecoveryOffer FindAnimalOffer(uint player, string cityId, string animalId, DateTime now, out string refusal)
        {
            refusal = ClerkRecoveryRules.NotOffered;
            var playerId = GetObjectUUID(player);
            var animal = Corrals.LoadAnimal(animalId, now);
            if (animal == null || animal.OwnerPlayerId != playerId)
                return null;

            var corral = Corrals.GetCorral(animal.CorralId);
            var definition = Corrals.GetDefinition(animal.Species);
            if (corral == null || definition == null || corral.ParentPropertyId != cityId)
                return null;

            if (ClerkRecoveryRules.CanReachAnimal(IsCitizenOf(playerId, cityId)))
            {
                refusal = ClerkRecoveryRules.CanReachAnimalText;
                return null;
            }

            return new ClerkRecoveryOffer(
                ClerkRecoverySourceKind.Animal,
                animal.Id,
                definition.Name,
                ClerkRecoveryRules.DescribeAnimalTitle(definition.Name, animal, now),
                ClerkRecoveryRules.DescribeAnimal(definition.Name, animal, now),
                ClerkRecoveryRules.GetAnimalActions(animal, now));
        }

        private static bool IsCitizenOf(string playerId, string cityId)
        {
            var dbPlayer = DB.Get<Player>(playerId);
            return dbPlayer != null && ElectionRules.IsCitizenOf(dbPlayer.CitizenPropertyId, cityId, dbPlayer.IsDeleted);
        }

        // What the clerk does

        /// <summary>Harvests a ripe crop the player cannot reach. The harvest, and any seed that came back, go to their overflow storage.</summary>
        public static ClerkRecoveryResult Harvest(uint player, string cityId, string plotId)
        {
            if (FindCropOffer(player, cityId, plotId, DateTime.UtcNow, out var refusal) == null)
                return ClerkRecoveryResult.Refused(refusal);

            var result = Farming.Harvest(player, plotId, true);
            return new ClerkRecoveryResult(result.Succeeded, result.Message);
        }

        /// <summary>Clears a crop the player cannot reach. Nothing is given, and its seed is lost.</summary>
        public static ClerkRecoveryResult Clear(uint player, string cityId, string plotId)
        {
            if (FindCropOffer(player, cityId, plotId, DateTime.UtcNow, out var refusal) == null)
                return ClerkRecoveryResult.Refused(refusal);

            var result = Farming.Clear(player, plotId);
            return new ClerkRecoveryResult(result.Succeeded, result.Message);
        }

        /// <summary>Collects from an animal the player cannot reach. The manure, and the milk from a Thune, go to their overflow storage.</summary>
        public static ClerkRecoveryResult Collect(uint player, string cityId, string animalId) =>
            ActOnAnimal(player, cityId, animalId, () => Corrals.Collect(player, animalId, true));

        /// <summary>Butchers an adult animal the player cannot reach. The meat and the second product go to their overflow storage.</summary>
        public static ClerkRecoveryResult Butcher(uint player, string cityId, string animalId) =>
            ActOnAnimal(player, cityId, animalId, () => Corrals.Butcher(player, animalId, true));

        /// <summary>Crates an animal the player cannot reach. The crate goes to their overflow storage.</summary>
        public static ClerkRecoveryResult Crate(uint player, string cityId, string animalId) =>
            ActOnAnimal(player, cityId, animalId, () => Corrals.Crate(player, animalId, true));

        private static ClerkRecoveryResult ActOnAnimal(uint player, string cityId, string animalId, Func<LivestockResult> action)
        {
            if (FindAnimalOffer(player, cityId, animalId, DateTime.UtcNow, out var refusal) == null)
                return ClerkRecoveryResult.Refused(refusal);

            var result = action();
            return new ClerkRecoveryResult(result.Succeeded, result.Message);
        }

        // The mayor

        /// <summary>The animals in the corral whose owners left the city more than 7 days ago (or whose characters are gone).</summary>
        public static List<Livestock> GetAbsentOwnersAnimals(WorldProperty corral, DateTime now)
        {
            if (corral == null)
                return new List<Livestock>();

            return Corrals.GetAnimalsInCorral(corral.Id)
                .Where(animal => IsOwnerAbsent(animal, corral.ParentPropertyId, now))
                .ToList();
        }

        private static bool IsOwnerAbsent(Livestock animal, string cityId, DateTime now)
        {
            var owner = DB.Get<Player>(animal.OwnerPlayerId);
            var isDeleted = owner == null || owner.IsDeleted;
            var isCitizen = owner != null && ElectionRules.IsCitizenOf(owner.CitizenPropertyId, cityId, owner.IsDeleted);
            DateTime? left = owner?.CityDepartures != null && owner.CityDepartures.TryGetValue(cityId, out var departed) ? departed : null;
            return ClerkRecoveryRules.IsAbsent(isCitizen, isDeleted, left, now);
        }

        /// <summary>
        /// The mayor sends the animals of owners who left the city more than 7 days ago away from the corral: each is crated, and the crate
        /// goes to its owner's overflow storage, so the corral can be cleared and picked up. The city power is
        /// <see cref="ClerkRecoveryRules.SendAwayPower"/>.
        /// </summary>
        public static ClerkRecoveryResult SendAbsentOwnersAnimalsAway(uint player, string corralId)
        {
            var corral = Corrals.GetCorral(corralId);
            var city = corral == null ? null : DB.Get<WorldProperty>(corral.ParentPropertyId);
            if (city == null)
                return ClerkRecoveryResult.Refused(LivestockRules.Describe(LivestockRefusal.NoCorral));
            if (!IsPlayer(player) || !CityPowers.Can(city, GetObjectUUID(player), ClerkRecoveryRules.SendAwayPower))
                return ClerkRecoveryResult.Refused(ClerkRecoveryRules.SendAwayNoPower);

            var now = DateTime.UtcNow;
            var sent = 0;
            foreach (var listed in GetAbsentOwnersAnimals(corral, now))
            {
                var animal = Corrals.LoadAnimal(listed.Id, now);
                var definition = animal == null ? null : Corrals.GetDefinition(animal.Species);
                if (definition == null)
                    continue;

                if (!Corrals.CanCreateCrate(definition))
                    return ClerkRecoveryResult.Refused(ClerkRecoveryRules.NoCratesText);

                var ownerId = animal.OwnerPlayerId;
                RunAnimal(animal, ClerkRecoveryRules.BuildCrateMark(Guid.NewGuid().ToString(), ownerId, now, LivestockRules.Pack(animal, now)));
                Corrals.Notify(ownerId, ClerkRecoveryRules.DescribeSentAway(definition.Name, city.CustomName));
                Log.Write(LogGroup.Property, $"{PlayerName.GetAuditName(player)}, mayor of city {city.Id}, sent {definition.Type} {animal.Id} of absent owner {ownerId} from corral {corralId} into the owner's overflow storage, crated.");
                sent++;
            }

            return sent == 0
                ? ClerkRecoveryResult.Refused(ClerkRecoveryRules.SendAwayNone)
                : ClerkRecoveryResult.Success(ClerkRecoveryRules.DescribeSentAwayResult(sent, city.CustomName));
        }

        // Marks

        /// <summary>Marks the crop with the harvest, writes it to the planter's overflow storage, and deletes the crop.</summary>
        internal static void RunCrop(GardenPlot record, ClerkRecoveryMark mark) =>
            ClerkRecoveryProcess.Run(new CropRecoveryStore(record.Id, record.PlotId), mark);

        /// <summary>Marks the animal with the action, writes what it gives to the owner's overflow storage, and deletes or updates the animal.</summary>
        internal static void RunAnimal(Livestock animal, ClerkRecoveryMark mark) =>
            ClerkRecoveryProcess.Run(new AnimalRecoveryStore(animal.Id), mark);

        /// <summary>
        /// Finishes a clerk harvest of the plot's crop that an error or a restart interrupted. Returns true when it did, and the crop is
        /// gone.
        /// </summary>
        public static bool RecoverPlot(string plotId)
        {
            if (string.IsNullOrWhiteSpace(plotId) || !ClerkRecoveryProcess.Recover(new CropRecoveryStore(GardenPlot.KeyFor(plotId), plotId)))
                return false;

            Log.Write(LogGroup.Property, $"Garden plot {plotId}: a clerk harvest that was interrupted was finished.");
            return true;
        }

        /// <summary>Finishes a clerk collection, butchering or crating of the animal that an error or a restart interrupted. Returns true when it did.</summary>
        public static bool RecoverAnimal(string animalId)
        {
            if (string.IsNullOrWhiteSpace(animalId) || !ClerkRecoveryProcess.Recover(new AnimalRecoveryStore(animalId)))
                return false;

            Log.Write(LogGroup.Property, $"Animal {animalId}: a clerk action that was interrupted was finished.");
            return true;
        }

        /// <summary>At server start, finishes the clerk actions on animals that a restart interrupted, before anything replays or removes an animal.</summary>
        public static void RecoverAnimalsAtServerStart()
        {
            foreach (var animal in Search(new DBQuery<Livestock>()))
            {
                if (animal.Recovery == null)
                    continue;

                try
                {
                    RecoverAnimal(animal.Id);
                }
                catch (Exception ex)
                {
                    Log.Write(LogGroup.Error, $"Animal {animal.Id} could not finish its clerk action at server start: {ex}");
                }
            }
        }

        // Overflow records

        /// <summary>
        /// Saves <paramref name="count"/> of an item to the player's overflow storage, in stacks, under record ids that start with
        /// <paramref name="recordPrefix"/>. The ids never change, so writing them again replaces the records instead of adding more.
        /// </summary>
        internal static void WriteOverflowItems(string playerId, string recordPrefix, string resref, int count)
        {
            var remaining = count;
            for (var chunk = 0; remaining > 0; chunk++)
            {
                var item = CreateObject(ObjectType.Item, resref, GetStartingLocation());
                if (!GetIsObjectValid(item))
                {
                    Log.Write(LogGroup.Error, $"Clerk recovery: item '{resref}' could not be created, so {remaining} owed to player {playerId} are lost.");
                    return;
                }

                SetItemStackSize(item, remaining);
                var size = Math.Max(1, GetItemStackSize(item));
                DB.Set(new OverflowItem
                {
                    Id = ClerkRecoveryRules.GetOverflowRecordId(recordPrefix, chunk),
                    PlayerId = playerId,
                    Name = GetName(item),
                    Resref = resref,
                    Tag = GetTag(item),
                    Quantity = size,
                    Data = ObjectPlugin.Serialize(item)
                });
                DestroyObject(item);
                remaining -= size;
            }
        }

        /// <summary>Saves a crate holding the animal to the player's overflow storage, under the repeatable id <c>{recordPrefix}:0</c>.</summary>
        internal static void WriteOverflowCrate(string playerId, string recordPrefix, LivestockCrate crate)
        {
            var definition = Corrals.GetDefinition(crate.Species);
            var item = definition == null ? OBJECT_INVALID : Corrals.CreateCrateItem(crate, definition, GetStartingLocation());
            if (!GetIsObjectValid(item))
            {
                Log.Write(LogGroup.Error, $"Clerk recovery: the crate for {crate.Species} could not be created, so the animal owed to player {playerId} is lost.");
                return;
            }

            FarmItems.StoreInOverflow(playerId, item, ClerkRecoveryRules.GetOverflowRecordId(recordPrefix, 0));
        }

        // Helpers

        private static bool IsPlayer(uint player) =>
            GetIsObjectValid(player) && GetIsPC(player) && !GetIsDM(player) && !GetIsDMPossessed(player);

        private static List<T> Search<T>(DBQuery<T> query) where T : EntityBase
        {
            var count = checked((int)DB.SearchCount(query));
            return count <= 0 ? new List<T>() : DB.Search(query.AddPaging(count, 0)).ToList();
        }
    }
}
