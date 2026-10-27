using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Reaction;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// Binds the Reaction engine to its window. The engine owns every rule and number; this class starts the craft through the
    /// session service, forwards clicks, and shows what the engine and <see cref="ReactionDisplay"/> report.
    /// </summary>
    public class ReactionViewModel : CraftMinigameViewModel<ReactionViewModel, CraftPayload>
    {
        public const string ContentElement = "reaction_content";
        public const string SetupPartial = "REACTION_SETUP";
        public const string CraftPartial = "REACTION_CRAFT";

        /// <summary>The number of action buttons: Measure, Charge, Refine, Dampen and Stabilize.</summary>
        public const int ActionCount = 5;

        private static readonly string[] ActionIds =
        {
            ReactionEngine.Measure,
            ReactionEngine.Charge,
            ReactionEngine.Refine,
            ReactionEngine.Dampen,
            ReactionEngine.Stabilize
        };

        private static readonly GuiColor Amber = new(255, 200, 0);

        private RecipeType _recipe;
        private uint _blueprintItem;
        private CraftSession _session;
        private ReactionEngine _engine;

        protected override CraftMinigameType Minigame => CraftMinigameType.Reaction;

        public string RecipeName { get => Get<string>(); set => Set(value); }
        public string RecipeLevelText { get => Get<string>(); set => Set(value); }
        public string SkillText { get => Get<string>(); set => Set(value); }
        public string BeginText { get => Get<string>(); set => Set(value); }
        public string StatusText { get => Get<string>(); set => Set(value); }
        public GuiColor StatusColor { get => Get<GuiColor>(); set => Set(value); }

        public GuiBindingList<string> RecipeDescription { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<GuiColor> RecipeColors { get => Get<GuiBindingList<GuiColor>>(); set => Set(value); }

        public string PhaseNowText { get => Get<string>(); set => Set(value); }
        public string PhaseNextText { get => Get<string>(); set => Set(value); }
        public string PhaseThenText { get => Get<string>(); set => Set(value); }
        public GuiColor PhaseNowColor { get => Get<GuiColor>(); set => Set(value); }
        public GuiColor PhaseNextColor { get => Get<GuiColor>(); set => Set(value); }
        public GuiColor PhaseThenColor { get => Get<GuiColor>(); set => Set(value); }
        public string PhaseEffectText { get => Get<string>(); set => Set(value); }
        public string CycleText { get => Get<string>(); set => Set(value); }
        public string PeakText { get => Get<string>(); set => Set(value); }

        public string PotencyText { get => Get<string>(); set => Set(value); }
        public string SubtletyText { get => Get<string>(); set => Set(value); }
        public string StabilityText { get => Get<string>(); set => Set(value); }
        public string CpText { get => Get<string>(); set => Set(value); }
        public float PotencyPercent { get => Get<float>(); set => Set(value); }
        public float SubtletyPercent { get => Get<float>(); set => Set(value); }
        public float StabilityPercent { get => Get<float>(); set => Set(value); }
        public float CpPercent { get => Get<float>(); set => Set(value); }

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

        public static string ActionTextProperty(int index) => $"ActionText{index}";
        public static string ActionSummaryProperty(int index) => $"ActionSummary{index}";
        public static string ActionTooltipProperty(int index) => $"ActionTooltip{index}";
        public static string ActionColorProperty(int index) => $"ActionColor{index}";
        public static string ActionEnabledProperty(int index) => $"ActionEnabled{index}";

        /// <summary>The actions the engine has resolved, which hold the XP of a craft the window ends to the modeled pace.</summary>
        protected override int? CraftActionsTaken => _engine?.Turn;

        protected override void Initialize(CraftPayload initialPayload)
        {
            _recipe = initialPayload.Recipe;
            _blueprintItem = initialPayload.BlueprintItem;
            _session = null;
            _engine = null;

            ClearCraftDisplay();
            ShowSetup();
            StartMinigameSession();
        }

        public Action OnClickBegin() => () =>
        {
            if (_session != null)
                return;

            var recipe = Craft.GetRecipe(_recipe);
            ReactionSettings settings;
            try
            {
                settings = ReactionSettings.ForRecipe(recipe, GetEspionageRank());
            }
            catch (InvalidOperationException)
            {
                SetStatus("This recipe cannot be worked at the reaction bench.", GuiColor.Red);
                return;
            }

            var request = new CraftSessionStartRequest(_recipe, _blueprintItem, Array.Empty<uint>());
            var result = CraftSessionService.Start(Player, request);
            if (!result.Succeeded)
            {
                SetStatus(result.Message, GuiColor.Red);
                return;
            }

            _session = result.Session;
            _engine = new ReactionEngine(ServiceMinigameRandom.Instance, settings);
            SetStatus(string.Empty, GuiColor.White);
            ChangePartialView(ContentElement, CraftPartial);
            RefreshCraft();
        };

        public Action OnClickAction(int index) => () => PerformAction(index);

        public Action OnClickAbort() => () =>
        {
            if (_engine == null || _session == null)
                return;

            ShowModal(
                $"Stop the reaction? Each stack of components has a {CraftOutcomeRules.ChanceToLoseItemPercent}% chance to be lost, and the attempt earns {(int)(CraftOutcomeRules.FailureXpFraction * 100)}% of the base XP.",
                () =>
                {
                    var settlement = CraftSessionService.Fail(Player, CraftSessionEndReason.WindowClosed, _engine?.Turn);
                    EndAttempt(DescribeFailure("You stopped the reaction.", settlement), GuiColor.Red);
                },
                null,
                "Abort",
                "Keep working");
        };

        private void PerformAction(int index)
        {
            if (_engine == null || _session == null)
                return;

            if (!CraftSessionService.IsActive(Player, _session))
            {
                EndAttempt("Your craft was interrupted.", GuiColor.Red);
                return;
            }

            var actionId = ActionIds[index];
            var phase = _engine.CurrentPhase;
            var result = _engine.Perform(actionId);
            if (!result.Performed)
            {
                SetStatus(result.RejectedReason, GuiColor.Red);
                RefreshCraft();
                return;
            }

            switch (result.Status)
            {
                case MinigameStatus.Succeeded:
                    CompleteSuccess();
                    break;
                case MinigameStatus.Failed:
                    var settlement = CraftSessionService.Fail(Player, CraftSessionEndReason.Broken, _engine.Turn);
                    EndAttempt(DescribeFailure("The piece broke.", settlement), GuiColor.Red);
                    break;
                default:
                    var name = _engine.Actions.First(action => action.Id == actionId).Name;
                    var effect = ReactionDisplay.DescribeOutcome(result.Outcome, _engine.PhaseAt);
                    SetStatus($"{name} in {ReactionDisplay.PhaseName(phase)}: {effect}", GuiColor.White);
                    RefreshCraft();
                    break;
            }
        }

        private void CompleteSuccess()
        {
            var success = CraftSessionService.Succeed(Player, _session, _engine.Meters.Quality, _engine.Turn);
            if (success == null)
            {
                EndAttempt("Your craft was interrupted.", GuiColor.Red);
                return;
            }

            var recipe = Craft.GetRecipe(_recipe);
            var itemName = Cache.GetItemNameByResref(recipe.Resref);
            EndAttempt(
                $"Crafted {recipe.Quantity}x {itemName}. Quality {success.Outcome.QualityPercent}%, +{success.Outcome.Xp} XP.",
                GuiColor.Green);
        }

        private static string DescribeFailure(string headline, CraftSettlement settlement) =>
            settlement.Settled
                ? $"{headline} {settlement.ItemsLost} of {settlement.ItemsLost + settlement.ItemsKept} items were lost. +{settlement.Xp} XP."
                : headline;

        /// <summary>Ends the attempt and returns to the setup view with the outcome shown, ready for another craft.</summary>
        private void EndAttempt(string message, GuiColor color)
        {
            _engine = null;
            _session = null;
            ShowSetup();
            SetStatus(message, color);
        }

        private void ShowSetup()
        {
            var recipe = Craft.GetRecipe(_recipe);
            var blueprint = Craft.GetBlueprintDetails(_blueprintItem);
            var hasBlueprint = blueprint.Recipe != RecipeType.Invalid;
            var itemName = Cache.GetItemNameByResref(recipe.Resref);

            RecipeName = $"Recipe: {recipe.Quantity}x {itemName}";
            RecipeLevelText = $"Recipe level: {recipe.Level}";
            SkillText = $"Your Espionage rank: {GetEspionageRank()}";
            BeginText = hasBlueprint
                ? $"Begin [{Craft.CalculateBlueprintCraftCreditCost(_blueprintItem):N0}cr]"
                : "Begin";

            var (description, colors) = Craft.BuildRecipeDetail(Player, _recipe, blueprint);
            RecipeDescription = description;
            RecipeColors = colors;

            SetStatus(string.Empty, GuiColor.White);
            ChangePartialView(ContentElement, SetupPartial);
        }

        private void RefreshCraft()
        {
            var meters = _engine.Meters;
            PotencyText = ReactionDisplay.MeterText("Potency", meters.Progress);
            SubtletyText = ReactionDisplay.MeterText("Subtlety (quality)", meters.Quality);
            StabilityText = ReactionDisplay.MeterText("Stability", meters.Integrity);
            CpText = ReactionDisplay.MeterText("CP", meters.Cp);
            PotencyPercent = meters.Progress.Fraction;
            SubtletyPercent = meters.Quality.Fraction;
            StabilityPercent = meters.Integrity.Fraction;
            CpPercent = meters.Cp.Fraction;

            var upcoming = _engine.UpcomingPhases;
            PhaseNowText = $"NOW: {ReactionDisplay.PhaseName(upcoming[0])}";
            PhaseNextText = $"NEXT: {ReactionDisplay.PhaseName(upcoming[1])}";
            PhaseThenText = $"THEN: {ReactionDisplay.PhaseName(upcoming[2])}";
            PhaseNowColor = PhaseColor(upcoming[0]);
            PhaseNextColor = PhaseColor(upcoming[1]);
            PhaseThenColor = PhaseColor(upcoming[2]);
            PhaseEffectText = ReactionDisplay.PhaseEffect(upcoming[0]);
            CycleText = $"Cycle: {ReactionDisplay.CycleStrip(_engine.Cycle, _engine.CycleIndex)}";
            PeakText = ReactionDisplay.PeakText(_engine.Settings);

            var previews = _engine.PreviewAll();
            for (var index = 0; index < ActionCount; index++)
            {
                var preview = previews[index];
                var view = ReactionDisplay.DescribeAction(preview, upcoming[0], _engine.PhaseAt);
                var cost = preview.CpCost > 0 ? $"{preview.Action.Name} [{preview.CpCost} CP]" : preview.Action.Name;

                Set(cost, ActionTextProperty(index));
                Set(view.Summary, ActionSummaryProperty(index));
                Set(view.Tooltip, ActionTooltipProperty(index));
                Set(KindColor(view.Kind), ActionColorProperty(index));
                Set(preview.Enabled, ActionEnabledProperty(index));
            }
        }

        private void ClearCraftDisplay()
        {
            PhaseNowText = string.Empty;
            PhaseNextText = string.Empty;
            PhaseThenText = string.Empty;
            PhaseNowColor = GuiColor.White;
            PhaseNextColor = GuiColor.White;
            PhaseThenColor = GuiColor.White;
            PhaseEffectText = string.Empty;
            CycleText = string.Empty;
            PeakText = string.Empty;
            PotencyText = string.Empty;
            SubtletyText = string.Empty;
            StabilityText = string.Empty;
            CpText = string.Empty;
            PotencyPercent = 0f;
            SubtletyPercent = 0f;
            StabilityPercent = 0f;
            CpPercent = 0f;

            for (var index = 0; index < ActionCount; index++)
            {
                Set(string.Empty, ActionTextProperty(index));
                Set(string.Empty, ActionSummaryProperty(index));
                Set(string.Empty, ActionTooltipProperty(index));
                Set(GuiColor.White, ActionColorProperty(index));
                Set(false, ActionEnabledProperty(index));
            }
        }

        private void SetStatus(string text, GuiColor color)
        {
            StatusText = text;
            StatusColor = color;
        }

        private int GetEspionageRank()
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(Player));
            return dbPlayer != null && dbPlayer.Skills.TryGetValue(SkillType.Espionage, out var skill) ? skill.Rank : 0;
        }

        private static GuiColor PhaseColor(ReactionPhase phase) => phase switch
        {
            ReactionPhase.Calm => new GuiColor(80, 200, 120),
            ReactionPhase.Rising => Amber,
            ReactionPhase.Peak => new GuiColor(255, 70, 70),
            ReactionPhase.Settling => new GuiColor(110, 170, 255),
            _ => GuiColor.White
        };

        private static GuiColor KindColor(ReactionActionKind kind) => kind switch
        {
            ReactionActionKind.FinishesItem => GuiColor.Green,
            ReactionActionKind.BreaksItem => GuiColor.Red,
            ReactionActionKind.Unavailable => GuiColor.Grey,
            _ => GuiColor.White
        };
    }
}
