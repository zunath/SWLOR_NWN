using SWLOR.Component.Crafting.Entity;
using SWLOR.Component.Crafting.Enums;
using SWLOR.Component.Crafting.Model;
using SWLOR.Component.Crafting.Service;
using SWLOR.Component.Crafting.UI.Payload;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Core.Bioware;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Core.Service;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Enums;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Crafting.UI.ViewModel
{
    internal class ResearchViewModel: GuiViewModelBase<ResearchViewModel, ResearchPayload>
    {
        private readonly IDatabaseService _db;
        private readonly IItemCacheService _itemCache;
        private readonly IRandomService _random;
        private readonly IPerkService _perkService;
        private readonly IItemService _itemService;
        private readonly ISkillService _skillService;

        public ResearchViewModel(IGuiService guiService, IDatabaseService db, IItemCacheService itemCache, IRandomService random, IPerkService perkService, IItemService itemService, ISkillService skillService) : base(guiService)
        {
            _db = db;
            _itemCache = itemCache;
            _random = random;
            _perkService = perkService;
            _itemService = itemService;
            _skillService = skillService;
        }
        
        private class ResearchJobDetails
        {
            public int Quantity { get; set; }
            public string RecipeName { get; set; }
            public int CurrentLevel { get; set; }
            public int CreditReduction { get; set; }
            public int EnhancementSlots { get; set; }
            public int LicensedRunsMinimum { get; set; }
            public int LicensedRunsMaximum { get; set; }
            public int CurrentLicensedRuns { get; set; }
            public int TimeReduction { get; set; }
            public int ItemBonuses { get; set; }
            public List<ItemProperty> GuaranteedBonuses { get; set; }
            public int CreditCost { get; set; }
            public int TimeCost { get; set; }
            public int RequiredPerkLevel { get; set; }

            public string TimeString
            {
                get
                {
                    var timeSpan = TimeSpan.FromSeconds(TimeCost);
                    return Time.GetTimeShortIntervals(timeSpan, false);
                }
            }
        }

        public const string PartialView = "PARTIAL_VIEW";
        public const string StartStageView = "START_STAGE_VIEW";
        public const string InProgressView = "IN_PROGRESS_VIEW";
        public const string StageCompleteView = "STAGE_COMPLETE_VIEW";

        private string _researchTerminalPropertyId;
        private uint _blueprintItem;
        private RecipeType _recipeType;

        private static readonly BlueprintBonuses _blueprintBonuses = new();

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

        public float JobProgress
        {
            get => Get<float>();
            set => Set(value);
        }

        public string JobProgressTime
        {
            get => Get<string>();
            set => Set(value);
        }

        private ResearchJob GetJob()
        {
            var query = new DBQuery<ResearchJob>()
                .AddFieldSearch(nameof(ResearchJob.ParentPropertyId), _researchTerminalPropertyId, false);
            var dbJob = _db.Search(query)
                .FirstOrDefault();

            return dbJob;
        }

        private ResearchJobDetails BuildResearchJobDetails(RecipeType recipeType, uint blueprintItem)
        {
            var recipe = Craft.GetRecipe(recipeType);
            var blueprint = Craft.GetBlueprintDetails(blueprintItem);
            var currentLevel = blueprint.Level <= 0 ? 0 : blueprint.Level;
            var licensedRunsMinimum = 1 + _perkService.GetPerkLevel(Player, PerkType.ScientificNetworking);
            var licensedRunsMaximum = licensedRunsMinimum + 2;
            var creditCost = Craft.CalculateBlueprintResearchCreditCost(_recipeType, currentLevel + 1, blueprint.CreditReduction);
            var timeCost = Craft.CalculateBlueprintResearchSeconds(_recipeType, currentLevel + 1, blueprint.TimeReduction);
            var perkLevel = recipe.Level / 10 + 1;
            if (perkLevel > 5)
                perkLevel = 5;

            return new ResearchJobDetails
            {
                Quantity = recipe.Quantity,
                RecipeName = _itemCache.GetItemNameByResref(recipe.Resref),
                CurrentLevel = currentLevel,
                CreditReduction = blueprint.CreditReduction,
                EnhancementSlots = blueprint.EnhancementSlots,
                LicensedRunsMinimum = licensedRunsMinimum,
                LicensedRunsMaximum = licensedRunsMaximum,
                CurrentLicensedRuns = blueprint.LicensedRuns,
                TimeReduction = blueprint.TimeReduction,
                ItemBonuses = blueprint.ItemBonuses,
                GuaranteedBonuses = blueprint.GuaranteedBonuses,
                CreditCost = creditCost,
                TimeCost = timeCost,
                RequiredPerkLevel = perkLevel
            };
        }

        protected override void Initialize(ResearchPayload initialPayload)
        {
            _researchTerminalPropertyId = initialPayload.PropertyId;
            _blueprintItem = initialPayload.BlueprintItem;
            _recipeType = initialPayload.SelectedRecipe;

            var now = DateTime.UtcNow;
            var dbJob = GetJob();

            // New Job
            if (dbJob == null)
            {
                ChangePartialView(PartialView, StartStageView);

                var researchJob = BuildResearchJobDetails(_recipeType, _blueprintItem);
                RecipeName = $"Recipe: {researchJob.Quantity}x {researchJob.RecipeName}";
                Level = $"Level: {researchJob.CurrentLevel}";
                CreditReduction = $"Credit Reduction: -{researchJob.CreditReduction}%";
                EnhancementSlots = $"Enhancement Slots: {researchJob.EnhancementSlots}";
                LicensedRuns = researchJob.CurrentLicensedRuns == 0
                    ? $"Licensed Runs: {researchJob.LicensedRunsMinimum}-{researchJob.LicensedRunsMaximum}"
                    : $"Licensed Runs: {researchJob.CurrentLicensedRuns}";
                TimeReduction = $"Time Reduction: -{researchJob.TimeReduction}%";
                ItemBonuses = $"Item Bonuses: {researchJob.ItemBonuses}";
                GuaranteedBonuses = _itemService.BuildItemPropertyList(researchJob.GuaranteedBonuses);
                CreditCost = $"Price: {researchJob.CreditCost}cr";
                TimeCost = $"Time: {researchJob.TimeString}";
                NextLevelBonus = $"Next Level: {GetUpgradeLevelBonus(researchJob.CurrentLevel + 1)}";
            }
            // In Progress
            else if (now < dbJob.DateCompleted)
            {
                ChangePartialView(PartialView, InProgressView);

                var recipe = Craft.GetRecipe(dbJob.Recipe);
                var delta = dbJob.DateCompleted - dbJob.DateStarted;
                var currentDelta = now - dbJob.DateStarted;
                var progressPercentage = (float)currentDelta.Ticks / (float)delta.Ticks;
                var deltaTime = dbJob.DateCompleted - now;
                var timeString = Time.GetTimeShortIntervals(deltaTime, false);

                RecipeName = $"Recipe: {recipe.Quantity}x {_itemCache.GetItemNameByResref(recipe.Resref)}";
                Level = $"Level: {dbJob.Level}";
                JobProgress = progressPercentage > 1f ? 1f : progressPercentage;
                JobProgressTime = $"Remaining: {timeString}";
            }
            // Complete
            else if (now >= dbJob.DateCompleted)
            {
                ChangePartialView(PartialView, StageCompleteView);

                var recipe = Craft.GetRecipe(dbJob.Recipe);

                RecipeName = $"Recipe: {recipe.Quantity}x {_itemCache.GetItemNameByResref(recipe.Resref)}";
                Level = $"Level: {dbJob.Level}";
                JobProgress = 1f;
                JobProgressTime = "COMPLETE";
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

        private string ValidateJob()
        {
            var researchJob = BuildResearchJobDetails(_recipeType, _blueprintItem);

            if (researchJob.CurrentLevel > 0 && !GetIsObjectValid(_blueprintItem))
            {
                return "Cannot locate blueprint.";
            }

            if (researchJob.CurrentLevel > 0 && GetItemPossessor(_blueprintItem) != Player)
            {
                return "Blueprint must be in your inventory.";
            }

            if (GetGold(Player) < researchJob.CreditCost)
            {
                return "Not enough credits!";
            }

            if (_perkService.GetPerkLevel(Player, PerkType.Research) < researchJob.RequiredPerkLevel)
            {
                return $"Research level {researchJob.RequiredPerkLevel} required.";
            }

            if (researchJob.CurrentLevel >= Craft.MaxResearchLevel)
            {
                return $"Blueprint cannot be researched any further.";
            }

            var recipe = Craft.GetRecipe(_recipeType);
            foreach (var req in recipe.Requirements)
            {
                if (req.GetType() != typeof(RecipeUnlockRequirement))
                    continue;

                var error = req.CheckRequirements(Player);
                if (!string.IsNullOrWhiteSpace(error))
                {
                    return error;
                }
            }

            var playerId = GetObjectUUID(Player);
            var maxConcurrentJobs = _perkService.GetPerkLevel(Player, PerkType.ResearchProjects) + 1;
            var dbQuery = new DBQuery<ResearchJob>()
                .AddFieldSearch(nameof(ResearchJob.PlayerId), playerId, false);
            var currentJobs = _db.Search(dbQuery).ToList();
            var currentJobCount = currentJobs.Count(x => x.ParentPropertyId != _researchTerminalPropertyId);

            if (currentJobCount >= maxConcurrentJobs)
            {
                return $"You may only have {maxConcurrentJobs} research job(s) active at one time.";
            }

            return string.Empty;
        }

        public Action ClickStartJob() => () =>
        {
            var researchJob = BuildResearchJobDetails(_recipeType, _blueprintItem);
            ShowModal($"This job will cost {researchJob.CreditCost}cr and take {researchJob.TimeString} to complete. Are you sure you want to start the job?",
                () =>
                {
                    var jobValidation = ValidateJob();
                    if (!string.IsNullOrWhiteSpace(jobValidation))
                    {
                        FloatingTextStringOnCreature(jobValidation, Player, false);
                        ChangePartialView(PartialView, StartStageView);
                        return;
                    }

                    var now = DateTime.UtcNow;
                    var dbResearchJob = new ResearchJob
                    {
                        ParentPropertyId = _researchTerminalPropertyId,
                        PlayerId = GetObjectUUID(Player),
                        DateStarted = now,
                        DateCompleted = now.AddSeconds(researchJob.TimeCost),
                        SerializedItem = GetIsObjectValid(_blueprintItem) ? ObjectPlugin.Serialize(_blueprintItem) : string.Empty,
                        Level = researchJob.CurrentLevel,
                        Recipe = _recipeType
                    };
                    _db.Set(dbResearchJob);

                    AssignCommand(Player, () => TakeGoldFromCreature(researchJob.CreditCost, Player, true));
                    DestroyObject(_blueprintItem);
                    _guiService.TogglePlayerWindow(Player, GuiWindowType.Research);
                }, () =>
                {
                    ChangePartialView(PartialView, StartStageView);
                });
        };

        public Action ClickCancelJob() => () =>
        {
            ShowModal("Canceling the research job will forfeit all credits spent and progress made towards the next level. You will not be refunded! Are you sure you want to cancel this research job?",
                () =>
                {
                    var dbJob = GetJob();

                    if (!string.IsNullOrWhiteSpace(dbJob.SerializedItem))
                    {
                        var item = ObjectPlugin.Deserialize(dbJob.SerializedItem);
                        ObjectPlugin.AcquireItem(Player, item);
                    }

                    _db.Delete<ResearchJob>(dbJob.Id);
                    _guiService.TogglePlayerWindow(Player, GuiWindowType.Research);
                    FloatingTextStringOnCreature("Research job cancelled!", Player, false);
                },
                () =>
                {
                    ChangePartialView(PartialView, InProgressView);
                });
        };

        public Action ClickCompleteJob() => () =>
        {
            var dbJob = GetJob();
            var recipe = Craft.GetRecipe(dbJob.Recipe);

            bool isNewBlueprint;
            uint item;
            if (string.IsNullOrWhiteSpace(dbJob.SerializedItem))
            {
                item = CreateItemOnObject("blueprint", Player);
                SetName(item, $"Blueprint: {_itemCache.GetItemNameByResref(recipe.Resref)}");
                isNewBlueprint = true;
            }
            else
            {
                item = ObjectPlugin.Deserialize(dbJob.SerializedItem);
                ObjectPlugin.AcquireItem(Player, item);
                isNewBlueprint = false;
            }

            var blueprintDetails = Craft.GetBlueprintDetails(item);

            void AddBlueprintBonus()
            {
                var hasEnhancementBonus = blueprintDetails.EnhancementSlots > 0 && 
                                          blueprintDetails.Level < Craft.MaxResearchLevel;

                int[] weights;

                if (hasEnhancementBonus)
                {
                    weights = new[]
                    {
                        310,
                        420,
                        420
                    };
                }
                else
                {
                    weights = new[]
                    {
                        300,
                        400,
                        400
                    };
                }
                
                var index = _random.GetRandomWeightedIndex(weights);

                if (index == 0) // 0 = Licensed Runs
                {
                    blueprintDetails.LicensedRuns += _random.D3(1);
                }
                else if (index == 1) // 1 = Credit Reduction
                {
                    blueprintDetails.CreditReduction += _random.D10(1);
                }
                else if (index == 2) // 2 = Time Reduction
                {
                    blueprintDetails.TimeReduction += _random.D10(1);
                }
            }

            void AddGuaranteedBonus(int tier)
            {
                var bonus = _blueprintBonuses.PickBonus(recipe.EnhancementType, tier, recipe.IsItemIntendedForCrafting);
                ItemProperty ip;

                switch (recipe.EnhancementType)
                {
                    case RecipeEnhancementType.Weapon:
                        ip = ItemPropertyCustom(ItemPropertyType.WeaponEnhancement, (int)bonus.Type, bonus.Amount);
                        BiowareXP2.IPSafeAddItemProperty(item, ip, 0f, AddItemPropertyPolicy.IgnoreExisting, false, false);
                        break;
                    case RecipeEnhancementType.Armor:
                        ip = ItemPropertyCustom(ItemPropertyType.ArmorEnhancement, (int)bonus.Type, bonus.Amount);
                        BiowareXP2.IPSafeAddItemProperty(item, ip, 0f, AddItemPropertyPolicy.IgnoreExisting, false, false);
                        break;
                    case RecipeEnhancementType.Food:
                        ip = ItemPropertyCustom(ItemPropertyType.FoodEnhancement, (int)bonus.Type, bonus.Amount);
                        BiowareXP2.IPSafeAddItemProperty(item, ip, 0f, AddItemPropertyPolicy.IgnoreExisting, false, false);
                        break;
                }
            }

            blueprintDetails.Level++;
            if (blueprintDetails.Level > Craft.MaxResearchLevel)
                blueprintDetails.Level = Craft.MaxResearchLevel;

            switch (blueprintDetails.Level)
            {
                case 1:
                    blueprintDetails.ItemBonuses++;
                    if(_random.Next(1000) <= 1)
                        AddGuaranteedBonus(1);
                    break;
                case 2:
                    blueprintDetails.LicensedRuns++;
                    if (_random.Next(1000) <= 2)
                        AddGuaranteedBonus(1);
                    break;
                case 3:
                    AddBlueprintBonus();
                    if (_random.Next(1000) <= 3)
                        AddGuaranteedBonus(1);
                    break;
                case 4:
                    blueprintDetails.LicensedRuns++;
                    if (_random.Next(1000) <= 10)
                        AddGuaranteedBonus(1);
                    break;
                case 5:
                    AddBlueprintBonus();
                    if (_random.Next(1000) <= 15)
                        AddGuaranteedBonus(1);
                    break;
                case 6:
                    blueprintDetails.ItemBonuses++;
                    if (_random.Next(1000) <= 20)
                        AddGuaranteedBonus(1);
                    break;
                case 7:
                    AddGuaranteedBonus(3);
                    if (_random.Next(1000) <= 50)
                        AddGuaranteedBonus(1);
                    break;
                case 8:
                    AddBlueprintBonus();
                    if (_random.Next(1000) <= 80)
                        AddGuaranteedBonus(1);
                    break;
                case 9:
                    blueprintDetails.ItemBonuses++;
                    if (_random.Next(1000) <= 100)
                        AddGuaranteedBonus(2);
                    break;
                case 10:
                    blueprintDetails.EnhancementSlots++;
                    if (_random.Next(1000) <= 120)
                        AddGuaranteedBonus(3);
                    break;
            }

            if (isNewBlueprint)
            {
                var scientificNetworking = _perkService.GetPerkLevel(Player, PerkType.ScientificNetworking);
                blueprintDetails.LicensedRuns = _random.D3(1) + scientificNetworking;
                blueprintDetails.Recipe = dbJob.Recipe;
            }

            Craft.SetBlueprintDetails(item, blueprintDetails);
            _guiService.TogglePlayerWindow(Player, GuiWindowType.Research);

            _db.Delete<ResearchJob>(dbJob.Id);
        };
    }
}
