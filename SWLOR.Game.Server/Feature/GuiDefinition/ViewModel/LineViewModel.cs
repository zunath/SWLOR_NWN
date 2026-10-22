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
        public string QuickReelText { get => Get<string>(); set => Set(value); }
        public string QuickReelSummary { get => Get<string>(); set => Set(value); }
        public string QuickReelTooltip { get => Get<string>(); set => Set(value); }
        public GuiColor QuickReelColor { get => Get<GuiColor>(); set => Set(value); }
        public bool QuickReelEnabled { get => Get<bool>(); set => Set(value); }
        public string FightLabelText { get => Get<string>(); set => Set(value); }

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

        public Action OnClickQuickReel() => () => PerformQuickReel();

        public Action OnClickClose() => () => Gui.ClosePlayerWindow(Player, WindowType);

        private void PerformQuickReel()
        {
            // A click queued behind the one that ended the fight must not replace the result the window already shows.
            if (_fight == null || _fight.Result != null)
                return;

            if (!FishingFights.Revalidate(_fight))
            {
                ShowEnd(FishingFightLifecycle.EscapedMessage, LineEnding.Escaped);
                return;
            }

            var result = FishingFights.QuickReel(_fight);
            if (result == null)
            {
                SetStatus("Quick Reel is only a choice before your first move.", GuiColor.Red);
                RefreshFight();
                return;
            }

            ShowEnd(result.Message, result.Ending);
        }

        private void PerformAction(int index)
        {
            // A click queued behind the one that ended the fight must not replace the result the window already shows.
            if (_fight == null || _fight.Result != null)
                return;

            if (!FishingFights.Revalidate(_fight))
            {
                ShowEnd(FishingFightLifecycle.EscapedMessage, LineEnding.Escaped);
                return;
            }

            // The turns keep the pace the XP multipliers assume, however fast the client sends clicks.
            var wait = FishingFights.SecondsUntilNextTurn(_fight);
            if (wait > 0)
            {
                SetStatus(
                    $"The fish is still moving. Wait {(int)Math.Ceiling(wait)} more {((int)Math.Ceiling(wait) == 1 ? "second" : "seconds")} before your next move.",
                    GuiColor.Red);
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

            FishingFights.TurnTaken(_fight);

            if (result.Status != MinigameStatus.InProgress)
            {
                var settled = FishingFights.Settle(_fight);
                ShowEnd(settled?.Message ?? string.Empty, engine.Ending);
                return;
            }

            var effect = LineActionText.OutcomeText(engine, result.Outcome);
            SetStatus(
                (actionId == LineActionIds.SetHook
                    ? $"{name}: {effect}"
                    : $"{name} against a {LineActionText.MoveName(move).ToLowerInvariant()}: {effect}") +
                $" Your next move is ready in {LineRules.TurnSeconds:0} seconds.",
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

            var canQuickReel = FishingFights.CanQuickReel(_fight);
            QuickReelText = "Quick Reel";
            QuickReelEnabled = canQuickReel;
            if (canQuickReel)
            {
                QuickReelSummary =
                    $"Reel it in at once: {FishingFights.QuickReelChance(_fight)}% to land it. No fight, and your XP is as it always was.";
                QuickReelColor = GuiColor.Green;
                QuickReelTooltip =
                    "Lands the fish by the old catch roll (your Agriculture rank against the fish's level), with no fight. " +
                    "You can only choose it before your first move.";
                FightLabelText = "Or fight it for more XP; skill can land more fish:";
            }
            else
            {
                QuickReelSummary = "Not a choice once the fight has begun.";
                QuickReelColor = GuiColor.Grey;
                QuickReelTooltip = "Quick Reel is only a choice before your first move.";
                FightLabelText = "Actions";
            }

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
            QuickReelText = "Quick Reel";
            QuickReelSummary = string.Empty;
            QuickReelTooltip = string.Empty;
            QuickReelColor = GuiColor.Grey;
            QuickReelEnabled = false;
            FightLabelText = string.Empty;
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
            QuickReelText = "Quick Reel";
            QuickReelSummary = string.Empty;
            QuickReelTooltip = string.Empty;
            QuickReelColor = GuiColor.White;
            QuickReelEnabled = false;
            FightLabelText = "Actions";
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
