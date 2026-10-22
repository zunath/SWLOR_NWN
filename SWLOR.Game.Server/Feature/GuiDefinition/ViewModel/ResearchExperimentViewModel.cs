using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;
using SWLOR.Game.Server.Service.CraftService.Research;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// Binds the Experiment engine to its window. The engine owns every rule and number; this class opens the experiment for the job
    /// that just started, saves it on the job after every action, forwards clicks, and settles it when the experiment ends or the
    /// player leaves.
    /// </summary>
    public class ResearchExperimentViewModel : CraftMinigameViewModel<ResearchExperimentViewModel, ResearchExperimentPayload>
    {
        public const string ContentElement = "research_experiment_content";
        public const string MainPartial = "RESEARCH_EXPERIMENT_MAIN";

        /// <summary>The evidence bars: one per candidate where evidence decides between two, the first alone otherwise.</summary>
        public const int EvidenceSlotCount = 2;

        /// <summary>The action buttons: up to two Tests, then Refine Method, Analyze and Conclude.</summary>
        public const int ActionCount = 5;

        private static readonly GuiColor[] EvidenceColors =
        {
            new(0, 191, 255),
            new(255, 165, 0)
        };

        private static readonly GuiColor FixedColor = new(180, 130, 255);

        private readonly string[] _slotActions = new string[ActionCount];
        private string _jobId;
        private ExperimentEngine _engine;
        private bool _over;

        protected override CraftMinigameType Minigame => CraftMinigameType.Experiment;

        public string RecipeName { get => Get<string>(); set => Set(value); }
        public string LevelText { get => Get<string>(); set => Set(value); }
        public string StatusText { get => Get<string>(); set => Set(value); }
        public GuiColor StatusColor { get => Get<GuiColor>(); set => Set(value); }
        public string ExtraBonusText { get => Get<string>(); set => Set(value); }
        public GuiColor ExtraBonusColor { get => Get<GuiColor>(); set => Set(value); }
        public string JobTimeText { get => Get<string>(); set => Set(value); }
        public string TrialsText { get => Get<string>(); set => Set(value); }
        public float TrialsPercent { get => Get<float>(); set => Set(value); }
        public string TimeCutText { get => Get<string>(); set => Set(value); }
        public float TimeCutPercent { get => Get<float>(); set => Set(value); }
        public string CpText { get => Get<string>(); set => Set(value); }
        public float CpPercent { get => Get<float>(); set => Set(value); }

        public string EvidenceText0 => Get<string>();
        public string EvidenceText1 => Get<string>();
        public string EvidenceProjection0 => Get<string>();
        public string EvidenceProjection1 => Get<string>();
        public float EvidencePercent0 => Get<float>();
        public float EvidencePercent1 => Get<float>();
        public bool EvidenceVisible0 => Get<bool>();
        public bool EvidenceVisible1 => Get<bool>();

        public string ActionText0 => Get<string>();
        public string ActionText1 => Get<string>();
        public string ActionText2 => Get<string>();
        public string ActionText3 => Get<string>();
        public string ActionText4 => Get<string>();
        public string ActionSummary0 => Get<string>();
        public string ActionSummary1 => Get<string>();
        public string ActionSummary2 => Get<string>();
        public string ActionSummary3 => Get<string>();
        public string ActionSummary4 => Get<string>();
        public string ActionTooltip0 => Get<string>();
        public string ActionTooltip1 => Get<string>();
        public string ActionTooltip2 => Get<string>();
        public string ActionTooltip3 => Get<string>();
        public string ActionTooltip4 => Get<string>();
        public GuiColor ActionColor0 => Get<GuiColor>();
        public GuiColor ActionColor1 => Get<GuiColor>();
        public GuiColor ActionColor2 => Get<GuiColor>();
        public GuiColor ActionColor3 => Get<GuiColor>();
        public GuiColor ActionColor4 => Get<GuiColor>();
        public bool ActionEnabled0 => Get<bool>();
        public bool ActionEnabled1 => Get<bool>();
        public bool ActionEnabled2 => Get<bool>();
        public bool ActionEnabled3 => Get<bool>();
        public bool ActionEnabled4 => Get<bool>();
        public bool ActionVisible0 => Get<bool>();
        public bool ActionVisible1 => Get<bool>();
        public bool ActionVisible2 => Get<bool>();
        public bool ActionVisible3 => Get<bool>();
        public bool ActionVisible4 => Get<bool>();

        public static string EvidenceTextProperty(int slot) => $"EvidenceText{slot}";
        public static string EvidenceProjectionProperty(int slot) => $"EvidenceProjection{slot}";
        public static string EvidencePercentProperty(int slot) => $"EvidencePercent{slot}";
        public static string EvidenceVisibleProperty(int slot) => $"EvidenceVisible{slot}";
        public static string ActionTextProperty(int index) => $"ActionText{index}";
        public static string ActionSummaryProperty(int index) => $"ActionSummary{index}";
        public static string ActionTooltipProperty(int index) => $"ActionTooltip{index}";
        public static string ActionColorProperty(int index) => $"ActionColor{index}";
        public static string ActionEnabledProperty(int index) => $"ActionEnabled{index}";
        public static string ActionVisibleProperty(int index) => $"ActionVisible{index}";

        /// <summary>The bar color of an evidence slot: one color per candidate of a random level.</summary>
        public static GuiColor EvidenceColor(int slot) => EvidenceColors[slot];

        protected override void Initialize(ResearchExperimentPayload initialPayload)
        {
            _jobId = initialPayload.JobId;
            _engine = null;
            _over = false;
            Array.Clear(_slotActions);

            ClearDisplay();
            ChangePartialView(ContentElement, MainPartial);

            var job = DB.Get<ResearchJob>(_jobId);
            if (job == null || job.PlayerId != GetObjectUUID(Player) || !ResearchExperimentLifecycle.IsOpen(job))
            {
                _over = true;
                SetStatus("This experiment is over.", GuiColor.Grey);
                OpenHelpOnFirstUse();
                return;
            }

            if (job.Experiment.TrialsUsed > 0)
            {
                // An experiment is played in one sitting; one already underway was left, so it ends with what it holds.
                SettleAndSave(job);
                _over = true;
                SetStatus("This experiment was left earlier and has ended.", GuiColor.Grey);
                OpenHelpOnFirstUse();
                return;
            }

            var recipe = Craft.GetRecipe(job.Recipe);
            var dbPlayer = DB.Get<Player>(GetObjectUUID(Player));
            var gearCp = dbPlayer != null && dbPlayer.CPBonus != null && dbPlayer.CPBonus.TryGetValue(SkillType.Fabrication, out var cp) ? cp : 0;
            var setup = ExperimentSetup.ForRecipe(
                recipe,
                job.Experiment.TargetLevel,
                Skill.GetCreatureSkillRank(Player, SkillType.Fabrication),
                gearCp,
                job.Experiment.ExtraBonus,
                licensedRunsGrant: job.Experiment.LicensedRunsGrant);

            _engine = new ExperimentEngine(ServiceMinigameRandom.Instance, setup);
            MapActions();

            RecipeName = $"Recipe: {recipe.Quantity}x {Cache.GetItemNameByResref(recipe.Resref)}";
            LevelText = setup.IsContested
                ? $"Research level {setup.TargetLevel}: a Credit Reduction or a Time Reduction. Evidence decides which."
                : setup.IsRandomLevel
                    ? $"Research level {setup.TargetLevel}: {setup.NameOf(ResearchCandidate.FixedGrant)} (rolled when the experiment opened)"
                    : $"Research level {setup.TargetLevel}: {ResearchGrantRules.Describe(setup.TargetLevel)}";
            SetStatus(
                setup.IsContested
                    ? "Test the reduction you want. The candidate with the most evidence wins."
                    : "Gather evidence, then Analyze it for a shorter job.",
                GuiColor.White);
            Refresh();
            OpenHelpOnFirstUse();
        }

        /// <summary>Leaving the window, walking away from the terminal or logging out ends a running experiment with its evidence so far.</summary>
        public override Action OnWindowClosed() => () =>
        {
            if (_over || string.IsNullOrEmpty(_jobId))
                return;

            var job = DB.Get<ResearchJob>(_jobId);
            if (!ResearchExperimentLifecycle.IsOpen(job))
                return;

            _over = true;
            SettleAndSave(job);
            SendMessageToPC(Player, "You left the experiment. The job keeps running with the evidence you gathered.");
        };

        public Action OnClickAction(int index) => () => PerformAction(index);

        public Action OnClickClose() => () => Gui.TogglePlayerWindow(Player, GuiWindowType.ResearchExperiment);

        private void PerformAction(int index)
        {
            if (_engine == null || _over || index < 0 || index >= ActionCount || _slotActions[index] == null)
                return;

            var job = DB.Get<ResearchJob>(_jobId);
            if (!ResearchExperimentLifecycle.IsOpen(job))
            {
                EndOver("The research job is no longer running.");
                return;
            }

            var result = _engine.Perform(_slotActions[index]);
            if (!result.Performed)
            {
                SetStatus(result.RejectedReason, GuiColor.Red);
                Refresh();
                return;
            }

            ResearchExperimentLifecycle.Record(job, _engine.Snapshot());

            if (_engine.Status != MinigameStatus.InProgress)
            {
                Finish(job);
                return;
            }

            DB.Set(job);
            var name = _engine.Actions.First(action => action.Id == _slotActions[index]).Name;
            SetStatus($"{name}: {result.Outcome.Label}. {ExperimentDisplay.DescribeOutcome(result.Outcome)}.", GuiColor.White);
            Refresh();
        }

        private void Finish(ResearchJob job)
        {
            _over = true;
            var xp = SettleAndSave(job);

            var result = ResultText(job);
            SetStatus("The experiment is over. " + result, GuiColor.Green);
            Refresh();
            SendMessageToPC(Player, $"Experiment complete. {result}{(xp > 0 ? $" +{xp} Fabrication XP." : string.Empty)}");
        }

        /// <summary>Settles the experiment with its evidence, gives its XP while the player is here, and saves the job. Returns the XP given.</summary>
        private int SettleAndSave(ResearchJob job)
        {
            ResearchExperimentLifecycle.Settle(job, ServiceMinigameRandom.Instance);
            var xp = ResearchExperimentService.GrantExperimentXp(Player, job);
            DB.Set(job);
            return xp;
        }

        private static string ResultText(ResearchJob job) =>
            string.Join(" ", ResearchExperimentLifecycle.Describe(job.Experiment)) +
            $" The job takes {DurationText(job.DateCompleted - job.DateStarted)}.";

        private void MapActions()
        {
            Array.Clear(_slotActions);
            var tests = _engine.Setup.Candidates.Select(ExperimentActionIds.TestFor).ToList();
            for (var index = 0; index < tests.Count; index++)
                _slotActions[index] = tests[index];

            _slotActions[EvidenceSlotCount] = ExperimentActionIds.RefineMethod;
            _slotActions[EvidenceSlotCount + 1] = ExperimentActionIds.Analyze;
            _slotActions[EvidenceSlotCount + 2] = ExperimentActionIds.Conclude;
        }

        private void Refresh()
        {
            var setup = _engine.Setup;
            var job = DB.Get<ResearchJob>(_jobId);

            TrialsText = ExperimentDisplay.MeterText("Trials used", _engine.Meters.Progress);
            TrialsPercent = _engine.Meters.Progress.Fraction;
            TimeCutText = $"Job time cut: {_engine.TimeCutPercent}% (cap {setup.TimeCutCap}%)";
            TimeCutPercent = setup.TimeCutCap <= 0 ? 0f : (float)_engine.TimeCutPercent / setup.TimeCutCap;
            CpText = ExperimentDisplay.MeterText("CP", _engine.Meters.Cp);
            CpPercent = _engine.Meters.Cp.Fraction;

            ExtraBonusText = setup.ExtraBonus
                ? $"Extra guaranteed bonus: YES (tier {setup.ExtraBonusTier}). It is added when the job completes."
                : "Extra guaranteed bonus: none this time.";
            ExtraBonusColor = setup.ExtraBonus ? GuiColor.Green : GuiColor.Grey;

            var fullSeconds = job?.Experiment?.FullDurationSeconds ?? 0;
            var cutSeconds = fullSeconds * (100 - _engine.TimeCutPercent) / 100.0;
            JobTimeText = _engine.TimeCutPercent > 0
                ? $"Job time: {DurationText(TimeSpan.FromSeconds(fullSeconds))} -> {DurationText(TimeSpan.FromSeconds(cutSeconds))}"
                : $"Job time: {DurationText(TimeSpan.FromSeconds(fullSeconds))}";

            for (var slot = 0; slot < EvidenceSlotCount; slot++)
            {
                if (slot >= setup.Candidates.Count)
                {
                    Set(string.Empty, EvidenceTextProperty(slot));
                    Set(string.Empty, EvidenceProjectionProperty(slot));
                    Set(0f, EvidencePercentProperty(slot));
                    Set(false, EvidenceVisibleProperty(slot));
                    continue;
                }

                var candidate = setup.Candidates[slot];
                var evidence = _engine.EvidenceOf(candidate);
                var label = candidate == ResearchCandidate.FixedGrant ? "Evidence" : $"{setup.NameOf(candidate)}: evidence";
                Set($"{label} {evidence}/{setup.Trials}", EvidenceTextProperty(slot));
                Set(ExperimentDisplay.Projection(candidate, evidence), EvidenceProjectionProperty(slot));
                Set(setup.Trials <= 0 ? 0f : Math.Min(1f, (float)evidence / setup.Trials), EvidencePercentProperty(slot));
                Set(true, EvidenceVisibleProperty(slot));
            }

            var previews = _engine.PreviewAll().ToDictionary(preview => preview.Action.Id);
            for (var index = 0; index < ActionCount; index++)
            {
                var actionId = _slotActions[index];
                if (actionId == null)
                {
                    SetAction(index, string.Empty, string.Empty, string.Empty, GuiColor.White, false, false);
                    continue;
                }

                var preview = previews[actionId];
                var view = ExperimentDisplay.DescribeAction(preview);
                var text = ExperimentActionIds.TryGetCandidate(actionId, out _) && preview.Enabled
                    ? $"{preview.Action.Name} [{preview.Outcomes[0].ChancePercent}%]"
                    : preview.CpCost > 0
                        ? $"{preview.Action.Name} [{preview.CpCost} CP]"
                        : preview.Action.Name;

                SetAction(index, text, view.Summary, view.Tooltip, KindColor(view.Kind), preview.Enabled && !_over, true);
            }
        }

        private void SetAction(int index, string text, string summary, string tooltip, GuiColor color, bool enabled, bool visible)
        {
            Set(text, ActionTextProperty(index));
            Set(summary, ActionSummaryProperty(index));
            Set(tooltip, ActionTooltipProperty(index));
            Set(color, ActionColorProperty(index));
            Set(enabled, ActionEnabledProperty(index));
            Set(visible, ActionVisibleProperty(index));
        }

        private void ClearDisplay()
        {
            RecipeName = string.Empty;
            LevelText = string.Empty;
            StatusText = string.Empty;
            StatusColor = GuiColor.White;
            ExtraBonusText = string.Empty;
            ExtraBonusColor = GuiColor.White;
            JobTimeText = string.Empty;
            TrialsText = string.Empty;
            TrialsPercent = 0f;
            TimeCutText = string.Empty;
            TimeCutPercent = 0f;
            CpText = string.Empty;
            CpPercent = 0f;

            for (var slot = 0; slot < EvidenceSlotCount; slot++)
            {
                Set(string.Empty, EvidenceTextProperty(slot));
                Set(string.Empty, EvidenceProjectionProperty(slot));
                Set(0f, EvidencePercentProperty(slot));
                Set(false, EvidenceVisibleProperty(slot));
            }

            for (var index = 0; index < ActionCount; index++)
                SetAction(index, string.Empty, string.Empty, string.Empty, GuiColor.White, false, false);
        }

        /// <summary>Ends the window's play without settling: the job is gone or already settled.</summary>
        private void EndOver(string message)
        {
            _over = true;
            SetStatus(message, GuiColor.Red);
            if (_engine != null)
                Refresh();
        }

        private void SetStatus(string text, GuiColor color)
        {
            StatusText = text;
            StatusColor = color;
        }

        private static string DurationText(TimeSpan span) => Time.GetTimeShortIntervals(span, false);

        private static GuiColor KindColor(ExperimentActionKind kind) => kind switch
        {
            ExperimentActionKind.EndsExperiment => GuiColor.Green,
            ExperimentActionKind.Unavailable => GuiColor.Grey,
            _ => GuiColor.White
        };
    }
}
