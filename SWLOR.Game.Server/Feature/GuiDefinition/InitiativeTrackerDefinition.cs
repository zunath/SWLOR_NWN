using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// The initiative tracker window: trackers in the area to join or start, then the joined tracker's
    /// entries sorted highest first, with the turn marker shown by a colored value.
    /// </summary>
    public class InitiativeTrackerDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<InitiativeTrackerViewModel> _builder = new();

        private const float ContentPanelWidth = 300f;
        private const float ButtonHeight = 32f;
        private const float ValueColumnWidth = 48f;
        private const float RemoveColumnWidth = 32f;
        private const float TurnButtonWidth = 92f;
        private const float JoinColumnWidth = 56f;

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.InitiativeTracker)
                .SetInitialGeometry(0, 0, 380f, 480f)
                .SetTitle("Initiative Tracker")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .DefinePartialView(InitiativeTrackerViewModel.BrowsePartial, AddBrowse)
                .DefinePartialView(InitiativeTrackerViewModel.TrackerPartial, host => AddTracker(host, false))
                .DefinePartialView(InitiativeTrackerViewModel.ManagerPartial, host => AddTracker(host, true));

            window.AddStandardLayout(layout => layout.SetContentPartialElement(InitiativeTrackerViewModel.MainContentElement));

            return _builder.Build();
        }

        private static void AddPanel(GuiGroup<InitiativeTrackerViewModel> host, Action<GuiColumn<InitiativeTrackerViewModel>> buildContent)
        {
            host.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddGroup(panel =>
                    {
                        panel.SetShowBorder(false);
                        panel.SetScrollbars(NuiScrollbars.None);
                        panel.AddColumn(buildContent);
                    })
                        .SetWidth(ContentPanelWidth);
                });
            });
        }

        private static void AddBrowse(GuiGroup<InitiativeTrackerViewModel> host)
        {
            AddPanel(host, content =>
            {
                content.AddRow(row =>
                {
                    row.AddLabel()
                        .BindText(model => model.BrowseStatusText)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                        .SetHeight(20f);
                });

                content.AddRow(listRow =>
                {
                    listRow.AddList(template =>
                    {
                        template.AddCell(cell =>
                        {
                            cell.AddLabel()
                                .BindText(model => model.SessionHostNames)
                                .BindTooltip(model => model.SessionTooltips)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left);
                        });

                        template.AddCell(cell =>
                        {
                            cell.SetIsVariable(false);
                            cell.SetWidth(JoinColumnWidth);
                            cell.AddButton()
                                .SetText("Join")
                                .SetWidth(JoinColumnWidth)
                                .BindOnClicked(model => model.OnClickJoin());
                        });
                    })
                        .SetRowHeight(30f)
                        .BindRowCount(model => model.SessionHostNames);
                });

                content.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("Start a Tracker")
                        .SetTooltip("Start a tracker in this area that others here can join.")
                        .SetWidth(140f)
                        .SetHeight(ButtonHeight)
                        .BindOnClicked(model => model.OnClickStart());
                    row.AddSpacer();
                });
            });
        }

        private static void AddTracker(GuiGroup<InitiativeTrackerViewModel> host, bool isManager)
        {
            AddPanel(host, content =>
            {
                content.AddRow(row =>
                {
                    row.AddLabel()
                        .BindText(model => model.HostText)
                        .BindTooltip(model => model.HostTooltip)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                        .SetHeight(20f);
                });

                AddEntryRows(content, isManager);

                content.AddRow(row =>
                {
                    row.AddLabel()
                        .BindText(model => model.EmptyListText)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                        .SetHeight(20f);
                });

                content.AddRow(row =>
                {
                    row.AddSpacer();
                    row.AddButton()
                        .SetText("-")
                        .SetTooltip("Move the turn fewer places at a time.")
                        .SetDisabledTooltip("Add your entry to move the turn.")
                        .BindIsEnabled(model => model.CanMoveTurn)
                        .SetWidth(ButtonHeight)
                        .SetHeight(ButtonHeight)
                        .BindOnClicked(model => model.OnClickDecreaseStep());
                    row.AddLabel()
                        .BindText(model => model.StepText)
                        .SetTooltip("How many places Prev and Next move the turn.")
                        .SetHorizontalAlign(NuiHorizontalAlign.Center)
                        .SetWidth(40f)
                        .SetHeight(ButtonHeight);
                    row.AddButton()
                        .SetText("+")
                        .SetTooltip("Move the turn more places at a time.")
                        .SetDisabledTooltip("Add your entry to move the turn.")
                        .BindIsEnabled(model => model.CanMoveTurn)
                        .SetWidth(ButtonHeight)
                        .SetHeight(ButtonHeight)
                        .BindOnClicked(model => model.OnClickIncreaseStep());
                    row.AddSpacer();
                });

                content.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("Prev")
                        .SetWidth(TurnButtonWidth)
                        .SetTooltip("Move the turn back.")
                        .SetDisabledTooltip("Add your entry to move the turn.")
                        .BindIsEnabled(model => model.CanMoveTurn)
                        .SetHeight(ButtonHeight)
                        .BindOnClicked(model => model.OnClickPrevious());
                    row.AddButton()
                        .SetText("Edit")
                        .SetWidth(TurnButtonWidth)
                        .SetTooltip("Set or remove your value.")
                        .SetHeight(ButtonHeight)
                        .BindOnClicked(model => model.OnClickEdit());
                    row.AddButton()
                        .SetText("Next")
                        .SetWidth(TurnButtonWidth)
                        .SetTooltip("Move the turn forward.")
                        .SetDisabledTooltip("Add your entry to move the turn.")
                        .BindIsEnabled(model => model.CanMoveTurn)
                        .SetHeight(ButtonHeight)
                        .BindOnClicked(model => model.OnClickNext());
                });

                content.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("Leave")
                        .SetTooltip("Leave this tracker and remove your entry.")
                        .SetWidth(90f)
                        .SetHeight(ButtonHeight)
                        .BindOnClicked(model => model.OnClickLeave());
                    row.AddSpacer();
                    if (isManager)
                    {
                        row.AddButton()
                            .SetText("Clear All")
                            .SetTooltip("Remove every entry and the turn marker.")
                            .SetWidth(90f)
                            .SetHeight(ButtonHeight)
                            .BindOnClicked(model => model.OnClickReset());
                    }
                });
            });
        }

        private static void AddEntryRows(GuiColumn<InitiativeTrackerViewModel> content, bool isManager)
        {
            content.AddRow(listRow =>
            {
                listRow.AddList(template =>
                {
                    template.AddCell(cell =>
                    {
                        cell.SetIsVariable(false);
                        cell.SetWidth(ValueColumnWidth);
                        cell.AddLabel()
                            .BindText(model => model.EntryValues)
                            .BindColor(model => model.EntryValueColors)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                    });

                    template.AddCell(cell =>
                    {
                        cell.AddLabel()
                            .BindText(model => model.EntryNames)
                            .BindTooltip(model => model.EntryNameTooltips)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                    });

                    if (isManager)
                    {
                        template.AddCell(cell =>
                        {
                            cell.SetIsVariable(false);
                            cell.SetWidth(RemoveColumnWidth);
                            cell.AddButton()
                                .SetText("X")
                                .SetTooltip("Remove this entry.")
                                .SetWidth(RemoveColumnWidth)
                                .BindOnClicked(model => model.OnClickRemoveEntry());
                        });
                    }
                })
                    .SetRowHeight(30f)
                    .BindRowCount(model => model.EntryNames);
            });
        }
    }
}
