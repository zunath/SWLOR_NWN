using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class IncubatorViewModel : GuiViewModelBase<IncubatorViewModel, IncubatorPayload>,
        IGuiRefreshable<PerkAcquiredRefreshEvent>,
        IGuiRefreshable<PerkRefundedRefreshEvent>
    {
        public const string PartialElement = "PARTIAL_VIEW";
        public const string NewJobPartial = "NEW_JOB_PARTIAL";
        public const string InProgressJobPartial = "IN_PROGRESS_JOB_PARTIAL";
        public const string StageCompleteJobPartial = "STAGE_COMPLETE_PARTIAL";
        public const string CompleteJobPartial = "COMPLETE_JOB_PARTIAL";

        private const int BaseSecondsBetweenStages = 129600; // 129600 = 36 hours
        private const int NumberOfStages = IncubationProjection.NumberOfStages;
        private const int MaxStageIncrease = 100; // 10.0%
        private const int MaxStat = 1000; // 100.0%

        private const string _blank = "Blank";
        private const string HeldElsewhereMessage = "Your inventory is full. Items you left in another incubator are still there; make room first.";

        private static readonly GuiColor DoneColor = new(110, 200, 110);
        private static readonly GuiColor NextColor = new(120, 180, 250);
        private static readonly GuiColor RunningColor = new(230, 180, 70);
        private static readonly GuiColor PendingColor = GuiColor.Grey;
        private static readonly GuiColor MetColor = new(110, 200, 110);
        private static readonly GuiColor UnmetColor = new(225, 100, 100);
        private static readonly GuiColor HintColor = new(230, 180, 70);
        private static readonly GuiColor PenaltyColor = new(225, 100, 100);

        private string _dnaItem;
        private string _hydrolaseItem;
        private string _isomeraseItem;
        private string _lyaseItem;

        private EnzymeColorType _lyaseColor;
        private EnzymeColorType _isomeraseColor;
        private EnzymeColorType _hydrolaseColor;

        private BeastType _dnaType;
        private string _incubatorPropertyId;
        private bool _showResistances;

        private readonly Dictionary<IncubationStatType, int> _baseStats = new();
        private readonly Dictionary<IncubationStatType, int> _stageStats = new();

        public string DNAItemResref { get => Get<string>(); set => Set(value); }
        public string HydrolaseItemResref { get => Get<string>(); set => Set(value); }
        public string IsomeraseItemResref { get => Get<string>(); set => Set(value); }
        public string LyaseItemResref { get => Get<string>(); set => Set(value); }

        public bool IsDNASlotEnabled { get => Get<bool>(); set => Set(value); }
        public string DNASlotTooltip { get => Get<string>(); set => Set(value); }
        public string BeastName { get => Get<string>(); set => Set(value); }
        public string BeastSubtitle { get => Get<string>(); set => Set(value); }
        public string ServerDayText { get => Get<string>(); set => Set(value); }

        public string Stage1Title { get => Get<string>(); set => Set(value); }
        public string Stage2Title { get => Get<string>(); set => Set(value); }
        public string Stage3Title { get => Get<string>(); set => Set(value); }
        public string Stage1Detail { get => Get<string>(); set => Set(value); }
        public string Stage2Detail { get => Get<string>(); set => Set(value); }
        public string Stage3Detail { get => Get<string>(); set => Set(value); }
        public GuiColor Stage1Color { get => Get<GuiColor>(); set => Set(value); }
        public GuiColor Stage2Color { get => Get<GuiColor>(); set => Set(value); }
        public GuiColor Stage3Color { get => Get<GuiColor>(); set => Set(value); }
        public string EnzymeTotalsText { get => Get<string>(); set => Set(value); }

        public string EnzymeSectionTitle { get => Get<string>(); set => Set(value); }
        public string HydrolaseDetail { get => Get<string>(); set => Set(value); }
        public string LyaseDetail { get => Get<string>(); set => Set(value); }
        public string IsomeraseDetail { get => Get<string>(); set => Set(value); }

        public string PurityHeader { get => Get<string>(); set => Set(value); }
        public string ResistanceToggleText { get => Get<string>(); set => Set(value); }
        public GuiBindingList<string> PurityNames { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<GuiColor> PurityNameColors { get => Get<GuiBindingList<GuiColor>>(); set => Set(value); }
        public GuiBindingList<string> PurityTooltips { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<float> PurityValues { get => Get<GuiBindingList<float>>(); set => Set(value); }
        public GuiBindingList<string> PurityTexts { get => Get<GuiBindingList<string>>(); set => Set(value); }

        public string MutationChanceText { get => Get<string>(); set => Set(value); }
        public GuiColor MutationChanceColor { get => Get<GuiColor>(); set => Set(value); }
        public bool IsMutationAllowed { get => Get<bool>(); set => Set(value); }
        public bool IsErraticGeniusEnabled { get => Get<bool>(); set => Set(value); }
        public bool IsErraticGeniusChecked { get => Get<bool>(); set => Set(value); }
        public string ErraticGeniusTooltip { get => Get<string>(); set => Set(value); }
        public string RecipeHeader { get => Get<string>(); set => Set(value); }
        public GuiBindingList<string> RecipeLines { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<GuiColor> RecipeLineColors { get => Get<GuiBindingList<GuiColor>>(); set => Set(value); }
        public string UnknownRecipesText { get => Get<string>(); set => Set(value); }

        public string OutcomeHeader { get => Get<string>(); set => Set(value); }
        public GuiBindingList<string> OutcomeNames { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> OutcomeChances { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<GuiColor> OutcomeColors { get => Get<GuiBindingList<GuiColor>>(); set => Set(value); }

        public bool IsStartJobEnabled { get => Get<bool>(); set => Set(value); }
        public bool IsContinueJobEnabled { get => Get<bool>(); set => Set(value); }
        public bool IsCompleteJobEnabled { get => Get<bool>(); set => Set(value); }
        public string ContinueButtonText { get => Get<string>(); set => Set(value); }
        public string EstimatedTimeToCompletion { get => Get<string>(); set => Set(value); }
        public float JobProgress { get => Get<float>(); set => Set(value); }
        public string JobProgressTime { get => Get<string>(); set => Set(value); }

        private void SwitchViews()
        {
            var dbJob = GetJob();
            var now = DateTime.UtcNow;
            IsCompleteJobEnabled = IsJobComplete(dbJob, now);
            if (dbJob == null)
            {
                ChangePartialView(PartialElement, NewJobPartial);
            }
            else
            {
                var delta = dbJob.DateCompleted - dbJob.DateStarted;
                var currentDelta = now - dbJob.DateStarted;
                var progressPercentage = delta.Ticks <= 0 ? 1f : (float)currentDelta.Ticks / delta.Ticks;
                JobProgress = progressPercentage > 1f ? 1f : progressPercentage;

                _dnaType = dbJob.BeastDNAType;
                LoadJobStats(dbJob);

                if (now >= dbJob.DateCompleted)
                {
                    JobProgressTime = $"Stage {dbJob.CurrentStage} complete";
                    ChangePartialView(PartialElement, dbJob.CurrentStage >= NumberOfStages
                        ? CompleteJobPartial
                        : StageCompleteJobPartial);
                }
                else
                {
                    var deltaTime = dbJob.DateCompleted - now;
                    JobProgressTime = $"Stage {dbJob.CurrentStage} - {Time.GetTimeShortIntervals(deltaTime, false)} left";
                    ChangePartialView(PartialElement, InProgressJobPartial);
                }
            }

            RefreshAllStats();
        }

        protected override void Initialize(IncubatorPayload initialPayload)
        {
            // Held items belong to the incubator they were placed in, so they never join another incubator's job.
            if (_incubatorPropertyId != initialPayload.PropertyId && !ReturnItemsHeldElsewhere())
            {
                FloatingTextStringOnCreature(HeldElsewhereMessage, Player, false);
                // Closed without the close handling, which would act on the other incubator's items.
                Gui.CloseWindow(Player, GuiWindowType.Incubator, Player);
                return;
            }

            _incubatorPropertyId = initialPayload.PropertyId;
            _showResistances = false;

            DNAItemResref = _blank;
            HydrolaseItemResref = _blank;
            IsomeraseItemResref = _blank;
            LyaseItemResref = _blank;
            IsErraticGeniusChecked = false;
            JobProgress = 0f;
            JobProgressTime = string.Empty;

            var job = GetJob();
            IsMutationAllowed = job == null || !job.IsMutationSuppressed;

            ClearStats();
            RestoreHeldItems();
            LoadPlayerStats();
            SwitchViews();

            WatchOnClient(model => model.IsErraticGeniusChecked);
            WatchOnClient(model => model.IsMutationAllowed);
        }

        /// <summary>A job can be completed once its last stage has run its full time.</summary>
        private static bool IsJobComplete(IncubationJob job, DateTime now)
        {
            return job != null &&
                   job.CurrentStage >= NumberOfStages &&
                   now >= job.DateCompleted;
        }

        private IncubationJob GetJob()
        {
            var dbQuery = new DBQuery<IncubationJob>()
                .AddFieldSearch(nameof(IncubationJob.ParentPropertyId), _incubatorPropertyId, false);
            return DB.Search(dbQuery).FirstOrDefault();
        }

        private int GetErraticGeniusBonus()
        {
            return Perk.GetPerkLevel(Player, PerkType.ErraticGenius) switch
            {
                1 => 2,
                2 => 4,
                3 => 8,
                _ => 0
            };
        }

        private void LoadPlayerStats()
        {
            var mutationBonus = GetErraticGeniusBonus();
            var job = GetJob();
            IsErraticGeniusEnabled = mutationBonus > 0 && job == null;
            ErraticGeniusTooltip = mutationBonus <= 0
                ? "Requires the Erratic Genius perk."
                : job == null
                    ? $"Adds {mutationBonus}% mutation chance when this job starts."
                    : "Erratic Genius only applies when a job starts.";

            if (!IsErraticGeniusEnabled)
                IsErraticGeniusChecked = false;

            RefreshIncubationTime();
        }

        private void RefreshButtons(IncubationJob job)
        {
            var enzymesReady = !string.IsNullOrWhiteSpace(_hydrolaseItem) &&
                               !string.IsNullOrWhiteSpace(_lyaseItem) &&
                               !string.IsNullOrWhiteSpace(_isomeraseItem);

            IsStartJobEnabled = job == null && enzymesReady && !string.IsNullOrWhiteSpace(_dnaItem);
            IsContinueJobEnabled = job != null && enzymesReady;
            ContinueButtonText = job == null ? "Start stage 1" : $"Start stage {job.CurrentStage + 1}";
        }

        private void RefreshAllStats()
        {
            var job = GetJob();
            var now = DateTime.UtcNow;
            var hasPendingStage = job == null || (now >= job.DateCompleted && job.CurrentStage < NumberOfStages);

            RefreshButtons(job);
            RefreshHeader(job, now);
            RefreshStageTracker(job, now);
            RefreshEnzymeTotals(job);
            RefreshPurities(hasPendingStage);
            RefreshMutation(job, hasPendingStage);
            RefreshRecipes(job, now, hasPendingStage);
            RefreshOutcomes(job, now);
        }

        private void RefreshHeader(IncubationJob job, DateTime now)
        {
            ServerDayText = $"Server day: {now.DayOfWeek} (UTC)";
            IsDNASlotEnabled = job == null;
            DNASlotTooltip = job == null
                ? (string.IsNullOrWhiteSpace(_dnaItem) ? "Add DNA" : "Remove DNA")
                : "The DNA is locked in this job.";
            if (job != null)
                DNAItemResref = string.IsNullOrWhiteSpace(job.DNAIconResref) ? _blank : job.DNAIconResref;

            if (_dnaType == BeastType.Invalid)
            {
                BeastName = "No DNA added";
                BeastSubtitle = "Click the slot and pick a DNA sample from your inventory.";
                return;
            }

            var beast = BeastMastery.GetBeastDetail(_dnaType);
            BeastName = $"{beast.Name} DNA";
            BeastSubtitle = $"{beast.Role} role";
        }

        private void RefreshStageTracker(IncubationJob job, DateTime now)
        {
            var titles = new string[NumberOfStages];
            var details = new string[NumberOfStages];
            var colors = new GuiColor[NumberOfStages];
            var stageTime = Time.GetTimeShortIntervals(TimeSpan.FromSeconds(CalculateIncubationSeconds()), false);

            for (var index = 0; index < NumberOfStages; index++)
            {
                var stage = index + 1;
                var started = job?.CurrentStage ?? 0;
                var finished = job != null && now >= job.DateCompleted;

                if (stage < started || (stage == started && finished))
                {
                    titles[index] = $"Stage {stage} - done";
                    details[index] = "Enzymes added";
                    colors[index] = DoneColor;
                }
                else if (stage == started)
                {
                    titles[index] = $"Stage {stage} - running";
                    details[index] = $"{Time.GetTimeShortIntervals(job.DateCompleted - now, false)} left";
                    colors[index] = RunningColor;
                }
                else if (stage == started + 1 && (job == null || finished))
                {
                    titles[index] = $"Stage {stage} - next";
                    details[index] = $"Add enzymes ({stageTime})";
                    colors[index] = NextColor;
                }
                else
                {
                    titles[index] = $"Stage {stage}";
                    details[index] = "Waiting";
                    colors[index] = PendingColor;
                }
            }

            Stage1Title = titles[0];
            Stage2Title = titles[1];
            Stage3Title = titles[2];
            Stage1Detail = details[0];
            Stage2Detail = details[1];
            Stage3Detail = details[2];
            Stage1Color = colors[0];
            Stage2Color = colors[1];
            Stage3Color = colors[2];

            var nextStage = (job?.CurrentStage ?? 0) + 1;
            EnzymeSectionTitle = nextStage <= NumberOfStages
                ? $"Enzymes for stage {nextStage}"
                : "Enzymes";
        }

        private void RefreshEnzymeTotals(IncubationJob job)
        {
            if (job == null)
            {
                EnzymeTotalsText = "Enzymes added so far: none";
                return;
            }

            var parts = new List<string>
            {
                DescribeColorCounts("Lyase", job.LyaseColors),
                DescribeColorCounts("Isomerase", job.IsomeraseColors),
                DescribeColorCounts("Hydrolase", job.HydrolaseColors)
            };
            EnzymeTotalsText = "So far - " + string.Join(" | ", parts);
        }

        private static string DescribeColorCounts(string type, Dictionary<EnzymeColorType, int> counts)
        {
            var used = counts
                .Where(x => x.Value > 0)
                .Select(x => $"{x.Value} {x.Key.ToString().ToLower()}")
                .ToList();
            return $"{type}: {(used.Count == 0 ? "none" : string.Join(", ", used))}";
        }

        private void RefreshPurities(bool hasPendingStage)
        {
            var names = new GuiBindingList<string>();
            var colors = new GuiBindingList<GuiColor>();
            var tooltips = new GuiBindingList<string>();
            var values = new GuiBindingList<float>();
            var texts = new GuiBindingList<string>();

            void AddRow(string name, string tooltip, float percent, float gain, GuiColor color)
            {
                names.Add(name);
                tooltips.Add(tooltip);
                colors.Add(color);
                values.Add(Math.Clamp(percent / 100f, 0f, 1f));
                texts.Add(gain > 0f ? $"{percent:0.0}% (+{gain:0.0})" : $"{percent:0.0}%");
            }

            foreach (var stat in IncubationProjection.MainPurityStats)
            {
                var (percent, gain) = GetProjectedPercent(stat, hasPendingStage);
                var color = stat == IncubationStatType.XPPenalty ? PenaltyColor : GuiColor.White;
                AddRow(IncubationProjection.GetStatName(stat), IncubationProjection.GetStatTooltip(stat), percent, gain, color);
            }

            if (_showResistances)
            {
                foreach (var stat in IncubationProjection.ResistancePurityStats)
                {
                    var (percent, gain) = GetProjectedPercent(stat, hasPendingStage);
                    AddRow(IncubationProjection.GetStatName(stat), IncubationProjection.GetStatTooltip(stat), percent, gain, GuiColor.White);
                }
            }
            else
            {
                var projected = IncubationProjection.ResistancePurityStats
                    .Select(stat => GetProjectedPercent(stat, hasPendingStage))
                    .ToList();
                AddRow($"Resistances ({projected.Count})",
                    "Average of the eight resistance purities. Use Show resistances to see each one.",
                    projected.Average(x => x.Percent),
                    projected.Average(x => x.Gain),
                    GuiColor.Grey);
            }

            PurityHeader = hasPendingStage
                ? "Purities (gain in brackets is this stage, max +10% per stat)"
                : "Purities";
            ResistanceToggleText = _showResistances ? "Hide resistances" : "Show resistances";

            PurityNames = names;
            PurityNameColors = colors;
            PurityTooltips = tooltips;
            PurityValues = values;
            PurityTexts = texts;
        }

        private (float Percent, float Gain) GetProjectedPercent(IncubationStatType stat, bool hasPendingStage)
        {
            _baseStats.TryGetValue(stat, out var baseValue);
            _stageStats.TryGetValue(stat, out var stageValue);
            var gain = hasPendingStage ? Math.Clamp(stageValue, 0, MaxStageIncrease) : 0;
            var total = Math.Min(baseValue + gain, MaxStat);
            return (total / 10f, (total - Math.Min(baseValue, MaxStat)) / 10f);
        }

        private int GetProjectedMutationChance(IncubationJob job, bool hasPendingStage)
        {
            _baseStats.TryGetValue(IncubationStatType.MutationChance, out var baseValue);
            _stageStats.TryGetValue(IncubationStatType.MutationChance, out var stageValue);
            var gain = hasPendingStage ? Math.Clamp(stageValue, 0, MaxStageIncrease) : 0;
            var erraticGenius = job == null && IsErraticGeniusChecked ? GetErraticGeniusBonus() * 10 : 0;
            return Math.Min(baseValue + gain + erraticGenius, MaxStat);
        }

        private void RefreshMutation(IncubationJob job, bool hasPendingStage)
        {
            var chance = GetProjectedMutationChance(job, hasPendingStage);
            _baseStats.TryGetValue(IncubationStatType.MutationChance, out var baseValue);
            var gain = (chance - baseValue) / 10f;

            if (!IsMutationAllowed)
            {
                MutationChanceText = "Mutation off - beast type is kept";
                MutationChanceColor = GuiColor.Grey;
                return;
            }

            MutationChanceText = gain > 0f
                ? $"Chance: {chance / 10f:0.0}% (+{gain:0.0} this stage)"
                : $"Chance: {chance / 10f:0.0}%";
            MutationChanceColor = GuiColor.White;
        }

        private void RefreshRecipes(IncubationJob job, DateTime now, bool hasPendingStage)
        {
            var lines = new GuiBindingList<string>();
            var colors = new GuiBindingList<GuiColor>();

            if (_dnaType == BeastType.Invalid)
            {
                RecipeHeader = "Known recipes";
                RecipeLines = lines;
                RecipeLineColors = colors;
                UnknownRecipesText = "Add DNA to see its mutation recipes.";
                return;
            }

            var beast = BeastMastery.GetBeastDetail(_dnaType);
            var projection = IncubationProjection.Create(
                job,
                hasPendingStage && !string.IsNullOrWhiteSpace(_lyaseItem) ? _lyaseColor : EnzymeColorType.Invalid,
                hasPendingStage && !string.IsNullOrWhiteSpace(_isomeraseItem) ? _isomeraseColor : EnzymeColorType.Invalid,
                hasPendingStage && !string.IsNullOrWhiteSpace(_hydrolaseItem) ? _hydrolaseColor : EnzymeColorType.Invalid,
                GetProjectedMutationChance(job, hasPendingStage),
                hasPendingStage);

            var unknown = 0;
            foreach (var mutation in beast.PossibleMutations)
            {
                if (!IncubationFieldNote.TryGetNoteForTarget(mutation.Type, out var note) ||
                    !KeyItem.HasKeyItem(Player, note.Note))
                {
                    unknown++;
                    continue;
                }

                var report = projection.Evaluate(mutation, now);
                lines.Add(BeastMastery.GetBeastDetail(mutation.Type).Name);
                colors.Add(GuiColor.White);

                foreach (var requirement in report.Lines)
                {
                    lines.Add($"  {(requirement.IsMet ? "[ok]" : "[ ]")} {requirement.Text}");
                    colors.Add(requirement.IsMet ? MetColor : requirement.IsWaitable ? HintColor : UnmetColor);
                }

                lines.Add($"  {report.Summary}");
                colors.Add(HintColor);
            }

            RecipeHeader = $"Known recipes for {beast.Name}";
            if (lines.Count == 0)
            {
                lines.Add(beast.PossibleMutations.Count == 0
                    ? "This beast cannot mutate."
                    : "You have no field notes for this beast.");
                colors.Add(GuiColor.Grey);
            }

            RecipeLines = lines;
            RecipeLineColors = colors;
            UnknownRecipesText = unknown == 0
                ? string.Empty
                : $"{unknown} unknown recipe{(unknown == 1 ? string.Empty : "s")} - find the field note{(unknown == 1 ? string.Empty : "s")}";
        }

        private void RefreshOutcomes(IncubationJob job, DateTime now)
        {
            var names = new GuiBindingList<string>();
            var chances = new GuiBindingList<string>();
            var colors = new GuiBindingList<GuiColor>();

            if (job != null && IsJobComplete(job, now))
            {
                var beast = BeastMastery.GetBeastDetail(job.BeastDNAType);
                var outcomes = IncubationProjection.GetOutcomes(beast, job, IsMutationAllowed);
                foreach (var outcome in outcomes)
                {
                    var known = outcome.Type == job.BeastDNAType ||
                                (IncubationFieldNote.TryGetNoteForTarget(outcome.Type, out var note) &&
                                 KeyItem.HasKeyItem(Player, note.Note));
                    var name = outcome.Type == job.BeastDNAType
                        ? $"{beast.Name} (no mutation)"
                        : known
                            ? BeastMastery.GetBeastDetail(outcome.Type).Name
                            : "Unknown mutation (no field note)";

                    names.Add(name);
                    chances.Add($"{outcome.Percent:0.0}%");
                    colors.Add(outcome.Type == job.BeastDNAType ? GuiColor.White : MetColor);
                }

                OutcomeHeader = $"Possible outcomes if you complete now ({now.DayOfWeek}, UTC)";
            }
            else
            {
                OutcomeHeader = "Possible outcomes";
            }

            OutcomeNames = names;
            OutcomeChances = chances;
            OutcomeColors = colors;
        }

        private int CalculateIncubationSeconds()
        {
            var social = GetAbilityScore(Player, AbilityType.Social) - 10;
            var socialBonus = 0.5f * (social <= 0 ? 0 : social);
            if (socialBonus > 10)
                socialBonus = 10;

            var timeReductionPercentage = 0.01f * (Perk.GetPerkLevel(Player, PerkType.IncubationProcessing) * 10 + socialBonus);
            return BaseSecondsBetweenStages - (int)(BaseSecondsBetweenStages * timeReductionPercentage);
        }

        private void RefreshIncubationTime()
        {
            var timespan = TimeSpan.FromSeconds(CalculateIncubationSeconds());
            EstimatedTimeToCompletion = $"Each stage takes {Time.GetTimeShortIntervals(timespan, false)}";
        }

        /// <summary>
        /// Returns the enzymes, then the DNA. An item without room stays in the incubator, and the DNA stays while any
        /// enzyme does.
        /// </summary>
        /// <returns>False when an item stayed.</returns>
        private bool RemoveDNA()
        {
            uint item;
            if (!string.IsNullOrWhiteSpace(_hydrolaseItem) &&
                Item.TryAcquireItem(Player, item = ObjectPlugin.Deserialize(_hydrolaseItem)))
            {
                SubtractItemStats(item);
                _hydrolaseItem = string.Empty;
                _hydrolaseColor = EnzymeColorType.Invalid;
                HydrolaseItemResref = _blank;
                HydrolaseDetail = "Empty";
            }
            if (!string.IsNullOrWhiteSpace(_lyaseItem) &&
                Item.TryAcquireItem(Player, item = ObjectPlugin.Deserialize(_lyaseItem)))
            {
                SubtractItemStats(item);
                _lyaseItem = string.Empty;
                _lyaseColor = EnzymeColorType.Invalid;
                LyaseItemResref = _blank;
                LyaseDetail = "Empty";
            }
            if (!string.IsNullOrWhiteSpace(_isomeraseItem) &&
                Item.TryAcquireItem(Player, item = ObjectPlugin.Deserialize(_isomeraseItem)))
            {
                SubtractItemStats(item);
                _isomeraseItem = string.Empty;
                _isomeraseColor = EnzymeColorType.Invalid;
                IsomeraseItemResref = _blank;
                IsomeraseDetail = "Empty";
            }

            var enzymesReturned = string.IsNullOrWhiteSpace(_hydrolaseItem) &&
                                  string.IsNullOrWhiteSpace(_lyaseItem) &&
                                  string.IsNullOrWhiteSpace(_isomeraseItem);
            if (enzymesReturned && !string.IsNullOrWhiteSpace(_dnaItem) &&
                Item.TryAcquireItem(Player, ObjectPlugin.Deserialize(_dnaItem)))
            {
                _dnaItem = string.Empty;
                DNAItemResref = _blank;
            }

            var allReturned = enzymesReturned && string.IsNullOrWhiteSpace(_dnaItem);
            if (allReturned)
            {
                IsErraticGeniusChecked = false;
                ClearStats();
            }

            return allReturned;
        }

        /// <summary>Returns items another incubator kept. Returns false when some still do not fit.</summary>
        private bool ReturnItemsHeldElsewhere()
        {
            if (!string.IsNullOrWhiteSpace(_hydrolaseItem) && Item.TryAcquireItem(Player, ObjectPlugin.Deserialize(_hydrolaseItem)))
            {
                _hydrolaseItem = string.Empty;
                _hydrolaseColor = EnzymeColorType.Invalid;
            }
            if (!string.IsNullOrWhiteSpace(_lyaseItem) && Item.TryAcquireItem(Player, ObjectPlugin.Deserialize(_lyaseItem)))
            {
                _lyaseItem = string.Empty;
                _lyaseColor = EnzymeColorType.Invalid;
            }
            if (!string.IsNullOrWhiteSpace(_isomeraseItem) && Item.TryAcquireItem(Player, ObjectPlugin.Deserialize(_isomeraseItem)))
            {
                _isomeraseItem = string.Empty;
                _isomeraseColor = EnzymeColorType.Invalid;
            }
            if (!string.IsNullOrWhiteSpace(_dnaItem) && Item.TryAcquireItem(Player, ObjectPlugin.Deserialize(_dnaItem)))
                _dnaItem = string.Empty;

            return string.IsNullOrWhiteSpace(_dnaItem) && string.IsNullOrWhiteSpace(_hydrolaseItem) &&
                   string.IsNullOrWhiteSpace(_lyaseItem) && string.IsNullOrWhiteSpace(_isomeraseItem);
        }

        /// <summary>Shows the items that stayed in the incubator when its window last closed.</summary>
        private void RestoreHeldItems()
        {
            HydrolaseDetail = "Empty";
            LyaseDetail = "Empty";
            IsomeraseDetail = "Empty";

            if (!string.IsNullOrWhiteSpace(_dnaItem))
            {
                var item = ObjectPlugin.Deserialize(_dnaItem);
                LoadDNAStats(item);
                DNAItemResref = Item.GetIconResref(item);
                DestroyObject(item);
            }
            if (!string.IsNullOrWhiteSpace(_hydrolaseItem))
            {
                var item = ObjectPlugin.Deserialize(_hydrolaseItem);
                _hydrolaseColor = AddItemStats(item);
                HydrolaseItemResref = Item.GetIconResref(item);
                HydrolaseDetail = DescribeEnzyme(item);
                DestroyObject(item);
            }
            if (!string.IsNullOrWhiteSpace(_lyaseItem))
            {
                var item = ObjectPlugin.Deserialize(_lyaseItem);
                _lyaseColor = AddItemStats(item);
                LyaseItemResref = Item.GetIconResref(item);
                LyaseDetail = DescribeEnzyme(item);
                DestroyObject(item);
            }
            if (!string.IsNullOrWhiteSpace(_isomeraseItem))
            {
                var item = ObjectPlugin.Deserialize(_isomeraseItem);
                _isomeraseColor = AddItemStats(item);
                IsomeraseItemResref = Item.GetIconResref(item);
                IsomeraseDetail = DescribeEnzyme(item);
                DestroyObject(item);
            }
        }

        private void ClearStats()
        {
            _dnaType = BeastType.Invalid;
            _baseStats.Clear();
            ResetPendingEnzymes();
        }

        private void ResetPendingEnzymes()
        {
            _stageStats.Clear();
            _lyaseColor = EnzymeColorType.Invalid;
            _isomeraseColor = EnzymeColorType.Invalid;
            _hydrolaseColor = EnzymeColorType.Invalid;
        }

        private void LoadJobStats(IncubationJob job)
        {
            _baseStats[IncubationStatType.MutationChance] = job.MutationChance;
            _baseStats[IncubationStatType.AttackPurity] = job.AttackPurity;
            _baseStats[IncubationStatType.AccuracyPurity] = job.AccuracyPurity;
            _baseStats[IncubationStatType.EvasionPurity] = job.EvasionPurity;
            _baseStats[IncubationStatType.LearningPurity] = job.LearningPurity;
            _baseStats[IncubationStatType.XPPenalty] = job.XPPenalty;
            _baseStats[IncubationStatType.PhysicalDefensePurity] = job.DefensePurities[CombatDamageType.Physical];
            _baseStats[IncubationStatType.ForceDefensePurity] = job.DefensePurities[CombatDamageType.Force];
            foreach (var stat in IncubationProjection.ResistancePurityStats)
            {
                var resistance = IncubationProjection.GetResistanceType(stat);
                _baseStats[stat] = job.ResistancePurities.TryGetValue(resistance, out var value) ? value : 0;
            }
        }

        public Action OnClickDNA() => () =>
        {
            if (GetJob() != null)
                return;

            if (!string.IsNullOrWhiteSpace(_dnaItem))
            {
                ShowModal("Will you remove the DNA from the incubator? All enzymes will also be removed.", () =>
                {
                    if (!RemoveDNA())
                        FloatingTextStringOnCreature(Item.InventoryFullMessage, Player, false);
                    SwitchViews();
                }, SwitchViews);
            }
            else
            {
                Targeting.EnterTargetingMode(Player, ObjectType.Item, "Select a DNA item from your inventory.", item =>
                {
                    if (GetResRef(item) != BeastMastery.DNAResref)
                    {
                        FloatingTextStringOnCreature("Only DNA items may be selected.", Player, false);
                        return;
                    }

                    var error = Item.CanBePersistentlyStored(Player, item);
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        FloatingTextStringOnCreature(error, Player, false);
                        return;
                    }

                    LoadDNAStats(item);

                    DNAItemResref = Item.GetIconResref(item);
                    _dnaItem = ObjectPlugin.Serialize(item);
                    DestroyObject(item);

                    RefreshAllStats();
                });
            }
        };

        private void LoadDNAStats(uint item)
        {
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                var type = GetItemPropertyType(ip);
                if (type == ItemPropertyType.DNAType)
                {
                    _dnaType = (BeastType)GetItemPropertySubType(ip);
                }
                else if (type == ItemPropertyType.Incubation)
                {
                    var subType = (IncubationStatType)GetItemPropertySubType(ip);
                    if (!IncubationProjection.IsKnownStat(subType))
                        throw new ArgumentOutOfRangeException(nameof(subType), subType, "Unknown incubation stat on DNA.");

                    _baseStats[subType] = GetItemPropertyCostTableValue(ip);
                }
            }
        }

        private EnzymeColorType AddItemStats(uint item)
        {
            return ApplyItemStats(item, 1);
        }

        private void SubtractItemStats(uint item)
        {
            ApplyItemStats(item, -1);
        }

        /// <summary>
        /// Adds (sign 1) or removes (sign -1) an enzyme's incubation stats from this stage's totals.
        /// Only Incubation properties count; the enzyme color property is read, never applied as a stat.
        /// </summary>
        private EnzymeColorType ApplyItemStats(uint item, int sign)
        {
            var colorType = EnzymeColorType.Invalid;
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                var ipType = GetItemPropertyType(ip);
                if (ipType == ItemPropertyType.Incubation)
                {
                    var type = (IncubationStatType)GetItemPropertySubType(ip);
                    if (!IncubationProjection.IsKnownStat(type))
                        throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown incubation stat on enzyme.");

                    _stageStats.TryGetValue(type, out var current);
                    _stageStats[type] = current + sign * GetItemPropertyCostTableValue(ip);
                }
                else if (ipType == ItemPropertyType.EnzymeColor)
                {
                    colorType = (EnzymeColorType)GetItemPropertySubType(ip);
                }
            }

            return colorType;
        }

        private static string DescribeEnzyme(uint item)
        {
            var color = EnzymeColorType.Invalid;
            var stats = new List<string>();
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                var ipType = GetItemPropertyType(ip);
                if (ipType == ItemPropertyType.EnzymeColor)
                {
                    color = (EnzymeColorType)GetItemPropertySubType(ip);
                }
                else if (ipType == ItemPropertyType.Incubation)
                {
                    var stat = (IncubationStatType)GetItemPropertySubType(ip);
                    var value = GetItemPropertyCostTableValue(ip) / 10f;
                    stats.Add($"{IncubationProjection.GetShortStatName(stat)} +{value:0.0}%");
                }
            }

            var colorText = color == EnzymeColorType.Invalid ? "Unknown" : color.ToString();
            return stats.Count == 0 ? colorText : $"{colorText}: {string.Join(", ", stats)}";
        }

        private delegate void EnzymeSlotSetter(string serialized, EnzymeColorType color, string icon, string detail);

        private Action OnClickEnzyme(
            string name,
            string resrefPrefix,
            Func<string> getSerialized,
            EnzymeSlotSetter setSlot) => () =>
        {
            var job = GetJob();
            if (job != null && DateTime.UtcNow < job.DateCompleted)
                return;
            if (job != null && job.CurrentStage >= NumberOfStages)
                return;

            var serialized = getSerialized();
            if (!string.IsNullOrWhiteSpace(serialized))
            {
                ShowModal($"Will you remove the {name} enzyme?", () =>
                {
                    var item = ObjectPlugin.Deserialize(serialized);
                    if (!Item.TryAcquireItem(Player, item))
                    {
                        FloatingTextStringOnCreature(Item.InventoryFullMessage, Player, false);
                        SwitchViews();
                        return;
                    }

                    SubtractItemStats(item);
                    setSlot(string.Empty, EnzymeColorType.Invalid, _blank, "Empty");
                    SwitchViews();
                }, SwitchViews);
            }
            else
            {
                Targeting.EnterTargetingMode(Player, ObjectType.Item, $"Select a {name} item from your inventory.",
                item =>
                {
                    if (!GetResRef(item).StartsWith(resrefPrefix))
                    {
                        FloatingTextStringOnCreature($"Only {name} items may be selected.", Player, false);
                        return;
                    }

                    var error = Item.CanBePersistentlyStored(Player, item);
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        FloatingTextStringOnCreature(error, Player, false);
                        return;
                    }

                    var icon = Item.GetIconResref(item);
                    var detail = DescribeEnzyme(item);
                    var color = AddItemStats(item);
                    setSlot(ObjectPlugin.Serialize(item), color, icon, detail);
                    DestroyObject(item);
                    RefreshAllStats();
                });
            }
        };

        public Action OnClickHydrolase() => OnClickEnzyme("Hydrolase", BeastMastery.HydrolaseResrefPrefix,
            () => _hydrolaseItem,
            (serialized, color, icon, detail) =>
            {
                _hydrolaseItem = serialized;
                _hydrolaseColor = color;
                HydrolaseItemResref = icon;
                HydrolaseDetail = detail;
            });

        public Action OnClickLyase() => OnClickEnzyme("Lyase", BeastMastery.LyaseResrefPrefix,
            () => _lyaseItem,
            (serialized, color, icon, detail) =>
            {
                _lyaseItem = serialized;
                _lyaseColor = color;
                LyaseItemResref = icon;
                LyaseDetail = detail;
            });

        public Action OnClickIsomerase() => OnClickEnzyme("Isomerase", BeastMastery.IsomeraseResrefPrefix,
            () => _isomeraseItem,
            (serialized, color, icon, detail) =>
            {
                _isomeraseItem = serialized;
                _isomeraseColor = color;
                IsomeraseItemResref = icon;
                IsomeraseDetail = detail;
            });

        public Action OnClickErraticGeniusToggled() => () =>
        {
            LoadPlayerStats();
            RefreshAllStats();
        };

        public Action OnClickAllowMutationToggled() => () =>
        {
            var job = GetJob();
            if (job != null)
            {
                job.IsMutationSuppressed = !IsMutationAllowed;
                DB.Set(job);
            }

            RefreshAllStats();
        };

        public Action OnClickToggleResistances() => () =>
        {
            _showResistances = !_showResistances;
            RefreshAllStats();
        };

        private string ValidateCreateJob()
        {
            var playerId = GetObjectUUID(Player);
            var maxConcurrentJobs = Perk.GetPerkLevel(Player, PerkType.IncubationManagement) + 1;
            var dbQuery = new DBQuery<IncubationJob>()
                .AddFieldSearch(nameof(IncubationJob.PlayerId), playerId, false);
            var currentJobs = DB.Search(dbQuery).ToList();
            var currentJobCount = currentJobs.Count(x => x.ParentPropertyId != _incubatorPropertyId);

            if (currentJobCount >= maxConcurrentJobs)
            {
                return $"You may only have {maxConcurrentJobs} incubation job(s) active at one time.";
            }

            var job = GetJob();
            if (job == null)
                return string.Empty;

            if (job.CurrentStage >= NumberOfStages)
                return "Max stage reached.";

            if (DateTime.UtcNow < job.DateCompleted)
                return "The current stage is still running.";

            if (IsErraticGeniusChecked && Perk.GetPerkLevel(Player, PerkType.ErraticGenius) <= 0)
            {
                return "You do not have the Erratic Genius perk purchased and cannot start this job.";
            }

            return string.Empty;
        }

        private int GetStageTotal(IncubationStatType stat)
        {
            _baseStats.TryGetValue(stat, out var baseValue);
            _stageStats.TryGetValue(stat, out var stageValue);
            return Math.Min(baseValue + Math.Clamp(stageValue, 0, MaxStageIncrease), MaxStat);
        }

        private void StartJob(IncubationJob job)
        {
            var incubationSeconds = CalculateIncubationSeconds();
            var now = DateTime.UtcNow;
            var hasPendingStage = true;

            var validationError = ValidateCreateJob();
            if (string.IsNullOrWhiteSpace(validationError))
            {
                job.MutationChance = GetProjectedMutationChance(job.CurrentStage <= 0 ? null : job, hasPendingStage);
                if (job.CurrentStage <= 0)
                    job.DNAIconResref = DNAItemResref;
                job.CurrentStage++;
                job.IsMutationSuppressed = !IsMutationAllowed;

                job.AttackPurity = GetStageTotal(IncubationStatType.AttackPurity);
                job.AccuracyPurity = GetStageTotal(IncubationStatType.AccuracyPurity);
                job.EvasionPurity = GetStageTotal(IncubationStatType.EvasionPurity);
                job.LearningPurity = GetStageTotal(IncubationStatType.LearningPurity);
                job.XPPenalty = GetStageTotal(IncubationStatType.XPPenalty);

                job.DefensePurities[CombatDamageType.Physical] = GetStageTotal(IncubationStatType.PhysicalDefensePurity);
                job.DefensePurities[CombatDamageType.Force] = GetStageTotal(IncubationStatType.ForceDefensePurity);

                foreach (var stat in IncubationProjection.ResistancePurityStats)
                {
                    job.ResistancePurities[IncubationProjection.GetResistanceType(stat)] = GetStageTotal(stat);
                }

                job.ResistancePurities = BeastResistanceCalculator.CreateResistancePurities(job.ResistancePurities);

                if (_lyaseColor != EnzymeColorType.Invalid)
                    job.LyaseColors[_lyaseColor]++;
                if (_hydrolaseColor != EnzymeColorType.Invalid)
                    job.HydrolaseColors[_hydrolaseColor]++;
                if (_isomeraseColor != EnzymeColorType.Invalid)
                    job.IsomeraseColors[_isomeraseColor]++;

                job.DateStarted = now;
                job.DateCompleted = now.AddSeconds(incubationSeconds);

                DB.Set(job);

                _dnaItem = string.Empty;
                _hydrolaseItem = string.Empty;
                _isomeraseItem = string.Empty;
                _lyaseItem = string.Empty;
                // The enzymes are consumed, so they no longer count toward the next stage.
                ResetPendingEnzymes();
                Gui.CloseWindow(Player, GuiWindowType.Incubator, Player);
                FloatingTextStringOnCreature($"Incubation stage {job.CurrentStage} started!", Player, false);
            }
            else
            {
                SendMessageToPC(Player, $"Unable to start Incubation Job. Reason: {validationError}");
                Log.Write(LogGroup.Incubation, $"Job could not be created on incubator Id {_incubatorPropertyId} due to reason: {validationError}");
            }
        }

        public Action OnClickStartJob() => () =>
        {
            ShowModal("Start stage 1 of this job? The DNA and enzymes go into the incubator.", () =>
            {
                var job = new IncubationJob
                {
                    ParentPropertyId = _incubatorPropertyId,
                    PlayerId = GetObjectUUID(Player),
                    BeastDNAType = _dnaType
                };
                StartJob(job);

                SwitchViews();
            }, SwitchViews);
        };

        public Action OnClickContinueJob() => () =>
        {
            var job = GetJob();
            if (job == null)
                return;

            ShowModal($"Start stage {job.CurrentStage + 1}? These enzymes go into the incubator.", () =>
            {
                StartJob(job);
                SwitchViews();
            }, SwitchViews);
        };

        public Action OnClickCancelJob() => () =>
        {
            ShowModal("Are you sure you want to cancel this job? All DNA and enzyme items will be permanently lost!",
            () =>
            {
                var dbJob = GetJob();
                if (dbJob == null)
                    return;

                DB.Delete<IncubationJob>(dbJob.Id);
                Gui.CloseWindow(Player, GuiWindowType.Incubator, Player);
                Log.Write(LogGroup.Incubation, $"Player '{GetName(Player)}' ({GetObjectUUID(Player)}) canceled incubation job '{dbJob.Id}' on incubator property Id '{dbJob.ParentPropertyId}'.");
                FloatingTextStringOnCreature("Incubation job cancelled!", Player, false);

                SwitchViews();
            }, SwitchViews);
        };

        public Action OnClickCompleteJob() => () =>
        {
            var job = GetJob();
            if (job == null)
                return;

            var outcomes = IncubationProjection.GetOutcomes(BeastMastery.GetBeastDetail(job.BeastDNAType), job, !job.IsMutationSuppressed);
            var mutationOdds = outcomes
                .Where(x => x.Type != job.BeastDNAType)
                .Sum(x => x.Percent);
            var prompt = mutationOdds > 0f
                ? $"Complete this job? It has a {mutationOdds:0.0}% chance to mutate into another beast."
                : "Complete this job? The egg keeps its DNA's beast type.";

            ShowModal(prompt, () =>
            {
                var current = GetJob();
                if (!IsJobComplete(current, DateTime.UtcNow))
                {
                    SwitchViews();
                    return;
                }

                BeastMastery.CreateBeastEgg(current, Player);
                Gui.CloseWindow(Player, GuiWindowType.Incubator, Player);

                SwitchViews();
            }, SwitchViews);
        };

        /// <summary>Runs whenever the window closes, whether the player closes it or the server does.</summary>
        public override Action OnWindowClosed() => () =>
        {
            if (!RemoveDNA())
                SendMessageToPC(Player, Item.ItemsHeldMessage);
        };

        public void Refresh(PerkAcquiredRefreshEvent payload)
        {
            LoadPlayerStats();
            RefreshAllStats();
        }

        public void Refresh(PerkRefundedRefreshEvent payload)
        {
            LoadPlayerStats();
            RefreshAllStats();
        }
    }
}
