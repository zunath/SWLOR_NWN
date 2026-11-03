using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.FarmhandService;
using SWLOR.Game.Server.Service.FarmingService;
using SWLOR.Game.Server.Service.LivestockService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.StationService;
using SWLOR.Game.Server.Service.TransferService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Farmhands (Player City Revamp, "Farmhands"): an industrial droid its owner assigns to one of their houses, where it
    /// tends their crops, or to a corral, where it feeds and collects from their animals out of a feed hopper into an output
    /// hopper. It keeps its schedule while nobody is around: <see cref="CatchUp"/> replays what it would have done since it was
    /// last processed (<see cref="FarmhandRules.Replay"/>) at a server start, when someone enters its city or house, and before
    /// any action on its crops or animals. The droid is assigned and returned the way a station building's droid is
    /// (<c>StationBuildings</c>): its controller is marked and saved with the character before the farmhand takes it, a
    /// <see cref="StationDroidReceipt"/> records that it did, and a droid sent back goes to the owner's overflow store.
    /// </summary>
    public static class Farmhands
    {
        /// <summary>A droid controller's marks, saved in its owner's character file while it moves to or from a post: "target id|assignment id".</summary>
        private const string AssigningVariable = "FARMHAND_DROID_ASSIGNING";
        private const string RecalledVariable = "FARMHAND_DROID_RECALLED";

        private static readonly HashSet<string> _running = new();

        // Records

        public static Farmhand Get(string targetId) =>
            string.IsNullOrWhiteSpace(targetId) ? null : DB.Get<Farmhand>(Farmhand.KeyFor(targetId));

        /// <summary>The house, or the corral structure, with the id. Null for anything else.</summary>
        public static WorldProperty GetTarget(string targetId)
        {
            var property = string.IsNullOrWhiteSpace(targetId) ? null : DB.Get<WorldProperty>(targetId);
            if (property == null || property.IsQueuedForDeletion)
                return null;

            return property.PropertyType == PropertyType.House || Corrals.GetCorral(targetId) != null ? property : null;
        }

        public static FarmhandTargetType GetTargetType(WorldProperty target) =>
            target == null ? FarmhandTargetType.Invalid
            : target.PropertyType == PropertyType.House ? FarmhandTargetType.House
            : target.StructureType == StructureType.Corral ? FarmhandTargetType.Corral
            : FarmhandTargetType.Invalid;

        /// <summary>Whether the character may put a droid to work at the target: a house they keep, or a corral in their city.</summary>
        public static bool HasAccess(uint player, WorldProperty target)
        {
            if (GetIsDM(player) || GetIsDMPossessed(player))
                return true;
            if (!GetIsPC(player) || target == null)
                return false;

            return target.PropertyType == PropertyType.House
                ? FarmhandRules.HasHouseAccess(HouseSpecializations.GetPermissionRecord(target.Id, GetObjectUUID(player)))
                : Corrals.HasAccess(player, target);
        }

        /// <summary>Whether the character may send another player's farmhand back: whoever manages the structures of the property it stands in.</summary>
        public static bool CanManage(uint player, WorldProperty target) =>
            target != null && Property.HasPropertyPermission(player, target.PropertyType == PropertyType.House ? target.Id : target.ParentPropertyId, PropertyPermissionType.EditStructures);

        private static List<GardenPlot> GetPlots(string houseId, string planterId)
        {
            var query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.ParentPropertyId), houseId, false)
                .AddFieldSearch(nameof(WorldProperty.StructureType), (int)StructureType.GardenPlot);
            var count = checked((int)DB.SearchCount(query));
            if (count <= 0)
                return new List<GardenPlot>();

            return DB.Search(query.AddPaging(count, 0))
                .Select(structure => DB.Get<GardenPlot>(GardenPlot.KeyFor(structure.Id)))
                .Where(plot => plot != null && plot.PlanterPlayerId == planterId)
                .ToList();
        }

        // Catching up

        /// <summary>
        /// Replays the farmhand's work up to now and saves it. Does nothing where no droid works, and nothing new when the
        /// farmhand was just processed. The animals and plots are saved first and the farmhand last, so a stop part way can give
        /// an animal a free meal at worst, never lose an event the animal already shows.
        /// </summary>
        public static void CatchUp(string targetId)
        {
            if (string.IsNullOrWhiteSpace(targetId) || !_running.Add(targetId))
                return;

            try
            {
                var farmhand = Get(targetId);
                if (farmhand == null)
                    return;

                var now = DateTime.UtcNow;
                var animals = farmhand.TargetType == FarmhandTargetType.Corral ? Corrals.GetAnimalsInCorral(targetId) : new List<Livestock>();
                var plots = farmhand.TargetType == FarmhandTargetType.House ? GetPlots(targetId, farmhand.OwnerPlayerId) : new List<GardenPlot>();
                var result = FarmhandRules.Replay(farmhand, animals, plots, Corrals.GetDefinition, Farming.GetCrop, now);
                if (!result.Ran)
                    return;

                foreach (var animal in result.Animals)
                    DB.Set(animal);
                foreach (var plot in result.Plots)
                    DB.Set(plot);
                foreach (var animal in result.WanderedOff)
                    Corrals.RemoveWanderedOff(animal);
                foreach (var plot in result.Withered)
                    Farming.RemoveWitheredCrop(plot.PlotId, now);

                DB.Set(farmhand);
                if (result.Events.Count > 0)
                    Log.Write(LogGroup.Property, $"Farmhand {farmhand.DroidName} at {targetId} caught up: {string.Join(", ", result.Events.GroupBy(e => e.Type).Select(g => $"{g.Count()} {g.Key}"))}.");

                foreach (var animal in result.Animals)
                    Corrals.RefreshName(animal);
            }
            finally
            {
                _running.Remove(targetId);
            }
        }

        /// <summary>Brings up to date the farmhand of the house a garden plot stands in. Called before any action on a plot.</summary>
        public static void CatchUpForPlot(string plotId)
        {
            var plot = Farming.GetPlotStructure(plotId);
            if (plot != null)
                CatchUp(plot.ParentPropertyId);
        }

        /// <summary>At server start, brings every farmhand up to date and finishes any droid return a restart interrupted.</summary>
        public static void CatchUpAll()
        {
            FinishDroidReturns();

            var farmhands = Search(new DBQuery<Farmhand>());
            foreach (var farmhand in farmhands)
                CatchUp(farmhand.TargetId);

            if (farmhands.Count > 0)
                Log.Write(LogGroup.Property, $"{farmhands.Count} farmhands were brought up to date at server start.");
        }

        /// <summary>Someone entered an area: the farmhands of the house it is, or of the city it is, are brought up to date.</summary>
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void OnAreaEnter()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var propertyId = Property.GetPropertyId(OBJECT_SELF);
            if (string.IsNullOrWhiteSpace(propertyId))
                return;

            CatchUp(propertyId);
            foreach (var farmhand in Search(new DBQuery<Farmhand>().AddFieldSearch(nameof(Farmhand.CityId), propertyId, false)))
                CatchUp(farmhand.TargetId);
        }

        private static List<T> Search<T>(DBQuery<T> query) where T : EntityBase
        {
            var count = checked((int)DB.SearchCount(query));
            return count <= 0 ? new List<T>() : DB.Search(query.AddPaging(count, 0)).ToList();
        }

        // The droid

        /// <summary>The droid's tier, which sets the highest crop and animal tier it handles.</summary>
        public static int GetDroidTier(uint controller) => Droid.LoadDroidItemPropertyDetails(controller).Tier;

        /// <summary>Assigns one of the player's droids to the house or corral: its controller is kept there until it is recalled.</summary>
        public static FarmhandRefusal AssignDroid(uint player, string targetId, uint controller)
        {
            CatchUp(targetId);

            var isPlayer = GetIsObjectValid(player) && GetIsPC(player) && !GetIsDM(player) && !GetIsDMPossessed(player);
            var target = GetTarget(targetId);
            var refusal = FarmhandRules.CheckAssign(
                isPlayer,
                target != null,
                isPlayer && HasAccess(player, target),
                Get(targetId) != null,
                isPlayer && GetIsObjectValid(controller) && GetItemPossessor(controller) == player && Space.GetPostableDroids(player).Contains(controller));
            if (refusal != FarmhandRefusal.None)
                return refusal;

            // The controller is marked and saved with the character before the farmhand takes it, so a restart in between
            // leaves a marked controller the next login settles: it stays if the farmhand never saved the droid, and goes if
            // the farmhand did.
            var now = DateTime.UtcNow;
            var assignmentId = Guid.NewGuid().ToString();
            var farmhand = new Farmhand
            {
                Id = Farmhand.KeyFor(targetId),
                TargetId = targetId,
                TargetType = GetTargetType(target),
                CityId = target.ParentPropertyId,
                OwnerPlayerId = GetObjectUUID(player),
                SerializedDroid = ObjectPlugin.Serialize(controller),
                DroidName = Droid.GetDroidName(controller),
                DroidTier = GetDroidTier(controller),
                DroidAssignmentId = assignmentId,
                LastProcessedUtc = now,
            };
            SetLocalString(controller, AssigningVariable, $"{targetId}|{assignmentId}");
            ExportSingleCharacter(player);

            DB.Set(farmhand);
            SaveReceipt(farmhand);
            DestroyObject(controller);

            SendMessageToPC(player, ColorToken.Green($"{farmhand.DroidName} now works {(farmhand.TargetType == FarmhandTargetType.House ? "your crops here" : "the corral")}. It handles tier 1 to {farmhand.DroidTier}."));
            Log.Write(LogGroup.Property, $"{PlayerName.GetAuditName(player)} assigned droid '{farmhand.DroidName}' (tier {farmhand.DroidTier}) to {farmhand.TargetType} {targetId}.");
            return FarmhandRefusal.None;
        }

        /// <summary>
        /// Recalls the droid. Its owner gets the controller back at once, and the hoppers' contents with it; whoever manages the
        /// property sends it to its owner's overflow store.
        /// </summary>
        public static FarmhandRefusal RecallDroid(uint player, string targetId)
        {
            CatchUp(targetId);

            var farmhand = Get(targetId);
            if (farmhand == null)
                return FarmhandRefusal.NoFarmhandHere;

            if (GetObjectUUID(player) != farmhand.OwnerPlayerId)
            {
                if (!CanManage(player, GetTarget(targetId)))
                    return FarmhandRefusal.NotTheOwner;

                ReturnDroidToOverflow(farmhand);
                SendMessageToPC(player, $"{farmhand.DroidName} was sent back to its owner.");
                return FarmhandRefusal.None;
            }

            var item = ObjectPlugin.Deserialize(farmhand.SerializedDroid);
            if (!Item.TryAcquireItem(player, item))
            {
                SendMessageToPC(player, ColorToken.Red(Item.InventoryFullShortMessage));
                return FarmhandRefusal.None;
            }

            // The returned controller is marked and saved with the character before the farmhand lets the droid go, so a restart
            // in between leaves a marked controller the next login settles by clearing the farmhand.
            SetLocalString(item, RecalledVariable, $"{targetId}|{farmhand.DroidAssignmentId}");
            ExportSingleCharacter(player);

            SendMessageToPC(player, ColorToken.Green($"{farmhand.DroidName} returns to you."));
            Release(farmhand, player);
            DeleteLocalString(item, RecalledVariable);
            return FarmhandRefusal.None;
        }

        /// <summary>
        /// The farmhand is gone. What its hoppers held goes to the owner: their inventory while they are on, and overflow storage for what
        /// does not fit. An owner who is on gets it through the transfer journal, which deletes the farmhand and saves their character with
        /// the items, so a restart cannot lose the hoppers' contents or give them twice. An owner who is off gets overflow records under
        /// ids made from the assignment, written before the farmhand is deleted, so a stop part way and a second try write the same ones.
        /// </summary>
        private static void Release(Farmhand farmhand, uint ownerIfOn)
        {
            var contents = GetHopperItems(farmhand.FeedHopper, farmhand.OutputHopper);
            if (contents.Count == 0)
            {
                DB.Delete<Farmhand>(farmhand.Id);
                return;
            }

            if (GetIsObjectValid(ownerIfOn) && GetIsPC(ownerIfOn))
            {
                var entry = TransferJournalRules.Create(farmhand.OwnerPlayerId, ReleaseKind, TransferDirection.Withdraw, farmhand.Id, "What the droid's hoppers held", DateTime.UtcNow, farmhand.DroidAssignmentId);
                entry.Items.AddRange(contents);
                TransferJournal.Withdraw(ownerIfOn, entry);
                return;
            }

            var recordPrefix = "farmhand-hopper:" + (string.IsNullOrEmpty(farmhand.DroidAssignmentId) ? farmhand.Id : farmhand.DroidAssignmentId);
            for (var index = 0; index < contents.Count; index++)
            {
                var item = contents[index];
                var itemIndex = index;
                CityProjects.WriteOverflow(farmhand.OwnerPlayerId, item.Resref, item.Count, chunk => $"{recordPrefix}:{itemIndex}:{chunk}", $"Farmhand {farmhand.Id}");
            }

            DB.Delete<Farmhand>(farmhand.Id);
        }

        /// <summary>The items the hoppers hold, one entry for each blueprint: the feed as the crops it is made of, and what the droid collected.</summary>
        private static List<TransferItem> GetHopperItems(Dictionary<CropType, int> feed, Dictionary<string, int> output)
        {
            var counts = new Dictionary<string, (string Name, int Count)>();
            foreach (var (cropType, count) in feed)
            {
                var crop = Farming.GetCrop(cropType);
                if (crop != null && count > 0)
                    counts[crop.HarvestResref] = (crop.Name, counts.GetValueOrDefault(crop.HarvestResref).Count + count);
            }

            foreach (var (resref, count) in output)
            {
                if (count > 0)
                    counts[resref] = (Cache.GetItemNameByResref(resref) ?? resref, counts.GetValueOrDefault(resref).Count + count);
            }

            return counts.Select(pair => TransferJournalRules.Plain(pair.Key, pair.Value.Name, pair.Value.Count)).ToList();
        }

        // Hoppers

        /// <summary>
        /// Loads the crops of one type the player carries into the feed hopper, up to its room. Returns how many went in. The crops are
        /// taken, the character is saved and the hopper is credited through the transfer journal, so a restart cannot leave the crops in
        /// the hopper and in the restored character too.
        /// </summary>
        public static FarmhandRefusal LoadFeed(uint player, string targetId, CropType cropType, out int loaded)
        {
            loaded = 0;
            CatchUp(targetId);

            var farmhand = Get(targetId);
            var crop = Farming.GetCrop(cropType);
            if (farmhand == null)
                return FarmhandRefusal.NoFarmhandHere;
            if (farmhand.OwnerPlayerId != GetObjectUUID(player))
                return FarmhandRefusal.NotTheOwner;
            if (FarmhandRules.GetFeedRoom(farmhand.FeedHopper) <= 0)
                return FarmhandRefusal.FeedHopperFull;
            if (crop == null)
                return FarmhandRefusal.NoCrops;

            var carried = FarmItems.Count(player, crop.HarvestResref);
            if (carried <= 0)
                return FarmhandRefusal.NoCrops;

            var taking = Math.Min(carried, FarmhandRules.GetFeedRoom(farmhand.FeedHopper));
            var entry = TransferJournalRules.Create(farmhand.OwnerPlayerId, FeedLoadKind, TransferDirection.Deposit, farmhand.Id, "The feed you loaded", DateTime.UtcNow, ((int)cropType).ToString());
            entry.Items.Add(TransferJournalRules.Plain(crop.HarvestResref, crop.Name, taking));
            switch (TransferJournal.Deposit(player, entry))
            {
                case TransferSettlement.Declined:
                    return FarmhandRefusal.NotSaved;
                case TransferSettlement.Apply:
                    loaded = taking;
                    return FarmhandRefusal.None;
                default:
                    return FarmhandRefusal.FeedHopperFull;
            }
        }

        /// <summary>Takes all the feed back out of the hopper, through the transfer journal.</summary>
        public static FarmhandRefusal TakeFeedBack(uint player, string targetId)
        {
            CatchUp(targetId);

            var farmhand = Get(targetId);
            if (farmhand == null)
                return FarmhandRefusal.NoFarmhandHere;
            if (farmhand.OwnerPlayerId != GetObjectUUID(player))
                return FarmhandRefusal.NotTheOwner;
            if (FarmhandRules.CountFeed(farmhand.FeedHopper) <= 0)
                return FarmhandRefusal.NothingToTake;

            Unload(player, farmhand, FeedHopperName, GetHopperItems(farmhand.FeedHopper, new Dictionary<string, int>()), "The feed you took back");
            return FarmhandRefusal.None;
        }

        /// <summary>Empties the output hopper into the owner's inventory, with what does not fit going to their overflow store, through the transfer journal.</summary>
        public static FarmhandRefusal EmptyOutput(uint player, string targetId)
        {
            CatchUp(targetId);

            var farmhand = Get(targetId);
            if (farmhand == null)
                return FarmhandRefusal.NoFarmhandHere;
            if (farmhand.OwnerPlayerId != GetObjectUUID(player))
                return FarmhandRefusal.NotTheOwner;
            if (FarmhandRules.CountOutput(farmhand.OutputHopper) <= 0)
                return FarmhandRefusal.NothingToTake;

            Unload(player, farmhand, OutputHopperName, GetHopperItems(new Dictionary<CropType, int>(), farmhand.OutputHopper), "What the droid collected");
            return FarmhandRefusal.None;
        }

        /// <summary>The hopper is cleared and its contents given to the owner in one journaled withdrawal.</summary>
        private static void Unload(uint player, Farmhand farmhand, string hopper, List<TransferItem> items, string summary)
        {
            var entry = TransferJournalRules.Create(farmhand.OwnerPlayerId, HopperUnloadKind, TransferDirection.Withdraw, farmhand.Id, summary, DateTime.UtcNow, hopper);
            entry.Items.AddRange(items);
            TransferJournal.Withdraw(player, entry);
        }

        // Transfers

        private const string FeedLoadKind = "farmhand.feed-load";
        private const string HopperUnloadKind = "farmhand.hopper-unload";
        private const string ReleaseKind = "farmhand.release";
        private const string FeedHopperName = "feed";
        private const string OutputHopperName = "output";

        /// <summary>Feed loaded into the hopper: credited with a receipt, and refused when the hopper has changed hands or has no room.</summary>
        internal static readonly TransferHandler FeedLoadTransfer = new(
            FeedLoadKind,
            entry => HoldsReceipt(entry),
            ApplyFeedLoad,
            CanTakeFeed);

        /// <summary>A hopper emptied: cleared with a receipt.</summary>
        internal static readonly TransferHandler HopperUnloadTransfer = new(
            HopperUnloadKind,
            entry => HoldsReceipt(entry),
            ApplyHopperUnload);

        /// <summary>A farmhand released with its hoppers' contents: the farmhand is deleted. It is released once its assignment is gone.</summary>
        internal static readonly TransferHandler ReleaseTransfer = new(
            ReleaseKind,
            IsReleased,
            ApplyRelease);

        private static bool HoldsReceipt(TransferEntry entry) =>
            TransferReceipts.Has(DB.Get<Farmhand>(entry.TargetId)?.TransferReceipts, entry.Id);

        private static bool CanTakeFeed(TransferEntry entry)
        {
            var farmhand = DB.Get<Farmhand>(entry.TargetId);
            return farmhand != null &&
                   farmhand.OwnerPlayerId == entry.PlayerId &&
                   FarmhandRules.GetFeedRoom(farmhand.FeedHopper) >= entry.Items.Sum(item => item.Count);
        }

        private static void ApplyFeedLoad(TransferEntry entry)
        {
            var farmhand = DB.Get<Farmhand>(entry.TargetId);
            if (farmhand == null || TransferReceipts.Has(farmhand.TransferReceipts, entry.Id))
                return;

            FarmhandRules.LoadFeed(farmhand.FeedHopper, (CropType)int.Parse(TransferJournalRules.ParsePayload(entry.Payload)[0]), entry.Items.Sum(item => item.Count));
            farmhand.TransferReceipts = TransferReceipts.Add(farmhand.TransferReceipts, entry.Id);
            DB.Set(farmhand);
        }

        private static void ApplyHopperUnload(TransferEntry entry)
        {
            var farmhand = DB.Get<Farmhand>(entry.TargetId);
            if (farmhand == null || TransferReceipts.Has(farmhand.TransferReceipts, entry.Id))
                return;

            if (TransferJournalRules.ParsePayload(entry.Payload)[0] == FeedHopperName)
                farmhand.FeedHopper.Clear();
            else
                farmhand.OutputHopper.Clear();

            farmhand.TransferReceipts = TransferReceipts.Add(farmhand.TransferReceipts, entry.Id);
            DB.Set(farmhand);
        }

        private static bool IsReleased(TransferEntry entry)
        {
            var farmhand = DB.Get<Farmhand>(entry.TargetId);
            return farmhand == null || farmhand.DroidAssignmentId != TransferJournalRules.ParsePayload(entry.Payload)[0];
        }

        private static void ApplyRelease(TransferEntry entry)
        {
            if (!IsReleased(entry))
                DB.Delete<Farmhand>(entry.TargetId);
        }

        // Losing a post

        /// <summary>A property was deleted. A house or corral that had a farmhand sends its droid back to its owner, through the overflow store.</summary>
        [NWNEventHandler(ScriptName.OnSwlorDeleteProperty)]
        public static void OnPropertyDeleted()
        {
            var propertyId = EventsPlugin.GetEventData("PROPERTY_ID");
            var farmhand = Get(propertyId);
            if (farmhand == null)
                return;

            ReturnDroidToOverflow(farmhand);
            Corrals.Notify(farmhand.OwnerPlayerId, $"{farmhand.DroidName} lost its post. It is in your overflow storage. Use /overflow to claim it.");
        }

        /// <summary>
        /// Sends the farmhand's droid to its owner's overflow store, with what its hoppers held. The overflow record's id comes from
        /// the assignment, so a return repeated after a restart writes the same record, and the next module load clears a
        /// farmhand whose droid's record is already written.
        /// </summary>
        private static void ReturnDroidToOverflow(Farmhand farmhand)
        {
            if (string.IsNullOrEmpty(farmhand.DroidAssignmentId))
            {
                farmhand.DroidAssignmentId = Guid.NewGuid().ToString();
                DB.Set(farmhand);
            }

            // The receipt is written again before the farmhand lets the droid go, so a controller still marked as being assigned
            // in the owner's character file is removed at their login rather than kept beside the copy.
            SaveReceipt(farmhand);
            DB.Set(new OverflowItem
            {
                Id = OverflowDroidId(farmhand.DroidAssignmentId),
                PlayerId = farmhand.OwnerPlayerId,
                Name = farmhand.DroidName,
                Resref = Droid.DroidControlItemResref,
                Tag = Droid.DroidControlItemResref,
                Quantity = 1,
                Data = farmhand.SerializedDroid,
            });
            Log.Write(LogGroup.Property, $"Droid '{farmhand.DroidName}' from {farmhand.TargetType} {farmhand.TargetId} was returned to player {farmhand.OwnerPlayerId}'s overflow store.");
            Release(farmhand, OBJECT_INVALID);
        }

        private static string OverflowDroidId(string assignmentId) => "farmhand-droid:" + assignmentId;

        /// <summary>Records that the farmhand took the droid of its assignment (<see cref="StationDroidReceipt"/>).</summary>
        private static void SaveReceipt(Farmhand farmhand)
        {
            if (string.IsNullOrEmpty(farmhand.DroidAssignmentId) || string.IsNullOrEmpty(farmhand.OwnerPlayerId))
                return;

            DB.Set(new StationDroidReceipt
            {
                Id = farmhand.DroidAssignmentId,
                OwnerPlayerId = farmhand.OwnerPlayerId,
                BuildingId = farmhand.TargetId,
            });
        }

        /// <summary>Clears any farmhand whose droid a restart left both at its post and in its owner's overflow store.</summary>
        private static void FinishDroidReturns()
        {
            foreach (var farmhand in Search(new DBQuery<Farmhand>()))
            {
                if (string.IsNullOrEmpty(farmhand.DroidAssignmentId) || DB.Get<OverflowItem>(OverflowDroidId(farmhand.DroidAssignmentId)) == null)
                    continue;

                Release(farmhand, OBJECT_INVALID);
                Log.Write(LogGroup.Property, $"Cleared the farmhand of {farmhand.TargetId}, whose droid was already in its owner's overflow store, after a restart.");
            }
        }

        /// <summary>
        /// Settles a droid move a restart interrupted, from the marks on the player's controllers: a controller being assigned
        /// goes if its post saved the droid and stays otherwise; a recalled controller's post lets the droid go if it still holds
        /// it. The receipts are cleared by the station buildings' login settle, which runs for every receipt of the character.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void SettleDroidMoves()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            foreach (var controller in Droid.GetControllers(player))
            {
                if (TryReadMark(controller, AssigningVariable, out var farmhand, out var assignmentId))
                {
                    DeleteLocalString(controller, AssigningVariable);
                    if (StationDroidRules.WasAssigned(HoldsAssignment(farmhand, assignmentId), DB.Get<StationDroidReceipt>(assignmentId) != null))
                    {
                        DestroyObject(controller);
                        Log.Write(LogGroup.Property, $"{PlayerName.GetAuditName(player)} - removed droid controller already taken by a farmhand post (assignment {assignmentId}) after a restart.");
                    }
                }

                if (TryReadMark(controller, RecalledVariable, out farmhand, out assignmentId))
                {
                    DeleteLocalString(controller, RecalledVariable);
                    var holds = HoldsAssignment(farmhand, assignmentId);
                    if (holds)
                    {
                        Release(farmhand, player);
                        Log.Write(LogGroup.Property, $"{PlayerName.GetAuditName(player)} - cleared recalled farmhand at {farmhand.TargetId} after a restart.");
                    }
                    else if (StationDroidRules.RecalledCopyInOverflow(holds, DB.Get<OverflowItem>(OverflowDroidId(assignmentId)) != null))
                    {
                        // A manager recalled the droid, or its post was deleted, before the owner's recall was saved: the copy goes.
                        DB.Delete<OverflowItem>(OverflowDroidId(assignmentId));
                        Log.Write(LogGroup.Property, $"{PlayerName.GetAuditName(player)} - removed the overflow copy of recalled farmhand droid (assignment {assignmentId}) after a restart.");
                    }
                }
            }
        }

        private static bool HoldsAssignment(Farmhand farmhand, string assignmentId) =>
            farmhand != null && !string.IsNullOrEmpty(assignmentId) && farmhand.DroidAssignmentId == assignmentId;

        private static bool TryReadMark(uint controller, string variable, out Farmhand farmhand, out string assignmentId)
        {
            farmhand = null;
            assignmentId = string.Empty;
            var mark = GetLocalString(controller, variable).Split('|');
            if (mark.Length != 2)
                return false;

            farmhand = Get(mark[0]);
            assignmentId = mark[1];
            return true;
        }
    }
}
