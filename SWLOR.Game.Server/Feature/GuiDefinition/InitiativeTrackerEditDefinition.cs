using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.InitiativeTrackerService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// The initiative tracker's edit window: your own value, and named entries for hosts and DMs.
    /// </summary>
    public class InitiativeTrackerEditDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<InitiativeTrackerEditViewModel> _builder = new();

        private const float ContentPanelWidth = 280f;
        private const float ButtonHeight = 32f;
        private static readonly string ValueTooltip =
            $"A whole number from {InitiativeTrackerSession.MinValue} to {InitiativeTrackerSession.MaxValue}.";

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.InitiativeTrackerEdit)
                .SetInitialGeometry(390f, 0, 350f, 340f)
                .SetTitle("Tracker Edit")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .DefinePartialView(InitiativeTrackerEditViewModel.NoTrackerPartial, AddNoTracker)
                .DefinePartialView(InitiativeTrackerEditViewModel.OwnPartial, host => AddContent(host, true, false))
                .DefinePartialView(InitiativeTrackerEditViewModel.OwnAndNamedPartial, host => AddContent(host, true, true))
                .DefinePartialView(InitiativeTrackerEditViewModel.NamedPartial, host => AddContent(host, false, true));

            window.AddStandardLayout(layout => layout.SetContentPartialElement(InitiativeTrackerEditViewModel.MainContentElement));

            return _builder.Build();
        }

        private static void AddPanel(GuiGroup<InitiativeTrackerEditViewModel> host, Action<GuiColumn<InitiativeTrackerEditViewModel>> buildContent)
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

        private static void AddNoTracker(GuiGroup<InitiativeTrackerEditViewModel> host)
        {
            AddPanel(host, content =>
            {
                content.AddRow(row =>
                {
                    row.AddLabel()
                        .SetText("Join or start a tracker first.")
                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                        .SetHeight(20f);
                });
            });
        }

        private static void AddContent(GuiGroup<InitiativeTrackerEditViewModel> host, bool showOwn, bool showNamed)
        {
            AddPanel(host, content =>
            {
                if (showOwn)
                    AddOwnEntry(content);

                if (showNamed)
                    AddNamedEntry(content);
            });
        }

        private static void AddOwnEntry(GuiColumn<InitiativeTrackerEditViewModel> content)
        {
            content.AddRow(row =>
            {
                row.AddLabel()
                    .SetText("YOUR ENTRY")
                    .SetColor(221, 181, 93)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                    .SetHeight(20f);
            });

            content.AddRow(row =>
            {
                row.AddTextEdit()
                    .SetPlaceholder("Value")
                    .SetTooltip(ValueTooltip)
                    .BindValue(model => model.ValueText)
                    .SetMaxLength(5)
                    .SetWidth(80f)
                    .SetHeight(ButtonHeight);
                row.AddButton()
                    .SetText("Set")
                    .SetTooltip("Add your entry with this value, or change it.")
                    .SetWidth(80f)
                    .SetHeight(ButtonHeight)
                    .BindOnClicked(model => model.OnClickSet());
                row.AddButton()
                    .SetText("Remove")
                    .SetTooltip("Take your entry off the tracker.")
                    .SetDisabledTooltip("You have no entry on the tracker.")
                    .BindIsEnabled(model => model.HasOwnEntry)
                    .SetWidth(80f)
                    .SetHeight(ButtonHeight)
                    .BindOnClicked(model => model.OnClickRemove());
                row.AddSpacer();
            });

            content.AddRow(row =>
            {
                row.AddButton()
                    .SetText("Roll d20")
                    .SetTooltip("Roll a d20 aloud and use it as your value.")
                    .SetWidth(120f)
                    .SetHeight(ButtonHeight)
                    .BindOnClicked(model => model.OnClickRoll());
                row.AddSpacer();
            });
        }

        private static void AddNamedEntry(GuiColumn<InitiativeTrackerEditViewModel> content)
        {
            content.AddRow(row =>
            {
                row.AddLabel()
                    .SetText("NAMED ENTRY")
                    .SetColor(221, 181, 93)
                    .SetTooltip("Add an entry for an NPC or anyone not on the tracker.")
                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                    .SetHeight(20f);
            });

            content.AddRow(row =>
            {
                row.AddTextEdit()
                    .SetPlaceholder("Name")
                    .BindValue(model => model.NamedEntryName)
                    .SetMaxLength(InitiativeTrackerSession.MaxNameLength)
                    .SetHeight(ButtonHeight);
                row.AddTextEdit()
                    .SetPlaceholder("Value")
                    .SetTooltip(ValueTooltip)
                    .BindValue(model => model.NamedEntryValue)
                    .SetMaxLength(5)
                    .SetWidth(70f)
                    .SetHeight(ButtonHeight);
            });

            content.AddRow(row =>
            {
                row.AddButton()
                    .SetText("Add")
                    .SetTooltip("Add this named entry to the tracker.")
                    .SetWidth(80f)
                    .SetHeight(ButtonHeight)
                    .BindOnClicked(model => model.OnClickAddNamed());
                row.AddSpacer();
            });
        }
    }
}
