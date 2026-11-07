using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// The HoloNet price board (plan section 13): read-only, every commodity's cheapest exchange and best-paying
    /// exchange from a snapshot up to an hour old.
    /// </summary>
    public class HoloNetBoardDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<HoloNetBoardViewModel> _builder = new();

        private const float ContentPanelWidth = 640f;
        private const float LabelHeight = 20f;
        private const float ExchangeWidth = 130f;
        private const float PriceWidth = 56f;

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.HoloNetBoard)
                .SetInitialGeometry(0, 0, 690f, 640f)
                .SetTitle("HoloNet Price Board")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .DefinePartialView(HoloNetBoardViewModel.MainContentPartial, AddMainContent);

            window.AddStandardLayout(layout => layout.SetContentPartialElement(HoloNetBoardViewModel.MainContentElement));

            return _builder.Build();
        }

        private static void AddMainContent(GuiGroup<HoloNetBoardViewModel> host)
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
                            content.AddRow(line =>
                            {
                                line.AddLabel()
                                    .BindText(model => model.AgeText)
                                    .SetColor(GuiColor.Cyan)
                                    .SetHeight(LabelHeight)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                            });

                            content.AddRow(line =>
                            {
                                line.AddLabel().SetText("Commodity").SetHeight(LabelHeight).SetHorizontalAlign(NuiHorizontalAlign.Left).SetColor(GuiColor.Cyan);
                                line.AddLabel().SetText("Cheapest at").SetWidth(ExchangeWidth).SetHeight(LabelHeight).SetHorizontalAlign(NuiHorizontalAlign.Left).SetColor(GuiColor.Cyan);
                                line.AddLabel().SetText("Buy").SetWidth(PriceWidth).SetHeight(LabelHeight).SetHorizontalAlign(NuiHorizontalAlign.Right).SetColor(GuiColor.Cyan);
                                line.AddLabel().SetText("Pays most at").SetWidth(ExchangeWidth).SetHeight(LabelHeight).SetHorizontalAlign(NuiHorizontalAlign.Left).SetColor(GuiColor.Cyan);
                                line.AddLabel().SetText("Sell").SetWidth(PriceWidth).SetHeight(LabelHeight).SetHorizontalAlign(NuiHorizontalAlign.Right).SetColor(GuiColor.Cyan);
                            });

                            content.AddRow(line =>
                            {
                                line.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.AddLabel()
                                            .BindText(model => model.RowNames)
                                            .BindTooltip(model => model.RowTooltips)
                                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                    });
                                    template.AddCell(cell =>
                                    {
                                        cell.SetIsVariable(false);
                                        cell.SetWidth(ExchangeWidth);
                                        cell.AddLabel()
                                            .BindText(model => model.RowBuyAt)
                                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                    });
                                    template.AddCell(cell =>
                                    {
                                        cell.SetIsVariable(false);
                                        cell.SetWidth(PriceWidth);
                                        cell.AddLabel()
                                            .BindText(model => model.RowBuyPrices)
                                            .SetHorizontalAlign(NuiHorizontalAlign.Right);
                                    });
                                    template.AddCell(cell =>
                                    {
                                        cell.SetIsVariable(false);
                                        cell.SetWidth(ExchangeWidth);
                                        cell.AddLabel()
                                            .BindText(model => model.RowSellAt)
                                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                    });
                                    template.AddCell(cell =>
                                    {
                                        cell.SetIsVariable(false);
                                        cell.SetWidth(PriceWidth);
                                        cell.AddLabel()
                                            .BindText(model => model.RowSellPrices)
                                            .SetHorizontalAlign(NuiHorizontalAlign.Right);
                                    });
                                })
                                    .BindRowCount(model => model.RowNames)
                                    .SetRowHeight(26f);
                            });
                        });
                    })
                        .SetWidth(ContentPanelWidth);
                });
            });
        }
    }
}
