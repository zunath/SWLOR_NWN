using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.DMToolService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// Searchable replacement for the native DM chooser.
    /// </summary>
    public class DMChooserDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<DMChooserViewModel> _builder = new();

        private const float ContentPanelWidth = 580f;
        private const float SideRailWidth = 280f;
        private const float IconCellWidth = 40f;
        private const float InfoCellWidth = 140f;
        private const float AreaCellWidth = 120f;
        private const float PlayersCellWidth = 60f;
        private const float ButtonHeight = 32f;
        private const float RailButtonWidth = 250f;

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.DMChooser)
                .SetInitialGeometry(0, 0, 900f, 600f)
                .SetTitle("Chooser")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetKeepsOpenOnAreaChange(true)
                .DefinePartialView(DMChooserViewModel.MainContentPartial, host => AddMainContent(host, false))
                .DefinePartialView(DMChooserViewModel.AreaContentPartial, host => AddMainContent(host, true));

            window.AddStandardLayout(layout =>
            {
                layout.SetContentPartialElement(DMChooserViewModel.MainContentElement);
                layout.AddSideColumn(AddActionRail, SideRailWidth);
            });

            return _builder.Build();
        }

        // Areas list only their name and player count (every area has the same icon), so long names get the rest of the row.
        private static void AddMainContent(GuiGroup<DMChooserViewModel> host, bool areas)
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
                                    .AddOption("Areas", (int)DMObjectCategory.Areas)
                                    .AddOption("Creatures", (int)DMObjectCategory.Creatures)
                                    .AddOption("Placeables", (int)DMObjectCategory.Placeables)
                                    .AddOption("Items", (int)DMObjectCategory.Items)
                                    .AddOption("Doors", (int)DMObjectCategory.Doors)
                                    .AddOption("Triggers", (int)DMObjectCategory.Triggers)
                                    .AddOption("Waypoints", (int)DMObjectCategory.Waypoints)
                                    .AddOption("Stores", (int)DMObjectCategory.Stores)
                                    .AddOption("Encounters", (int)DMObjectCategory.Encounters)
                                    .BindSelectedIndex(model => model.SelectedCategory)
                                    .SetWidth(200f)
                                    .SetHeight(ButtonHeight);

                                filters.AddComboBox()
                                    .AddOption("Current Area", (int)DMObjectSearchScope.CurrentArea)
                                    .AddOption("All Areas", (int)DMObjectSearchScope.AllAreas)
                                    .BindSelectedIndex(model => model.SelectedScope)
                                    .BindIsEnabled(model => model.IsScopeEnabled)
                                    .SetDisabledTooltip("Areas are always listed from the whole module")
                                    .SetWidth(200f)
                                    .SetHeight(ButtonHeight);

                                filters.AddSpacer();
                            });

                            content.AddRow(searchRow =>
                            {
                                searchRow.AddTextEdit()
                                    .SetPlaceholder("Search by name, tag, or resref")
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
                                if (!areas)
                                {
                                    header.AddSpacer()
                                        .SetWidth(IconCellWidth);
                                }

                                header.AddLabel()
                                    .SetText("Name")
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetHeight(20f);

                                if (areas)
                                {
                                    header.AddLabel()
                                        .SetText("Players")
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                        .SetWidth(PlayersCellWidth)
                                        .SetHeight(20f);
                                    return;
                                }

                                header.AddLabel()
                                    .BindText(model => model.InfoHeader)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetWidth(InfoCellWidth)
                                    .SetHeight(20f);

                                header.AddLabel()
                                    .SetText("Area")
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetWidth(AreaCellWidth)
                                    .SetHeight(20f);
                            });

                            content.AddRow(listRow =>
                            {
                                listRow.AddList(template =>
                                {
                                    if (!areas)
                                        AddIconCell(template);

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
                                        cell.SetWidth(areas ? PlayersCellWidth : InfoCellWidth);
                                        cell.AddLabel()
                                            .BindText(model => model.ResultInfo)
                                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                    });

                                    if (areas)
                                        return;

                                    template.AddCell(cell =>
                                    {
                                        cell.SetIsVariable(false);
                                        cell.SetWidth(AreaCellWidth);
                                        cell.AddLabel()
                                            .BindText(model => model.ResultAreas)
                                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                    });
                                })
                                    .SetRowHeight(40f)
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
                        });
                    })
                        .SetWidth(ContentPanelWidth);
                });
            });
        }

        private static void AddIconCell(GuiListTemplate<DMChooserViewModel> template)
        {
            template.AddCell(cell =>
            {
                cell.SetIsVariable(false);
                cell.SetWidth(IconCellWidth);
                cell.AddGroup(group =>
                {
                    group.AddImage()
                        .BindResref(model => model.ResultIcons)
                        .SetHorizontalAlign(NuiHorizontalAlign.Center)
                        .SetVerticalAlign(NuiVerticalAlign.Middle)
                        .SetAspect(NuiAspect.Fit);
                })
                    .SetScrollbars(NuiScrollbars.None);
            });
        }

        private static void AddActionRail(GuiColumn<DMChooserViewModel> col)
        {
            DMDetailsRail.AddHeader(col, model => model.DetailIcon, model => model.DetailTitle);
            DMDetailsRail.AddBody(col, model => model.SelectedDetails, 230f);

            AddActionButton(col, "Jump To", "Move to the selected area or object",
                model => model.OnClickJumpTo(), model => model.CanJumpTo);
            AddActionButton(col, "Bring Here", "Move the selected creature to you",
                model => model.OnClickBringHere(), model => model.CanBringHere);
            AddActionButton(col, "Examine", "Examine the selected object",
                model => model.OnClickExamine(), model => model.CanExamine);
            AddActionButton(col, "Destroy", "Destroy the selected creature, placeable, or item",
                model => model.OnClickDestroy(), model => model.CanDestroy);

            col.AddRow(row => row.AddSpacer());
        }

        private static void AddActionButton(
            GuiColumn<DMChooserViewModel> col,
            string text,
            string tooltip,
            System.Linq.Expressions.Expression<Func<DMChooserViewModel, Action>> onClick,
            System.Linq.Expressions.Expression<Func<DMChooserViewModel, bool>> isEnabled)
        {
            col.AddRow(row =>
            {
                row.AddButton()
                    .SetText(text)
                    .SetTooltip(tooltip)
                    .SetWidth(RailButtonWidth)
                    .SetHeight(ButtonHeight)
                    .BindIsEnabled(isEnabled)
                    .BindOnClicked(onClick);
            });
        }
    }
}
