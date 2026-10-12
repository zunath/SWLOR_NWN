using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.DMToolService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// Searchable replacement for the native DM creator, organized by the same palettes and categories.
    /// </summary>
    public class DMCreatorDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<DMCreatorViewModel> _builder = new();

        private const float ContentPanelWidth = 580f;
        private const float SideRailWidth = 280f;
        private const float ButtonHeight = 32f;
        private const float RailButtonWidth = 250f;
        private const float IconCellWidth = 40f;
        private const float InfoCellWidth = 170f;
        private const float CategoryComboWidth = 280f;

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.DMCreator)
                .SetInitialGeometry(40, 0, 900f, 600f)
                .SetTitle("Creator")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetKeepsOpenOnAreaChange(true)
                .DefinePartialView(DMCreatorViewModel.MainContentPartial, AddMainContent);

            window.AddStandardLayout(layout =>
            {
                layout.SetContentPartialElement(DMCreatorViewModel.MainContentElement);
                layout.AddSideColumn(AddActionRail, SideRailWidth);
            });

            return _builder.Build();
        }

        private static void AddMainContent(GuiGroup<DMCreatorViewModel> host)
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
                                    .AddOption("Creatures", (int)DMBlueprintCategory.Creatures)
                                    .AddOption("Placeables", (int)DMBlueprintCategory.Placeables)
                                    .AddOption("Items", (int)DMBlueprintCategory.Items)
                                    .AddOption("Stores", (int)DMBlueprintCategory.Stores)
                                    .AddOption("Waypoints", (int)DMBlueprintCategory.Waypoints)
                                    .BindSelectedIndex(model => model.SelectedType)
                                    .SetTooltip("What to create")
                                    .SetWidth(140f)
                                    .SetHeight(ButtonHeight);

                                filters.AddComboBox()
                                    .AddOption("Custom", (int)DMBlueprintSource.Custom)
                                    .AddOption("Standard", (int)DMBlueprintSource.Standard)
                                    .BindSelectedIndex(model => model.SelectedSource)
                                    .SetTooltip("Custom: the module's palette. Standard: the base game's palette.")
                                    .SetWidth(110f)
                                    .SetHeight(ButtonHeight);

                                // A combo without a width shrinks the whole panel, so the category combo is fixed too.
                                filters.AddComboBox()
                                    .BindOptions(model => model.CategoryOptions)
                                    .BindSelectedIndex(model => model.SelectedCategoryIndex)
                                    .SetTooltip("Palette category")
                                    .SetWidth(CategoryComboWidth)
                                    .SetHeight(ButtonHeight);

                                filters.AddSpacer();
                            });

                            content.AddRow(searchRow =>
                            {
                                searchRow.AddTextEdit()
                                    .SetPlaceholder("Search by name, category, or resref")
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
                                header.AddSpacer()
                                    .SetWidth(IconCellWidth);

                                header.AddLabel()
                                    .SetText("Name")
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetHeight(20f);

                                header.AddLabel()
                                    .BindText(model => model.InfoHeader)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetWidth(InfoCellWidth)
                                    .SetHeight(20f);
                            });

                            content.AddRow(listRow =>
                            {
                                listRow.AddList(template =>
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
                                        cell.SetWidth(InfoCellWidth);
                                        cell.AddLabel()
                                            .BindText(model => model.ResultInfo)
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

        private static void AddActionRail(GuiColumn<DMCreatorViewModel> col)
        {
            DMDetailsRail.AddHeader(col, model => model.DetailIcon, model => model.DetailTitle);
            DMDetailsRail.AddBody(col, model => model.DetailBody, 250f);

            col.AddRow(row =>
            {
                row.AddButton()
                    .SetText("Create at Target")
                    .SetTooltip("Click the ground or an object to create the blueprint there. Items clicked onto a creature or container go into its inventory.")
                    .SetWidth(RailButtonWidth)
                    .SetHeight(ButtonHeight)
                    .BindIsEnabled(model => model.HasSelection)
                    .BindOnClicked(model => model.OnClickCreateAtTarget());
            });

            col.AddRow(row =>
            {
                row.AddButton()
                    .SetText("Create at My Location")
                    .SetWidth(RailButtonWidth)
                    .SetHeight(ButtonHeight)
                    .BindIsEnabled(model => model.HasSelection)
                    .BindOnClicked(model => model.OnClickCreateHere());
            });

            col.AddRow(row =>
            {
                row.AddButton()
                    .SetText("Create in My Inventory")
                    .SetWidth(RailButtonWidth)
                    .SetHeight(ButtonHeight)
                    .BindIsEnabled(model => model.CanCreateInInventory)
                    .SetDisabledTooltip("Only items can be created in your inventory")
                    .BindOnClicked(model => model.OnClickCreateInInventory());
            });

            col.AddRow(row => row.AddSpacer());
        }
    }
}
