using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.DMToolService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// DM search over online and offline characters with staff identity details and common actions.
    /// </summary>
    public class DMPlayerSearchDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<DMPlayerSearchViewModel> _builder = new();

        private const float ContentPanelWidth = 560f;
        private const float SideRailWidth = 270f;
        private const float ButtonHeight = 32f;
        private const float RailButtonWidth = 250f;

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.DMPlayerSearch)
                .SetInitialGeometry(80, 0, 890f, 600f)
                .SetTitle("Player Search")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetKeepsOpenOnAreaChange(true)
                .DefinePartialView(DMPlayerSearchViewModel.MainContentPartial, AddMainContent);

            window.AddStandardLayout(layout =>
            {
                layout.SetContentPartialElement(DMPlayerSearchViewModel.MainContentElement);
                layout.AddSideColumn(AddActionRail, SideRailWidth);
            });

            return _builder.Build();
        }

        private static void AddMainContent(GuiGroup<DMPlayerSearchViewModel> host)
        {
            host.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddGroup(panel =>
                    {
                        panel.SetShowBorder(false);
                        panel.SetScrollbars(NuiScrollbars.None);
                        panel.AddColumn(content =>
                        {
                            content.AddRow(filters =>
                            {
                                filters.AddComboBox()
                                    .AddOption("Online", (int)DMPlayerSearchScope.Online)
                                    .AddOption("Offline", (int)DMPlayerSearchScope.Offline)
                                    .AddOption("Online and Offline", (int)DMPlayerSearchScope.All)
                                    .BindSelectedIndex(model => model.SelectedScope)
                                    .SetWidth(220f)
                                    .SetHeight(ButtonHeight);

                                filters.AddSpacer();
                            });

                            content.AddRow(searchRow =>
                            {
                                searchRow.AddTextEdit()
                                    .SetPlaceholder("Name, descriptor, account, CD key, or player ID")
                                    .SetTooltip("Each word matches the start of a word. Player IDs must be entered whole.")
                                    .BindValue(model => model.SearchText)
                                    .SetMaxLength(64)
                                    .SetHeight(ButtonHeight);

                                searchRow.AddButton()
                                    .SetText("X")
                                    .SetTooltip("Clear the search")
                                    .SetWidth(35f)
                                    .SetHeight(ButtonHeight)
                                    .BindOnClicked(model => model.OnClickClearSearch());

                                searchRow.AddButton()
                                    .SetText("Search")
                                    .SetWidth(100f)
                                    .SetHeight(ButtonHeight)
                                    .BindOnClicked(model => model.OnClickSearch());
                            });

                            content.AddRow(header =>
                            {
                                header.AddLabel()
                                    .SetText("Name")
                                    .SetTooltip("Select one character for their details and actions, or several to bring them here together. Selections stay across searches and pages.")
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetHeight(20f);

                                header.AddLabel()
                                    .SetText("Descriptor")
                                    .SetTooltip("Orange descriptors are disguises. Select the character to see the disguise.")
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetWidth(150f)
                                    .SetHeight(20f);

                                header.AddLabel()
                                    .SetText("Account")
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetWidth(110f)
                                    .SetHeight(20f);

                                header.AddLabel()
                                    .SetText("Status")
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetWidth(70f)
                                    .SetHeight(20f);
                            });

                            content.AddRow(listRow =>
                            {
                                listRow.AddList(template =>
                                {
                                    // Each name toggles independently, so several characters can be selected at once.
                                    template.AddCell(cell =>
                                    {
                                        cell.AddToggleButton()
                                            .BindText(model => model.ResultNames)
                                            .BindIsToggled(model => model.ResultSelections)
                                            .BindOnClicked(model => model.OnSelectResult());
                                    });

                                    template.AddCell(cell =>
                                    {
                                        cell.SetIsVariable(false);
                                        cell.SetWidth(150f);
                                        cell.AddLabel()
                                            .BindText(model => model.ResultDescriptors)
                                            .BindColor(model => model.ResultDescriptorColors)
                                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                    });

                                    template.AddCell(cell =>
                                    {
                                        cell.SetIsVariable(false);
                                        cell.SetWidth(110f);
                                        cell.AddLabel()
                                            .BindText(model => model.ResultAccounts)
                                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                    });

                                    template.AddCell(cell =>
                                    {
                                        cell.SetIsVariable(false);
                                        cell.SetWidth(70f);
                                        cell.AddLabel()
                                            .BindText(model => model.ResultStatuses)
                                            .BindColor(model => model.ResultStatusColors)
                                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                    });
                                })
                                    .SetRowHeight(30f)
                                    .BindRowCount(model => model.ResultNames);
                            });

                            content.AddPagination(
                                model => model.PageNumbers,
                                model => model.SelectedPageIndex,
                                model => model.OnClickPreviousPage(),
                                model => model.OnClickNextPage());

                            content.AddRow(status =>
                            {
                                status.AddLabel()
                                    .BindText(model => model.ResultSummary)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetHeight(20f);
                            });

                            content.AddRow(selection =>
                            {
                                selection.AddLabel()
                                    .BindText(model => model.SelectionSummary)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetHeight(20f);

                                selection.AddButton()
                                    .SetText("Clear Selection")
                                    .SetWidth(130f)
                                    .SetHeight(ButtonHeight)
                                    .BindIsEnabled(model => model.HasSelection)
                                    .BindOnClicked(model => model.OnClickClearSelection());
                            });
                        });
                    })
                        .SetWidth(ContentPanelWidth);
                });
            });
        }

        private static void AddActionRail(GuiColumn<DMPlayerSearchViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddLabel()
                    .SetText("SELECTED")
                    .SetColor(221, 181, 93)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                    .SetHeight(20f);
            });

            // A text box rather than a label so DMs can select and copy IDs and CD keys.
            col.AddRow(row =>
            {
                row.AddTextEdit()
                    .BindValue(model => model.SelectedDetails)
                    .SetIsMultiline(true)
                    .SetHasWordWrap(true)
                    .SetMaxLength(2000)
                    .SetHeight(250f);
            });

            AddActionButton(col, "Jump To", "Go to the character, or to an offline character's last saved location",
                model => model.OnClickJumpTo(), model => model.CanJumpTo);
            AddActionButton(col, "Jump Here", "Bring the online character to you",
                model => model.OnClickJumpHere(), model => model.IsSelectedOnline);
            col.AddRow(row =>
            {
                row.AddButton()
                    .BindText(model => model.BringSelectedText)
                    .SetTooltip("Bring every selected online character to you")
                    .SetDisabledTooltip("Select one or more online characters first.")
                    .SetWidth(RailButtonWidth)
                    .SetHeight(ButtonHeight)
                    .BindIsEnabled(model => model.CanBringSelected)
                    .BindOnClicked(model => model.OnClickBringSelected());
            });
            AddActionButton(col, "Examine", "Open the DM examine window for the online character",
                model => model.OnClickExamine(), model => model.IsSelectedOnline);
            AddActionButton(col, "Send Message", "Send the online character a private DM message",
                model => model.OnClickSendMessage(), model => model.IsSelectedOnline);
            AddActionButton(col, "Boot", "Disconnect the online character from the server",
                model => model.OnClickBoot(), model => model.IsSelectedOnline);

            col.AddRow(row => row.AddSpacer());
        }

        private static void AddActionButton(
            GuiColumn<DMPlayerSearchViewModel> col,
            string text,
            string tooltip,
            System.Linq.Expressions.Expression<Func<DMPlayerSearchViewModel, Action>> onClick,
            System.Linq.Expressions.Expression<Func<DMPlayerSearchViewModel, bool>> isEnabled)
        {
            col.AddRow(row =>
            {
                row.AddButton()
                    .SetText(text)
                    .SetTooltip(tooltip)
                    .BindDisabledTooltip(model => model.SingleActionReason)
                    .SetWidth(RailButtonWidth)
                    .SetHeight(ButtonHeight)
                    .BindIsEnabled(isEnabled)
                    .BindOnClicked(onClick);
            });
        }
    }
}
