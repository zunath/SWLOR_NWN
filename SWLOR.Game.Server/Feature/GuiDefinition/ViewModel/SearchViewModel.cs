using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Gathering;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.ScavengeService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// Binds the Search engine to its window. The engine owns every rule and number, and the scavenge lifecycle owns what the visit means
    /// for the site, the XP and the loot; this class forwards clicks and shows what they report.
    /// </summary>
    public class SearchViewModel : CraftMinigameViewModel<SearchViewModel, SearchPayload>
    {
        public const string ContentElement = "search_content";
        public const string BoardPartial = "SEARCH_BOARD";

        /// <summary>The number of action buttons: Search, Dig Deeper and Sift.</summary>
        public const int ActionCount = 3;

        public const int SpotCount = SearchBoard.Spots;

        private const string BoardHeader = "Search the site";

        private static readonly string[] ActionIds =
        {
            SearchActionIds.Search,
            SearchActionIds.DigDeeper,
            SearchActionIds.Sift
        };

        private static readonly string[] ActionNames = { "Search", "Dig Deeper", "Sift" };

        private static readonly GuiColor UnknownColor = new(235, 235, 235);
        private static readonly GuiColor PromisingColor = new(255, 200, 60);
        private static readonly GuiColor OrdinaryColor = new(150, 175, 205);
        private static readonly GuiColor FoundColor = new(90, 220, 110);
        private static readonly GuiColor EmptyColor = new(150, 150, 150);
        private static readonly GuiColor SpoiledColor = new(230, 70, 70);
        private static readonly GuiColor RiskyColor = new(255, 165, 0);
        private static readonly GuiColor LastSearchColor = new(255, 120, 120);

        private ScavengeSession _session;
        private int _selected = -1;

        // One entry per row of the style list: None first, then each style the scavenger owns that does something in Search.
        private readonly List<GatheringStyle> _styleRows = new();

        protected override CraftMinigameType Minigame => CraftMinigameType.Search;

        public string HeaderText { get => Get<string>(); set => Set(value); }
        public string StatusText { get => Get<string>(); set => Set(value); }
        public GuiColor StatusColor { get => Get<GuiColor>(); set => Set(value); }
        public string QuickSearchText { get => Get<string>(); set => Set(value); }
        public string QuickSearchSummary { get => Get<string>(); set => Set(value); }
        public string QuickSearchTooltip { get => Get<string>(); set => Set(value); }
        public GuiColor QuickSearchColor { get => Get<GuiColor>(); set => Set(value); }
        public bool QuickSearchEnabled { get => Get<bool>(); set => Set(value); }
        public string BoardLabelText { get => Get<string>(); set => Set(value); }
        public string SearchesText { get => Get<string>(); set => Set(value); }
        public string FindsText { get => Get<string>(); set => Set(value); }
        public float SearchesPercent { get => Get<float>(); set => Set(value); }
        public string SelectedText { get => Get<string>(); set => Set(value); }
        public string CloseText { get => Get<string>(); set => Set(value); }
        public string StyleHeader { get => Get<string>(); set => Set(value); }
        public GuiBindingList<string> StyleLabels { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> StyleButtonTexts { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<bool> StyleButtonEnabled { get => Get<GuiBindingList<bool>>(); set => Set(value); }

        public string SpotText0 => Get<string>();
        public string SpotText1 => Get<string>();
        public string SpotText2 => Get<string>();
        public string SpotText3 => Get<string>();
        public string SpotText4 => Get<string>();
        public string SpotText5 => Get<string>();
        public string SpotText6 => Get<string>();
        public string SpotText7 => Get<string>();
        public string SpotText8 => Get<string>();
        public string SpotTooltip0 => Get<string>();
        public string SpotTooltip1 => Get<string>();
        public string SpotTooltip2 => Get<string>();
        public string SpotTooltip3 => Get<string>();
        public string SpotTooltip4 => Get<string>();
        public string SpotTooltip5 => Get<string>();
        public string SpotTooltip6 => Get<string>();
        public string SpotTooltip7 => Get<string>();
        public string SpotTooltip8 => Get<string>();
        public GuiColor SpotColor0 => Get<GuiColor>();
        public GuiColor SpotColor1 => Get<GuiColor>();
        public GuiColor SpotColor2 => Get<GuiColor>();
        public GuiColor SpotColor3 => Get<GuiColor>();
        public GuiColor SpotColor4 => Get<GuiColor>();
        public GuiColor SpotColor5 => Get<GuiColor>();
        public GuiColor SpotColor6 => Get<GuiColor>();
        public GuiColor SpotColor7 => Get<GuiColor>();
        public GuiColor SpotColor8 => Get<GuiColor>();

        public string ActionText0 => Get<string>();
        public string ActionText1 => Get<string>();
        public string ActionText2 => Get<string>();
        public string ActionSummary0 => Get<string>();
        public string ActionSummary1 => Get<string>();
        public string ActionSummary2 => Get<string>();
        public string ActionTooltip0 => Get<string>();
        public string ActionTooltip1 => Get<string>();
        public string ActionTooltip2 => Get<string>();
        public GuiColor ActionColor0 => Get<GuiColor>();
        public GuiColor ActionColor1 => Get<GuiColor>();
        public GuiColor ActionColor2 => Get<GuiColor>();
        public bool ActionEnabled0 => Get<bool>();
        public bool ActionEnabled1 => Get<bool>();
        public bool ActionEnabled2 => Get<bool>();

        public static string SpotTextProperty(int index) => $"SpotText{index}";
        public static string SpotTooltipProperty(int index) => $"SpotTooltip{index}";
        public static string SpotColorProperty(int index) => $"SpotColor{index}";
        public static string ActionTextProperty(int index) => $"ActionText{index}";
        public static string ActionSummaryProperty(int index) => $"ActionSummary{index}";
        public static string ActionTooltipProperty(int index) => $"ActionTooltip{index}";
        public static string ActionColorProperty(int index) => $"ActionColor{index}";
        public static string ActionEnabledProperty(int index) => $"ActionEnabled{index}";

        protected override void Initialize(SearchPayload initialPayload)
        {
            _session = initialPayload.Session;
            _selected = -1;

            // The record holds the selected style; a death, a login or a trip to space may have cost the scavenger its effect.
            // The session read its effects before this window opened, so they are read again while the board is untouched.
            CraftingStyleSelection.Restore(Player);
            GatheringStyles.RemoveUnowned(Player);
            if (_session != null && ScavengeSessions.IsActive(_session))
                RefreshSessionEffects();
            ClearDisplay();
            ChangePartialView(ContentElement, BoardPartial);
            if (_session != null && ScavengeSessions.IsActive(_session))
                RefreshBoard();
            else
                ShowEnd(_session?.Result?.Message ?? "The search is over.");

            OpenHelpOnFirstUse();
        }

        // Closing the window ends the visit, keeping what was found. This must not call the base method, which settles a craft saved from a station.
        public override Action OnWindowClosed() => () => ScavengeSessions.Abandon(_session, ScavengeEnd.WindowClosed);

        public Action OnClickSpot(int spot) => () => SelectSpot(spot);

        public Action OnClickAction(int index) => () => PerformAction(index);

        public Action OnClickQuickSearch() => () => PerformQuickSearch();

        public Action OnClickClose() => () => Gui.ClosePlayerWindow(Player, WindowType);

        public Action OnClickStyle() => () =>
        {
            var row = NuiGetEventArrayIndex();
            if (_session == null || _session.Result != null || row < 0 || row >= _styleRows.Count)
                return;

            if (!ScavengeSessions.CanQuickSearch(_session))
            {
                SetStatus("Your style is locked once you have begun searching.", GuiColor.Red);
                return;
            }

            GatheringStyles.Select(Player, _styleRows[row]);
            RefreshSessionEffects();
            RefreshBoard();
        };

        private void RefreshSessionEffects() =>
            ScavengeSessions.RefreshEffects(_session, SearchEffects.FromStats(stat => Stat.GetStatAdjustment(Player, stat)));

        private void SelectSpot(int spot)
        {
            if (_session == null || _session.Result != null)
                return;

            _selected = spot;
            RefreshBoard();
        }

        private void PerformQuickSearch()
        {
            // A click queued behind the one that ended the visit must not replace the result the window already shows.
            if (_session == null || _session.Result != null)
                return;

            if (!ScavengeSessions.Revalidate(_session))
            {
                ShowEnd(_session.Result?.Message ?? "The search is over.");
                return;
            }

            var result = ScavengeSessions.QuickSearch(_session);
            if (result == null)
            {
                SetStatus("Quick Search is only a choice before your first action.", GuiColor.Red);
                RefreshBoard();
                return;
            }

            ShowEnd(result.Message);
        }

        private void PerformAction(int index)
        {
            // A click queued behind the one that ended the visit must not replace the result the window already shows.
            if (_session == null || _session.Result != null)
                return;

            if (_selected < 0)
            {
                SetStatus("Choose a spot on the board first.", GuiColor.Red);
                return;
            }

            var step = ScavengeSessions.Act(_session, SearchActionIds.For(ActionIds[index], _selected));
            if (!step.Performed)
            {
                if (_session.Result != null)
                {
                    ShowEnd(_session.Result.Message);
                    return;
                }

                SetStatus(step.Message, GuiColor.Red);
                RefreshBoard();
                return;
            }

            // Dig Deeper plays the dig clip; Search and Sift play the search clip.
            var gathering = ActionIds[index] == SearchActionIds.DigDeeper ? GatheringAction.Dig : GatheringAction.Search;
            NamedAnimation.Play(Player, WorkAnimation.ForGathering(gathering));

            var engine = _session.Engine;
            var text = SearchActionText.EventText(engine, engine.Last);
            if (step.Result != null)
            {
                ShowEnd($"{text} {step.Result.Message}");
                return;
            }

            SetStatus($"{text} Your next action is ready in {SearchRules.ActionSeconds:0} seconds.", StatusColorFor(engine.Last));
            RefreshBoard();
        }

        private void RefreshStyles()
        {
            _styleRows.Clear();
            var owned = GatheringStyles.Owned(Player).Where(style => GatheringStyles.SearchDescription(style).Length > 0).ToList();
            var active = GatheringStyles.Active(Player);
            if (owned.Count > 0 || active != GatheringStyle.None)
            {
                _styleRows.Add(GatheringStyle.None);
                _styleRows.AddRange(owned);
            }

            var locked = _session == null || _session.Result != null || !ScavengeSessions.CanQuickSearch(_session);
            var labels = new GuiBindingList<string>();
            var buttons = new GuiBindingList<string>();
            var enabled = new GuiBindingList<bool>();
            foreach (var style in _styleRows)
            {
                var selected = style == active || (style == GatheringStyle.None && !owned.Contains(active));
                labels.Add(style == GatheringStyle.None
                    ? "None: Search plays by its usual rules."
                    : $"{GatheringStyles.Name(style)}: {GatheringStyles.SearchDescription(style)}");
                buttons.Add(selected ? "Selected" : "Select");
                enabled.Add(!locked && !selected);
            }

            StyleHeader = _styleRows.Count == 0
                ? $"Styles: learn the Careful Hands style at Gathering {GatheringStyles.StyleRank} to choose it here."
                : locked
                    ? "Style: locked once you begin searching."
                    : "Style: pick one before your first action. Only one can be active at a time.";
            StyleLabels = labels;
            StyleButtonTexts = buttons;
            StyleButtonEnabled = enabled;
        }

        private void RefreshBoard()
        {
            var engine = _session.Engine;
            HeaderText = BoardHeader;
            RefreshStyles();
            SearchesText = SearchActionText.SearchesText(engine);
            FindsText = $"Finds: {engine.Finds}";
            SearchesPercent = engine.Setup.Searches <= 0 ? 0f : (float)engine.SearchesSpent / engine.Setup.Searches;
            CloseText = "Leave the site";

            var canQuick = ScavengeSessions.CanQuickSearch(_session);
            QuickSearchText = "Quick Search";
            QuickSearchEnabled = canQuick;
            if (canQuick)
            {
                var hardLook = CraftRankMilestones.HardLookLevelForRank(engine.Setup.Rank);
                QuickSearchSummary =
                    $"Search it all at once the old way: one search and a {ScavengeQuickSearch.SecondAttemptChance(hardLook)}% chance of a second. " +
                    "No board, and the XP a search always paid.";
                QuickSearchColor = GuiColor.Green;
                QuickSearchTooltip =
                    "Resolves the site by the old rules, with no Dig Deeper, taking as long as the old search did. You can only choose it before your first action.";
                BoardLabelText = $"Or play the board for more XP; skill can find a little more ({engine.Setup.Searches} searches, {engine.Setup.PromisingSpots} promising spots):";
            }
            else
            {
                QuickSearchSummary = "Not a choice once the board has begun.";
                QuickSearchColor = GuiColor.Grey;
                QuickSearchTooltip = "Quick Search is only a choice before your first action.";
                BoardLabelText = $"The board ({engine.Setup.Searches} searches, {engine.Setup.PromisingSpots} promising spots):";
            }

            for (var spot = 0; spot < SpotCount; spot++)
            {
                var text = SearchActionText.SpotButtonText(engine, spot);
                Set(spot == _selected ? "> " + text.Replace("\n", " <\n") : text, SpotTextProperty(spot));
                Set(SearchActionText.SpotTooltip(engine, spot), SpotTooltipProperty(spot));
                Set(SpotColorFor(engine, spot), SpotColorProperty(spot));
            }

            if (_selected < 0)
            {
                SelectedText = "Choose a spot, then an action.";
                for (var index = 0; index < ActionCount; index++)
                    SetActionRow(index, ActionNames[index], "Choose a spot on the board.", string.Empty, GuiColor.Grey, false);

                return;
            }

            SelectedText = $"{SearchActionText.SpotTitle(_selected)}: {SearchActionText.GroundText(engine.Knowledge(_selected))}, {SearchActionText.SpotState(engine, _selected)}.";
            for (var index = 0; index < ActionCount; index++)
            {
                var view = SearchActionText.Describe(engine, _selected, ActionIds[index]);
                SetActionRow(index, ActionNames[index], view.Summary, view.Tooltip, KindColor(view.Kind), view.Kind != SearchActionKind.Unavailable);
            }
        }

        /// <summary>Shows how the visit ended: the actions stop, and the only thing left to do is close the window.</summary>
        private void ShowEnd(string message)
        {
            HeaderText = "Search finished";
            CloseText = "Close";
            QuickSearchText = "Quick Search";
            QuickSearchSummary = string.Empty;
            QuickSearchTooltip = string.Empty;
            QuickSearchColor = GuiColor.Grey;
            QuickSearchEnabled = false;
            BoardLabelText = string.Empty;
            SelectedText = string.Empty;
            RefreshStyles();
            SetStatus(message, GuiColor.Green);

            for (var index = 0; index < ActionCount; index++)
                SetActionRow(index, ActionNames[index], string.Empty, string.Empty, GuiColor.Grey, false);
        }

        private void ClearDisplay()
        {
            HeaderText = BoardHeader;
            QuickSearchText = "Quick Search";
            QuickSearchSummary = string.Empty;
            QuickSearchTooltip = string.Empty;
            QuickSearchColor = GuiColor.White;
            QuickSearchEnabled = false;
            BoardLabelText = string.Empty;
            SearchesText = string.Empty;
            FindsText = string.Empty;
            SearchesPercent = 0f;
            SelectedText = string.Empty;
            CloseText = "Leave the site";
            SetStatus(string.Empty, GuiColor.White);
            StyleHeader = string.Empty;
            StyleLabels = new GuiBindingList<string>();
            StyleButtonTexts = new GuiBindingList<string>();
            StyleButtonEnabled = new GuiBindingList<bool>();

            for (var spot = 0; spot < SpotCount; spot++)
            {
                Set(SearchActionText.SpotTitle(spot), SpotTextProperty(spot));
                Set(string.Empty, SpotTooltipProperty(spot));
                Set(UnknownColor, SpotColorProperty(spot));
            }

            for (var index = 0; index < ActionCount; index++)
                SetActionRow(index, ActionNames[index], string.Empty, string.Empty, GuiColor.White, false);
        }

        private void SetActionRow(int index, string text, string summary, string tooltip, GuiColor color, bool enabled)
        {
            Set(text, ActionTextProperty(index));
            Set(summary, ActionSummaryProperty(index));
            Set(tooltip, ActionTooltipProperty(index));
            Set(color, ActionColorProperty(index));
            Set(enabled, ActionEnabledProperty(index));
        }

        private void SetStatus(string text, GuiColor color)
        {
            StatusText = text;
            StatusColor = color;
        }

        private static GuiColor SpotColorFor(SearchEngine engine, int spot)
        {
            if (engine.IsSpoiled(spot))
                return SpoiledColor;
            if (engine.IsSearched(spot))
                return engine.HasFound(spot) ? FoundColor : EmptyColor;

            return engine.Knowledge(spot) switch
            {
                SearchSpotKnowledge.Promising => PromisingColor,
                SearchSpotKnowledge.Ordinary => OrdinaryColor,
                _ => UnknownColor
            };
        }

        private static GuiColor StatusColorFor(SearchEvent last) => last.Result switch
        {
            SearchEventResult.Found => GuiColor.Green,
            SearchEventResult.Spoiled => GuiColor.Red,
            _ => GuiColor.White
        };

        private static GuiColor KindColor(SearchActionKind kind) => kind switch
        {
            SearchActionKind.Risky => RiskyColor,
            SearchActionKind.LastSearch => LastSearchColor,
            SearchActionKind.Unavailable => GuiColor.Grey,
            _ => GuiColor.White
        };
    }
}
