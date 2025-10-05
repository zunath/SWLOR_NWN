using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Component.Crafting.Model;
using SWLOR.NWN.API.Engine;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Core.Bioware;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Component.Crafting.UI.Payload;
using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Domain.Crafting.Contracts;
using SWLOR.Shared.Domain.Crafting.Enums;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Perk.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Repositories;
using SWLOR.Shared.Domain.Skill.Contracts;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Crafting.UI.ViewModel
{
    internal class ResearchViewModel: GuiViewModelBase<ResearchViewModel, ResearchPayload>
    {
        private readonly IDatabaseService _db;
        private readonly IServiceProvider _serviceProvider;
        private readonly IResearchJobRepository _researchJobRepository;

        public ResearchViewModel(
            IGuiService guiService, 
            IDatabaseService db, 
            IServiceProvider serviceProvider,
            IResearchJobRepository researchJobRepository) : base(guiService)
        {
            _db = db;
            _serviceProvider = serviceProvider;
            _researchJobRepository = researchJobRepository;
            
            // Initialize lazy services
            _itemCache = new Lazy<IItemCacheService>(() => _serviceProvider.GetRequiredService<IItemCacheService>());
            _random = new Lazy<IRandomService>(() => _serviceProvider.GetRequiredService<IRandomService>());
            _perkService = new Lazy<IPerkService>(() => _serviceProvider.GetRequiredService<IPerkService>());
            _itemService = new Lazy<IItemService>(() => _serviceProvider.GetRequiredService<IItemService>());
            _skillService = new Lazy<ISkillService>(() => _serviceProvider.GetRequiredService<ISkillService>());
            _craftService = new Lazy<ICraftService>(() => _serviceProvider.GetRequiredService<ICraftService>());
            _timeService = new Lazy<ITimeService>(() => _serviceProvider.GetRequiredService<ITimeService>());
            _objectPlugin = new Lazy<IObjectPluginService>(() => _serviceProvider.GetRequiredService<IObjectPluginService>());

            _blueprintBonuses = new BlueprintBonuses(Random);
        }

        // Lazy-loaded services to break circular dependencies
        private readonly Lazy<IItemCacheService> _itemCache;
        private readonly Lazy<IRandomService> _random;
        private readonly Lazy<IPerkService> _perkService;
        private readonly Lazy<IItemService> _itemService;
        private readonly Lazy<ISkillService> _skillService;
        private readonly Lazy<ICraftService> _craftService;
        private readonly Lazy<ITimeService> _timeService;
        private readonly Lazy<IObjectPluginService> _objectPlugin;
        
        private IItemCacheService ItemCache => _itemCache.Value;
        private IRandomService Random => _random.Value;
        private IPerkService PerkService => _perkService.Value;
        private IItemService ItemService => _itemService.Value;
        private ISkillService SkillService => _skillService.Value;
        private ICraftService CraftService => _craftService.Value;
        private ITimeService TimeService => _timeService.Value;
        private IObjectPluginService ObjectPlugin => _objectPlugin.Value;
        
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

            private readonly ITimeService TimeService;

            public string TimeString
            {
                get
                {
                    var timeSpan = TimeSpan.FromSeconds(TimeCost);
                    return TimeService.GetTimeShortIntervals(timeSpan, false);
                }
            }

            public ResearchJobDetails(ITimeService timeService)
            {
                TimeService = timeService;
            }
        }

        public const string PartialView = "PARTIAL_VIEW";
        public const string StartStageView = "START_STAGE_VIEW";
        public const string InProgressView = "IN_PROGRESS_VIEW";
        public const string StageCompleteView = "STAGE_COMPLETE_VIEW";

        private string _researchTerminalPropertyId;
        private uint _blueprintItem;
        private RecipeType _recipeType;

        private readonly BlueprintBonuses _blueprintBonuses;

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
            var dbJob = _researchJobRepository.GetByParentPropertyId(_researchTerminalPropertyId)
                .FirstOrDefault();

            return dbJob;
        }

        private ResearchJobDetails BuildResearchJobDetails(RecipeType recipeType, uint blueprintItem)
        {
            var recipe = CraftService.GetRecipe(recipeType);
            var blueprint = CraftService.GetBlueprintDetails(blueprintItem);
            var currentLevel = blueprint.Level <= 0 ? 0 : blueprint.Level;
            var licensedRunsMinimum = 1 + PerkService.GetPerkLevel(Player, PerkType.ScientificNetworking);
            var licensedRunsMaximum = licensedRunsMinimum + 2;
            var creditCost = CraftService.CalculateBlueprintResearchCreditCost(_recipeType, currentLevel + 1, blueprint.CreditReduction);
            var timeCost = CraftService.CalculateBlueprintResearchSeconds(_recipeType, currentLevel + 1, blueprint.TimeReduction);
            var perkLevel = recipe.Level / 10 + 1;
            if (perkLevel > 5)
                perkLevel = 5;

            return new ResearchJobDetails(TimeService)
            {
                Quantity = recipe.Quantity,
                RecipeName = ItemCache.GetItemNameByResref(recipe.Resref),
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
                GuaranteedBonuses = (GuiBindingList<string>)ItemService.BuildItemPropertyList(researchJob.GuaranteedBonuses);
                CreditCost = $"Price: {researchJob.CreditCost}cr";
                TimeCost = $"Time: {researchJob.TimeString}";
                NextLevelBonus = $"Next Level: {GetUpgradeLevelBonus(researchJob.CurrentLevel + 1)}";
            }
            // In Progress
            else if (now < dbJob.DateCompleted)
            {
                ChangePartialView(PartialView, InProgressView);

                var recipe = CraftService.GetRecipe(dbJob.Recipe);
                var delta = dbJob.DateCompleted - dbJob.DateStarted;
                var currentDelta = now - dbJob.DateStarted;
                var progressPercentage = (float)currentDelta.Ticks / (float)delta.Ticks;
                var deltaTime = dbJob.DateCompleted - now;
                var timeString = TimeService.GetTimeShortIntervals(deltaTime, false);

                RecipeName = $"Recipe: {recipe.Quantity}x {ItemCache.GetItemNameByResref(recipe.Resref)}";
                Level = $"Level: {dbJob.Level}";
                JobProgress = progressPercentage > 1f ? 1f : progressPercentage;
                JobProgressTime = $"Remaining: {timeString}";
            }
            // Complete
            else if (now >= dbJob.DateCompleted)
            {
                ChangePartialView(PartialView, StageCompleteView);

                var recipe = CraftService.GetRecipe(dbJob.Recipe);

                RecipeName = $"Recipe: {recipe.Quantity}x {ItemCache.GetItemNameByResref(recipe.Resref)}";
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

            if (PerkService.GetPerkLevel(Player, PerkType.Research) < researchJob.RequiredPerkLevel)
            {
                return $"Research level {researchJob.RequiredPerkLevel} required.";
            }

            if (researchJob.CurrentLevel >= CraftService.MaxResearchLevel)
            {
                return $"Blueprint cannot be researched any further.";
            }

            var recipe = CraftService.GetRecipe(_recipeType);
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
            var maxConcurrentJobs = PerkService.GetPerkLevel(Player, PerkType.ResearchProjects) + 1;
            var currentJobs = _researchJobRepository.GetByPlayerId(playerId).ToList();
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
            var recipe = CraftService.GetRecipe(dbJob.Recipe);

            bool isNewBlueprint;
            uint item;
            if (string.IsNullOrWhiteSpace(dbJob.SerializedItem))
            {
                item = CreateItemOnObject("blueprint", Player);
                SetName(item, $"Blueprint: {ItemCache.GetItemNameByResref(recipe.Resref)}");
                isNewBlueprint = true;
            }
            else
            {
                item = ObjectPlugin.Deserialize(dbJob.SerializedItem);
                ObjectPlugin.AcquireItem(Player, item);
                isNewBlueprint = false;
            }

            var blueprintDetails = CraftService.GetBlueprintDetails(item);

            void AddBlueprintBonus()
            {
                var hasEnhancementBonus = blueprintDetails.EnhancementSlots > 0 && 
                                          blueprintDetails.Level < CraftService.MaxResearchLevel;

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
                
                var index = Random.GetRandomWeightedIndex(weights);

                if (index == 0) // 0 = Licensed Runs
                {
                    blueprintDetails.LicensedRuns += Random.D3(1);
                }
                else if (index == 1) // 1 = Credit Reduction
                {
                    blueprintDetails.CreditReduction += Random.D10(1);
                }
                else if (index == 2) // 2 = Time Reduction
                {
                    blueprintDetails.TimeReduction += Random.D10(1);
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
            if (blueprintDetails.Level > CraftService.MaxResearchLevel)
                blueprintDetails.Level = CraftService.MaxResearchLevel;

            switch (blueprintDetails.Level)
            {
                case 1:
                    blueprintDetails.ItemBonuses++;
                    if(Random.Next(1000) <= 1)
                        AddGuaranteedBonus(1);
                    break;
                case 2:
                    blueprintDetails.LicensedRuns++;
                    if (Random.Next(1000) <= 2)
                        AddGuaranteedBonus(1);
                    break;
                case 3:
                    AddBlueprintBonus();
                    if (Random.Next(1000) <= 3)
                        AddGuaranteedBonus(1);
                    break;
                case 4:
                    blueprintDetails.LicensedRuns++;
                    if (Random.Next(1000) <= 10)
                        AddGuaranteedBonus(1);
                    break;
                case 5:
                    AddBlueprintBonus();
                    if (Random.Next(1000) <= 15)
                        AddGuaranteedBonus(1);
                    break;
                case 6:
                    blueprintDetails.ItemBonuses++;
                    if (Random.Next(1000) <= 20)
                        AddGuaranteedBonus(1);
                    break;
                case 7:
                    AddGuaranteedBonus(3);
                    if (Random.Next(1000) <= 50)
                        AddGuaranteedBonus(1);
                    break;
                case 8:
                    AddBlueprintBonus();
                    if (Random.Next(1000) <= 80)
                        AddGuaranteedBonus(1);
                    break;
                case 9:
                    blueprintDetails.ItemBonuses++;
                    if (Random.Next(1000) <= 100)
                        AddGuaranteedBonus(2);
                    break;
                case 10:
                    blueprintDetails.EnhancementSlots++;
                    if (Random.Next(1000) <= 120)
                        AddGuaranteedBonus(3);
                    break;
            }

            if (isNewBlueprint)
            {
                var scientificNetworking = PerkService.GetPerkLevel(Player, PerkType.ScientificNetworking);
                blueprintDetails.LicensedRuns = Random.D3(1) + scientificNetworking;
                blueprintDetails.Recipe = dbJob.Recipe;
            }

            CraftService.SetBlueprintDetails(item, blueprintDetails);
            _guiService.TogglePlayerWindow(Player, GuiWindowType.Research);

            _db.Delete<ResearchJob>(dbJob.Id);
        };
    }
}
