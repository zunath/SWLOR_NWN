using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>The audience tracker: who is performing in range, how much Inspired is banked, and what the watcher leaves with.</summary>
    public class PerformanceWatchingDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<PerformanceWatchingViewModel> _builder = new();

        private const float ContentPanelWidth = 400f;
        private const float RowHeight = 34f;
        private const float FollowButtonWidth = 96f;
        private const int VisibleRows = 5;

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.PerformanceWatching)
                .SetInitialGeometry(20f, 60f, 450f, 700f)
                .SetTitle("Watching")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .BindOnClosed(model => model.OnWindowClosed())
                .DefinePartialView(PerformanceWatchingViewModel.MainContentPartial, AddContent);

            // No tabs: the standard layout is still the root, and one partial holds the whole body.
            window.AddStandardLayout(layout => layout
                .SetContentPartialElement(PerformanceWatchingViewModel.ContentElement));

            return _builder.Build();
        }

        private static void AddContent(GuiGroup<PerformanceWatchingViewModel> host)
        {
            host.AddColumn(col => col.AddRow(row => row.AddGroup(panel =>
            {
                panel.SetShowBorder(false);
                panel.SetScrollbars(NuiScrollbars.None);
                panel.AddColumn(content =>
                {
                    AddPerformers(content);
                    AddBanked(content);
                    AddDebt(content);
                    AddLeaveWith(content);
                    AddSongOption(content);
                });
            }).SetWidth(ContentPanelWidth)));
        }

        private static void AddPerformers(GuiColumn<PerformanceWatchingViewModel> content)
        {
            content.AddRow(row => row.AddLabel()
                .BindText(model => model.HeaderText)
                .SetColor(PerformanceWindowColors.Dim)
                .SetHeight(24f)
                .SetHorizontalAlign(NuiHorizontalAlign.Left));

            // One row per performer in range; Follow picks the performer whose bonuses you receive for that kind.
            content.AddRow(row => row.AddList(template =>
                {
                    template.AddCell(cell => cell.AddLabel()
                        .BindText(model => model.RowTexts)
                        .BindColor(model => model.RowColors)
                        .BindTooltip(model => model.RowTexts)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                        .SetVerticalAlign(NuiVerticalAlign.Middle));

                    template.AddCell(cell =>
                    {
                        cell.SetIsVariable(false);
                        cell.SetWidth(FollowButtonWidth);
                        cell.AddButton()
                            .BindText(model => model.FollowTexts)
                            .BindIsEnabled(model => model.FollowEnabled)
                            .SetWidth(FollowButtonWidth - 4f)
                            .BindOnClicked(model => model.OnFollowRow());
                    });
                })
                .BindRowCount(model => model.RowTexts)
                .SetRowHeight(RowHeight)
                .SetHeight(RowHeight * VisibleRows + 6f));
        }

        private static void AddBanked(GuiColumn<PerformanceWatchingViewModel> content)
        {
            AddCaption(content, "INSPIRED BANKED");

            // No row height: a row holding a progress bar derives its height from the bar (layout rule R2c).
            content.AddRow(row => row.AddProgressBar()
                .BindValue(model => model.BankedFraction)
                .SetColor(PerformanceWindowColors.Gold)
                .SetHeight(18f));

            content.AddRow(row => row.AddText()
                .BindText(model => model.BankedText)
                .SetShowBorder(false)
                .SetScrollbars(NuiScrollbars.None)
                .SetHeight(64f));
        }

        private static void AddDebt(GuiColumn<PerformanceWatchingViewModel> content)
        {
            AddCaption(content, "XP DEBT");
            content.AddRow(row => row.AddText()
                .BindText(model => model.DebtText)
                .SetShowBorder(false)
                .SetScrollbars(NuiScrollbars.None)
                .SetHeight(44f));
        }

        private static void AddLeaveWith(GuiColumn<PerformanceWatchingViewModel> content)
        {
            AddCaption(content, "YOU WILL LEAVE WITH");
            content.AddRow(row => row.AddText()
                .BindText(model => model.LeaveWithText)
                .SetShowBorder(false)
                .SetScrollbars(NuiScrollbars.None)
                .SetHeight(64f));
        }

        private static void AddSongOption(GuiColumn<PerformanceWatchingViewModel> content)
        {
            // No row height: a row holding a checkbox derives its height from the checkbox (layout rule R2c).
            content.AddRow(row => row.AddCheckBox()
                .SetText("Play the performer's song")
                .BindIsChecked(model => model.PlayPerformerSong)
                .SetWidth(260f)
                .SetHeight(24f));
        }

        private static void AddCaption(GuiColumn<PerformanceWatchingViewModel> content, string text)
        {
            content.AddRow(row => row.AddLabel()
                .SetText(text)
                .SetColor(PerformanceWindowColors.Dim)
                .SetHeight(22f)
                .SetHorizontalAlign(NuiHorizontalAlign.Left));
        }
    }
}
