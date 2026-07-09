using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.FarmingService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class PlanterViewModel: GuiViewModelBase<PlanterViewModel, PlanterPayload>
    {
        public const string PartialElement = "PARTIAL_VIEW";
        public const string NoCropPartial = "NO_CROP_PARTIAL";
        public const string GrowingPartial = "GROWING_PARTIAL";
        public const string HarvestPartial = "HARVEST_PARTIAL";

        private string _propertyId;
        private StructureType _structureType;
        private List<CropType> _plantableCrops = new();

        public string MaxConcurrentCropsText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<GuiComboEntry> CropOptions
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public int SelectedCropIndex
        {
            get => Get<int>();
            set
            {
                Set(value);
                RefreshSelectedCropDetails();
            }
        }

        public string SelectedCropDescription
        {
            get => Get<string>();
            set => Set(value);
        }

        public string SelectedCropYields
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsPlantEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string GrowingCropName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string StageLabel
        {
            get => Get<string>();
            set => Set(value);
        }

        public float GrowthProgress
        {
            get => Get<float>();
            set => Set(value);
        }

        public string TimeRemainingText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string GrowingTendBonusText
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsTendEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string FertilizerStatusText
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsFertilizeEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string HarvestCropName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string HarvestTendBonusText
        {
            get => Get<string>();
            set => Set(value);
        }

        protected override void Initialize(PlanterPayload initialPayload)
        {
            _propertyId = initialPayload.PropertyId;

            var dbProperty = DB.Get<WorldProperty>(_propertyId);
            _structureType = dbProperty?.StructureType ?? StructureType.Invalid;

            CropOptions = new GuiBindingList<GuiComboEntry>();
            SwitchViews();

            WatchOnClient(model => model.SelectedCropIndex);
        }

        private FarmingJob GetJob()
        {
            var dbQuery = new DBQuery<FarmingJob>()
                .AddFieldSearch(nameof(FarmingJob.ParentPropertyId), _propertyId, false);

            return DB.Search(dbQuery).FirstOrDefault();
        }

        private uint FindItemByResref(string resref)
        {
            for (var item = GetFirstItemInInventory(Player); GetIsObjectValid(item); item = GetNextItemInInventory(Player))
            {
                if (GetResRef(item) == resref)
                    return item;
            }

            return OBJECT_INVALID;
        }

        private static void ConsumeOneItem(uint item)
        {
            var stackSize = GetItemStackSize(item);
            if (stackSize > 1)
                SetItemStackSize(item, stackSize - 1);
            else
                DestroyObject(item);
        }

        private int GetPlayerAgricultureRank()
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);

            return dbPlayer.Skills[SkillType.Agriculture].Rank;
        }

        private void RefreshMaxConcurrentCropsText()
        {
            var playerId = GetObjectUUID(Player);
            var rank = GetPlayerAgricultureRank();
            var current = Farming.GetActiveCropCount(playerId);
            var max = Farming.GetMaxConcurrentCrops(rank);

            MaxConcurrentCropsText = $"Max concurrent crops: {current} / {max}";
        }

        private void LoadPlantableCrops()
        {
            var seen = new HashSet<CropType>();

            for (var item = GetFirstItemInInventory(Player); GetIsObjectValid(item); item = GetNextItemInInventory(Player))
            {
                var cropType = Farming.GetCropTypeBySeedResref(GetResRef(item));
                if (cropType == CropType.Invalid)
                    continue;

                seen.Add(cropType);
            }

            _plantableCrops = seen
                .OrderBy(x => Farming.GetCropDetail(x).RequiredRank)
                .ToList();

            var options = new GuiBindingList<GuiComboEntry>();

            for (var index = 0; index < _plantableCrops.Count; index++)
            {
                var detail = Farming.GetCropDetail(_plantableCrops[index]);
                var totalSeconds = detail.SecondsPerStage * Farming.NumberOfStages;
                var growTime = Time.GetTimeShortIntervals(TimeSpan.FromSeconds(totalSeconds), false);

                options.Add(new GuiComboEntry($"{detail.Name} (Rank {detail.RequiredRank}) - {growTime}", index));
            }

            CropOptions = options;
        }

        private void RefreshSelectedCropDetails()
        {
            if (SelectedCropIndex < 0 || SelectedCropIndex >= _plantableCrops.Count)
            {
                SelectedCropDescription = string.Empty;
                SelectedCropYields = string.Empty;
                IsPlantEnabled = false;
                return;
            }

            var detail = Farming.GetCropDetail(_plantableCrops[SelectedCropIndex]);

            SelectedCropDescription = detail.Description;
            SelectedCropYields = detail.Yields.Count <= 0
                ? "No yields."
                : string.Join(", ", detail.Yields.Select(y => $"{y.Value}x {Cache.GetItemNameByResref(y.Key)}"));

            IsPlantEnabled = true;
        }

        private void SwitchViews()
        {
            var job = GetJob();

            if (job == null)
            {
                RefreshMaxConcurrentCropsText();
                LoadPlantableCrops();
                SelectedCropIndex = -1;

                ChangePartialView(PartialElement, NoCropPartial);
                return;
            }

            var now = DateTime.UtcNow;
            var stage = Farming.GetCurrentStage(job.DatePlanted, job.StageDurationSeconds, now);
            var detail = Farming.GetCropDetail(job.CropType);

            if (stage >= Farming.NumberOfStages)
            {
                HarvestCropName = $"Crop: {detail.Name}";
                HarvestTendBonusText = $"Tend Bonus: +{job.TendBonusPercent}%";

                ChangePartialView(PartialElement, HarvestPartial);
            }
            else
            {
                GrowingCropName = $"Crop: {detail.Name}";
                StageLabel = $"Stage {stage + 1} of {Farming.NumberOfStages}";

                var totalDuration = (double)job.StageDurationSeconds * Farming.NumberOfStages;
                var elapsed = (now - job.DatePlanted).TotalSeconds;
                var progress = totalDuration <= 0 ? 1f : (float)(elapsed / totalDuration);
                GrowthProgress = progress > 1f ? 1f : (progress < 0f ? 0f : progress);

                var stageEndTime = job.DatePlanted.AddSeconds((double)job.StageDurationSeconds * (stage + 1));
                var remaining = stageEndTime - now;
                if (remaining < TimeSpan.Zero)
                    remaining = TimeSpan.Zero;

                TimeRemainingText = $"Time Remaining: {Time.GetTimeShortIntervals(remaining, false)}";
                GrowingTendBonusText = $"Tend Bonus: +{job.TendBonusPercent}%, Pristine Bonus: +{job.PristineChanceBonusPercent}%";
                IsTendEnabled = stage + 1 > job.LastTendedStage;

                var canFertilize = stage + 1 > job.LastFertilizedStage;
                IsFertilizeEnabled = canFertilize;
                FertilizerStatusText = canFertilize
                    ? "Fertilizer: available this stage"
                    : "Fertilizer: already applied this stage";

                ChangePartialView(PartialElement, GrowingPartial);
            }
        }

        public Action OnClickPlant() => () =>
        {
            if (SelectedCropIndex < 0 || SelectedCropIndex >= _plantableCrops.Count)
                return;

            var cropType = _plantableCrops[SelectedCropIndex];
            var detail = Farming.GetCropDetail(cropType);

            ShowModal($"Plant {detail.Name}? This will consume one seed.", () =>
            {
                var playerId = GetObjectUUID(Player);
                var rank = GetPlayerAgricultureRank();

                if (GetJob() != null)
                {
                    SendMessageToPC(Player, "A crop is already growing in this planter.");
                    SwitchViews();
                    return;
                }

                if (rank < detail.RequiredRank)
                {
                    SendMessageToPC(Player, $"You must be Agriculture rank {detail.RequiredRank} to plant this crop.");
                    SwitchViews();
                    return;
                }

                var activeCount = Farming.GetActiveCropCount(playerId);
                var maxCrops = Farming.GetMaxConcurrentCrops(rank);

                if (activeCount >= maxCrops)
                {
                    SendMessageToPC(Player, $"You may only have {maxCrops} crop(s) growing at one time.");
                    SwitchViews();
                    return;
                }

                var seedItem = FindItemByResref(detail.SeedResref);

                if (!GetIsObjectValid(seedItem))
                {
                    SendMessageToPC(Player, "You no longer have a seed for that crop.");
                    SwitchViews();
                    return;
                }

                ConsumeOneItem(seedItem);

                var growthSpeedBonus = Stat.GetStatAdjustment(Player, StatType.CropGrowthSpeedPercentBonus) +
                                        Farming.GetStructureGrowthSpeedPercentBonus(_structureType);
                var stageDurationSeconds = Farming.CalculateStageDurationSeconds(detail.SecondsPerStage, growthSpeedBonus);

                var job = new FarmingJob
                {
                    ParentPropertyId = _propertyId,
                    PlayerId = playerId,
                    CropType = cropType,
                    DatePlanted = DateTime.UtcNow,
                    StageDurationSeconds = stageDurationSeconds,
                    LastTendedStage = 0,
                    TendBonusPercent = 0
                };

                DB.Set(job);

                FloatingTextStringOnCreature($"You plant {detail.Name}.", Player, false);
                SwitchViews();
            }, SwitchViews);
        };

        public Action OnClickTend() => () =>
        {
            var job = GetJob();
            if (job == null || job.PlayerId != GetObjectUUID(Player))
                return;

            var now = DateTime.UtcNow;
            var stage = Farming.GetCurrentStage(job.DatePlanted, job.StageDurationSeconds, now);

            if (stage >= Farming.NumberOfStages)
            {
                SwitchViews();
                return;
            }

            if (stage + 1 <= job.LastTendedStage)
                return;

            var nutrientItem = FindItemByResref(Farming.NutrientSolutionResref);
            if (!GetIsObjectValid(nutrientItem))
            {
                SendMessageToPC(Player, "Tending a crop requires a Nutrient Solution.");
                return;
            }

            ConsumeOneItem(nutrientItem);

            job.LastTendedStage = stage + 1;
            job.TendBonusPercent += Farming.TendYieldBonusPercentPerStage;
            DB.Set(job);

            var rank = GetPlayerAgricultureRank();
            var detail = Farming.GetCropDetail(job.CropType);
            var xp = Skill.GetDeltaXP(detail.Level - rank) / 4;
            if (xp < 1)
                xp = 1;

            Skill.GiveSkillXP(Player, SkillType.Agriculture, xp, false, false);

            SwitchViews();
        };

        private void ApplyFertilizer(string fertilizerResref, string fertilizerName, Action<FarmingJob, DateTime> applyEffect)
        {
            var job = GetJob();
            if (job == null || job.PlayerId != GetObjectUUID(Player))
                return;

            var now = DateTime.UtcNow;
            var stage = Farming.GetCurrentStage(job.DatePlanted, job.StageDurationSeconds, now);

            if (stage >= Farming.NumberOfStages)
            {
                SwitchViews();
                return;
            }

            if (stage + 1 <= job.LastFertilizedStage)
            {
                SendMessageToPC(Player, "This crop has already been fertilized this stage.");
                return;
            }

            var fertilizerItem = FindItemByResref(fertilizerResref);
            if (!GetIsObjectValid(fertilizerItem))
            {
                SendMessageToPC(Player, $"You do not have any {fertilizerName}.");
                return;
            }

            ConsumeOneItem(fertilizerItem);

            job.LastFertilizedStage = stage + 1;
            applyEffect(job, now);
            DB.Set(job);

            FloatingTextStringOnCreature($"You apply {fertilizerName}.", Player, false);
            SwitchViews();
        }

        public Action OnClickGrowthFertilizer() => () =>
        {
            ApplyFertilizer(Farming.GrowthFertilizerResref, "Growth Accelerant", (job, now) =>
            {
                job.DatePlanted = Farming.CalculateAcceleratedPlantDate(
                    job.DatePlanted,
                    job.StageDurationSeconds,
                    now,
                    Farming.GrowthFertilizerRemainingTimePercent);
            });
        };

        public Action OnClickYieldFertilizer() => () =>
        {
            ApplyFertilizer(Farming.YieldFertilizerResref, "Yield Compost", (job, _) =>
            {
                job.TendBonusPercent += Farming.YieldFertilizerBonusPercent;
            });
        };

        public Action OnClickQualityFertilizer() => () =>
        {
            ApplyFertilizer(Farming.QualityFertilizerResref, "Quality Nutrient", (job, _) =>
            {
                job.PristineChanceBonusPercent += Farming.QualityFertilizerPristinePercent;
            });
        };

        public Action OnClickClearCrop() => () =>
        {
            ShowModal("This will destroy the crop with no refund.", () =>
            {
                var job = GetJob();
                if (job != null && job.PlayerId == GetObjectUUID(Player))
                    DB.Delete<FarmingJob>(job.Id);

                SwitchViews();
            }, SwitchViews);
        };

        public Action OnClickHarvest() => () =>
        {
            var job = GetJob();
            if (job == null || job.PlayerId != GetObjectUUID(Player))
                return;

            var detail = Farming.GetCropDetail(job.CropType);
            var craftsmanship = Stat.CalculateCraftsmanship(Player, SkillType.Agriculture);
            var yieldBonus = job.TendBonusPercent + Stat.GetStatAdjustment(Player, StatType.HarvestYieldPercentBonus);

            foreach (var (resref, baseQuantity) in detail.Yields)
            {
                var quantity = Farming.CalculateYieldQuantity(baseQuantity, yieldBonus, craftsmanship);
                CreateItemOnObject(resref, Player, quantity);
            }

            if (!string.IsNullOrWhiteSpace(detail.PristineResref))
            {
                var pristineChance = Farming.CalculatePristineChancePercent(
                    Stat.CalculateControl(Player, SkillType.Agriculture),
                    job.PristineChanceBonusPercent,
                    Stat.GetStatAdjustment(Player, StatType.PristineHarvestChancePercentBonus));

                if (SWLOR.Game.Server.Service.Random.D100(1) <= pristineChance)
                {
                    CreateItemOnObject(detail.PristineResref, Player);
                    FloatingTextStringOnCreature("You harvest pristine produce!", Player, false);
                }
            }

            var rank = GetPlayerAgricultureRank();
            var xp = Skill.GetDeltaXP(detail.Level - rank);
            if (xp < 1)
                xp = 1;

            Skill.GiveSkillXP(Player, SkillType.Agriculture, xp, false, false);

            DB.Delete<FarmingJob>(job.Id);

            FloatingTextStringOnCreature($"You harvest {detail.Name}.", Player, false);
            SwitchViews();
        };
    }
}
