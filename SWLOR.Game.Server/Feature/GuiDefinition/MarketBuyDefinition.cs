using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class MarketBuyDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<MarketBuyViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.MarketBuying)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 1000f, 600f)
                .BindTitle(model => model.WindowTitle)

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddTextEdit()
                            .SetPlaceholder("Search")
                            .BindValue(model => model.SearchText);

                        row.AddButton()
                            .SetText("X")
                            .SetHeight(35f)
                            .SetWidth(35f)
                            .BindOnClicked(model => model.OnClickClearSearch());

                        row.AddButton()
                            .SetText("Search")
                            .SetHeight(35f)
                            .BindOnClicked(model => model.OnClickSearch());

                        // Text-based sort button (current implementation)
                        row.AddButton()
                            .BindText(model => model.SortByPriceText)
                            .SetHeight(35f)
                            .SetWidth(120f)
                            .BindOnClicked(model => model.OnClickSortByPrice());
                    });

                    col.AddRow(row =>
                    {
                        row.AddColumn(col2 =>
                        {
                            col2.AddRow(row2 =>
                            {
                                row2.AddButton()
                                    .SetText("Clear Filters")
                                    .SetHeight(35f)
                                    .SetWidth(180f)
                                    .BindOnClicked(model => model.OnClickClearFilters());
                            });

                            col2.AddRow(row2 =>
                            {
                                row2.AddList(template =>
                                    {
                                        template.AddCell(cell =>
                                        {
                                            cell.AddToggleButton()
                                                .BindText(model => model.CategoryNames)
                                                .BindIsToggled(model => model.CategoryToggles)
                                                .BindOnClicked(model => model.OnClickCategory());
                                        });
                                    })
                                    .BindRowCount(model => model.CategoryNames);
                            });
                        })
                            .SetWidth(180f);

                        row.AddColumn(col2 =>
                        {
                            col2.AddRow(row2 =>
                            {
                                row2.AddColumn(col3 =>
                                {
                                    col3.AddTable<MarketBuyViewModel>(t => t
                                        .AddComponentColumn("", 40f, cell =>
                                        {
                                            cell.AddGroup(group =>
                                            {
                                                group.AddImage()
                                                    .BindResref(model => model.ItemIconResrefs)
                                                    .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                                    .SetVerticalAlign(NuiVerticalAlign.Top)
                                                    .BindTooltip(model => model.ItemNames);
                                            });
                                        })
                                        .AddComponentColumn("", 0f, cell =>
                                        {
                                            cell.AddText()
                                                .BindText(model => model.ItemNames)
                                                .BindTooltip(model => model.ItemNames);
                                        }, isVariable: true)
                                        .AddColumn("", 120f, model => model.ItemPriceNames)
                                        .AddComponentColumn("", 40f, cell =>
                                        {
                                            cell.AddButton()
                                                .SetText("?")
                                                .SetWidth(40f)
                                                .SetHeight(40f)
                                                .BindOnClicked(model => model.OnClickExamine());
                                        })
                                        .AddComponentColumn("", 0f, cell =>
                                        {
                                            cell.AddButton()
                                                .SetText("Buy")
                                                .BindOnClicked(model => model.OnClickBuy())
                                                .BindIsEnabled(model => model.ItemBuyEnabled);
                                        }, isVariable: true)
                                        .SetShowHeader(false)
                                        .SetPadding(50f)
                                        .SetRowHeight(40f)
                                        .BindRowCount(model => model.ItemNames));
                                });
                            });

                            col2.AddPagination(
                                model => model.PageNumbers,
                                model => model.SelectedPageIndex,
                                model => model.OnClickPreviousPage(),
                                model => model.OnClickNextPage());
                        });

                    });
                });


            return _builder.Build();
        }
    }
}
