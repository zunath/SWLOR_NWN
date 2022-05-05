using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    internal class BankDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<BankViewModel> _builder = new();


        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Bank)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 476.57895f, 530.2632f)
                .SetTitle("Bank")
                
                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddProgressBar()
                            .BindValue(model => model.StoragePercentage)
                            .BindTooltip(model => model.ItemCountText);
                    });

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
                    });

                    col.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.SetWidth(32f);
                                cell.SetIsVariable(false);

                                cell.AddGroup(group =>
                                {
                                    group.AddImage()
                                        .BindResref(model => model.ItemResrefs)
                                        .SetMargin(0f);
                                })
                                    .SetScrollbars(NuiScrollbars.None);
                            });

                            template.AddCell(cell =>
                            {
                                cell.AddLabel()
                                    .BindText(model => model.ItemNames)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetVerticalAlign(NuiVerticalAlign.Middle);
                            });

                            template.AddCell(cell =>
                            {
                                cell.SetWidth(75f);
                                cell.SetIsVariable(false);

                                cell.AddButton()
                                    .SetText("Withdraw")
                                    .BindOnClicked(model => model.OnClickWithdraw())
                                    .SetHeight(35f);
                            });

                        })
                            .BindRowCount(model => model.ItemNames);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddButton()
                            .SetText("Deposit")
                            .BindOnClicked(model => model.OnClickDeposit())
                            .SetHeight(35f)
                            .BindIsEnabled(model => model.IsDepositEnabled);
                        row.AddSpacer();
                    });

                });


            return _builder.Build();
        }
    }
}
