using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Shared.Core.Beamdog;

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
                                row2.AddList(template =>
                                {

                                    template.AddCell(cell =>
                                    {
                                        cell.SetWidth(40f);
                                        cell.SetIsVariable(false);

                                        cell.AddGroup(group =>
                                        {
                                            group.AddImage()
                                                .BindResref(model => model.ItemIconResrefs)
                                                .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                                .SetVerticalAlign(NuiVerticalAlign.Top)
                                                .BindTooltip(model => model.ItemNames);
                                        });
                                    })
                                        .SetPadding(50f);

                                    template.AddCell(cell =>
                                    {
                                        cell.AddText()
                                            .BindText(model => model.ItemNames)
                                            .BindTooltip(model => model.ItemNames);
                                    });

                                    template.AddCell(cell =>
                                    {
                                        cell.SetIsVariable(false);
                                        cell.SetWidth(120f);

                                        cell.AddLabel()
                                            .BindText(model => model.ItemPriceNames);
                                    });

                                    template.AddCell(cell =>
                                    {
                                        cell.AddLabel()
                                            .BindText(model => model.ItemSellerNames)
                                            .BindTooltip(model => model.ItemSellerNames);
                                    });

                                    template.AddCell(cell =>
                                    {
                                        cell.SetWidth(40f);
                                        cell.SetIsVariable(false);
                                        cell.AddButton()
                                            .SetText("?")
                                            .SetWidth(40f)
                                            .SetHeight(40f)
                                            .BindOnClicked(model => model.OnClickExamine());
                                    });

                                    template.AddCell(cell =>
                                    {
                                        cell.AddButton()
                                            .SetText("Buy")
                                            .BindOnClicked(model => model.OnClickBuy())
                                            .BindIsEnabled(model => model.ItemBuyEnabled);
                                    });
                                })
                                    .BindRowCount(model => model.ItemNames)
                                    .SetRowHeight(40f);
                            });

                            col2.AddRow(row2 =>
                            {
                                row2.AddSpacer();
                                row2.AddButton()
                                    .SetText("<")
                                    .SetWidth(32f)
                                    .SetHeight(35f)
                                    .BindOnClicked(model => model.OnClickPreviousPage());

                                row2.AddComboBox()
                                    .BindOptions(model => model.PageNumbers)
                                    .BindSelectedIndex(model => model.SelectedPageIndex);

                                row2.AddButton()
                                    .SetText(">")
                                    .SetWidth(32f)
                                    .SetHeight(35f)
                                    .BindOnClicked(model => model.OnClickNextPage());

                                row2.AddSpacer();
                            });
                        });

                    });
                });


            return _builder.Build();
        }
    }
}
