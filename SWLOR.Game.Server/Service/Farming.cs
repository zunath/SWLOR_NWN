using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.FarmingService;
using SWLOR.Game.Server.Service.GuiService;
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
    /// Garden plots (Player City Revamp, "Farming"): planting, tending, harvesting and clearing a crop, the garden permission
    /// that gates planting and tending, the plot's placeable, and what happens to a crop when its plot goes away. The rules
    /// (tiers, the crop cap, timing, yields, XP) are <see cref="FarmingRules"/>; this class reads and writes the records and
    /// the inventory and applies them.
    /// </summary>
    public static class Farming
    {
        /// <summary>The name of a garden plot's placeable with nothing in it.</summary>
        public const string PlotName = "Garden Plot";

        private static readonly Dictionary<CropType, CropDefinition> _crops = new();
        private static readonly Dictionary<string, CropDefinition> _cropsBySeed = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>When the module loads, reads the crop definitions.</summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheCrops()
        {
            _crops.Clear();
            _cropsBySeed.Clear();

            var definitions = typeof(ICropListDefinition).Assembly
                .GetTypes()
                .Where(type => typeof(ICropListDefinition).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);

            foreach (var type in definitions)
            {
                var list = (ICropListDefinition)Activator.CreateInstance(type);
                foreach (var (cropType, crop) in list.BuildCrops())
                {
                    _crops[cropType] = crop;
                    _cropsBySeed[crop.SeedResref] = crop;
                }
            }

            Console.WriteLine($"Loaded {_crops.Count} crops.");
        }

        public static CropDefinition GetCrop(CropType type) => _crops.GetValueOrDefault(type);

        public static IReadOnlyCollection<CropDefinition> GetAllCrops() => _crops.Values;

        public static CropDefinition GetCropBySeed(string seedResref) =>
            string.IsNullOrWhiteSpace(seedResref) ? null : _cropsBySeed.GetValueOrDefault(seedResref);

        // Records

        /// <summary>The crop growing in a plot, or null when it is empty.</summary>
        public static GardenPlot GetPlot(string plotId) =>
            string.IsNullOrWhiteSpace(plotId) ? null : DB.Get<GardenPlot>(GardenPlot.KeyFor(plotId));

        /// <summary>The garden plot structure with the id, or null when it is not a garden plot.</summary>
        public static WorldProperty GetPlotStructure(string plotId)
        {
            var property = string.IsNullOrWhiteSpace(plotId) ? null : DB.Get<WorldProperty>(plotId);
            return property is { PropertyType: PropertyType.Structure, StructureType: StructureType.GardenPlot } ? property : null;
        }

        /// <summary>
        /// Removes the plot's crop if it has withered, and returns what withered (<see cref="CropType.Invalid"/> when nothing did).
        /// A withered crop and its seed are lost, so the plot is empty again. Every action calls this first, and so does
        /// <see cref="ProcessPlotsAtServerStart"/>.
        /// </summary>
        public static CropType RemoveWitheredCrop(string plotId, DateTime now)
        {
            var record = GetPlot(plotId);
            if (record == null || FarmingRules.GetStage(record, now) != GardenPlotStage.Withered)
                return CropType.Invalid;

            DB.Delete<GardenPlot>(record.Id);
            Log.Write(LogGroup.Property, $"Garden plot {plotId}: {record.Crop} planted by {record.PlanterPlayerId} withered.");
            RefreshName(plotId);
            return record.Crop;
        }

        /// <summary>The crops a CD key has growing, across every character on it. Records whose plot is gone are removed.</summary>
        public static int CountGrowingCrops(string cdKey, string playerId, DateTime now)
        {
            var query = string.IsNullOrWhiteSpace(cdKey)
                ? new DBQuery<GardenPlot>().AddFieldSearch(nameof(GardenPlot.PlanterPlayerId), playerId, false)
                : new DBQuery<GardenPlot>().AddFieldSearch(nameof(GardenPlot.PlanterCdKey), cdKey, false);
            var count = checked((int)DB.SearchCount(query));
            if (count <= 0)
                return 0;

            var standing = new List<GardenPlot>();
            foreach (var record in DB.Search(query.AddPaging(count, 0)).ToList())
            {
                if (GetPlotStructure(record.PlotId) == null)
                {
                    DB.Delete<GardenPlot>(record.Id);
                    continue;
                }

                standing.Add(record);
            }

            return FarmingRules.CountGrowingCrops(standing, now);
        }

        // Permission

        /// <summary>
        /// Whether the character can plant in and tend the plots of the property the plot stands in: the house owner's guests the
        /// owner has granted it, or the city's mayor and anyone the mayor has granted it. Staff always can.
        /// </summary>
        public static bool HasGardenPermission(uint player, WorldProperty plot)
        {
            if (GetIsDM(player) || GetIsDMPossessed(player))
                return true;

            if (!GetIsPC(player) || plot == null)
                return false;

            var permission = DB.Search(new DBQuery<WorldPropertyPermission>()
                    .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), plot.ParentPropertyId, false)
                    .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), GetObjectUUID(player), false))
                .FirstOrDefault();

            return permission != null &&
                   permission.Permissions.TryGetValue(PropertyPermissionType.TendGardens, out var allowed) &&
                   allowed;
        }

        // Bonuses that later parts of the revamp supply

        /// <summary>Whether the property holds twice the garden plots and adds 1 to a crop's harvest. The Greenhouse specialization supplies it.</summary>
        public static bool IsGreenhouse(WorldProperty property) => false;

        /// <summary>Whether the plot's city has completed its Home Ground project, which adds 1 to a harvest. The Home Ground project supplies it.</summary>
        public static bool HasHomeGround(WorldProperty plot) => false;

        // Actions

        /// <summary>Plants the seed in the plot, using the seed up. The character needs garden permission and the crop's tier.</summary>
        public static FarmingResult Plant(uint player, string plotId, string seedResref)
        {
            var now = DateTime.UtcNow;
            var isPlayer = IsFarmer(player);
            var structure = GetPlotStructure(plotId);
            var crop = GetCropBySeed(seedResref);
            if (crop == null)
                return FarmingResult.Refused(FarmingRefusal.UnknownCrop);

            RemoveWitheredCrop(plotId, now);
            var record = GetPlot(plotId);
            var playerId = isPlayer ? GetObjectUUID(player) : string.Empty;
            var cdKey = isPlayer ? GetPCPublicCDKey(player) : string.Empty;
            var seed = isPlayer ? FindItem(player, crop.SeedResref) : OBJECT_INVALID;

            var refusal = FarmingRules.CheckPlant(
                isPlayer,
                structure != null,
                record,
                now,
                isPlayer && HasGardenPermission(player, structure),
                isPlayer ? Perk.GetPlayerEffectivePerkLevel(player, PerkType.CropManagement) : 0,
                crop.Tier,
                isPlayer ? CountGrowingCrops(cdKey, playerId, now) : 0,
                GetIsObjectValid(seed));
            if (refusal != FarmingRefusal.None)
                return FarmingResult.Refused(refusal);

            UseUpOne(seed);
            DB.Set(new GardenPlot
            {
                Id = GardenPlot.KeyFor(plotId),
                PlotId = plotId,
                PlanterPlayerId = playerId,
                PlanterCdKey = cdKey,
                Crop = crop.Type,
                PlantedUtc = now,
                TendReadyUtc = FarmingRules.GetTendReadyUtc(now, crop.GrowHours),
                RipeUtc = FarmingRules.GetRipeUtc(now, crop.GrowHours),
                WitherUtc = FarmingRules.GetWitherUtc(FarmingRules.GetRipeUtc(now, crop.GrowHours)),
                LastProcessedUtc = now
            });

            RefreshName(plotId);
            Log.Write(LogGroup.Property, $"{PlayerName.GetAuditName(player)} planted {crop.Type} in garden plot {plotId}.");
            return FarmingResult.Success(FarmingRules.DescribePlanted(crop.Name, crop.GrowHours));
        }

        /// <summary>Tends the crop, once. The planter can, and so can anyone with garden permission, and the tender earns the tending XP.</summary>
        public static FarmingResult Tend(uint player, string plotId)
        {
            var now = DateTime.UtcNow;
            var isPlayer = IsFarmer(player);
            var structure = GetPlotStructure(plotId);
            if (structure == null)
                return FarmingResult.Refused(FarmingRefusal.NoPlot);

            RemoveWitheredCrop(plotId, now);
            var record = GetPlot(plotId);
            var refusal = FarmingRules.CheckTend(
                isPlayer,
                record,
                now,
                isPlayer && record != null && record.PlanterPlayerId == GetObjectUUID(player),
                isPlayer && HasGardenPermission(player, structure));
            if (refusal != FarmingRefusal.None)
                return FarmingResult.Refused(refusal);

            var crop = GetCrop(record.Crop);
            if (crop == null)
                return FarmingResult.Refused(FarmingRefusal.UnknownCrop);

            record.Tended = true;
            DB.Set(record);

            var rank = Skill.GetCreatureSkillRank(player, SkillType.Agriculture);
            Skill.GiveSkillXP(player, SkillType.Agriculture, FarmingRules.GetTendXp(crop.Level, rank), false, false);
            Log.Write(LogGroup.Property, $"{PlayerName.GetAuditName(player)} tended {crop.Type} in garden plot {plotId}.");
            return FarmingResult.Success(FarmingRules.DescribeTended(crop.Name));
        }

        /// <summary>
        /// Harvests a ripe crop for its planter. The yield, the seed roll and the Agriculture XP use the perk and skill ranks the
        /// planter holds now. The items go to their inventory, and any that do not fit go to their overflow storage.
        /// </summary>
        public static FarmingResult Harvest(uint player, string plotId)
        {
            var now = DateTime.UtcNow;
            var isPlayer = IsFarmer(player);
            var structure = GetPlotStructure(plotId);
            if (structure == null)
                return FarmingResult.Refused(FarmingRefusal.NoPlot);

            RemoveWitheredCrop(plotId, now);
            var record = GetPlot(plotId);
            var playerId = isPlayer ? GetObjectUUID(player) : string.Empty;
            var refusal = FarmingRules.CheckHarvest(isPlayer, record, now, isPlayer && record != null && record.PlanterPlayerId == playerId);
            if (refusal != FarmingRefusal.None)
                return FarmingResult.Refused(refusal);

            var crop = GetCrop(record.Crop);
            if (crop == null)
                return FarmingResult.Refused(FarmingRefusal.UnknownCrop);

            var yield = FarmingRules.GetYield(
                record.Tended,
                record.Fertilized,
                Perk.GetPlayerEffectivePerkLevel(player, PerkType.BountifulHarvest),
                IsGreenhouse(DB.Get<WorldProperty>(structure.ParentPropertyId)),
                HasHomeGround(structure));
            var seedPercent = FarmingRules.GetSeedSaverPercent(Perk.GetPlayerEffectivePerkLevel(player, PerkType.SeedSaver));
            var seedReturned = FarmingRules.ReturnsSeed(seedPercent, Random.D100(1));
            var rank = Skill.GetCreatureSkillRank(player, SkillType.Agriculture);

            // The record goes first, so a stop between the two steps can lose a harvest but never repeat it.
            DB.Delete<GardenPlot>(record.Id);
            var itemsInOverflow = GiveItems(player, playerId, crop.HarvestResref, yield);
            var seedInOverflow = seedReturned && GiveItems(player, playerId, crop.SeedResref, 1) > 0;
            Skill.GiveSkillXP(player, SkillType.Agriculture, FarmingRules.GetHarvestXp(crop.Level, rank, yield), false, false);

            RefreshName(plotId);
            Log.Write(LogGroup.Property, $"{PlayerName.GetAuditName(player)} harvested {yield} from {crop.Type} in garden plot {plotId} (tended {record.Tended}, fertilized {record.Fertilized}, seed back {seedReturned}, {itemsInOverflow} to overflow).");
            return FarmingResult.Success(FarmingRules.DescribeHarvest(crop.Name, yield, itemsInOverflow, seedReturned, seedInOverflow));
        }

        /// <summary>Clears the crop, whatever its stage. Its seed is lost. A planter can always clear their own crop.</summary>
        public static FarmingResult Clear(uint player, string plotId)
        {
            var now = DateTime.UtcNow;
            var isPlayer = IsFarmer(player);
            if (GetPlotStructure(plotId) == null)
                return FarmingResult.Refused(FarmingRefusal.NoPlot);

            var record = GetPlot(plotId);
            var refusal = FarmingRules.CheckClear(
                isPlayer,
                record,
                now,
                isPlayer && record != null && record.PlanterPlayerId == GetObjectUUID(player));
            if (refusal != FarmingRefusal.None)
                return FarmingResult.Refused(refusal);

            DB.Delete<GardenPlot>(record.Id);
            RefreshName(plotId);
            Log.Write(LogGroup.Property, $"{PlayerName.GetAuditName(player)} cleared {record.Crop} from garden plot {plotId}.");
            return FarmingResult.Success(FarmingRules.DescribeCleared(GetCrop(record.Crop)?.Name ?? "crop"));
        }

        // The structure

        /// <summary>A garden plot is placed or moved, or the property it stands in loads: make it usable and name it for its crop.</summary>
        public static void OnPlotSpawned(WorldProperty plot, uint placeable)
        {
            SetUseableFlag(placeable, true);
            SetEventScript(placeable, EventScript.Placeable_OnUsed, ScriptName.OnGardenPlotUsed);
            RefreshName(placeable, GetPlot(plot.Id));
        }

        /// <summary>
        /// A garden plot is picked up. A crop in it is lost, and its seed goes back to the planter: into their inventory when they
        /// are on and it fits, otherwise into their overflow storage. A withered crop's seed is already lost.
        /// </summary>
        public static void OnPlotRetrieved(WorldProperty plot, uint placeable)
        {
            var record = GetPlot(plot.Id);
            if (record == null)
                return;

            DB.Delete<GardenPlot>(record.Id);
            var crop = GetCrop(record.Crop);
            if (crop == null || FarmingRules.GetStage(record, DateTime.UtcNow) == GardenPlotStage.Withered)
                return;

            var planter = GetObjectByUUID(record.PlanterPlayerId);
            var planterIsOn = GetIsObjectValid(planter) && GetIsPC(planter);
            var inOverflow = GiveItems(planterIsOn ? planter : OBJECT_INVALID, record.PlanterPlayerId, crop.SeedResref, 1, placeable) > 0;
            Log.Write(LogGroup.Property, $"Garden plot {plot.Id} was picked up with {record.Crop} planted by {record.PlanterPlayerId}; the seed went to {(inOverflow ? "overflow storage" : "their inventory")}.");

            if (planterIsOn)
            {
                SendMessageToPC(planter, inOverflow
                    ? $"The {crop.Name} you planted was lost when its garden plot was picked up. Its seed is in your overflow storage; use /overflow to claim it."
                    : $"The {crop.Name} you planted was lost when its garden plot was picked up. Its seed came back to you.");
            }
        }

        /// <summary>A property is deleted: a garden plot's crop goes with it, as with every structure of a lost city.</summary>
        [NWNEventHandler(ScriptName.OnSwlorDeleteProperty)]
        public static void OnPropertyDeleted()
        {
            var propertyId = EventsPlugin.GetEventData("PROPERTY_ID");
            if (string.IsNullOrWhiteSpace(propertyId) || !DB.Exists<GardenPlot>(GardenPlot.KeyFor(propertyId)))
                return;

            DB.Delete<GardenPlot>(GardenPlot.KeyFor(propertyId));
            Log.Write(LogGroup.Property, $"Garden plot {propertyId} was deleted with its crop.");
        }

        /// <summary>A garden plot was used: open its window.</summary>
        [NWNEventHandler(ScriptName.OnGardenPlotUsed)]
        public static void UsePlot()
        {
            var player = GetLastUsedBy();
            if (!IsFarmer(player))
                return;

            var plot = OBJECT_SELF;
            var plotId = Property.GetPropertyId(plot);
            if (GetPlotStructure(plotId) == null)
                return;

            Gui.TogglePlayerWindow(player, GuiWindowType.GardenPlot, new GardenPlotPayload(plotId), plot);
        }

        /// <summary>
        /// At server start, removes the crops that withered while the server was down and the records of plots that no longer
        /// stand, so a withered crop does not hold a plot or count against its planter's crops.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void ProcessPlotsAtServerStart()
        {
            var count = checked((int)DB.SearchCount(new DBQuery<GardenPlot>()));
            if (count <= 0)
                return;

            var now = DateTime.UtcNow;
            var withered = 0;
            var orphaned = 0;
            foreach (var record in DB.Search(new DBQuery<GardenPlot>().AddPaging(count, 0)).ToList())
            {
                if (GetPlotStructure(record.PlotId) == null)
                {
                    DB.Delete<GardenPlot>(record.Id);
                    orphaned++;
                }
                else if (FarmingRules.GetStage(record, now) == GardenPlotStage.Withered)
                {
                    DB.Delete<GardenPlot>(record.Id);
                    withered++;
                }
            }

            Log.Write(LogGroup.Property, $"Garden plots processed at server start: {withered} withered crops and {orphaned} records of missing plots removed.");
        }

        // The window

        /// <summary>The seeds the player carries, one entry per crop, with how many.</summary>
        public static List<(CropDefinition Crop, int Count)> GetSeedsCarried(uint player)
        {
            var counts = new Dictionary<CropType, int>();
            for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item); item = GetNextItemInInventory(player))
            {
                var crop = GetCropBySeed(GetResRef(item));
                if (crop == null)
                    continue;

                counts[crop.Type] = counts.GetValueOrDefault(crop.Type) + GetItemStackSize(item);
            }

            return counts
                .Select(pair => (Crop: GetCrop(pair.Key), Count: pair.Value))
                .OrderBy(entry => entry.Crop.Tier)
                .ThenBy(entry => entry.Crop.Name)
                .ToList();
        }

        // Helpers

        private static bool IsFarmer(uint player) =>
            GetIsObjectValid(player) && GetIsPC(player) && !GetIsDM(player) && !GetIsDMPossessed(player);

        private static uint FindItem(uint player, string resref)
        {
            for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item); item = GetNextItemInInventory(player))
            {
                if (GetResRef(item) == resref)
                    return item;
            }

            return OBJECT_INVALID;
        }

        private static void UseUpOne(uint item)
        {
            var size = GetItemStackSize(item);
            if (size > 1)
                SetItemStackSize(item, size - 1);
            else
                DestroyObject(item);
        }

        private static void RefreshName(string plotId)
        {
            var placeable = Property.GetPlaceableByPropertyId(plotId);
            if (GetIsObjectValid(placeable))
                RefreshName(placeable, GetPlot(plotId));
        }

        private static void RefreshName(uint placeable, GardenPlot record)
        {
            var crop = record == null ? null : GetCrop(record.Crop);
            SetName(placeable, crop == null ? PlotName : $"{PlotName} ({crop.Name})");
        }

        /// <summary>
        /// Gives a player <paramref name="count"/> of an item: into their inventory while there is room, and the rest into their
        /// overflow storage, which they claim with /overflow. Returns how many went to overflow storage.
        /// </summary>
        /// <param name="player">The player, or invalid when they are off: everything then goes to overflow storage.</param>
        /// <param name="where">The object whose place an item is created at before it is saved to overflow storage. The player when omitted.</param>
        private static int GiveItems(uint player, string playerId, string resref, int count, uint where = OBJECT_INVALID)
        {
            var overflow = 0;
            for (var unit = 0; unit < count; unit++)
            {
                if (GetIsObjectValid(player) && TryPutInInventory(player, resref))
                    continue;

                if (StoreInOverflow(playerId, resref, GetLocation(GetIsObjectValid(where) ? where : player)))
                    overflow++;
            }

            return overflow;
        }

        /// <summary>
        /// Creates one item in the player's inventory. Returns false, leaving nothing behind, when it did not arrive there: the
        /// inventory count decides, since an item that merged into a stack can come back as an invalid handle, and a created item
        /// the engine left on the ground is destroyed.
        /// </summary>
        private static bool TryPutInInventory(uint player, string resref)
        {
            var before = NwnQuickCraftWorld.CountComponent(player, resref);
            var item = CreateItemOnObject(resref, player);
            if (NwnQuickCraftWorld.CountComponent(player, resref) > before)
                return true;

            if (GetIsObjectValid(item))
                DestroyObject(item);
            return false;
        }

        private static bool StoreInOverflow(string playerId, string resref, Location location)
        {
            var item = CreateObject(ObjectType.Item, resref, location);
            if (!GetIsObjectValid(item))
            {
                Log.Write(LogGroup.Property, $"Garden plot item '{resref}' for player {playerId} could not be created, so it is lost.");
                return false;
            }

            DB.Set(new OverflowItem
            {
                PlayerId = playerId,
                Name = GetName(item),
                Resref = resref,
                Tag = GetTag(item),
                Quantity = 1,
                Data = ObjectPlugin.Serialize(item)
            });
            DestroyObject(item);
            return true;
        }
    }
}
