using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.HostedEventService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// The DM's /events window: schedule, start and end events, and set where players arrive.
    /// </summary>
    public class DMEventsDefinition : IGuiWindowDefinition
    {
        // The window cannot shrink below the size that shows every button.
        public const float MinimumWindowWidth = 830f;
        public const float MinimumWindowHeight = 680f;
        private const float EventRailWidth = 220f;
        private const float ContentPanelWidth = 560f;
        private const float ContentWidth = ContentPanelWidth - 24f;
        private const float CaptionWidth = 100f;
        private const float ButtonHeight = 32f;
        private readonly GuiWindowBuilder<DMEventsViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.DMEvents)
                .SetInitialGeometry(0, 0, MinimumWindowWidth, MinimumWindowHeight)
                .SetTitle("DM Events")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .DefinePartialView(DMEventsViewModel.MainContentPartial, AddContent);

            window.AddStandardLayout(layout => layout
                .AddLeadingColumn(AddEventRail, EventRailWidth)
                .SetContentPartialElement(DMEventsViewModel.ContentElement));

            return _builder.Build();
        }

        private static void AddEventRail(GuiColumn<DMEventsViewModel> column)
        {
            column.AddRow(row => row.AddLabel()
                .SetText("Events")
                .SetHeight(24f)
                .SetHorizontalAlign(NuiHorizontalAlign.Left));

            column.AddRow(row => row.AddList(template => template.AddCell(cell =>
                    cell.AddToggleButton()
                        .BindText(model => model.EventNames)
                        .BindIsToggled(model => model.EventSelected)
                        .BindOnClicked(model => model.OnSelectEvent())))
                .BindRowCount(model => model.EventNames)
                .SetRowHeight(30f)
                .SetHeight(470f));

            column.AddRow(row => row.AddButton()
                .SetText("New Event")
                .SetTooltip("Clear the form to schedule a new event.")
                .SetHeight(ButtonHeight)
                .BindOnClicked(model => model.OnClickNew()));
        }

        private static void AddContent(GuiGroup<DMEventsViewModel> host)
        {
            host.AddColumn(root => root.AddRow(row => row.AddGroup(panel =>
            {
                panel.SetShowBorder(false);
                panel.SetScrollbars(NuiScrollbars.None);
                panel.AddColumn(content =>
                {
                    content.AddRow(r => r.AddLabel()
                        .BindText(model => model.HeaderText)
                        .SetWidth(ContentWidth)
                        .SetHeight(24f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left));

                    content.AddRow(r =>
                    {
                        AddCaption(r, "Name");
                        r.AddTextEdit()
                            .SetPlaceholder("Event name")
                            .SetMaxLength(HostedEventRules.MaxNameLength)
                            .BindIsEnabled(model => model.IsDetailsEditable)
                            .BindValue(model => model.EventName);
                    });

                    content.AddRow(r =>
                    {
                        AddCaption(r, "Host");
                        r.AddTextEdit()
                            .SetPlaceholder("Name players will see")
                            .SetMaxLength(HostedEventRules.MaxHostLength)
                            .BindIsEnabled(model => model.IsDetailsEditable)
                            .BindValue(model => model.HostName);
                    });

                    content.AddRow(r =>
                    {
                        AddCaption(r, "Starts");
                        r.AddTextEdit()
                            .SetPlaceholder(HostedEventRules.StartTimeFormat)
                            .SetTooltip("Type the start time in your time zone, shown beside Now. Change it in DM Settings.")
                            .SetMaxLength(16)
                            .SetWidth(170f)
                            .BindIsEnabled(model => model.IsDetailsEditable)
                            .BindValue(model => model.StartText);
                        r.AddLabel()
                            .BindText(model => model.ServerTimeText)
                            .SetHeight(ButtonHeight)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                    });

                    content.AddRow(r => r.AddTextEdit()
                        .SetPlaceholder("Description players see in /events")
                        .SetIsMultiline(true)
                        .SetMaxLength(HostedEventRules.MaxDescriptionLength)
                        .SetWidth(ContentWidth)
                        .SetHeight(120f)
                        .BindIsEnabled(model => model.IsDetailsEditable)
                        .BindValue(model => model.Description));

                    content.AddRow(r =>
                    {
                        r.AddCheckBox()
                            .SetText("Players can join")
                            .SetTooltip("Shows a Join button in /events that sends players to the arrival point while the event runs.")
                            .SetWidth(200f)
                            .BindIsChecked(model => model.IsJoinOpen);
                        r.AddCheckBox()
                            .SetText("Announce when started")
                            .SetTooltip("Tells every player who has event announcements on that the event started.")
                            .SetWidth(240f)
                            .BindIsChecked(model => model.AnnounceOnStart);
                    });

                    content.AddRow(r => r.AddLabel()
                        .BindText(model => model.ArrivalText)
                        .SetWidth(ContentWidth)
                        .SetHeight(24f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left));

                    content.AddRow(r =>
                    {
                        r.AddButton()
                            .SetText("Set to My Location")
                            .SetTooltip("Players who join arrive where you, or the creature you possess, are standing now.")
                            .SetWidth(160f)
                            .SetHeight(ButtonHeight)
                            .BindIsEnabled(model => model.IsSaved)
                            .BindOnClicked(model => model.OnClickSetArrival());
                        r.AddButton()
                            .SetText("Clear")
                            .SetTooltip("Remove the arrival point. Players cannot join until a new one is set.")
                            .SetWidth(80f)
                            .SetHeight(ButtonHeight)
                            .BindIsEnabled(model => model.HasArrival)
                            .BindOnClicked(model => model.OnClickClearArrival());
                        r.AddButton()
                            .SetText("Go to Arrival")
                            .SetWidth(130f)
                            .SetHeight(ButtonHeight)
                            .BindIsEnabled(model => model.HasArrival)
                            .BindOnClicked(model => model.OnClickGoToArrival());
                    });

                    content.AddRow(r => r.AddLabel()
                        .BindText(model => model.SummaryText)
                        .SetWidth(ContentWidth)
                        .SetHeight(24f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left));

                    content.AddRow(r =>
                    {
                        r.AddButton()
                            .SetText("Save")
                            .SetWidth(80f)
                            .SetHeight(ButtonHeight)
                            .BindOnClicked(model => model.OnClickSave());
                        r.AddButton()
                            .SetText("Start")
                            .SetTooltip("Saves your changes and starts the event now.")
                            .SetWidth(80f)
                            .SetHeight(ButtonHeight)
                            .BindIsEnabled(model => model.IsStartEnabled)
                            .BindOnClicked(model => model.OnClickStart());
                        r.AddButton()
                            .BindText(model => model.EndText)
                            .SetWidth(120f)
                            .SetHeight(ButtonHeight)
                            .BindIsEnabled(model => model.IsSaved)
                            .BindOnClicked(model => model.OnClickEnd());
                        r.AddButton()
                            .SetText("Send Everyone Back")
                            .SetTooltip("Sends every online player at the event back to where they joined from, even in combat.")
                            .SetWidth(170f)
                            .SetHeight(ButtonHeight)
                            .BindIsEnabled(model => model.IsSendBackEnabled)
                            .BindOnClicked(model => model.OnClickSendBack());
                    });

                    content.AddRow(r => r.AddText()
                        .BindText(model => model.FeedbackText)
                        .SetWidth(ContentWidth)
                        .SetHeight(50f)
                        .SetShowBorder(false)
                        .SetScrollbars(NuiScrollbars.Auto));
                });
            }).SetWidth(ContentPanelWidth)));
        }

        private static void AddCaption(GuiRow<DMEventsViewModel> row, string text)
        {
            row.AddLabel()
                .SetText(text)
                .SetWidth(CaptionWidth)
                .SetHeight(ButtonHeight)
                .SetHorizontalAlign(NuiHorizontalAlign.Left);
        }
    }
}
