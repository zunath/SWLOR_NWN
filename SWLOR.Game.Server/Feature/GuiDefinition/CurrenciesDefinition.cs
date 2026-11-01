using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class CurrenciesDefinition: IGuiWindowDefinition
    {
        private const float UseCellWidth = 60f;

        private readonly GuiWindowBuilder<CurrenciesViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Currencies)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 480f, 340f)
                .SetTitle("Currencies")

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.SetHeight(24f);

                        row.AddSpacer()
                            .SetWidth(40f);

                        row.AddLabel()
                            .SetText("Currency")
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Middle);

                        row.AddSpacer()
                            .SetWidth(UseCellWidth);

                        row.AddLabel()
                            .SetText("Balance")
                            .SetWidth(90f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Right)
                            .SetVerticalAlign(NuiVerticalAlign.Middle);

                        row.AddSpacer()
                            .SetWidth(18f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.SetWidth(40f);
                                cell.SetIsVariable(false);

                                cell.AddGroup(group =>
                                {
                                    group.AddImage()
                                        .BindResref(model => model.CurrencyIcons)
                                        .SetWidth(28f)
                                        .SetHeight(28f)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                        .SetVerticalAlign(NuiVerticalAlign.Middle)
                                        .BindTooltip(model => model.CurrencyDescriptions);
                                })
                                    .SetShowBorder(false)
                                    .SetScrollbars(NuiScrollbars.None);
                            });

                            template.AddCell(cell =>
                            {
                                cell.AddLabel()
                                    .BindText(model => model.CurrencyNames)
                                    .BindTooltip(model => model.CurrencyDescriptions)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetVerticalAlign(NuiVerticalAlign.Middle);
                            });

                            template.AddCell(cell =>
                            {
                                cell.SetWidth(UseCellWidth);
                                cell.SetIsVariable(false);

                                // The group takes the list's row visibility so the button keeps its own.
                                cell.AddGroup(group =>
                                {
                                    group.AddButton()
                                        .SetText("Use")
                                        .SetWidth(52f)
                                        .SetHeight(28f)
                                        .SetMargin(0f)
                                        .SetTooltip("Spend a Language Token to master a language.")
                                        .BindIsVisible(model => model.CurrencyHasUse)
                                        .BindIsEnabled(model => model.CurrencyCanUse)
                                        .BindOnClicked(model => model.OnClickUse());
                                })
                                    .SetShowBorder(false)
                                    .SetScrollbars(NuiScrollbars.None)
                                    .SetPadding(0f);
                            });

                            template.AddCell(cell =>
                            {
                                cell.SetWidth(90f);
                                cell.SetIsVariable(false);

                                cell.AddLabel()
                                    .BindText(model => model.CurrencyAmountText)
                                    .BindTooltip(model => model.CurrencyDescriptions)
                                    .SetColor(255, 236, 155)
                                    .SetPadding(4f)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Right)
                                    .SetVerticalAlign(NuiVerticalAlign.Middle);
                            });

                            template.AddCell(cell =>
                            {
                                cell.SetWidth(18f);
                                cell.SetIsVariable(false);

                                cell.AddSpacer();
                            });
                        })
                            .BindRowCount(model => model.CurrencyNames)
                            .SetRowHeight(40f);
                    });
                })

                ;

            return _builder.Build();
        }
    }
}
