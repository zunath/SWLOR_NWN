using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Gathering;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.ProspectingService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// Binds the Prospecting engine to its window. The engine owns every rule and number, and the lifecycle owns what a session means for
    /// the node, the charges, the loot and the XP; this class forwards clicks and shows what they report.
    /// </summary>
    public class ProspectingViewModel : CraftMinigameViewModel<ProspectingViewModel, ProspectingPayload>
    {
        public const string ContentElement = "prospecting_content";
        public const string NodePartial = "PROSPECTING_NODE";

        /// <summary>The number of action buttons: Survey, Strike, Heavy Strike, Extract and Leave.</summary>
        public const int ActionCount = 5;

        /// <summary>The rows that list the seams the player has found: a node holds at most this many.</summary>
        public const int SeamRows = ProspectingRules.MaxSeams;

        private static readonly string[] ActionIds =
        {
            ProspectingActionIds.Survey,
            ProspectingActionIds.Strike,
            ProspectingActionIds.HeavyStrike,
            ProspectingActionIds.Extract,
            ProspectingActionIds.Leave
        };

        private static readonly string[] ActionNames = { "Survey", "Strike", "Heavy Strike", "Extract", "Leave" };

        private ProspectingSession _session;

        // One entry per row of the style list: None first, then each style the harvester owns.
        private readonly List<GatheringStyle> _styleRows = new();

        protected override CraftMinigameType Minigame => CraftMinigameType.Prospecting;

        public string HeaderText { get => Get<string>(); set => Set(value); }
        public string StatusText { get => Get<string>(); set => Set(value); }
        public GuiColor StatusColor { get => Get<GuiColor>(); set => Set(value); }
        public string YieldText { get => Get<string>(); set => Set(value); }
        public string StabilityText { get => Get<string>(); set => Set(value); }
        public string ChargesText { get => Get<string>(); set => Set(value); }
        public string GatheredText { get => Get<string>(); set => Set(value); }
        public float YieldPercent { get => Get<float>(); set => Set(value); }
        public float StabilityPercent { get => Get<float>(); set => Set(value); }
        public float ChargesPercent { get => Get<float>(); set => Set(value); }
        public string QuickHarvestText { get => Get<string>(); set => Set(value); }
        public string QuickHarvestTooltip { get => Get<string>(); set => Set(value); }
        public bool IsQuickHarvestEnabled { get => Get<bool>(); set => Set(value); }
        public string CloseText { get => Get<string>(); set => Set(value); }
        public string StyleHeader { get => Get<string>(); set => Set(value); }
        public GuiBindingList<string> StyleLabels { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> StyleButtonTexts { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<bool> StyleButtonEnabled { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public string CoreSampleText { get => Get<string>(); set => Set(value); }

        public string SeamText0 => Get<string>();
        public string SeamText1 => Get<string>();
        public string SeamText2 => Get<string>();
        public GuiColor SeamColor0 => Get<GuiColor>();
        public GuiColor SeamColor1 => Get<GuiColor>();
        public GuiColor SeamColor2 => Get<GuiColor>();

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

        public static string SeamTextProperty(int index) => $"SeamText{index}";
        public static string SeamColorProperty(int index) => $"SeamColor{index}";
        public static string ActionTextProperty(int index) => $"ActionText{index}";
        public static string ActionSummaryProperty(int index) => $"ActionSummary{index}";
        public static string ActionTooltipProperty(int index) => $"ActionTooltip{index}";
        public static string ActionColorProperty(int index) => $"ActionColor{index}";
        public static string ActionEnabledProperty(int index) => $"ActionEnabled{index}";

        protected override void Initialize(ProspectingPayload initialPayload)
        {
            _session = initialPayload.Session;

            GatheringStyles.RemoveUnowned(Player);
            ClearDisplay();
            ChangePartialView(ContentElement, NodePartial);
            if (_session != null && ProspectingSessions.IsActive(_session))
                RefreshSession();
            else
                ShowEnd("This session is over.");

            OpenHelpOnFirstUse();
        }

        // Closing the window ends the session. This must not call the base method, which settles a craft saved from a station.
        public override Action OnWindowClosed() => () => ProspectingSessions.Abandon(_session, ProspectingEnd.WindowClosed);

        public Action OnClickAction(int index) => () => PerformAction(index);

        public Action OnClickQuickHarvest() => () => PerformQuickHarvest();

        public Action OnClickClose() => () => Gui.ClosePlayerWindow(Player, WindowType);

        public Action OnClickStyle() => () =>
        {
            var row = NuiGetEventArrayIndex();
            if (_session == null || _session.Settled || row < 0 || row >= _styleRows.Count)
                return;

            if (_session.HasActed || _session.Working != null)
            {
                SetStatus("Your style is locked once you have begun working the node.", GuiColor.Red);
                return;
            }

            GatheringStyles.Select(Player, _styleRows[row]);
            ProspectingSessions.RefreshEffects(_session, ProspectingEffects.FromStats(stat => Stat.GetStatAdjustment(Player, stat)));
            RefreshSession();
        };

        private void PerformAction(int index)
        {
            // A click queued behind the one that ended the session must not replace the result the window already shows.
            if (_session == null || _session.Settled)
                return;

            var actionId = ActionIds[index];
            var start = ProspectingSessions.StartAction(_session, actionId, OnResolved);
            if (!start.Started)
            {
                SetStatus(start.Reason, GuiColor.Red);
                if (!_session.Settled)
                    RefreshSession();

                return;
            }

            // Leave resolved at once; any other action is now taking its seconds.
            if (!_session.Settled && _session.Working != null)
                ShowWorking(ActionNames[index]);
        }

        private void PerformQuickHarvest()
        {
            if (_session == null || _session.Settled)
                return;

            var start = ProspectingSessions.StartQuickHarvest(_session, OnQuickHarvestProgress);
            if (!start.Started)
            {
                SetStatus(start.Reason, GuiColor.Red);
                if (!_session.Settled)
                    RefreshSession();

                return;
            }

            ShowWorking("Quick Harvest");
        }

        private void OnResolved(ProspectingActionReport report)
        {
            if (!report.Result?.Performed ?? true)
            {
                SetStatus(report.Message ?? string.Empty, GuiColor.Red);
                if (!_session.Settled)
                    RefreshSession();

                return;
            }

            var name = ActionNames[Array.IndexOf(ActionIds, report.ActionId)];
            var parts = new List<string> { $"{name}: {ProspectingActionText.OutcomeText(report.Result.Outcome)}" };
            if (report.ActionId == ProspectingActionIds.Survey)
            {
                parts.Add(report.SeamsFound switch
                {
                    0 => "You find no more seams.",
                    1 => "You find a seam.",
                    _ => $"You find {report.SeamsFound} seams."
                });
            }

            if (report.Delivered.Items > 0)
                parts.Add($"You gather {DeliveredText(report.Delivered)} (+{report.Xp} Gathering XP).");

            var gemShattered = report.ActionId == ProspectingActionIds.HeavyStrike && _session.Engine.Shattered > _shatteredBefore;
            if (gemShattered)
                parts.Add("A gem pocket shattered.");

            _shatteredBefore = _session.Engine.Shattered;
            SetStatus(string.Join(" ", parts), report.Ended ? ColorForEnding() : GuiColor.White);
            if (report.Ended)
                ShowEnd(report.Message);
            else
                RefreshSession();
        }

        private int _shatteredBefore;

        private void OnQuickHarvestProgress(QuickHarvestReport report)
        {
            SetStatus(
                report.Finished
                    ? $"{report.Message} You harvested {report.UnitsDone} of {report.UnitsTotal} units."
                    : $"Quick Harvest: {report.UnitsDone} of {report.UnitsTotal} units (+{report.Xp} Gathering XP).",
                report.Finished ? GuiColor.Green : GuiColor.White);

            var left = Math.Max(0, report.UnitsTotal - report.UnitsDone);
            YieldText = $"Yield: {Math.Max(0, _session.Engine.PoolAtStart - report.UnitsDone)} of {_session.Engine.PoolAtStart} units left";
            YieldPercent = Fraction(_session.Engine.PoolAtStart - report.UnitsDone, _session.Engine.PoolAtStart);
            ChargesText = ChargeText(_session.HarvesterCharges, _session.Engine.ChargesAtStart);
            ChargesPercent = Fraction(_session.HarvesterCharges, _session.Engine.ChargesAtStart);
            GatheredText = $"Gathered this session: {report.UnitsDone}. Gathering XP earned: {_session.XpPaid}.";
            if (report.Finished)
                ShowEnd(report.Message, keepStatus: true);
            else if (left > 0)
                QuickHarvestText = $"Harvesting {left} more {(left == 1 ? "unit" : "units")}...";
        }

        private void RefreshSession()
        {
            var engine = _session.Engine;
            var node = _session.Request;

            HeaderText = $"Working a tier {node.NodeTier} node (vein level {GatheringLevels.ForTier(node.NodeTier)})";
            YieldText = ProspectingActionText.MeterText("Yield", engine.Pool, engine.PoolAtStart, "units left; the last one spends the node");
            StabilityText = ProspectingActionText.MeterText(
                "Stability", engine.Stability, ProspectingRules.MaxStability, "the node collapses at 0");
            ChargesText = ChargeText(engine.Charges, engine.ChargesAtStart);
            YieldPercent = Fraction(engine.Pool, engine.PoolAtStart);
            StabilityPercent = Fraction(engine.Stability, ProspectingRules.MaxStability);
            ChargesPercent = Fraction(engine.Charges, engine.ChargesAtStart);
            GatheredText = $"Gathered this session: {engine.Items}. Gathering XP earned: {_session.XpPaid}.";
            CloseText = "Leave and close";

            var seams = engine.RevealedSeams;
            for (var row = 0; row < SeamRows; row++)
            {
                if (row < seams.Count)
                {
                    Set(ProspectingActionText.SeamText(seams[row]), SeamTextProperty(row));
                    Set(seams[row].Kind == ProspectingSeamKind.GemPocket ? new GuiColor(110, 200, 255) : new GuiColor(255, 190, 90), SeamColorProperty(row));
                }
                else
                {
                    Set(row == 0 && seams.Count == 0 ? "No seams found yet. A Survey looks for them." : string.Empty, SeamTextProperty(row));
                    Set(GuiColor.Grey, SeamColorProperty(row));
                }
            }

            var previews = engine.PreviewAll();
            for (var index = 0; index < ActionCount; index++)
            {
                var preview = previews[index];
                var view = ProspectingActionText.Describe(engine, preview);

                Set(preview.Action.Name, ActionTextProperty(index));
                Set(view.Summary, ActionSummaryProperty(index));
                Set(view.Tooltip, ActionTooltipProperty(index));
                Set(KindColor(view.Kind), ActionColorProperty(index));
                Set(preview.Enabled, ActionEnabledProperty(index));
            }

            RefreshStyles();
            CoreSampleText = CoreSampleSummary(engine);
            RefreshQuickHarvest();
        }

        /// <summary>The seams a harvester with Core Sample can see before finding them, or nothing for anyone else.</summary>
        private static string CoreSampleSummary(ProspectingEngine engine)
        {
            if (!engine.Setup.Effects.SeesSeamTypes)
                return string.Empty;

            var hidden = engine.SeenHiddenSeams;
            if (hidden.Count == 0)
                return "Core Sample: no seams are hidden in the node.";

            var rich = hidden.Count(kind => kind == ProspectingSeamKind.Rich);
            var pockets = hidden.Count - rich;
            var parts = new List<string>();
            if (rich > 0)
                parts.Add(rich == 1 ? "1 rich seam" : $"{rich} rich seams");

            if (pockets > 0)
                parts.Add(pockets == 1 ? "1 gem pocket" : $"{pockets} gem pockets");

            return $"Core Sample: {(hidden.Count == 1 ? "1 seam is" : $"{hidden.Count} seams are")} hidden in the node: {string.Join(" and ", parts)}.";
        }

        private void RefreshStyles()
        {
            _styleRows.Clear();
            var owned = GatheringStyles.Owned(Player).Where(style => GatheringStyles.ProspectingDescription(style).Length > 0).ToList();
            var active = GatheringStyles.Active(Player);
            if (owned.Count > 0 || active != GatheringStyle.None)
            {
                _styleRows.Add(GatheringStyle.None);
                _styleRows.AddRange(owned);
            }

            var locked = _session == null || _session.Settled || _session.HasActed || _session.Working != null;
            var labels = new GuiBindingList<string>();
            var buttons = new GuiBindingList<string>();
            var enabled = new GuiBindingList<bool>();
            foreach (var style in _styleRows)
            {
                labels.Add(style == GatheringStyle.None
                    ? "None: Prospecting plays by its usual rules."
                    : $"{GatheringStyles.Name(style)}: {GatheringStyles.ProspectingDescription(style)}");
                var selected = style == active || (style == GatheringStyle.None && !owned.Contains(active));
                buttons.Add(selected ? "Selected" : "Select");
                enabled.Add(!locked && !selected);
            }

            StyleHeader = _styleRows.Count == 0
                ? $"Styles: learn the Strip Mining style at Gathering {GatheringStyles.StyleRank} to choose it here."
                : locked
                    ? "Style: locked once you begin working the node."
                    : "Style: pick one before your first action. Only one can be active at a time.";
            StyleLabels = labels;
            StyleButtonTexts = buttons;
            StyleButtonEnabled = enabled;
        }

        private void RefreshQuickHarvest()
        {
            var node = _session.Engine.ToNode();
            var units = ProspectingQuickHarvest.Units(node, _session.HarvesterCharges);
            var enabled = !_session.Settled && !_session.HasActed && units > 0;
            var xp = ProspectingQuickHarvest.XpPerUnit(_session.Request.NodeTier, _session.Request.Rank);

            IsQuickHarvestEnabled = enabled;
            QuickHarvestText = _session.HasActed
                ? "Only offered before you begin working the node."
                : $"Takes the {units} {(units == 1 ? "unit" : "units")} left, no seams: {ProspectingRules.HarvestSeconds:0} seconds and 1 charge each, {xp} XP each.";
            QuickHarvestTooltip =
                "Harvests the node the way every harvest used to work: one unit, one charge and one delay at a time, with no seams and no choices.";
        }

        /// <summary>An action or a Quick Harvest is taking its seconds: nothing else can be clicked until it resolves.</summary>
        private void ShowWorking(string name)
        {
            SetStatus($"{name}...", GuiColor.White);
            IsQuickHarvestEnabled = false;
            RefreshStyles();
            for (var index = 0; index < ActionCount; index++)
                Set(false, ActionEnabledProperty(index));
        }

        /// <summary>Shows how the session ended: the actions stop, and the only thing left to do is close the window.</summary>
        private void ShowEnd(string message, bool keepStatus = false)
        {
            HeaderText = "Session over";
            CloseText = "Close";
            IsQuickHarvestEnabled = false;
            if (!keepStatus && !string.IsNullOrEmpty(message))
                StatusText = string.IsNullOrEmpty(StatusText) ? message : $"{StatusText} {message}";

            QuickHarvestText = string.Empty;
            RefreshStyles();
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
            HeaderText = "Prospecting";
            YieldText = string.Empty;
            StabilityText = string.Empty;
            ChargesText = string.Empty;
            GatheredText = string.Empty;
            YieldPercent = 0f;
            StabilityPercent = 0f;
            ChargesPercent = 0f;
            QuickHarvestText = string.Empty;
            QuickHarvestTooltip = string.Empty;
            IsQuickHarvestEnabled = false;
            CloseText = "Leave and close";
            SetStatus(string.Empty, GuiColor.White);
            _shatteredBefore = 0;
            StyleHeader = string.Empty;
            StyleLabels = new GuiBindingList<string>();
            StyleButtonTexts = new GuiBindingList<string>();
            StyleButtonEnabled = new GuiBindingList<bool>();
            CoreSampleText = string.Empty;

            for (var row = 0; row < SeamRows; row++)
            {
                Set(string.Empty, SeamTextProperty(row));
                Set(GuiColor.Grey, SeamColorProperty(row));
            }

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

        private GuiColor ColorForEnding() => _session.Finished == ProspectingEnding.Collapsed ? GuiColor.Red : GuiColor.Green;

        private static string ChargeText(int charges, int max) => $"Harvester charges: {charges} of {max} (each action spends some)";

        private static string DeliveredText(HarvestDelivery delivery)
        {
            var parts = new List<string>();
            if (delivery.Rolls > 0)
                parts.Add(delivery.Rolls == 1 ? "1 unit" : $"{delivery.Rolls} units");

            if (delivery.ExtraOreRolls > 0)
                parts.Add(delivery.ExtraOreRolls == 1 ? "1 extra ore roll" : $"{delivery.ExtraOreRolls} extra ore rolls");

            if (delivery.Gems > 0)
                parts.Add(delivery.Gems == 1 ? "1 gem" : $"{delivery.Gems} gems");

            return string.Join(", ", parts);
        }

        private static float Fraction(int value, int max) => max <= 0 ? 0f : (float)Math.Clamp(value, 0, max) / max;

        private static GuiColor KindColor(ProspectingActionKind kind) => kind switch
        {
            ProspectingActionKind.Collapses => GuiColor.Red,
            ProspectingActionKind.Ends => new GuiColor(255, 190, 90),
            ProspectingActionKind.Unavailable => GuiColor.Grey,
            _ => GuiColor.White
        };
    }
}
