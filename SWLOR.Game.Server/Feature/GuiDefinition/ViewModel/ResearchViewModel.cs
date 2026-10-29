using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;
using SWLOR.Game.Server.Service.CraftService.Research;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum.Item;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    internal class ResearchViewModel: GuiViewModelBase<ResearchViewModel, ResearchPayload>
    {
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
            public int RequiredResearchTier { get; set; }

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

        private static BlueprintBonuses _blueprintBonuses = new();

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

        /// <summary>A job with an experiment cannot be cancelled, so the button stays disabled for it.</summary>
        public bool IsCancelEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public GuiBindingList<string> ExperimentLines
        {
            get => Get<GuiBindingList<string>>();
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

        private ResearchJobDetails BuildResearchJobDetails(RecipeType recipeType, uint blueprintItem)
        {
            var recipe = Craft.GetRecipe(recipeType);
            var blueprint = Craft.GetBlueprintDetails(blueprintItem);
            var currentLevel = blueprint.Level <= 0 ? 0 : blueprint.Level;
            var licensedRunsMinimum = 1 + CraftRankMilestones.GetBlueprintLicensedRunBonus(Player);
            var licensedRunsMaximum = licensedRunsMinimum + 2;
            var creditCost = Craft.CalculateBlueprintResearchCreditCost(_recipeType, currentLevel + 1, blueprint.CreditReduction);
            var timeCost = Craft.CalculateBlueprintResearchSeconds(_recipeType, currentLevel + 1, blueprint.TimeReduction);
            var researchTier = recipe.Level / 10 + 1;
            if (researchTier > 5)
                researchTier = 5;

            return new ResearchJobDetails
            {
                Quantity = recipe.Quantity,
                RecipeName = Cache.GetItemNameByResref(recipe.Resref),
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
                RequiredResearchTier = researchTier
            };
        }

        protected override void Initialize(ResearchPayload initialPayload)
        {
            _researchTerminalPropertyId = initialPayload.PropertyId;
            _blueprintItem = initialPayload.BlueprintItem;
            _recipeType = initialPayload.SelectedRecipe;

            var now = DateTime.UtcNow;
            var dbJob = GetJob();
            ExperimentLines = new GuiBindingList<string>();

            // An experiment left open (a crash, or a window closed without being settled) ends with its saved evidence. Only its owner's
            // window settles it: another player looking at the terminal never ends an experiment its owner may still be working.
            var jobChanged = false;
            var owned = ResearchExperimentLifecycle.IsOwnedBy(dbJob, GetObjectUUID(Player));
            if (owned && ResearchExperimentLifecycle.IsOpen(dbJob) && !Gui.IsWindowOpen(Player, GuiWindowType.ResearchExperiment))
            {
                ResearchExperimentLifecycle.Settle(dbJob, ServiceMinigameRandom.Instance);
                jobChanged = true;
            }

            // A settled experiment whose XP was never given (the player logged out or was dead when it settled) pays it now, once.
            // The payout records the XP on the player before it marks the job, so a stop in between never pays it twice; a dead
            // player is paid the next time the window opens.
            if (owned && ResearchExperimentLifecycle.IsExperimentXpOwed(dbJob))
            {
                ResearchExperimentService.GrantExperimentXp(Player, dbJob);
                jobChanged |= dbJob.Experiment.XpGranted;
            }

            if (jobChanged)
                DB.Set(dbJob);

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
                GuaranteedBonuses = Item.BuildItemPropertyList(researchJob.GuaranteedBonuses);
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

                RecipeName = $"Recipe: {recipe.Quantity}x {Cache.GetItemNameByResref(recipe.Resref)}";
                Level = $"Level: {dbJob.Level}";
                JobProgress = progressPercentage > 1f ? 1f : progressPercentage;
                JobProgressTime = $"Remaining: {timeString}";
                IsCancelEnabled = ResearchExperimentLifecycle.CanCancel(dbJob);
                ExperimentLines = BuildExperimentLines(dbJob);
            }
            // Complete
            else if (now >= dbJob.DateCompleted)
            {
                ChangePartialView(PartialView, StageCompleteView);

                var recipe = Craft.GetRecipe(dbJob.Recipe);

                RecipeName = $"Recipe: {recipe.Quantity}x {Cache.GetItemNameByResref(recipe.Resref)}";
                Level = $"Level: {dbJob.Level}";
                JobProgress = 1f;
                JobProgressTime = "COMPLETE";
                ExperimentLines = BuildExperimentLines(dbJob);
            }
        }

        private static GuiBindingList<string> BuildExperimentLines(ResearchJob job)
        {
            var lines = new GuiBindingList<string>();
            foreach (var line in ResearchExperimentLifecycle.Describe(job.Experiment))
                lines.Add(line);

            return lines;
        }

        private string GetUpgradeLevelBonus(int level)
        {
            return ResearchGrantRules.Describe(level);
        }

        private string ValidateJob()
        {
            if (Gui.IsWindowOpen(Player, GuiWindowType.ResearchExperiment))
            {
                return "Finish your current research experiment first.";
            }

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

            if (CraftRankMilestones.GetResearchTier(Player) < researchJob.RequiredResearchTier)
            {
                return $"Fabrication rank {CraftRankMilestones.RequiredFabricationRankForResearchTier(researchJob.RequiredResearchTier)} is required to research this recipe.";
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
            var maxConcurrentJobs = CraftRankMilestones.GetMaxConcurrentResearchJobs(Player);
            var dbQuery = new DBQuery<ResearchJob>()
                .AddFieldSearch(nameof(ResearchJob.PlayerId), playerId, false);
            var currentJobs = DB.Search(dbQuery).ToList();
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
            ShowModal($"This job will cost {researchJob.CreditCost}cr and take {researchJob.TimeString} to complete. A short experiment follows that can shorten the job; closing it early keeps the evidence you gathered. Are you sure you want to start the job?",
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
                    var targetLevel = researchJob.CurrentLevel + 1;
                    var extraBonus = ResearchGrantRules.RollExtraBonus(targetLevel, ServiceMinigameRandom.Instance);
                    var licensedRunsGrant = ResearchGrantRules.RollLicensedRunsGrant(
                        targetLevel, researchJob.EnhancementSlots > 0, ServiceMinigameRandom.Instance);
                    var trials = ExperimentTuning.FromStatTotals(stat => Stat.GetStatAdjustment(Player, stat)).Trials;
                    var dbResearchJob = new ResearchJob
                    {
                        ParentPropertyId = _researchTerminalPropertyId,
                        PlayerId = GetObjectUUID(Player),
                        DateStarted = now,
                        DateCompleted = now.AddSeconds(researchJob.TimeCost),
                        SerializedItem = GetIsObjectValid(_blueprintItem) ? ObjectPlugin.Serialize(_blueprintItem) : string.Empty,
                        Level = researchJob.CurrentLevel,
                        Recipe = _recipeType,
                        Experiment = ResearchExperimentLifecycle.Open(
                            targetLevel, trials, researchJob.TimeCost, extraBonus, licensedRunsGrant)
                    };
                    ResearchExperimentService.StampStart(Player, dbResearchJob);

                    AssignCommand(Player, () => TakeGoldFromCreature(researchJob.CreditCost, Player, true));
                    DestroyObject(_blueprintItem);
                    NwnCraftSessionWorld.Instance.PersistCharacter(Player);
                    Gui.TogglePlayerWindow(Player, GuiWindowType.Research);
                    Gui.TogglePlayerWindow(
                        Player,
                        GuiWindowType.ResearchExperiment,
                        new ResearchExperimentPayload(dbResearchJob.Id),
                        TetherObject);
                }, () =>
                {
                    ChangePartialView(PartialView, StartStageView);
                });
        };

        public Action ClickCancelJob() => () =>
        {
            if (!ResearchExperimentLifecycle.IsOwnedBy(GetJob(), GetObjectUUID(Player)))
            {
                FloatingTextStringOnCreature(ResearchExperimentLifecycle.NotOwnerMessage, Player, false);
                return;
            }

            if (!ResearchExperimentLifecycle.CanCancel(GetJob()))
            {
                FloatingTextStringOnCreature(ResearchExperimentLifecycle.CannotCancelMessage, Player, false);
                return;
            }

            ShowModal("Canceling the research job will forfeit all credits spent and progress made towards the next level. You will not be refunded! Are you sure you want to cancel this research job?",
                () =>
                {
                    var dbJob = GetJob();
                    if (!ResearchExperimentLifecycle.IsOwnedBy(dbJob, GetObjectUUID(Player)))
                    {
                        FloatingTextStringOnCreature(ResearchExperimentLifecycle.NotOwnerMessage, Player, false);
                        return;
                    }

                    if (!string.IsNullOrWhiteSpace(dbJob.SerializedItem) &&
                        !Item.TryAcquireItem(Player, ObjectPlugin.Deserialize(dbJob.SerializedItem)))
                    {
                        FloatingTextStringOnCreature(Item.InventoryFullMessage, Player, false);
                        return;
                    }

                    ResearchExperimentService.FinishDelivery(Player, dbJob);
                    Gui.TogglePlayerWindow(Player, GuiWindowType.Research);
                    FloatingTextStringOnCreature("Research job cancelled!", Player, false);
                },
                () =>
                {
                    ChangePartialView(PartialView, InProgressView);
                });
        };

        /// <summary>
        /// Delivers each completed research result using its saved output variant and recipe identity.
        /// </summary>
        public Action ClickCompleteJob() => () =>
        {
            var dbJob = GetJob();
            if (!ResearchExperimentLifecycle.IsOwnedBy(dbJob, GetObjectUUID(Player)))
            {
                FloatingTextStringOnCreature(ResearchExperimentLifecycle.NotOwnerMessage, Player, false);
                return;
            }

            var recipe = Craft.GetRecipe(dbJob.Recipe);

            if (ResearchExperimentLifecycle.IsOpen(dbJob))
            {
                if (Gui.IsWindowOpen(Player, GuiWindowType.ResearchExperiment))
                {
                    FloatingTextStringOnCreature("Finish your experiment first.", Player, false);
                    return;
                }

                ResearchExperimentLifecycle.Settle(dbJob, ServiceMinigameRandom.Instance);
            }

            bool isNewBlueprint;
            uint item;
            if (string.IsNullOrWhiteSpace(dbJob.SerializedItem))
            {
                var probe = CreateItemOnObject("blueprint", GetObjectByTag("TEMP_ITEM_STORAGE"));
                var fits = Item.FitsInInventory(Player, probe);
                DestroyObject(probe);
                if (!fits)
                {
                    FloatingTextStringOnCreature(Item.InventoryFullMessage, Player, false);
                    return;
                }

                item = CreateItemOnObject("blueprint", Player);
                SetName(item, $"Blueprint: {Cache.GetItemNameByResref(recipe.Resref)}");
                isNewBlueprint = true;
            }
            else
            {
                item = ObjectPlugin.Deserialize(dbJob.SerializedItem);
                if (!Item.TryAcquireItem(Player, item))
                {
                    FloatingTextStringOnCreature(Item.InventoryFullMessage, Player, false);
                    return;
                }

                isNewBlueprint = false;
            }

            var blueprintDetails = Craft.GetBlueprintDetails(item);

            void AddGuaranteedBonus(int tier)
            {
                var bonus = _blueprintBonuses.PickBonus(recipe.EnhancementType, tier, recipe.IsItemIntendedForCrafting);
                ItemProperty ip;

                switch (recipe.EnhancementType)
                {
                    case RecipeEnhancementType.Weapon:
                        ip = ItemPropertyCustom(ItemPropertyType.WeaponEnhancement, (int)bonus.Type, bonus.Amount);
                        BiowareXP2.IPSafeAddItemProperty(item, ip, 0f, AddItemPropertyPolicy.IgnoreExisting, false, false);
                        if (bonus.DamageType != CombatDamageType.Invalid &&
                            !bonus.DamageType.IsPhysicalDamageType())
                        {
                            ip = ItemPropertyCustom(ItemPropertyType.WeaponDamageType, (int)bonus.DamageType);
                            BiowareXP2.IPSafeAddItemProperty(item, ip, 0f, AddItemPropertyPolicy.IgnoreExisting, false, false);
                        }
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

            var completion = ResearchGrantRules.Complete(
                blueprintDetails,
                dbJob.Experiment,
                ServiceMinigameRandom.Instance,
                AddGuaranteedBonus);

            if (isNewBlueprint)
            {
                var licensedRunBonus = CraftRankMilestones.GetBlueprintLicensedRunBonus(Player);
                blueprintDetails.LicensedRuns = Random.D3(1) + licensedRunBonus;
                blueprintDetails.Recipe = dbJob.Recipe;
            }

            Craft.SetBlueprintDetails(item, blueprintDetails);
            Gui.TogglePlayerWindow(Player, GuiWindowType.Research);

            var experimentXp = ResearchExperimentService.GrantExperimentXp(Player, dbJob);
            var completionXp = ResearchExperimentService.GrantCompletionXp(Player, dbJob, completion.Level);
            ResearchExperimentService.FinishDelivery(Player, dbJob);

            SendMessageToPC(Player, DescribeCompletion(completion, experimentXp + completionXp));
        };

        private static string DescribeCompletion(ResearchCompletion completion, int xp)
        {
            var gained = completion.Grant == ResearchLevelGrant.RandomBlueprintBonus
                ? completion.Candidate == ResearchCandidate.LicensedRuns
                    ? $"Licensed Runs +{completion.Amount}"
                    : $"{ResearchGrantRules.CandidateName(completion.Candidate, completion.Level)} +{completion.Amount}%"
                : ResearchGrantRules.Describe(completion.Level);
            var extra = completion.ExtraBonusTier > 0 ? $" An extra guaranteed bonus (tier {completion.ExtraBonusTier}) was added." : string.Empty;
            var xpText = xp > 0 ? $" +{xp} Fabrication XP." : string.Empty;
            return $"Research complete: the blueprint is now level {completion.Level}. {gained}.{extra}{xpText}";
        }
    }
}
