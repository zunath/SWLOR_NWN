using System.Linq;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
using SWLOR.Game.Server.Service.FishingService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// Binds the Line engine to its window. The engine owns every rule and number, and the fishing lifecycle owns what the fight
    /// means for the bait, the XP and the fish; this class forwards clicks and shows what they report.
    /// </summary>
    public class LineViewModel : CraftMinigameViewModel<LineViewModel, LinePayload>
    {
        public const string ContentElement = "line_content";
        public const string FightPartial = "LINE_FIGHT";

        /// <summary>The number of action buttons: Set Hook, Reel, Give Line and Hold.</summary>
        public const int ActionCount = 4;

        private const string FightHeader = "Something is on the line!";

        private static readonly string[] ActionIds =
        {
            LineActionIds.SetHook,
            LineActionIds.Reel,
            LineActionIds.GiveLine,
            LineActionIds.Hold
        };

        private static readonly string[] ActionNames = { "Set Hook", "Reel", "Give Line", "Hold" };

        private FishingFight _fight;

        protected override CraftMinigameType Minigame => CraftMinigameType.Line;

        public string HeaderText { get => Get<string>(); set => Set(value); }
        public string StatusText { get => Get<string>(); set => Set(value); }
        public GuiColor StatusColor { get => Get<GuiColor>(); set => Set(value); }
        public string MoveNowText { get => Get<string>(); set => Set(value); }
        public GuiColor MoveNowColor { get => Get<GuiColor>(); set => Set(value); }
        public string MoveHintText { get => Get<string>(); set => Set(value); }
        public string MoveThenText { get => Get<string>(); set => Set(value); }
        public string DistanceText { get => Get<string>(); set => Set(value); }
        public string TensionText { get => Get<string>(); set => Set(value); }
        public string StaminaText { get => Get<string>(); set => Set(value); }
        public float DistancePercent { get => Get<float>(); set => Set(value); }
        public float TensionPercent { get => Get<float>(); set => Set(value); }
        public float StaminaPercent { get => Get<float>(); set => Set(value); }
        public string CloseText { get => Get<string>(); set => Set(value); }

        public string ActionText0 => Get<string>();
        public string ActionText1 => Get<string>();
        public string ActionText2 => Get<string>();
        public string ActionText3 => Get<string>();
        public string ActionSummary0 => Get<string>();
        public string ActionSummary1 => Get<string>();
        public string ActionSummary2 => Get<string>();
        public string ActionSummary3 => Get<string>();
        public string ActionTooltip0 => Get<string>();
        public string ActionTooltip1 => Get<string>();
        public string ActionTooltip2 => Get<string>();
        public string ActionTooltip3 => Get<string>();
        public GuiColor ActionColor0 => Get<GuiColor>();
        public GuiColor ActionColor1 => Get<GuiColor>();
        public GuiColor ActionColor2 => Get<GuiColor>();
        public GuiColor ActionColor3 => Get<GuiColor>();
        public bool ActionEnabled0 => Get<bool>();
        public bool ActionEnabled1 => Get<bool>();
        public bool ActionEnabled2 => Get<bool>();
        public bool ActionEnabled3 => Get<bool>();

        public static string ActionTextProperty(int index) => $"ActionText{index}";
        public static string ActionSummaryProperty(int index) => $"ActionSummary{index}";
        public static string ActionTooltipProperty(int index) => $"ActionTooltip{index}";
        public static string ActionColorProperty(int index) => $"ActionColor{index}";
        public static string ActionEnabledProperty(int index) => $"ActionEnabled{index}";

        protected override void Initialize(LinePayload initialPayload)
        {
            _fight = initialPayload.Fight;

            ClearDisplay();
            ChangePartialView(ContentElement, FightPartial);
            if (_fight != null && FishingFights.IsActive(_fight))
                RefreshFight();
            else
                ShowEnd("The fish got away.", LineEnding.Escaped);

            OpenHelpOnFirstUse();
        }

        // Closing the window lets the fish go. This must not call the base method, which settles a craft saved from a station.
        public override Action OnWindowClosed() => () => FishingFights.Abandon(_fight, FishingFightEnd.WindowClosed);

        public Action OnClickAction(int index) => () => PerformAction(index);

        public Action OnClickClose() => () => Gui.ClosePlayerWindow(Player, WindowType);

        private void PerformAction(int index)
        {
            if (_fight == null)
                return;

            if (!FishingFights.IsActive(_fight))
            {
                ShowEnd(FishingFightLifecycle.EscapedMessage, LineEnding.Escaped);
                return;
            }

            var engine = _fight.Engine;
            var actionId = ActionIds[index];
            var name = engine.Actions.First(action => action.Id == actionId).Name;
            var move = engine.Move;
            var result = engine.Perform(actionId);
            if (!result.Performed)
            {
                SetStatus(result.RejectedReason, GuiColor.Red);
                RefreshFight();
                return;
            }

            if (result.Status != MinigameStatus.InProgress)
            {
                var settled = FishingFights.Settle(_fight);
                ShowEnd(settled?.Message ?? string.Empty, engine.Ending);
                return;
            }

            var effect = LineActionText.OutcomeText(engine, result.Outcome);
            SetStatus(
                actionId == LineActionIds.SetHook
                    ? $"{name}: {effect}"
                    : $"{name} against a {LineActionText.MoveName(move).ToLowerInvariant()}: {effect}",
                GuiColor.White);
            RefreshFight();
        }

        private void RefreshFight()
        {
            var engine = _fight.Engine;
            var setup = engine.Setup;
            var forecast = engine.Forecast;

            HeaderText = FightHeader;
            MoveNowText = $"The fish will {LineActionText.MoveName(forecast[0])}";
            MoveNowColor = MoveColor(forecast[0]);
            MoveHintText = LineActionText.MoveHint(setup, forecast[0]);
            MoveThenText = forecast.Count > 1 ? $"Then: {LineActionText.MoveName(forecast[1])}" : string.Empty;

            DistanceText = LineActionText.MeterText(
                "Distance", engine.Distance, setup.DistanceMax, $"0 lands the fish, {setup.DistanceMax} lets it escape");
            TensionText = LineActionText.MeterText(
                "Tension", engine.Tension, engine.TensionCap, $"the line snaps at {engine.TensionCap}");
            StaminaText = LineActionText.MeterText("Fish Stamina", engine.Stamina, engine.StaminaMax, "an exhausted fish only rests");
            DistancePercent = Fraction(engine.Distance, setup.DistanceMax);
            TensionPercent = Fraction(engine.Tension, engine.TensionCap);
            StaminaPercent = Fraction(engine.Stamina, engine.StaminaMax);
            CloseText = "Let the fish go";

            var previews = engine.PreviewAll();
            for (var index = 0; index < ActionCount; index++)
            {
                var preview = previews[index];
                var view = LineActionText.Describe(engine, preview);

                Set(preview.Action.Name, ActionTextProperty(index));
                Set(view.Summary, ActionSummaryProperty(index));
                Set(view.Tooltip, ActionTooltipProperty(index));
                Set(KindColor(view.Kind), ActionColorProperty(index));
                Set(preview.Enabled, ActionEnabledProperty(index));
            }
        }

        /// <summary>Shows how the fight ended: the actions stop, and the only thing left to do is close the window.</summary>
        private void ShowEnd(string message, LineEnding ending)
        {
            HeaderText = ending switch
            {
                LineEnding.Landed => "You landed it!",
                LineEnding.Snapped => "The line snapped.",
                _ => "It got away."
            };
            MoveNowText = string.Empty;
            MoveHintText = string.Empty;
            MoveThenText = string.Empty;
            CloseText = "Close";
            SetStatus(message, ending == LineEnding.Landed ? GuiColor.Green : GuiColor.Red);

            for (var index = 0; index < ActionCount; index++)
            {
                Set(ActionNames[index], ActionTextProperty(index));
                Set(string.Empty, ActionSummaryProperty(index));
                Set(string.Empty, ActionTooltipProperty(index));
                Set(GuiColor.Grey, ActionColorProperty(index));
                Set(false, ActionEnabledProperty(index));
            }
        }

        private void ClearDisplay()
        {
            HeaderText = FightHeader;
            MoveNowText = string.Empty;
            MoveNowColor = GuiColor.White;
            MoveHintText = string.Empty;
            MoveThenText = string.Empty;
            DistanceText = string.Empty;
            TensionText = string.Empty;
            StaminaText = string.Empty;
            DistancePercent = 0f;
            TensionPercent = 0f;
            StaminaPercent = 0f;
            CloseText = "Let the fish go";
            SetStatus(string.Empty, GuiColor.White);

            for (var index = 0; index < ActionCount; index++)
            {
                Set(ActionNames[index], ActionTextProperty(index));
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

        private static float Fraction(int value, int max) => max <= 0 ? 0f : (float)value / max;

        private static GuiColor MoveColor(FishMove move) => move switch
        {
            FishMove.Run => new GuiColor(255, 165, 0),
            FishMove.Dive => new GuiColor(110, 170, 255),
            FishMove.Thrash => new GuiColor(255, 70, 70),
            _ => new GuiColor(80, 200, 120)
        };

        private static GuiColor KindColor(LineActionKind kind) => kind switch
        {
            LineActionKind.LandsFish => GuiColor.Green,
            LineActionKind.LosesFish => GuiColor.Red,
            LineActionKind.Unavailable => GuiColor.Grey,
            _ => GuiColor.White
        };
    }
}
