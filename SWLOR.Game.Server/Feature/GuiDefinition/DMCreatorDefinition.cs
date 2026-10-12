using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.DMToolService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// Searchable replacement for the native DM creator.
    /// </summary>
    public class DMCreatorDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<DMCreatorViewModel> _builder = new();

        private const float ContentPanelWidth = 560f;
        private const float SideRailWidth = 240f;
        private const float ButtonHeight = 32f;
        private const float RailButtonWidth = 220f;

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.DMCreator)
                .SetInitialGeometry(40, 0, 860f, 600f)
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
                                    .BindSelectedIndex(model => model.SelectedCategory)
                                    .SetTooltip("What to create")
                                    .SetWidth(200f)
                                    .SetHeight(ButtonHeight);

                                filters.AddComboBox()
                                    .AddOption("Custom", (int)DMBlueprintSource.Custom)
                                    .AddOption("Standard", (int)DMBlueprintSource.Standard)
                                    .BindSelectedIndex(model => model.SelectedSource)
                                    .SetTooltip("Custom: module and hak blueprints. Standard: base game blueprints.")
                                    .SetWidth(200f)
                                    .SetHeight(ButtonHeight);

                                filters.AddSpacer();
                            });

                            content.AddRow(searchRow =>
                            {
                                searchRow.AddTextEdit()
                                    .SetPlaceholder("Search by name, resref, or tag")
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
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetHeight(20f);

                                header.AddLabel()
                                    .SetText("Resref")
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetWidth(140f)
                                    .SetHeight(20f);

                                header.AddLabel()
                                    .SetText("Tag")
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetWidth(150f)
                                    .SetHeight(20f);
                            });

                            content.AddRow(listRow =>
                            {
                                listRow.AddList(template =>
                                {
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
                                        cell.SetWidth(140f);
                                        cell.AddLabel()
                                            .BindText(model => model.ResultResrefs)
                                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                    });

                                    template.AddCell(cell =>
                                    {
                                        cell.SetIsVariable(false);
                                        cell.SetWidth(150f);
                                        cell.AddLabel()
                                            .BindText(model => model.ResultTags)
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
                        });
                    })
                        .SetWidth(ContentPanelWidth);
                });
            });
        }

        private static void AddActionRail(GuiColumn<DMCreatorViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddLabel()
                    .SetText("SELECTED")
                    .SetColor(221, 181, 93)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                    .SetHeight(20f);
            });

            col.AddRow(row =>
            {
                row.AddText()
                    .BindText(model => model.SelectedDetails)
                    .SetShowBorder(true)
                    .SetScrollbars(NuiScrollbars.Auto)
                    .SetHeight(150f);
            });

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
