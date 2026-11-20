using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// The player's /events window: running and upcoming DM events, Join and Leave.
    /// </summary>
    public class EventsDefinition : IGuiWindowDefinition
    {
        // The window cannot shrink below the size that shows Join and Leave Event.
        public const float MinimumWindowWidth = 570f;
        public const float MinimumWindowHeight = 620f;
        private const float ContentPanelWidth = 520f;
        private const float ContentWidth = ContentPanelWidth - 24f;
        private const float RowHeight = 30f;
        private readonly GuiWindowBuilder<EventsViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.Events)
                .SetInitialGeometry(0, 0, MinimumWindowWidth, MinimumWindowHeight)
                .SetTitle("Events")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .DefinePartialView(EventsViewModel.MainContentPartial, AddContent);

            window.AddStandardLayout(layout => layout.SetContentPartialElement(EventsViewModel.ContentElement));
            return _builder.Build();
        }

        private static void AddContent(GuiGroup<EventsViewModel> host)
        {
            host.AddColumn(root => root.AddRow(row => row.AddGroup(panel =>
            {
                panel.SetShowBorder(false);
                panel.SetScrollbars(NuiScrollbars.None);
                panel.AddColumn(content =>
                {
                    content.AddRow(r => r.AddLabel()
                        .BindText(model => model.ListSummaryText)
                        .SetWidth(ContentWidth)
                        .SetHeight(24f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left));

                    content.AddRow(r => r.AddList(template =>
                    {
                        template.AddCell(cell => cell.AddToggleButton()
                            .BindText(model => model.EventNames)
                            .BindIsToggled(model => model.EventSelected)
                            .BindOnClicked(model => model.OnSelectEvent()));

                        template.AddCell(cell =>
                        {
                            cell.SetIsVariable(false);
                            cell.SetWidth(110f);
                            cell.AddLabel()
                                .BindText(model => model.EventStatuses)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left);
                        });
                    })
                        .BindRowCount(model => model.EventNames)
                        .SetRowHeight(RowHeight)
                        .SetWidth(ContentWidth)
                        .SetHeight(160f));

                    content.AddRow(r => r.AddLabel()
                        .BindText(model => model.SelectedName)
                        .SetWidth(ContentWidth)
                        .SetHeight(26f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left));

                    content.AddRow(r => r.AddLabel()
                        .BindText(model => model.SelectedHost)
                        .SetWidth(ContentWidth)
                        .SetHeight(20f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left));

                    content.AddRow(r => r.AddLabel()
                        .BindText(model => model.SelectedTiming)
                        .SetWidth(ContentWidth)
                        .SetHeight(20f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left));

                    content.AddRow(r => r.AddText()
                        .BindText(model => model.SelectedDescription)
                        .SetWidth(ContentWidth)
                        .SetHeight(120f)
                        .SetScrollbars(NuiScrollbars.Auto));

                    content.AddRow(r =>
                    {
                        r.AddButton()
                            .SetText("Join")
                            .SetWidth(120f)
                            .SetHeight(32f)
                            .BindIsEnabled(model => model.IsJoinEnabled)
                            .BindTooltip(model => model.JoinTooltip)
                            .BindOnClicked(model => model.OnClickJoin());

                        r.AddLabel()
                            .BindText(model => model.JoinHintText)
                            .SetHeight(32f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                    });

                    content.AddRow(r =>
                    {
                        r.AddButton()
                            .SetText("Leave Event")
                            .SetTooltip("Go back to where you joined the event from. Same as /leaveevent.")
                            .SetWidth(120f)
                            .SetHeight(32f)
                            .BindIsEnabled(model => model.IsLeaveEnabled)
                            .BindOnClicked(model => model.OnClickLeave());

                        r.AddLabel()
                            .BindText(model => model.CurrentEventText)
                            .SetHeight(32f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                    });

                    content.AddRow(r => r.AddText()
                        .BindText(model => model.StatusText)
                        .SetWidth(ContentWidth)
                        .SetHeight(44f)
                        .SetShowBorder(false)
                        .SetScrollbars(NuiScrollbars.Auto));
                });
            }).SetWidth(ContentPanelWidth)));
        }
    }
}
