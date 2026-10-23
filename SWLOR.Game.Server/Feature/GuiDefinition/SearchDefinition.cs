using System.Linq.Expressions;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Gathering;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>The Search window: the board a scavenge site opens.</summary>
    public class SearchDefinition : IGuiWindowDefinition
    {
        private const float PanelWidth = 560f;
        private const float SpotWidth = 170f;
        private const float SpotHeight = 56f;
        private const float ActionButtonWidth = 130f;
        private const float ActionButtonHeight = 32f;
        private const float QuickSearchButtonWidth = 180f;
        private const float QuickSearchButtonHeight = 40f;
        private const float MeterLabelHeight = 20f;
        private const float MeterBarHeight = 18f;
        private const float StyleRowHeight = 40f;
        private const float StyleListHeight = 2 * StyleRowHeight + 8f;

        private static readonly string ActionSecondsText = SearchRules.ActionSeconds.ToString("0", System.Globalization.CultureInfo.InvariantCulture);

        private static readonly string HelpText =
            "TWO CHOICES\n" +
            "A scavenge site offers Quick Search or the board. Quick Search searches the whole site at once by the old rules: one " +
            "search, and a chance of a second that your Gathering rank sets, with the XP a search always paid. It has no Dig Deeper and " +
            "takes as long as the old search did. The board takes longer, pays more XP for your time, and lets skill find a little more. " +
            "Quick Search is only a choice before your first action.\n\n" +
            "THE BOARD\n" +
            "The ground is nine spots. Some of them are promising, and a Search finds several times as often on promising ground, but " +
            "you cannot see which. Your Gathering rank sets how often a Search of promising ground finds something. Every action " +
            "costs one search, and the board ends when they are spent. A site can be searched once, by one scavenger at a time.\n\n" +
            "ACTIONS\n" +
            "Click a spot, then an action:\n" +
            "- Search: searches the spot once. A spot can be searched only once.\n" +
            "- Dig Deeper: searches a spot you have already searched, again, for a rare item. Each dig risks ruining the spot, and the " +
            "risk grows with every dig there. A ruined spot gives nothing more.\n" +
            "- Sift: shows whether the spots next to the one you sift are promising. It finds nothing, but it tells you where to search.\n" +
            "Each button shows the exact chances before you click.\n\n" +
            "PERKS AND STYLES\n" +
            "The Treasure Seeker perk line never touches Quick Search. Most of its perks make a find, and above all a Dig Deeper find, favour " +
            "the site's rare items; the rest make a Search find a little more often and a Dig Deeper safer. At Gathering " + GatheringStyles.StyleRank +
            " it offers the Careful Hands style: Searches find a little less often, and a Dig Deeper favours rare items much more. Pick the style in " +
            "the list below before your first action. Only one style can be active at a time, and it stays until you pick another.\n\n" +
            "THE PACE\n" +
            "Your first action is free. After every action you wait " + ActionSecondsText + " seconds before the next one counts.\n\n" +
            "XP AND LEAVING\n" +
            "Every action pays Gathering XP: what the site pays when a Search or a Dig Deeper finds something, and a quarter of that when it finds nothing. A Sift finds nothing, so it pays the quarter. Closing this window, " +
            "moving away, entering combat or dying ends the visit: you keep what you found and lose the searches you did not spend, and " +
            "the site stays searched. What the site gave is put in its inventory when you finish.";

        private readonly GuiWindowBuilder<SearchViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.Search)
                .SetInitialGeometry(0, 0, 620f, 820f)
                .SetTitle("Search the Site")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .BindOnClosed(model => model.OnWindowClosed())
                .DefinePartialView(SearchViewModel.BoardPartial, AddBoard)
                .DefinePartialView(CraftMinigameHelp.PartialName, group =>
                    CraftMinigameHelpPartial.Build(group, "How Searching Works", HelpText, model => model.OnCloseHelp()));

            window.AddStandardLayout(layout => layout.SetContentPartialElement(SearchViewModel.ContentElement));

            return _builder.Build();
        }

        private static void AddBoard(GuiGroup<SearchViewModel> host)
        {
            host.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddGroup(panel =>
                    {
                        panel.SetShowBorder(false);
                        panel.SetScrollbars(NuiScrollbars.None);
                        panel.AddColumn(AddContent);
                    })
                        .SetWidth(PanelWidth);
                });
            });
        }

        private static void AddContent(GuiColumn<SearchViewModel> content)
        {
            content.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.HeaderText)
                    .SetHeight(24f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                    .SetVerticalAlign(NuiVerticalAlign.Middle);
                CraftMinigameHelpPartial.AddHelpButton(row, "How searching works", model => model.OnHelp());
            });
            content.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.StatusText)
                    .BindColor(model => model.StatusColor)
                    .SetHeight(24f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Center);
            });

            // Quick Search is the first choice, and the one the window encourages: the scavenger may take it at once or play the board.
            content.AddRow(row =>
            {
                row.AddButton()
                    .BindText(model => model.QuickSearchText)
                    .BindTooltip(model => model.QuickSearchTooltip)
                    .BindIsEnabled(model => model.QuickSearchEnabled)
                    .BindIsEncouraged(model => model.QuickSearchEnabled)
                    .BindOnClicked(model => model.OnClickQuickSearch())
                    .SetWidth(QuickSearchButtonWidth)
                    .SetHeight(QuickSearchButtonHeight);
                row.AddLabel()
                    .BindText(model => model.QuickSearchSummary)
                    .BindColor(model => model.QuickSearchColor)
                    .SetHeight(QuickSearchButtonHeight)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                    .SetVerticalAlign(NuiVerticalAlign.Middle);
            });
            content.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.StyleHeader)
                    .SetHeight(MeterLabelHeight)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });
            content.AddRow(row => row.AddList(template =>
                {
                    template.AddCell(cell => cell.AddText()
                        .BindText(model => model.StyleLabels)
                        .SetShowBorder(false)
                        .SetScrollbars(NuiScrollbars.None));
                    template.AddCell(cell =>
                    {
                        cell.SetWidth(90f);
                        cell.SetIsVariable(false);
                        cell.AddButton()
                            .BindText(model => model.StyleButtonTexts)
                            .BindIsEnabled(model => model.StyleButtonEnabled)
                            .BindOnClicked(model => model.OnClickStyle());
                    });
                })
                .BindRowCount(model => model.StyleLabels)
                .SetRowHeight(StyleRowHeight)
                .SetHeight(StyleListHeight));
            content.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.BoardLabelText)
                    .SetHeight(24f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });

            content.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.SearchesText)
                    .SetHeight(MeterLabelHeight)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                row.AddLabel()
                    .BindText(model => model.FindsText)
                    .SetHeight(MeterLabelHeight)
                    .SetHorizontalAlign(NuiHorizontalAlign.Right);
            });
            content.AddRow(row =>
            {
                row.AddProgressBar()
                    .BindValue(model => model.SearchesPercent)
                    .SetHeight(MeterBarHeight)
                    .SetColor(70, 130, 230);
            });

            // The ground: three rows of three wide text buttons, each coloured by what the scavenger knows of the spot.
            for (var boardRow = 0; boardRow < SearchBoard.Size; boardRow++)
            {
                var firstSpot = boardRow * SearchBoard.Size;
                content.AddRow(row =>
                {
                    row.AddSpacer();
                    for (var column = 0; column < SearchBoard.Size; column++)
                    {
                        var spot = firstSpot + column;
                        row.AddButton()
                            .BindText(Binding<string>(SearchViewModel.SpotTextProperty(spot)))
                            .BindTooltip(Binding<string>(SearchViewModel.SpotTooltipProperty(spot)))
                            .BindColor(Binding<GuiColor>(SearchViewModel.SpotColorProperty(spot)))
                            .BindOnClicked(model => model.OnClickSpot(spot))
                            .SetWidth(SpotWidth)
                            .SetHeight(SpotHeight);
                    }

                    row.AddSpacer();
                });
            }

            content.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.SelectedText)
                    .SetHeight(24f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });

            for (var action = 0; action < SearchViewModel.ActionCount; action++)
            {
                var index = action;
                content.AddRow(row =>
                {
                    row.AddButton()
                        .BindText(Binding<string>(SearchViewModel.ActionTextProperty(index)))
                        .BindTooltip(Binding<string>(SearchViewModel.ActionTooltipProperty(index)))
                        .BindIsEnabled(Binding<bool>(SearchViewModel.ActionEnabledProperty(index)))
                        .BindOnClicked(model => model.OnClickAction(index))
                        .SetWidth(ActionButtonWidth)
                        .SetHeight(ActionButtonHeight);
                    row.AddLabel()
                        .BindText(Binding<string>(SearchViewModel.ActionSummaryProperty(index)))
                        .BindColor(Binding<GuiColor>(SearchViewModel.ActionColorProperty(index)))
                        .SetHeight(ActionButtonHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                        .SetVerticalAlign(NuiVerticalAlign.Middle);
                });
            }

            content.AddRow(row =>
            {
                row.AddSpacer();
                row.AddButton()
                    .BindText(model => model.CloseText)
                    .SetTooltip("Closing the window ends the visit. You keep what you found and lose the searches you have not spent.")
                    .SetWidth(180f)
                    .SetHeight(34f)
                    .BindOnClicked(model => model.OnClickClose());
                row.AddSpacer();
            });
        }

        private static Expression<Func<SearchViewModel, TProperty>> Binding<TProperty>(string propertyName)
        {
            var model = Expression.Parameter(typeof(SearchViewModel), "model");
            var property = Expression.Property(model, propertyName);
            return Expression.Lambda<Func<SearchViewModel, TProperty>>(property, model);
        }
    }
}
