using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.IncubatorWindow;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class IncubatorViewModel : GuiViewModelBase<IncubatorViewModel, IncubatorPayload>,
        IGuiRefreshable<PerkAcquiredRefreshEvent>,
        IGuiRefreshable<PerkRefundedRefreshEvent>,
        IIncubatorHost
    {
        private IDBService DB { get; }
        private IGuiService Gui { get; }
        private IncubatorItemService IncubatorItemService { get; }
        private IncubatorJobCalculator IncubatorJobCalculator { get; }
        private IncubatorJobService IncubatorJobService { get; }
        private IncubatorPanelService IncubatorPanelService { get; }

        public IncubatorViewModel(IDBService db, IGuiService gui, IncubatorItemService incubatorItemService, IncubatorJobCalculator incubatorJobCalculator, IncubatorJobService incubatorJobService, IncubatorPanelService incubatorPanelService)
        {
            DB = db;
            Gui = gui;
            IncubatorItemService = incubatorItemService;
            IncubatorJobCalculator = incubatorJobCalculator;
            IncubatorJobService = incubatorJobService;
            IncubatorPanelService = incubatorPanelService;
        }

        public const string PartialElement = "PARTIAL_VIEW";
        public const string NewJobPartial = "NEW_JOB_PARTIAL";
        public const string InProgressJobPartial = "IN_PROGRESS_JOB_PARTIAL";
        public const string StageCompleteJobPartial = "STAGE_COMPLETE_PARTIAL";
        public const string CompleteJobPartial = "COMPLETE_JOB_PARTIAL";

        private const string HeldElsewhereMessage = "Your inventory is full. Items you left in another incubator are still there; make room first.";

        private readonly IncubatorSession _session = new();

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
        public string HydrolaseTitle { get => Get<string>(); set => Set(value); }
        public string LyaseTitle { get => Get<string>(); set => Set(value); }
        public string IsomeraseTitle { get => Get<string>(); set => Set(value); }
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
            var dbJob = IncubatorJobCalculator.GetJob(this);
            var now = Clock.UtcNow;
            IsCompleteJobEnabled = IncubatorJobCalculator.IsJobComplete(dbJob, now);
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

                _session.DnaType = dbJob.BeastDNAType;
                IncubatorItemService.LoadJobStats(this, dbJob);

                if (now >= dbJob.DateCompleted)
                {
                    JobProgressTime = $"Stage {dbJob.CurrentStage} complete";
                    ChangePartialView(PartialElement, dbJob.CurrentStage >= IncubatorJobCalculator.NumberOfStages
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
            if (_session.IncubatorPropertyId != initialPayload.PropertyId && !IncubatorItemService.ReturnItemsHeldElsewhere(this))
            {
                FloatingTextStringOnCreature(HeldElsewhereMessage, Player, false);
                // Closed without the close handling, which would act on the other incubator's items.
                Gui.CloseWindow(Player, GuiWindowType.Incubator, Player);
                return;
            }

            _session.IncubatorPropertyId = initialPayload.PropertyId;
            _session.ShowResistances = false;

            DNAItemResref = IncubatorSession.Blank;
            HydrolaseItemResref = IncubatorSession.Blank;
            IsomeraseItemResref = IncubatorSession.Blank;
            LyaseItemResref = IncubatorSession.Blank;
            IsErraticGeniusChecked = false;
            JobProgress = 0f;
            JobProgressTime = string.Empty;

            var job = IncubatorJobCalculator.GetJob(this);
            IsMutationAllowed = job == null || !job.IsMutationSuppressed;

            _session.ClearStats();
            IncubatorItemService.RestoreHeldItems(this);
            LoadPlayerStats();
            SwitchViews();

            WatchOnClient(model => model.IsErraticGeniusChecked);
            WatchOnClient(model => model.IsMutationAllowed);
        }

        private void LoadPlayerStats()
        {
            var mutationBonus = IncubatorJobCalculator.GetErraticGeniusBonus(this);
            var job = IncubatorJobCalculator.GetJob(this);
            IsErraticGeniusEnabled = mutationBonus > 0 && job == null;
            ErraticGeniusTooltip = mutationBonus <= 0
                ? "Requires the Erratic Genius perk."
                : job == null
                    ? $"Adds {mutationBonus}% mutation chance when this job starts."
                    : "Erratic Genius only applies when a job starts.";

            if (!IsErraticGeniusEnabled)
                IsErraticGeniusChecked = false;

            IncubatorPanelService.RefreshIncubationTime(this);
        }

        private void RefreshButtons(IncubationJob job)
        {
            var enzymesReady = !string.IsNullOrWhiteSpace(_session.HydrolaseItem) &&
                               !string.IsNullOrWhiteSpace(_session.LyaseItem) &&
                               !string.IsNullOrWhiteSpace(_session.IsomeraseItem);

            IsStartJobEnabled = job == null && enzymesReady && !string.IsNullOrWhiteSpace(_session.DnaItem);
            IsContinueJobEnabled = job != null && enzymesReady;
            ContinueButtonText = job == null ? "Start stage 1" : $"Start stage {job.CurrentStage + 1}";
        }

        private void RefreshAllStats()
        {
            var job = IncubatorJobCalculator.GetJob(this);
            var now = Clock.UtcNow;
            var hasPendingStage = job == null || (now >= job.DateCompleted && job.CurrentStage < IncubatorJobCalculator.NumberOfStages);

            RefreshButtons(job);
            IncubatorPanelService.RefreshHeader(this, job);
            IncubatorPanelService.RefreshStageTracker(this, job, now);
            IncubatorPanelService.RefreshEnzymeTotals(this, job);
            IncubatorPanelService.RefreshPurities(this, hasPendingStage);
            IncubatorPanelService.RefreshMutation(this, job, hasPendingStage);
            IncubatorPanelService.RefreshRecipes(this, job, hasPendingStage);
            IncubatorPanelService.RefreshOutcomes(this, job, now);
        }

        public Action OnClickDNA() => IncubatorItemService.OnClickDNA(this);

        public Action OnClickHydrolase() => IncubatorItemService.OnClickEnzyme(this, "Hydrolase", BeastMasteryService.HydrolaseResrefPrefix,
            () => _session.HydrolaseItem,
            (serialized, color, icon, detail) =>
            {
                _session.HydrolaseItem = serialized;
                _session.HydrolaseColor = color;
                HydrolaseItemResref = icon;
                HydrolaseTitle = IncubatorItemService.EnzymeTitle("Hydrolase", color);
                HydrolaseDetail = detail;
            });

        public Action OnClickLyase() => IncubatorItemService.OnClickEnzyme(this, "Lyase", BeastMasteryService.LyaseResrefPrefix,
            () => _session.LyaseItem,
            (serialized, color, icon, detail) =>
            {
                _session.LyaseItem = serialized;
                _session.LyaseColor = color;
                LyaseItemResref = icon;
                LyaseTitle = IncubatorItemService.EnzymeTitle("Lyase", color);
                LyaseDetail = detail;
            });

        public Action OnClickIsomerase() => IncubatorItemService.OnClickEnzyme(this, "Isomerase", BeastMasteryService.IsomeraseResrefPrefix,
            () => _session.IsomeraseItem,
            (serialized, color, icon, detail) =>
            {
                _session.IsomeraseItem = serialized;
                _session.IsomeraseColor = color;
                IsomeraseItemResref = icon;
                IsomeraseTitle = IncubatorItemService.EnzymeTitle("Isomerase", color);
                IsomeraseDetail = detail;
            });

        public Action OnClickErraticGeniusToggled() => () =>
        {
            LoadPlayerStats();
            RefreshAllStats();
        };

        public Action OnClickAllowMutationToggled() => () =>
        {
            var job = IncubatorJobCalculator.GetJob(this);
            if (job != null)
            {
                job.IsMutationSuppressed = !IsMutationAllowed;
                DB.Set(job);
            }

            RefreshAllStats();
        };

        public Action OnClickToggleResistances() => () =>
        {
            _session.ShowResistances = !_session.ShowResistances;
            RefreshAllStats();
        };

        public Action OnClickStartJob() => IncubatorJobService.OnClickStartJob(this);

        public Action OnClickContinueJob() => IncubatorJobService.OnClickContinueJob(this);

        public Action OnClickCancelJob() => IncubatorJobService.OnClickCancelJob(this);

        public Action OnClickCompleteJob() => IncubatorJobService.OnClickCompleteJob(this);

        /// <summary>Runs whenever the window closes, whether the player closes it or the server does.</summary>
        public override Action OnWindowClosed() => () =>
        {
            if (!IncubatorItemService.RemoveDNA(this))
                SendMessageToPC(Player, ItemService.ItemsHeldMessage);
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

        IncubatorSession IIncubatorHost.Session => _session;
        uint IIncubatorHost.Player => Player;
        void IIncubatorHost.ShowModal(string prompt, Action confirmAction, Action cancelAction) => ShowModal(prompt, confirmAction, cancelAction);
        void IIncubatorHost.SwitchViews() => SwitchViews();
        void IIncubatorHost.RefreshAllStats() => RefreshAllStats();
        bool IIncubatorHost.IsMutationAllowed => IsMutationAllowed;
        bool IIncubatorHost.IsErraticGeniusChecked { get => IsErraticGeniusChecked; set => IsErraticGeniusChecked = value; }
        string IIncubatorHost.DNAItemResref { get => DNAItemResref; set => DNAItemResref = value; }
        string IIncubatorHost.HydrolaseItemResref { get => HydrolaseItemResref; set => HydrolaseItemResref = value; }
        string IIncubatorHost.IsomeraseItemResref { get => IsomeraseItemResref; set => IsomeraseItemResref = value; }
        string IIncubatorHost.LyaseItemResref { get => LyaseItemResref; set => LyaseItemResref = value; }
        string IIncubatorHost.DNASlotTooltip { get => DNASlotTooltip; set => DNASlotTooltip = value; }
        string IIncubatorHost.BeastName { get => BeastName; set => BeastName = value; }
        string IIncubatorHost.BeastSubtitle { get => BeastSubtitle; set => BeastSubtitle = value; }
        string IIncubatorHost.ServerDayText { get => ServerDayText; set => ServerDayText = value; }
        string IIncubatorHost.Stage1Title { get => Stage1Title; set => Stage1Title = value; }
        string IIncubatorHost.Stage2Title { get => Stage2Title; set => Stage2Title = value; }
        string IIncubatorHost.Stage3Title { get => Stage3Title; set => Stage3Title = value; }
        string IIncubatorHost.Stage1Detail { get => Stage1Detail; set => Stage1Detail = value; }
        string IIncubatorHost.Stage2Detail { get => Stage2Detail; set => Stage2Detail = value; }
        string IIncubatorHost.Stage3Detail { get => Stage3Detail; set => Stage3Detail = value; }
        string IIncubatorHost.EnzymeTotalsText { get => EnzymeTotalsText; set => EnzymeTotalsText = value; }
        string IIncubatorHost.EnzymeSectionTitle { get => EnzymeSectionTitle; set => EnzymeSectionTitle = value; }
        string IIncubatorHost.HydrolaseTitle { get => HydrolaseTitle; set => HydrolaseTitle = value; }
        string IIncubatorHost.LyaseTitle { get => LyaseTitle; set => LyaseTitle = value; }
        string IIncubatorHost.IsomeraseTitle { get => IsomeraseTitle; set => IsomeraseTitle = value; }
        string IIncubatorHost.HydrolaseDetail { get => HydrolaseDetail; set => HydrolaseDetail = value; }
        string IIncubatorHost.LyaseDetail { get => LyaseDetail; set => LyaseDetail = value; }
        string IIncubatorHost.IsomeraseDetail { get => IsomeraseDetail; set => IsomeraseDetail = value; }
        string IIncubatorHost.PurityHeader { get => PurityHeader; set => PurityHeader = value; }
        string IIncubatorHost.ResistanceToggleText { get => ResistanceToggleText; set => ResistanceToggleText = value; }
        string IIncubatorHost.MutationChanceText { get => MutationChanceText; set => MutationChanceText = value; }
        string IIncubatorHost.RecipeHeader { get => RecipeHeader; set => RecipeHeader = value; }
        string IIncubatorHost.UnknownRecipesText { get => UnknownRecipesText; set => UnknownRecipesText = value; }
        string IIncubatorHost.OutcomeHeader { get => OutcomeHeader; set => OutcomeHeader = value; }
        string IIncubatorHost.EstimatedTimeToCompletion { get => EstimatedTimeToCompletion; set => EstimatedTimeToCompletion = value; }
        bool IIncubatorHost.IsDNASlotEnabled { get => IsDNASlotEnabled; set => IsDNASlotEnabled = value; }
        GuiColor IIncubatorHost.Stage1Color { get => Stage1Color; set => Stage1Color = value; }
        GuiColor IIncubatorHost.Stage2Color { get => Stage2Color; set => Stage2Color = value; }
        GuiColor IIncubatorHost.Stage3Color { get => Stage3Color; set => Stage3Color = value; }
        GuiColor IIncubatorHost.MutationChanceColor { get => MutationChanceColor; set => MutationChanceColor = value; }
        GuiBindingList<string> IIncubatorHost.PurityNames { get => PurityNames; set => PurityNames = value; }
        GuiBindingList<string> IIncubatorHost.PurityTooltips { get => PurityTooltips; set => PurityTooltips = value; }
        GuiBindingList<string> IIncubatorHost.PurityTexts { get => PurityTexts; set => PurityTexts = value; }
        GuiBindingList<string> IIncubatorHost.RecipeLines { get => RecipeLines; set => RecipeLines = value; }
        GuiBindingList<string> IIncubatorHost.OutcomeNames { get => OutcomeNames; set => OutcomeNames = value; }
        GuiBindingList<string> IIncubatorHost.OutcomeChances { get => OutcomeChances; set => OutcomeChances = value; }
        GuiBindingList<GuiColor> IIncubatorHost.PurityNameColors { get => PurityNameColors; set => PurityNameColors = value; }
        GuiBindingList<GuiColor> IIncubatorHost.RecipeLineColors { get => RecipeLineColors; set => RecipeLineColors = value; }
        GuiBindingList<GuiColor> IIncubatorHost.OutcomeColors { get => OutcomeColors; set => OutcomeColors = value; }
        GuiBindingList<float> IIncubatorHost.PurityValues { get => PurityValues; set => PurityValues = value; }
    }
}
