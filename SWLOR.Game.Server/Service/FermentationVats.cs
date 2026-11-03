using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.EnzymeCultureService;
using SWLOR.Game.Server.Service.FarmingService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.TransferService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Fermentation Vats and the enzyme cultures made in them (Player City Revamp, "Farming for other crafts"): starting a batch,
    /// collecting its enzyme, the starter each color needs, and what happens to a batch when its vat goes away. The rules (the
    /// 2 day batch, the inputs, Agriculture 20, the one batch per CD key, the starter) are <see cref="EnzymeCultureRules"/> and
    /// the stat rolls <see cref="EnzymeRolls"/>; this class reads and writes the records and the inventory and applies them.
    /// A vat is a structure in a house, used through a conversation on it (<see cref="FermentationVatDialog"/>). A batch runs on
    /// the clock, so it finishes on time across a restart without any timer.
    /// </summary>
    public static class FermentationVats
    {
        /// <summary>The name of a vat's placeable.</summary>
        public const string VatName = "Fermentation Vat";

        /// <summary>
        /// The item local that marks an enzyme a vat made. A marked enzyme works in an incubator like any other but cannot be the
        /// starter of a vat, so a color still has to be hunted once per vat.
        /// </summary>
        public const string CulturedVariable = "ENZYME_CULTURED";

        /// <summary>
        /// The item local that carries a vat's remembered colors while it is a structure item in a pack: the resrefs of the enzymes
        /// it has copied, joined by commas (<see cref="EnzymeCultureRules.EncodeStarters"/>). Picking the vat up writes it, and
        /// placing the item again restores it, so moving a vat to another house does not start it over.
        /// </summary>
        public const string StartersVariable = "VAT_STARTERS";

        /// <summary>What a cultured enzyme's name ends with, so a player can tell it from a hunted one.</summary>
        public const string CulturedSuffix = " (Cultured)";

        // Records

        /// <summary>The vat structure with the id, or null when it is not a Fermentation Vat.</summary>
        public static WorldProperty GetVatStructure(string vatId)
        {
            var property = string.IsNullOrWhiteSpace(vatId) ? null : DB.Get<WorldProperty>(vatId);
            return property is { PropertyType: PropertyType.Structure, StructureType: StructureType.FermentationVat } ? property : null;
        }

        /// <summary>What the vat remembers, or null for a vat that has copied nothing yet.</summary>
        public static FermentationVat GetRecord(string vatId) =>
            string.IsNullOrWhiteSpace(vatId) ? null : DB.Get<FermentationVat>(FermentationVat.KeyFor(vatId));

        /// <summary>The batch in the vat, or null when it is empty.</summary>
        public static FermentationJob GetJob(string vatId) =>
            string.IsNullOrWhiteSpace(vatId) ? null : DB.Get<FermentationJob>(FermentationJob.KeyFor(vatId));

        /// <summary>The batches a CD key has in any vat, across every character on it. Without a CD key, the character's own.</summary>
        public static List<FermentationJob> GetJobsOfCdKey(string cdKey, string playerId)
        {
            var query = string.IsNullOrWhiteSpace(cdKey)
                ? new DBQuery<FermentationJob>().AddFieldSearch(nameof(FermentationJob.OwnerPlayerId), playerId, false)
                : new DBQuery<FermentationJob>().AddFieldSearch(nameof(FermentationJob.OwnerCdKey), cdKey, false);
            return Search(query);
        }

        // Access

        /// <summary>
        /// Whether the character can use the vat: staff, the owner of the house it stands in, and anyone the owner has given garden
        /// permission, since a vat is farm equipment (<see cref="Farming.HasGardenPermission"/>).
        /// </summary>
        public static bool HasAccess(uint player, WorldProperty vat) => Farming.HasGardenPermission(player, vat);

        // Items

        /// <summary>The hunted enzyme of the type and color the character carries, or invalid. A cultured one does not count.</summary>
        public static uint FindStarter(uint player, EnzymeType type, EnzymeColorType color)
        {
            for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item); item = GetNextItemInInventory(player))
            {
                if (EnzymeCultureRules.CanBeStarter(GetResRef(item), GetLocalInt(item, CulturedVariable) != 0, type, color))
                    return item;
            }

            return OBJECT_INVALID;
        }

        /// <summary>The crops the character carries two or more of, which a batch can use, lowest tier first.</summary>
        public static List<(CropDefinition Crop, int Count)> GetUsableCrops(uint player)
        {
            return Farming.GetAllCrops()
                .Where(EnzymeCultureRules.IsCultureCrop)
                .Select(crop => (Crop: crop, Count: FarmItems.Count(player, crop.HarvestResref)))
                .Where(entry => entry.Count >= EnzymeCultureRules.CropsPerBatch)
                .OrderBy(entry => entry.Crop.Tier)
                .ThenBy(entry => entry.Crop.Name)
                .ToList();
        }

        // Actions

        /// <summary>
        /// Starts a batch of the enzyme with 2 of the crop, 1 manure (the lowest tier carried) and 1 Distilled Water. A color the vat
        /// has not copied also uses up a hunted enzyme of it as the starter, which the vat remembers.
        /// </summary>
        public static FermentationResult Start(uint player, string vatId, EnzymeType type, EnzymeColorType color, CropType cropType)
        {
            var now = DateTime.UtcNow;
            var isPlayer = IsFarmer(player);
            var structure = GetVatStructure(vatId);
            var crop = Farming.GetCrop(cropType);
            var record = GetRecord(vatId);
            var starters = record?.Starters ?? new List<string>();
            var playerId = isPlayer ? GetObjectUUID(player) : string.Empty;
            var cdKey = isPlayer ? GetPCPublicCDKey(player) : string.Empty;
            var cropResref = crop?.HarvestResref ?? string.Empty;
            var manure = isPlayer ? EnzymeCultureRules.PickManure(resref => FarmItems.Count(player, resref)) : string.Empty;
            var starter = isPlayer && EnzymeCultureRules.IsEnzyme(type, color) ? FindStarter(player, type, color) : OBJECT_INVALID;

            var refusal = EnzymeCultureRules.CheckStart(
                isPlayer,
                structure != null,
                isPlayer && HasAccess(player, structure),
                GetJob(vatId),
                isPlayer ? GetJobsOfCdKey(cdKey, playerId) : new List<FermentationJob>(),
                now,
                type,
                color,
                isPlayer ? Skill.GetCreatureSkillRank(player, SkillType.Agriculture) : 0,
                EnzymeCultureRules.IsCultureCrop(crop),
                isPlayer && crop != null ? FarmItems.Count(player, cropResref) : 0,
                !string.IsNullOrEmpty(manure),
                isPlayer ? FarmItems.Count(player, EnzymeCultureRules.WaterResref) : 0,
                starters,
                GetIsObjectValid(starter));
            if (refusal != FermentationRefusal.None)
                return FermentationResult.Refused(refusal);

            var needsStarter = !EnzymeCultureRules.HasStarter(starters, type, color);
            var starterData = needsStarter ? ObjectPlugin.Serialize(starter) : string.Empty;

            // The ingredients and the starter are taken and the character file saved before the batch is recorded (the transfer journal),
            // so a restart cannot leave them in the restored character with the batch fermenting, or lose the starter. A batch the vat
            // can no longer take sends them to the character's overflow store.
            var job = new FermentationJob
            {
                Id = FermentationJob.KeyFor(vatId),
                VatId = vatId,
                OwnerPlayerId = playerId,
                OwnerCdKey = cdKey,
                Type = type,
                Color = color,
                StartedUtc = now,
                CompleteUtc = EnzymeCultureRules.GetCompleteUtc(now),
                CropResref = cropResref,
                ManureResref = manure,
                WaterResref = EnzymeCultureRules.WaterResref,
                StarterData = starterData
            };
            var entry = TransferJournalRules.Create(playerId, StartKind, TransferDirection.Deposit, job.Id, "The ingredients of your fermentation batch", now);
            entry.Payload = JsonConvert.SerializeObject(job);
            entry.Items.Add(TransferJournalRules.Plain(cropResref, crop.Name, EnzymeCultureRules.CropsPerBatch));
            entry.Items.Add(TransferJournalRules.Plain(manure, Cache.GetItemNameByResref(manure) ?? manure, EnzymeCultureRules.ManurePerBatch));
            entry.Items.Add(TransferJournalRules.Plain(EnzymeCultureRules.WaterResref, Cache.GetItemNameByResref(EnzymeCultureRules.WaterResref) ?? EnzymeCultureRules.WaterResref, EnzymeCultureRules.WaterPerBatch));
            if (needsStarter)
                entry.Items.Add(TransferJournalRules.Held(GetResRef(starter), GetName(starter), starterData));

            switch (TransferJournal.Deposit(player, entry, needsStarter ? new[] { starter } : new uint[0]))
            {
                case TransferSettlement.Declined:
                    return FermentationResult.Refused(FermentationRefusal.NotSaved);
                case TransferSettlement.Apply:
                    break;
                default:
                    return FermentationResult.Refused(FermentationRefusal.VatBusy);
            }

            Log.Write(LogGroup.Property, $"{PlayerName.GetAuditName(player)} started a batch of {EnzymeCultureRules.GetResref(type, color)} in fermentation vat {vatId}{(needsStarter ? ", using a hunted starter" : string.Empty)}.");
            return FermentationResult.Success(EnzymeCultureRules.DescribeStarted(type, color, needsStarter));
        }

        /// <summary>
        /// Collects a finished batch for the character who started it: one cultured enzyme of the type and color, rolled like a
        /// hunted one from the lowest brackets, and the Agriculture XP of a level 20 recipe. An enzyme that does not fit in the
        /// inventory goes to their overflow store.
        /// </summary>
        public static FermentationResult Collect(uint player, string vatId)
        {
            var now = DateTime.UtcNow;
            var isPlayer = IsFarmer(player);
            var job = GetJob(vatId);
            var playerId = isPlayer ? GetObjectUUID(player) : string.Empty;
            var refusal = EnzymeCultureRules.CheckCollect(isPlayer, GetVatStructure(vatId) != null, job, playerId, now);
            if (refusal == FermentationRefusal.NotDone)
                return FermentationResult.Refused(refusal, EnzymeCultureRules.DescribeBatch(job, now, true));
            if (refusal != FermentationRefusal.None)
                return FermentationResult.Refused(refusal);

            var rolls = EnzymeRolls.Roll(job.Type, EnzymeCultureRules.MaxPrimaryBracket, EnzymeCultureRules.MaxSecondaryBracket);
            var item = CreateEnzyme(job, rolls, GetLocation(player));
            if (!GetIsObjectValid(item))
                return FermentationResult.Refused(FermentationRefusal.NoBatch, "The vat cannot make that enzyme right now, so the batch stays in it.");

            // The batch is deleted and the character is saved holding the enzyme in one journaled withdrawal (the transfer journal); an
            // enzyme that does not fit goes to the owner's overflow store. The enzyme is saved in the entry exactly as rolled, so a
            // restart delivers the same one.
            var entry = TransferJournalRules.Create(job.OwnerPlayerId, CollectKind, TransferDirection.Withdraw, job.Id, "Your cultured enzyme", now, job.StartedUtc.Ticks.ToString());
            entry.Items.Add(TransferJournalRules.Held(GetResRef(item), GetName(item), ObjectPlugin.Serialize(item)));
            DestroyObject(item);
            TransferJournal.Withdraw(player, entry, out var overflowByItem);
            var inOverflow = overflowByItem[0] > 0;

            var rank = Skill.GetCreatureSkillRank(player, SkillType.Agriculture);
            Skill.GiveSkillXP(player, SkillType.Agriculture, EnzymeCultureRules.GetXp(rank), false, false);

            Log.Write(LogGroup.Property, $"{PlayerName.GetAuditName(player)} collected a cultured {EnzymeCultureRules.GetResref(job.Type, job.Color)} from fermentation vat {vatId} ({string.Join(", ", rolls.Select(roll => $"{roll.Stat} {roll.Amount}"))}{(inOverflow ? "; to overflow" : string.Empty)}).");
            return FermentationResult.Success(EnzymeCultureRules.DescribeCollected(job.Type, job.Color, inOverflow));
        }

        /// <summary>
        /// Makes the enzyme item of the batch with the rolled stats, marked as cultured. The blueprint carries the enzyme's color;
        /// the stats go on the same way the loot table puts them on a hunted one.
        /// </summary>
        private static uint CreateEnzyme(FermentationJob job, IReadOnlyList<EnzymeStatRoll> rolls, Location location)
        {
            var item = CreateObject(ObjectType.Item, EnzymeCultureRules.GetResref(job.Type, job.Color), location);
            if (!GetIsObjectValid(item))
            {
                Log.Write(LogGroup.Property, $"The enzyme item '{EnzymeCultureRules.GetResref(job.Type, job.Color)}' for fermentation vat {job.VatId} could not be created.");
                return OBJECT_INVALID;
            }

            foreach (var roll in rolls)
            {
                var property = ItemPropertyCustom(ItemPropertyType.Incubation, (int)roll.Stat, roll.Amount);
                BiowareXP2.IPSafeAddItemProperty(item, property, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            }

            SetLocalInt(item, CulturedVariable, 1);
            SetName(item, GetName(item) + CulturedSuffix);
            return item;
        }

        // The structure

        /// <summary>
        /// A vat is placed or moved, or the house it stands in loads: make it usable and name it. A vat that has no record yet is
        /// new in the world, so it takes the colors its structure item carried (<see cref="StartersVariable"/>), and gets its record.
        /// </summary>
        public static void OnVatSpawned(WorldProperty vat, uint placeable)
        {
            SetName(placeable, VatName);
            SetUseableFlag(placeable, true);
            SetEventScript(placeable, EventScript.Placeable_OnUsed, ScriptName.OnFermentationVatUsed);
            RestoreStarters(vat);
        }

        /// <summary>
        /// A vat is being picked up: writes the colors it remembers onto the structure item the player is about to receive, so the
        /// next place it is put down it still knows them. A color a running batch is using a starter for is left off, as that
        /// starter goes back to its owner with the batch (<see cref="EnzymeCultureRules.GetCarriedStarters"/>).
        /// </summary>
        public static void WriteStarters(string vatId, uint item)
        {
            if (!GetIsObjectValid(item))
                return;

            var carried = EnzymeCultureRules.GetCarriedStarters(GetRecord(vatId)?.Starters, GetJob(vatId), DateTime.UtcNow);
            var text = EnzymeCultureRules.EncodeStarters(carried);
            if (string.IsNullOrEmpty(text))
                DeleteLocalString(item, StartersVariable);
            else
                SetLocalString(item, StartersVariable, text);
        }

        /// <summary>
        /// Gives a vat that has no record the colors its placed item carried, which the structure keeps a copy of, and saves the
        /// record. A vat with a record is left alone, so a house loading does not read the item again.
        /// </summary>
        private static void RestoreStarters(WorldProperty vat)
        {
            if (vat == null || DB.Exists<FermentationVat>(FermentationVat.KeyFor(vat.Id)))
                return;

            var starters = new List<string>();
            if (!string.IsNullOrWhiteSpace(vat.SerializedItem))
            {
                var item = ObjectPlugin.Deserialize(vat.SerializedItem);
                if (GetIsObjectValid(item))
                {
                    starters = EnzymeCultureRules.DecodeStarters(GetLocalString(item, StartersVariable));
                    DestroyObject(item);
                }
            }

            DB.Set(new FermentationVat { Id = FermentationVat.KeyFor(vat.Id), VatId = vat.Id, Starters = starters });
            if (starters.Count > 0)
                Log.Write(LogGroup.Property, $"Fermentation vat {vat.Id} was placed with {starters.Count} remembered enzymes: {string.Join(", ", starters)}.");
        }

        /// <summary>A vat was used: open its menu.</summary>
        [NWNEventHandler(ScriptName.OnFermentationVatUsed)]
        public static void UseVat()
        {
            var player = GetLastUsedBy();
            if (!IsFarmer(player))
                return;

            var vat = OBJECT_SELF;
            if (GetVatStructure(Property.GetPropertyId(vat)) == null)
                return;

            ConversationMenu.Start(player, vat, nameof(FermentationVatDialog));
        }

        // When a vat goes away

        /// <summary>
        /// A property was deleted. When it was a vat (picked up, or lost with its house or city), what it remembered goes, and the
        /// batch in it goes to its owner's overflow store: the enzyme if the batch had finished, the ingredients if not.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSwlorDeleteProperty)]
        public static void OnPropertyDeleted()
        {
            var propertyId = EventsPlugin.GetEventData("PROPERTY_ID");
            if (string.IsNullOrWhiteSpace(propertyId))
                return;

            var job = GetJob(propertyId);
            if (job != null)
                SendToOverflow(job, DateTime.UtcNow);

            if (DB.Exists<FermentationVat>(FermentationVat.KeyFor(propertyId)))
                DB.Delete<FermentationVat>(FermentationVat.KeyFor(propertyId));
        }

        /// <summary>
        /// At server start, sends the batches of vats that no longer stand to their owners' overflow stores, and removes the
        /// records of vats that are gone. A batch that finished while the server was down needs nothing: it is done by the clock.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void ProcessAtServerStart()
        {
            var now = DateTime.UtcNow;
            var recovered = 0;
            var orphaned = 0;
            foreach (var job in Search(new DBQuery<FermentationJob>()))
            {
                if (GetVatStructure(job.VatId) != null)
                    continue;

                SendToOverflow(job, now);
                recovered++;
            }

            foreach (var record in Search(new DBQuery<FermentationVat>()))
            {
                if (GetVatStructure(record.VatId) != null)
                    continue;

                DB.Delete<FermentationVat>(record.Id);
                orphaned++;
            }

            Log.Write(LogGroup.Property, $"Fermentation vats processed at server start: {recovered} batches of missing vats sent to overflow storage and {orphaned} records of missing vats removed.");
        }

        /// <summary>
        /// Sends the batch of a vat that is gone to its owner's overflow store: the enzyme when the batch had finished, otherwise
        /// the ingredients, the starter included as the same item the owner gave. Every overflow record has an id made from the
        /// batch's, so a stop part way and a second try write the same records again, and the batch is deleted last.
        /// </summary>
        private static void SendToOverflow(FermentationJob job, DateTime now)
        {
            var where = GetStartingLocation();
            var owner = job.OwnerPlayerId;
            var name = EnzymeCultureRules.GetName(job.Type, job.Color);

            if (EnzymeCultureRules.GetRecovery(job, now) == FermentationRecovery.DeliverEnzyme)
            {
                var rolls = EnzymeRolls.Roll(job.Type, EnzymeCultureRules.MaxPrimaryBracket, EnzymeCultureRules.MaxSecondaryBracket);
                var enzyme = CreateEnzyme(job, rolls, where);
                if (GetIsObjectValid(enzyme))
                    FarmItems.StoreInOverflow(owner, enzyme, $"fermentation:{job.Id}:enzyme");

                Corrals.Notify(owner, $"Your fermentation vat was removed with a finished batch in it. The cultured {name} enzyme is in your overflow storage. Use /overflow to claim it.");
            }
            else
            {
                for (var crop = 1; crop <= EnzymeCultureRules.CropsPerBatch; crop++)
                    ReturnItem(owner, job.CropResref, $"fermentation:{job.Id}:crop:{crop}", where);
                ReturnItem(owner, job.ManureResref, $"fermentation:{job.Id}:manure", where);
                ReturnItem(owner, job.WaterResref, $"fermentation:{job.Id}:water", where);

                if (!string.IsNullOrEmpty(job.StarterData))
                {
                    var starter = ObjectPlugin.Deserialize(job.StarterData);
                    if (GetIsObjectValid(starter))
                        FarmItems.StoreInOverflow(owner, starter, $"fermentation:{job.Id}:starter");
                    else
                        Log.Write(LogGroup.Property, $"The starter enzyme of fermentation vat {job.VatId} could not be restored, so it is lost.");
                }

                Corrals.Notify(owner, $"Your fermentation vat was removed while a batch of {name} was fermenting. The ingredients are in your overflow storage. Use /overflow to claim them.");
            }

            DB.Delete<FermentationJob>(job.Id);
            Log.Write(LogGroup.Property, $"The batch of {job.Type} {job.Color} in missing fermentation vat {job.VatId} went to player {owner}'s overflow storage ({(EnzymeCultureRules.GetRecovery(job, now) == FermentationRecovery.DeliverEnzyme ? "the enzyme" : "the ingredients")}).");
        }

        private static void ReturnItem(string playerId, string resref, string recordId, Location where)
        {
            if (string.IsNullOrWhiteSpace(resref))
                return;

            var item = CreateObject(ObjectType.Item, resref, where);
            if (!GetIsObjectValid(item))
            {
                Log.Write(LogGroup.Property, $"Fermentation ingredient '{resref}' for player {playerId} could not be created, so it is lost.");
                return;
            }

            FarmItems.StoreInOverflow(playerId, item, recordId);
        }

        // Transfers

        private const string StartKind = "fermentation.start";
        private const string CollectKind = "fermentation.collect";

        /// <summary>
        /// A batch started from ingredients: the vat remembers its color and records the batch, last, under the entry's start time, which
        /// is its receipt. Refused when the vat is gone or busy, or the CD key already has a batch running.
        /// </summary>
        internal static readonly TransferHandler StartTransfer = new(
            StartKind,
            IsStarted,
            ApplyStart,
            CanTakeBatch);

        /// <summary>A batch collected: the batch is deleted. It is collected once the vat holds no batch started at the entry's time.</summary>
        internal static readonly TransferHandler CollectTransfer = new(
            CollectKind,
            IsCollected,
            ApplyCollection);

        private static bool IsStarted(TransferEntry entry)
        {
            var job = DB.Get<FermentationJob>(entry.TargetId);
            return job != null && job.OwnerPlayerId == entry.PlayerId && job.StartedUtc == entry.CreatedUtc;
        }

        private static bool CanTakeBatch(TransferEntry entry)
        {
            var job = JsonConvert.DeserializeObject<FermentationJob>(entry.Payload);
            return job != null &&
                   GetVatStructure(job.VatId) != null &&
                   GetJob(job.VatId) == null &&
                   EnzymeCultureRules.CountRunning(GetJobsOfCdKey(job.OwnerCdKey, job.OwnerPlayerId), DateTime.UtcNow) < EnzymeCultureRules.MaxRunningPerCdKey;
        }

        private static void ApplyStart(TransferEntry entry)
        {
            var job = JsonConvert.DeserializeObject<FermentationJob>(entry.Payload);
            if (job == null || IsStarted(entry))
                return;

            // The vat's colors are idempotent, so they come first; the batch carries the receipt and is saved last.
            var record = GetRecord(job.VatId) ?? new FermentationVat { Id = FermentationVat.KeyFor(job.VatId), VatId = job.VatId };
            record.Starters = EnzymeCultureRules.WithStarter(record.Starters, job.Type, job.Color);
            DB.Set(record);
            DB.Set(job);
        }

        private static bool IsCollected(TransferEntry entry)
        {
            var job = DB.Get<FermentationJob>(entry.TargetId);
            return job == null || job.StartedUtc.Ticks.ToString() != TransferJournalRules.ParsePayload(entry.Payload)[0];
        }

        private static void ApplyCollection(TransferEntry entry)
        {
            if (!IsCollected(entry))
                DB.Delete<FermentationJob>(entry.TargetId);
        }

        // Helpers

        private static bool IsFarmer(uint player) =>
            GetIsObjectValid(player) && GetIsPC(player) && !GetIsDM(player) && !GetIsDMPossessed(player);

        private static List<T> Search<T>(DBQuery<T> query) where T : EntityBase
        {
            var count = checked((int)DB.SearchCount(query));
            return count <= 0 ? new List<T>() : DB.Search(query.AddPaging(count, 0)).ToList();
        }
    }
}
