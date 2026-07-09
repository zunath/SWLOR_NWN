using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.FarmingService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Service
{
    public static class Farming
    {
        /// <summary>
        /// The number of growth stages every crop passes through before it can be harvested.
        /// </summary>
        public const int NumberOfStages = 3;

        /// <summary>
        /// Yield bonus percent granted for each growth stage the player tends.
        /// </summary>
        public const int TendYieldBonusPercentPerStage = 15;

        /// <summary>
        /// Item consumed when tending a crop.
        /// </summary>
        public const string NutrientSolutionResref = "nutrient_sol";

        /// <summary>
        /// Fertilizer which shortens the crop's remaining growth time.
        /// </summary>
        public const string GrowthFertilizerResref = "fert_growth";

        /// <summary>
        /// Fertilizer which increases harvest yield.
        /// </summary>
        public const string YieldFertilizerResref = "fert_yield";

        /// <summary>
        /// Fertilizer which increases the pristine harvest chance.
        /// </summary>
        public const string QualityFertilizerResref = "fert_quality";

        /// <summary>
        /// Percent of remaining growth time removed by a growth fertilizer application.
        /// </summary>
        public const int GrowthFertilizerRemainingTimePercent = 15;

        /// <summary>
        /// Yield bonus percent granted by a yield fertilizer application.
        /// </summary>
        public const int YieldFertilizerBonusPercent = 15;

        /// <summary>
        /// Pristine chance percent granted by a quality fertilizer application.
        /// </summary>
        public const int QualityFertilizerPristinePercent = 5;

        private const int BaseConcurrentCropLimit = 2;
        private const int RanksPerAdditionalCrop = 10;
        private const int CraftsmanshipPointsPerYieldPercent = 4;
        private const int HydroponicRackGrowthSpeedPercentBonus = 15;
        private const int BasePristineChancePercent = 5;
        private const int ControlPointsPerPristinePercent = 8;

        /// <summary>
        /// Local variable holding a stable identifier for planters placed directly in
        /// public areas rather than through the property system.
        /// </summary>
        public const string StaticPlanterIdVariable = "STATIC_PLANTER_ID";

        private static readonly Dictionary<CropType, CropDetail> _crops = new();
        private static readonly Dictionary<CropType, CropDetail> _activeCrops = new();
        private static readonly Dictionary<string, CropType> _cropTypesBySeedResref = new();

        /// <summary>
        /// When the skill cache has finished loading, crop data is cached.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSwlorSkillCache)]
        public static void CacheData()
        {
            CacheCrops();
        }

        private static void CacheCrops()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(ICropListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var instance = (ICropListDefinition)Activator.CreateInstance(type);
                var crops = instance.BuildCrops();

                foreach (var (cropType, crop) in crops)
                {
                    if (_crops.ContainsKey(cropType))
                    {
                        Log.Write(LogGroup.Error, $"ERROR: Duplicate crop detected: {cropType}", true);
                        continue;
                    }

                    _crops[cropType] = crop;

                    if (!crop.IsActive)
                        continue;

                    if (_cropTypesBySeedResref.ContainsKey(crop.SeedResref))
                    {
                        Log.Write(LogGroup.Error, $"ERROR: Duplicate crop seed resref detected: {crop.SeedResref}", true);
                        continue;
                    }

                    _activeCrops[cropType] = crop;
                    _cropTypesBySeedResref[crop.SeedResref] = cropType;
                }
            }

            Console.WriteLine($"Loaded {_crops.Count} crop types.");
        }

        /// <summary>
        /// Retrieves the detail for a given crop type.
        /// </summary>
        public static CropDetail GetCropDetail(CropType cropType)
        {
            return _crops[cropType];
        }

        /// <summary>
        /// Retrieves the crop type associated with a seed item resref.
        /// Returns CropType.Invalid if the resref is not a seed.
        /// </summary>
        public static CropType GetCropTypeBySeedResref(string resref)
        {
            return _cropTypesBySeedResref.TryGetValue(resref, out var cropType)
                ? cropType
                : CropType.Invalid;
        }

        /// <summary>
        /// Retrieves all active crops.
        /// </summary>
        public static Dictionary<CropType, CropDetail> GetActiveCrops()
        {
            return _activeCrops.ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Determines the maximum number of crops a player may grow at once, based on Agriculture rank.
        /// </summary>
        public static int GetMaxConcurrentCrops(int agricultureRank)
        {
            return BaseConcurrentCropLimit + agricultureRank / RanksPerAdditionalCrop;
        }

        /// <summary>
        /// Counts the number of crops a player is currently growing.
        /// </summary>
        public static int GetActiveCropCount(string playerId)
        {
            var query = new DBQuery<FarmingJob>()
                .AddFieldSearch(nameof(FarmingJob.PlayerId), playerId, false);
            return (int)DB.SearchCount(query);
        }

        /// <summary>
        /// Determines the growth speed bonus granted by the planter structure itself.
        /// </summary>
        public static int GetStructureGrowthSpeedPercentBonus(StructureType structureType)
        {
            return structureType == StructureType.HydroponicRack
                ? HydroponicRackGrowthSpeedPercentBonus
                : 0;
        }

        /// <summary>
        /// Calculates the duration of a single growth stage after growth speed bonuses are applied.
        /// </summary>
        public static int CalculateStageDurationSeconds(int baseSecondsPerStage, int growthSpeedPercentBonus)
        {
            if (growthSpeedPercentBonus < 0)
                growthSpeedPercentBonus = 0;

            return baseSecondsPerStage * 100 / (100 + growthSpeedPercentBonus);
        }

        /// <summary>
        /// Determines the current growth stage of a crop. Stages are 0-based while growing;
        /// a result equal to NumberOfStages means the crop is fully grown and ready to harvest.
        /// </summary>
        public static int GetCurrentStage(DateTime datePlanted, int stageDurationSeconds, DateTime now)
        {
            if (stageDurationSeconds <= 0)
                return NumberOfStages;

            var elapsedSeconds = (now - datePlanted).TotalSeconds;
            if (elapsedSeconds < 0)
                elapsedSeconds = 0;

            var stage = (int)(elapsedSeconds / stageDurationSeconds);
            return stage > NumberOfStages
                ? NumberOfStages
                : stage;
        }

        /// <summary>
        /// Calculates the quantity of a produce item awarded at harvest. Always yields at least one.
        /// </summary>
        public static int CalculateYieldQuantity(int baseQuantity, int tendBonusPercent, int craftsmanship)
        {
            if (tendBonusPercent < 0)
                tendBonusPercent = 0;
            if (craftsmanship < 0)
                craftsmanship = 0;

            var craftsmanshipBonusPercent = craftsmanship / CraftsmanshipPointsPerYieldPercent;
            var quantity = baseQuantity * (100 + tendBonusPercent + craftsmanshipBonusPercent) / 100;

            return quantity < 1
                ? 1
                : quantity;
        }

        /// <summary>
        /// Calculates the percent chance of receiving pristine produce at harvest.
        /// </summary>
        public static int CalculatePristineChancePercent(int control, int jobPristineBonusPercent, int statPristineBonusPercent)
        {
            if (control < 0)
                control = 0;
            if (jobPristineBonusPercent < 0)
                jobPristineBonusPercent = 0;
            if (statPristineBonusPercent < 0)
                statPristineBonusPercent = 0;

            return BasePristineChancePercent +
                   control / ControlPointsPerPristinePercent +
                   jobPristineBonusPercent +
                   statPristineBonusPercent;
        }

        /// <summary>
        /// Calculates the adjusted planting date after a growth fertilizer removes a percentage
        /// of the crop's remaining growth time. Shifting the planting date earlier preserves
        /// stage boundaries under the lazy growth model.
        /// </summary>
        public static DateTime CalculateAcceleratedPlantDate(DateTime datePlanted, int stageDurationSeconds, DateTime now, int remainingTimePercentReduction)
        {
            var totalSeconds = (double)stageDurationSeconds * NumberOfStages;
            var elapsedSeconds = (now - datePlanted).TotalSeconds;

            if (elapsedSeconds < 0)
                elapsedSeconds = 0;

            var remainingSeconds = totalSeconds - elapsedSeconds;
            if (remainingSeconds <= 0)
                return datePlanted;

            var reductionSeconds = remainingSeconds * remainingTimePercentReduction / 100d;
            return datePlanted.AddSeconds(-reductionSeconds);
        }

        /// <summary>
        /// Resolves the stable identifier used to key farming jobs for a planter placeable.
        /// Property-placed planters use their property id; planters placed directly in public
        /// areas use the STATIC_PLANTER_ID local variable instead.
        /// </summary>
        public static string GetPlanterId(uint planter)
        {
            var propertyId = Property.GetPropertyId(planter);

            return string.IsNullOrWhiteSpace(propertyId)
                ? GetLocalString(planter, StaticPlanterIdVariable)
                : propertyId;
        }

        /// <summary>
        /// When a planter structure is used, open the planter window for the player,
        /// unless another player's crop is growing in it.
        /// </summary>
        [NWNEventHandler(ScriptName.OnPlanterUsed)]
        public static void UsePlanter()
        {
            var player = GetLastUsedBy();
            var playerId = GetObjectUUID(player);
            var planter = OBJECT_SELF;

            var planterPropertyId = GetPlanterId(planter);

            if (string.IsNullOrWhiteSpace(planterPropertyId))
            {
                SendMessageToPC(player, "This planter cannot be used.");
                return;
            }

            var dbQuery = new DBQuery<FarmingJob>()
                .AddFieldSearch(nameof(FarmingJob.ParentPropertyId), planterPropertyId, false);
            var farmingJob = DB.Search(dbQuery).FirstOrDefault();

            if (farmingJob != null && farmingJob.PlayerId != playerId)
            {
                var now = DateTime.UtcNow;
                var stage = GetCurrentStage(farmingJob.DatePlanted, farmingJob.StageDurationSeconds, now);

                if (stage >= NumberOfStages)
                {
                    SendMessageToPC(player, "Another player's crop is growing here. It is ready for harvest.");
                }
                else
                {
                    var dateCompleted = farmingJob.DatePlanted.AddSeconds((double)farmingJob.StageDurationSeconds * NumberOfStages);
                    var delta = dateCompleted - now;
                    var completionTime = Time.GetTimeLongIntervals(delta, false);
                    SendMessageToPC(player, $"Another player's crop is growing here. It will be ready for harvest in: {completionTime}.");
                }

                return;
            }

            var payload = new PlanterPayload(planterPropertyId, farmingJob?.Id ?? string.Empty);
            Gui.TogglePlayerWindow(player, GuiWindowType.Planter, payload, player);
        }

        /// <summary>
        /// When a property is deleted, remove any farming job associated with it.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSwlorDeleteProperty)]
        public static void OnRemoveProperty()
        {
            var propertyId = EventsPlugin.GetEventData("PROPERTY_ID");
            var dbQuery = new DBQuery<FarmingJob>()
                .AddFieldSearch(nameof(FarmingJob.ParentPropertyId), propertyId, false);
            var dbJobs = DB.Search(dbQuery).ToList();

            foreach (var dbJob in dbJobs)
            {
                DB.Delete<FarmingJob>(dbJob.Id);
            }
        }
    }
}
