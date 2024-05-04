using System;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    internal class ResearchViewModel: GuiViewModelBase<ResearchViewModel, ResearchPayload>
    {
        public const string PartialView = "PARTIAL_VIEW";
        public const string StartStageView = "START_STAGE_VIEW";
        public const string InProgressView = "IN_PROGRESS_VIEW";
        public const string StageCompleteView = "STAGE_COMPLETE_VIEW";

        private string _researchTerminalPropertyId;
        private uint _blueprintItem;
        private RecipeType _recipeType;

        public string RecipeName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string CreditReduction
        {
            get => Get<string>();
            set => Set(value);
        }

        public string EnhancementSlots
        {
            get => Get<string>();
            set => Set(value);
        }

        public string ItemBonuses
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<string> GuaranteedBonuses
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public string Level
        {
            get => Get<string>();
            set => Set(value);
        }

        public string LicensedRuns
        {
            get => Get<string>();
            set => Set(value);
        }

        public string TimeReduction
        {
            get => Get<string>();
            set => Set(value);
        }

        public string CreditCost
        {
            get => Get<string>();
            set => Set(value);
        }

        public string TimeCost
        {
            get => Get<string>();
            set => Set(value);
        }

        public string NextLevelBonus
        {
            get => Get<string>();
            set => Set(value);
        }

        private ResearchJob GetJob()
        {
            var query = new DBQuery<ResearchJob>()
                .AddFieldSearch(nameof(ResearchJob.ParentPropertyId), _researchTerminalPropertyId, false);
            var dbJob = DB.Search(query)
                .FirstOrDefault();

            return dbJob;
        }

        protected override void Initialize(ResearchPayload initialPayload)
        {
            _researchTerminalPropertyId = initialPayload.PropertyId;
            _blueprintItem = initialPayload.BlueprintItem;
            _recipeType = initialPayload.SelectedRecipe;

            var now = DateTime.UtcNow;
            var job = GetJob();

            // New Job
            if (job == null)
            {
                var recipe = Craft.GetRecipe(_recipeType);
                var blueprint = Craft.GetBlueprintDetails(_blueprintItem);
                const int CurrentLevel = 0;
                var licensedRunsMinimum = 1 + Perk.GetPerkLevel(Player, PerkType.ScientificNetworking);
                var licensedRunsMaximum = licensedRunsMinimum + 2;
                var creditCost = Craft.CalculateBlueprintResearchCreditCost(_recipeType, CurrentLevel+1, blueprint.CreditReduction);
                var timeCost = Craft.CalculateBlueprintResearchSeconds(_recipeType, CurrentLevel+1, blueprint.TimeReduction);
                var timeSpan = TimeSpan.FromSeconds(timeCost);
                var timeString = Time.GetTimeShortIntervals(timeSpan, false);

                RecipeName = $"Recipe: {Cache.GetItemNameByResref(recipe.Resref)}";
                Level = $"Level: {CurrentLevel}";
                CreditReduction = $"Credit Reduction: -{blueprint.CreditReduction}%";
                EnhancementSlots = $"Enhancement Slots: {blueprint.EnhancementSlots}";
                LicensedRuns = $"Licensed Runs: {licensedRunsMinimum}-{licensedRunsMaximum}";
                TimeReduction = $"Time Reduction: -{blueprint.TimeReduction}%";
                ItemBonuses = $"Item Bonuses: {blueprint.ItemBonuses}";
                GuaranteedBonuses = Item.BuildItemPropertyList(blueprint.GuaranteedBonuses);
                CreditCost = $"Price: {creditCost}cr";
                TimeCost = $"Time: {timeString}";
                NextLevelBonus = $"Next Level: {GetUpgradeLevelBonus(CurrentLevel + 1)}";

                ChangePartialView(PartialView, StartStageView);
            }
            // In Progress
            else if (now < job.DateCompleted)
            {
                ChangePartialView(PartialView, InProgressView);
            }
            // Complete
            else if (now >= job.DateCompleted)
            {
                ChangePartialView(PartialView, StageCompleteView);
            }
        }

        private string GetUpgradeLevelBonus(int level)
        {
            switch (level)
            {
                case 1:
                    return "Item Bonus +1";
                case 2:
                    return "Licensed Run +1";
                case 3:
                    return "Blueprint Bonus";
                case 4:
                    return "Licensed Run +1";
                case 5:
                    return "Blueprint Bonus";
                case 6:
                    return "Item Bonus +1";
                case 7:
                    return "Guaranteed Item Bonus";
                case 8:
                    return "Blueprint Bonus";
                case 9:
                    return "Item Bonus +1";
                case 10:
                    return "Enhancement Slot +1";

                default:
                    return "MAX";
            }
        }
    }
}
