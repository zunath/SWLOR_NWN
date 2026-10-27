using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// A dock's Trade Exchange (plan section 13): local buy and sell prices and the cargo of a ship docked here.
    /// </summary>
    public class TradeExchangeDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<TradeExchangeViewModel> _builder = new();

        private const float ContentPanelWidth = 560f;
        private const float LabelHeight = 20f;
        private const float ButtonHeight = 32f;
        private const float RoleWidth = 96f;
        private const float PriceWidth = 56f;
        private const float AboardWidth = 56f;
        private const float ButtonWidth = 56f;

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.TradeExchange)
                .SetInitialGeometry(0, 0, 610f, 640f)
                .SetTitle("Trade Exchange")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .DefinePartialView(TradeExchangeViewModel.MainContentPartial, AddMainContent);

            window.AddStandardLayout(layout => layout.SetContentPartialElement(TradeExchangeViewModel.MainContentElement));

            return _builder.Build();
        }

        private static void AddMainContent(GuiGroup<TradeExchangeViewModel> host)
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
                            AddShipRows(content);
                            AddMarketList(content);
                        });
                    })
                        .SetWidth(ContentPanelWidth);
                });
            });
        }

        private static void AddShipRows(GuiColumn<TradeExchangeViewModel> content)
        {
            content.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.ExchangeName)
                    .SetColor(GuiColor.Cyan)
                    .SetHeight(LabelHeight)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });

            content.AddRow(row =>
            {
                row.AddLabel()
                    .SetText("Ship")
                    .SetWidth(60f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                row.AddComboBox()
                    .BindOptions(model => model.ShipOptions)
                    .BindSelectedIndex(model => model.SelectedShipIndex)
                    .BindIsEnabled(model => model.HasShip)
                    .SetTooltip("Your ships docked here. The exchange trades with the cargo bay of the ship you pick.")
                    .SetWidth(300f)
                    .SetHeight(ButtonHeight);
                row.AddSpacer();
            });

            content.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.CargoText)
                    .SetHeight(LabelHeight)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                row.AddLabel()
                    .BindText(model => model.CreditsText)
                    .SetHeight(LabelHeight)
                    .SetWidth(200f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Right);
            });

            content.AddRow(row =>
            {
                row.AddLabel()
                    .SetText("Units per order")
                    .SetWidth(120f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                row.AddTextEdit()
                    .SetPlaceholder("Units")
                    .BindValue(model => model.UnitsText)
                    .SetMaxLength(5)
                    .SetWidth(90f)
                    .SetHeight(ButtonHeight);
                row.AddLabel()
                    .SetText("Buy takes what fits and what you can afford.")
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });

            content.AddRow(row =>
            {
                row.AddLabel()
                    .SetText("No ship of yours is docked here. Dock at this starport to trade.")
                    .BindIsVisible(model => model.NoShipVisible)
                    .SetColor(GuiColor.Red)
                    .SetHeight(LabelHeight)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });
        }

        private static void AddMarketList(GuiColumn<TradeExchangeViewModel> content)
        {
            content.AddRow(row =>
            {
                row.AddLabel().SetText("Commodity").SetHeight(LabelHeight).SetHorizontalAlign(NuiHorizontalAlign.Left).SetColor(GuiColor.Cyan);
                row.AddLabel().SetText("Here").SetWidth(RoleWidth).SetHeight(LabelHeight).SetHorizontalAlign(NuiHorizontalAlign.Left).SetColor(GuiColor.Cyan);
                row.AddLabel().SetText("Buy").SetWidth(PriceWidth).SetHeight(LabelHeight).SetHorizontalAlign(NuiHorizontalAlign.Right).SetColor(GuiColor.Cyan);
                row.AddLabel().SetText("Sell").SetWidth(PriceWidth).SetHeight(LabelHeight).SetHorizontalAlign(NuiHorizontalAlign.Right).SetColor(GuiColor.Cyan);
                row.AddLabel().SetText("Aboard").SetWidth(AboardWidth).SetHeight(LabelHeight).SetHorizontalAlign(NuiHorizontalAlign.Right).SetColor(GuiColor.Cyan);
                row.AddSpacer().SetWidth(ButtonWidth * 2 + 16f);
            });

            content.AddRow(row =>
            {
                row.AddList(template =>
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
                        cell.SetWidth(RoleWidth);
                        cell.AddLabel()
                            .BindText(model => model.RowRoles)
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
                        cell.SetWidth(PriceWidth);
                        cell.AddLabel()
                            .BindText(model => model.RowSellPrices)
                            .SetHorizontalAlign(NuiHorizontalAlign.Right);
                    });
                    template.AddCell(cell =>
                    {
                        cell.SetIsVariable(false);
                        cell.SetWidth(AboardWidth);
                        cell.AddLabel()
                            .BindText(model => model.RowAboard)
                            .SetHorizontalAlign(NuiHorizontalAlign.Right);
                    });
                    template.AddCell(cell =>
                    {
                        cell.SetIsVariable(false);
                        cell.SetWidth(ButtonWidth);
                        cell.AddButton()
                            .SetText("Buy")
                            .BindIsEnabled(model => model.RowCanBuy)
                            .SetTooltip("Buys the units per order at the Buy price, which rises as you buy.")
                            .BindOnClicked(model => model.OnClickBuy());
                    });
                    template.AddCell(cell =>
                    {
                        cell.SetIsVariable(false);
                        cell.SetWidth(ButtonWidth);
                        cell.AddButton()
                            .SetText("Sell")
                            .BindIsEnabled(model => model.RowCanSell)
                            .SetTooltip("Sells the units per order at the Sell price, which falls as you sell.")
                            .BindOnClicked(model => model.OnClickSell());
                    });
                })
                    .BindRowCount(model => model.RowNames)
                    .SetRowHeight(32f);
            });

            content.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.OtherCargoText)
                    .BindIsVisible(model => model.OtherCargoVisible)
                    .SetHeight(LabelHeight)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });
        }
    }
}
