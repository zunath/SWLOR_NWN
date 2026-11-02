using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.FarmingService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class GardenPlotViewModel : GuiViewModelBase<GardenPlotViewModel, GardenPlotPayload>
    {
        public const string MainContentElement = "gardenplot_content";
        public const string MainContentPartial = "GARDENPLOT_MAIN";

        private string _plotId;
        private readonly List<CropDefinition> _seeds = new();
        private int _selectedSeedIndex = -1;
        private readonly List<int> _fertilizerTiers = new();
        private bool _refreshing;
        private int _cropManagementRank;
        private int _growingCrops;

        public string CropText { get => Get<string>(); set => Set(value); }
        public string PlanterText { get => Get<string>(); set => Set(value); }
        public string StageText { get => Get<string>(); set => Set(value); }
        public string TimeLeftText { get => Get<string>(); set => Set(value); }
        public string MessageText { get => Get<string>(); set => Set(value); }
        public string SeedHeaderText { get => Get<string>(); set => Set(value); }

        public bool CanPlant { get => Get<bool>(); set => Set(value); }
        public bool CanTend { get => Get<bool>(); set => Set(value); }
        public bool CanHarvest { get => Get<bool>(); set => Set(value); }
        public bool CanClear { get => Get<bool>(); set => Set(value); }

        public string PlantTooltip { get => Get<string>(); set => Set(value); }
        public string TendTooltip { get => Get<string>(); set => Set(value); }
        public string HarvestTooltip { get => Get<string>(); set => Set(value); }
        public string ClearTooltip { get => Get<string>(); set => Set(value); }

        public GuiBindingList<string> SeedNames { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<bool> SeedToggles { get => Get<GuiBindingList<bool>>(); set => Set(value); }

        public GuiBindingList<GuiComboEntry> FertilizerOptions { get => Get<GuiBindingList<GuiComboEntry>>(); set => Set(value); }
        public bool CanPickFertilizer { get => Get<bool>(); set => Set(value); }

        /// <summary>The fertilizer picked with the seed: 0 for none, then one entry per tier the player carries.</summary>
        public int SelectedFertilizerIndex
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_refreshing)
                    RefreshPlantButton();
            }
        }

        protected override void Initialize(GardenPlotPayload initialPayload)
        {
            _plotId = initialPayload?.PlotId ?? string.Empty;
            _refreshing = true;

            // Every bound property gets a value before the view is shown.
            CropText = string.Empty;
            PlanterText = string.Empty;
            StageText = string.Empty;
            TimeLeftText = string.Empty;
            MessageText = string.Empty;
            SeedHeaderText = string.Empty;
            CanPlant = false;
            CanTend = false;
            CanHarvest = false;
            CanClear = false;
            PlantTooltip = string.Empty;
            TendTooltip = string.Empty;
            HarvestTooltip = string.Empty;
            ClearTooltip = string.Empty;
            SeedNames = new GuiBindingList<string>();
            SeedToggles = new GuiBindingList<bool>();
            FertilizerOptions = new GuiBindingList<GuiComboEntry>();
            CanPickFertilizer = false;
            SelectedFertilizerIndex = 0;
            WatchOnClient(model => model.SelectedFertilizerIndex);

            Farmhands.CatchUpForPlot(_plotId);
            var withered = Farming.RemoveWitheredCrop(_plotId, DateTime.UtcNow);
            Refresh();
            if (withered != CropType.Invalid)
                MessageText = $"The {Farming.GetCrop(withered)?.Name ?? "crop"} that grew here withered, and its seed is lost.";

            ChangePartialView(MainContentElement, MainContentPartial);
        }

        /// <summary>Reads the plot and the player again and sets what the window shows and which actions are on.</summary>
        private void Refresh()
        {
            _refreshing = true;
            var now = DateTime.UtcNow;
            var structure = Farming.GetPlotStructure(_plotId);
            var record = Farming.GetPlot(_plotId);
            var stage = FarmingRules.GetStage(record, now);
            var crop = record == null ? null : Farming.GetCrop(record.Crop);
            var playerId = GetObjectUUID(Player);
            var isPlanter = record != null && record.PlanterPlayerId == playerId;
            var hasPermission = structure != null && Farming.HasGardenPermission(Player, structure);

            CropText = crop == null ? "Nothing is planted." : $"{crop.Name} (tier {crop.Tier})";
            PlanterText = crop == null ? string.Empty : $"Planted by: {DescribePlanter(record, isPlanter)}";
            StageText = crop == null ? string.Empty : $"Stage: {FarmingRules.DescribeStage(stage)}{(record.Tended ? ", tended" : string.Empty)}";
            TimeLeftText = DescribeTimeLeft(record, stage, now);

            CanTend = SetTooltip(FarmingRules.CheckTend(true, record, now, isPlanter, hasPermission), tip => TendTooltip = tip);
            CanHarvest = SetTooltip(FarmingRules.CheckHarvest(true, record, now, isPlanter), tip => HarvestTooltip = tip);
            CanClear = SetTooltip(FarmingRules.CheckClear(true, record, now, isPlanter), tip => ClearTooltip = tip);

            _cropManagementRank = Perk.GetPlayerEffectivePerkLevel(Player, PerkType.CropManagement);
            _growingCrops = Farming.CountGrowingCrops(GetPCPublicCDKey(Player), playerId, now);
            LoadSeeds(record == null);
            LoadFertilizers(record == null);
            _refreshing = false;
            UpdatePlantButton(structure, record, hasPermission, now);
        }

        private void LoadFertilizers(bool plotIsEmpty)
        {
            _fertilizerTiers.Clear();
            var options = new GuiBindingList<GuiComboEntry> { new("No fertilizer", 0) };
            if (plotIsEmpty)
            {
                foreach (var (tier, count) in Farming.GetFertilizerCarried(Player))
                {
                    _fertilizerTiers.Add(tier);
                    options.Add(new GuiComboEntry(FertilizerRules.DescribeOption(tier, count), tier));
                }
            }

            FertilizerOptions = options;
            SelectedFertilizerIndex = 0;
            CanPickFertilizer = _fertilizerTiers.Count > 0;
        }

        /// <summary>The fertilizer item picked, or empty for none.</summary>
        private string GetSelectedFertilizer()
        {
            var index = SelectedFertilizerIndex - 1;
            return index >= 0 && index < _fertilizerTiers.Count ? FertilizerRules.GetResref(_fertilizerTiers[index]) : string.Empty;
        }

        private void RefreshPlantButton()
        {
            var structure = Farming.GetPlotStructure(_plotId);
            UpdatePlantButton(
                structure,
                Farming.GetPlot(_plotId),
                structure != null && Farming.HasGardenPermission(Player, structure),
                DateTime.UtcNow);
        }

        private void LoadSeeds(bool plotIsEmpty)
        {
            _seeds.Clear();
            var names = new GuiBindingList<string>();
            var toggles = new GuiBindingList<bool>();
            if (plotIsEmpty)
            {
                foreach (var (crop, count) in Farming.GetSeedsCarried(Player))
                {
                    _seeds.Add(crop);
                    names.Add($"{crop.GetSeedLabel()} (tier {crop.Tier}) x{count}");
                    toggles.Add(false);
                }
            }

            _selectedSeedIndex = -1;
            SeedNames = names;
            SeedToggles = toggles;
            SeedHeaderText = !plotIsEmpty
                ? string.Empty
                : _seeds.Count == 0
                    ? "You carry no seeds."
                    : "Pick a seed to plant:";
        }

        private void UpdatePlantButton(WorldProperty structure, GardenPlot record, bool hasPermission, DateTime now)
        {
            if (_selectedSeedIndex < 0 || _selectedSeedIndex >= _seeds.Count)
            {
                CanPlant = false;
                PlantTooltip = record == null ? "Pick a seed first." : FarmingRules.Describe(FarmingRefusal.PlotOccupied);
                return;
            }

            var crop = _seeds[_selectedSeedIndex];
            var fertilizer = GetSelectedFertilizer();
            var refusal = FarmingRules.CheckPlant(
                true,
                structure != null,
                record,
                now,
                hasPermission,
                _cropManagementRank,
                crop.Tier,
                _growingCrops,
                true,
                Farming.GetSiteRefusal(structure, crop),
                WoodlotRules.GetFertilizerRefusal(crop.IsSapling, fertilizer.Length > 0),
                FertilizerRules.GetRefusal(fertilizer, true, crop.Tier));
            CanPlant = refusal == FarmingRefusal.None;
            PlantTooltip = FarmingRules.Describe(refusal);
        }

        private static bool SetTooltip(FarmingRefusal refusal, System.Action<string> setTooltip)
        {
            setTooltip(FarmingRules.Describe(refusal));
            return refusal == FarmingRefusal.None;
        }

        private string DescribePlanter(GardenPlot record, bool isPlanter)
        {
            if (isPlanter)
                return "you";

            var dbPlanter = DB.Get<Player>(record.PlanterPlayerId);
            return PlayerName.GetPlainDisplayNameByPlayerId(Player, record.PlanterPlayerId, dbPlanter?.Name);
        }

        private static string DescribeTimeLeft(GardenPlot record, GardenPlotStage stage, DateTime now)
        {
            return stage switch
            {
                GardenPlotStage.Growing or GardenPlotStage.ReadyToTend =>
                    $"Ripe in: {FarmingRules.FormatDuration(FarmingRules.GetTimeLeft(record, now))}",
                GardenPlotStage.Ripe =>
                    $"Withers in: {FarmingRules.FormatDuration(FarmingRules.GetTimeLeft(record, now))}",
                _ => string.Empty
            };
        }

        private void Show(FarmingResult result)
        {
            Refresh();
            MessageText = result.Message;
        }

        public Action OnClickSeed() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _seeds.Count)
                return;

            _selectedSeedIndex = index;
            for (var seed = 0; seed < SeedToggles.Count; seed++)
                SeedToggles[seed] = seed == index;

            RefreshPlantButton();
        };

        public Action OnClickPlant() => () =>
        {
            if (_selectedSeedIndex < 0 || _selectedSeedIndex >= _seeds.Count)
                return;

            Show(Farming.Plant(Player, _plotId, _seeds[_selectedSeedIndex].SeedResref, GetSelectedFertilizer()));
        };

        public Action OnClickTend() => () => Show(Farming.Tend(Player, _plotId));

        public Action OnClickHarvest() => () => Show(Farming.Harvest(Player, _plotId));

        public Action OnClickClear() => () =>
        {
            var crop = Farming.GetPlot(_plotId) is { } plot ? Farming.GetCrop(plot.Crop) : null;
            ShowModal(
                $"Clear the {crop?.Name ?? "crop"}? The crop and its seed are lost.",
                () => Show(Farming.Clear(Player, _plotId)),
                null,
                "Clear",
                "Keep");
        };
    }
}
