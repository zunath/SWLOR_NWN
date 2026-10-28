using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// A dock's Trade Exchange (plan section 13): the Market tab trades commodities with a ship docked here at local
    /// prices; the Freight tab offers this exchange's freight contracts and delivers the player's.
    /// </summary>
    public class TradeExchangeDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<TradeExchangeViewModel> _builder = new();

        private const float TabRowHeight = 28f;
        private const float TabPanelHeight = 48f;
        private const float ContentPanelWidth = 610f;
        private const float LabelHeight = 20f;
        private const float ButtonHeight = 32f;
        private const float RoleWidth = 96f;
        private const float PriceWidth = 56f;
        private const float AboardWidth = 56f;
        private const float ButtonWidth = 56f;
        private const float WideButtonWidth = 76f;
        private const float RouteWidth = 150f;
        private const float NumberWidth = 54f;

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.TradeExchange)
                .SetInitialGeometry(0, 0, 660f, 680f)
                .SetTitle("Trade Exchange")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .DefinePartialView(TradeExchangeViewModel.MarketTabPartial, AddMarketTab)
                .DefinePartialView(TradeExchangeViewModel.FreightTabPartial, AddFreightTab);

            window.AddStandardLayout(layout =>
            {
                layout.SetTabPanelHeight(TabPanelHeight);
                layout.AddTabRow(row =>
                {
                    row.SetHeight(TabRowHeight);
                    row.AddToggles()
                        .AddOption("Market")
                        .AddOption("Freight")
                        .BindSelectedValue(model => model.TabToggleValue)
                        .SetWidth(232f)
                        .SetHeight(TabRowHeight);
                });
                layout.SetContentPartialElement(TradeExchangeViewModel.TabContentElement);
            });

            return _builder.Build();
        }

        private static void AddPanel(GuiGroup<TradeExchangeViewModel> host, System.Action<GuiColumn<TradeExchangeViewModel>> content)
        {
            host.AddColumn(col => col.AddRow(row => row.AddGroup(panel =>
            {
                panel.SetShowBorder(false);
                panel.SetScrollbars(NuiScrollbars.None);
                panel.AddColumn(content);
            }).SetWidth(ContentPanelWidth)));
        }

        private static void AddMarketTab(GuiGroup<TradeExchangeViewModel> host)
        {
            AddPanel(host, content =>
            {
                AddShipRows(content);
                AddUnitsRow(content);
                AddMarketList(content);
                AddHoloNetRow(content);
            });
        }

        private static void AddFreightTab(GuiGroup<TradeExchangeViewModel> host)
        {
            AddPanel(host, content =>
            {
                AddShipRows(content);
                AddOfferList(content);
                AddContractList(content);
                AddHoloNetRow(content);
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
                    .BindText(model => model.StandingText)
                    .BindIsVisible(model => model.StandingVisible)
                    .SetHeight(LabelHeight)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });

            content.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.TariffText)
                    .BindIsVisible(model => model.TariffVisible)
                    .SetColor(new GuiColor(255, 200, 0))
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
                    .SetText("No ship of yours is docked here. Dock at this starport to trade.")
                    .BindIsVisible(model => model.NoShipVisible)
                    .SetColor(GuiColor.Red)
                    .SetHeight(LabelHeight)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });
        }

        private static void AddUnitsRow(GuiColumn<TradeExchangeViewModel> content)
        {
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

        private static void AddOfferList(GuiColumn<TradeExchangeViewModel> content)
        {
            content.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.OffersTitle)
                    .SetColor(GuiColor.Cyan)
                    .SetHeight(LabelHeight)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });

            content.AddRow(row =>
            {
                row.AddLabel().SetText("Sealed load to").SetHeight(LabelHeight).SetHorizontalAlign(NuiHorizontalAlign.Left).SetColor(GuiColor.Cyan);
                row.AddLabel().SetText("Volume").SetWidth(NumberWidth).SetHeight(LabelHeight).SetHorizontalAlign(NuiHorizontalAlign.Right).SetColor(GuiColor.Cyan);
                row.AddLabel().SetText("Fee").SetWidth(NumberWidth).SetHeight(LabelHeight).SetHorizontalAlign(NuiHorizontalAlign.Right).SetColor(GuiColor.Cyan);
                row.AddLabel().SetText("Deposit").SetWidth(NumberWidth).SetHeight(LabelHeight).SetHorizontalAlign(NuiHorizontalAlign.Right).SetColor(GuiColor.Cyan);
                row.AddLabel().SetText("Minutes").SetWidth(NumberWidth).SetHeight(LabelHeight).SetHorizontalAlign(NuiHorizontalAlign.Right).SetColor(GuiColor.Cyan);
                row.AddSpacer().SetWidth(WideButtonWidth + 8f);
            });

            content.AddRow(row =>
            {
                row.AddList(template =>
                {
                    template.AddCell(cell =>
                    {
                        cell.AddLabel()
                            .BindText(model => model.OfferRoutes)
                            .BindTooltip(model => model.OfferTooltips)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                    });
                    template.AddCell(cell =>
                    {
                        cell.SetIsVariable(false);
                        cell.SetWidth(NumberWidth);
                        cell.AddLabel()
                            .BindText(model => model.OfferVolumes)
                            .SetHorizontalAlign(NuiHorizontalAlign.Right);
                    });
                    template.AddCell(cell =>
                    {
                        cell.SetIsVariable(false);
                        cell.SetWidth(NumberWidth);
                        cell.AddLabel()
                            .BindText(model => model.OfferFees)
                            .SetHorizontalAlign(NuiHorizontalAlign.Right);
                    });
                    template.AddCell(cell =>
                    {
                        cell.SetIsVariable(false);
                        cell.SetWidth(NumberWidth);
                        cell.AddLabel()
                            .BindText(model => model.OfferDeposits)
                            .SetHorizontalAlign(NuiHorizontalAlign.Right);
                    });
                    template.AddCell(cell =>
                    {
                        cell.SetIsVariable(false);
                        cell.SetWidth(NumberWidth);
                        cell.AddLabel()
                            .BindText(model => model.OfferMinutes)
                            .SetHorizontalAlign(NuiHorizontalAlign.Right);
                    });
                    template.AddCell(cell =>
                    {
                        cell.SetIsVariable(false);
                        cell.SetWidth(WideButtonWidth);
                        cell.AddButton()
                            .SetText("Accept")
                            .BindIsEnabled(model => model.OfferCanAccept)
                            .SetTooltip("Pay the deposit and load the sealed freight into the ship you picked.")
                            .BindOnClicked(model => model.OnClickAcceptOffer());
                    });
                })
                    .BindRowCount(model => model.OfferRoutes)
                    .SetRowHeight(32f);
                row.SetHeight(150f);
            });

            content.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.ContractsTitle)
                    .SetColor(GuiColor.Cyan)
                    .SetHeight(LabelHeight)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });
        }

        private static void AddContractList(GuiColumn<TradeExchangeViewModel> content)
        {
            content.AddRow(row =>
            {
                row.AddList(template =>
                {
                    template.AddCell(cell =>
                    {
                        cell.AddLabel()
                            .BindText(model => model.ContractRoutes)
                            .BindTooltip(model => model.ContractTooltips)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                    });
                    template.AddCell(cell =>
                    {
                        cell.SetIsVariable(false);
                        cell.SetWidth(RouteWidth);
                        cell.AddLabel()
                            .BindText(model => model.ContractDue)
                            .BindColor(model => model.ContractDueColors)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                    });
                    template.AddCell(cell =>
                    {
                        cell.SetIsVariable(false);
                        cell.SetWidth(WideButtonWidth);
                        cell.AddButton()
                            .SetText("Deliver")
                            .BindIsEnabled(model => model.ContractCanDeliver)
                            .SetTooltip("Hand over the sealed load from the ship you picked for the fee, and the deposit when on time.")
                            .BindOnClicked(model => model.OnClickDeliver());
                    });
                    template.AddCell(cell =>
                    {
                        cell.SetIsVariable(false);
                        cell.SetWidth(WideButtonWidth);
                        cell.AddButton()
                            .SetText("Abandon")
                            .SetTooltip("Give up the contract. The sealed load is unloaded and the deposit is forfeit.")
                            .BindOnClicked(model => model.OnClickAbandon());
                    });
                })
                    .BindRowCount(model => model.ContractRoutes)
                    .SetRowHeight(32f);
                row.SetHeight(120f);
            });
        }

        private static void AddHoloNetRow(GuiColumn<TradeExchangeViewModel> content)
        {
            content.AddRow(row =>
            {
                row.AddButton()
                    .SetText("HoloNet Prices")
                    .SetTooltip("Best buy and sell prices across the exchanges, up to an hour old.")
                    .BindOnClicked(model => model.OnClickHoloNet())
                    .SetWidth(150f)
                    .SetHeight(ButtonHeight);
                row.AddSpacer();
            });
        }
    }
}
