using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class QuestContractBoardDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<QuestContractBoardViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.QuestContractBoard)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 900f, 560f)
                .SetTitle("Quest Contract Board")

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddToggleButton()
                            .SetText("Browse Contracts")
                            .BindIsToggled(model => model.IsBrowseTabToggled)
                            .BindOnClicked(model => model.OnClickBrowseTab())
                            .SetHeight(32f)
                            .SetWidth(180f);

                        row.AddToggleButton()
                            .SetText("My Contracts")
                            .BindIsToggled(model => model.IsMyContractsTabToggled)
                            .BindOnClicked(model => model.OnClickMyContractsTab())
                            .SetHeight(32f)
                            .SetWidth(180f);

                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.BindIsVisible(model => model.IsBrowseVisible);

                        row.AddColumn(left =>
                        {
                            left.SetWidth(400f);

                            left.AddRow(r =>
                            {
                                r.AddTextEdit()
                                    .SetPlaceholder("Search Title")
                                    .BindValue(model => model.SearchText);

                                r.AddButton()
                                    .SetText("X")
                                    .SetHeight(35f)
                                    .SetWidth(35f)
                                    .BindOnClicked(model => model.OnClickClearSearch());

                                r.AddButton()
                                    .SetText("Search")
                                    .SetHeight(35f)
                                    .BindOnClicked(model => model.OnClickSearch());
                            });

                            left.AddRow(r =>
                            {
                                r.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.AddToggleButton()
                                            .BindText(model => model.BrowseLabels)
                                            .BindIsToggled(model => model.BrowseToggles)
                                            .BindOnClicked(model => model.OnClickSelectContract());
                                    });
                                })
                                    .BindRowCount(model => model.BrowseLabels)
                                    .SetRowHeight(32f);
                            });
                        });

                        row.AddColumn(right =>
                        {
                            right.AddRow(r =>
                            {
                                r.AddText()
                                    .BindText(model => model.DetailText)
                                    .SetScrollbars(NuiScrollbars.Auto)
                                    .SetShowBorder(false);
                            });

                            right.AddRow(r =>
                            {
                                r.AddLabel()
                                    .BindText(model => model.StatusText)
                                    .SetColor(255, 0, 0)
                                    .SetHeight(24f);
                            });

                            right.AddRow(r =>
                            {
                                r.AddButton()
                                    .SetText("Accept")
                                    .BindIsVisible(model => model.IsAcceptVisible)
                                    .BindIsEnabled(model => model.IsAcceptEnabled)
                                    .BindOnClicked(model => model.OnClickAccept())
                                    .SetHeight(32f);

                                r.AddButton()
                                    .SetText("Turn In")
                                    .BindIsVisible(model => model.IsTurnInVisible)
                                    .BindOnClicked(model => model.OnClickTurnIn())
                                    .SetHeight(32f);

                                r.AddButton()
                                    .SetText("Abandon")
                                    .BindIsVisible(model => model.IsAbandonVisible)
                                    .BindOnClicked(model => model.OnClickAbandon())
                                    .SetHeight(32f);

                                r.AddButton()
                                    .SetText("Take Down")
                                    .BindIsVisible(model => model.IsTakeDownVisible)
                                    .BindOnClicked(model => model.OnClickTakeDown())
                                    .SetHeight(32f);
                            });
                        });
                    });

                    col.AddRow(row =>
                    {
                        row.BindIsVisible(model => model.IsMyContractsVisible);

                        row.AddColumn(inner =>
                        {
                            inner.AddRow(r =>
                            {
                                r.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.AddToggleButton()
                                            .BindText(model => model.MyContractLabels)
                                            .BindIsToggled(model => model.MyContractToggles)
                                            .BindColor(model => model.MyContractColors)
                                            .BindOnClicked(model => model.OnClickSelectMyContract());
                                    });
                                })
                                    .BindRowCount(model => model.MyContractLabels)
                                    .SetRowHeight(32f);
                            });

                            inner.AddRow(r =>
                            {
                                r.AddSpacer();

                                r.AddButton()
                                    .SetText("New/Edit Draft")
                                    .BindOnClicked(model => model.OnClickEditDraft())
                                    .SetHeight(32f)
                                    .SetWidth(150f);

                                r.AddButton()
                                    .SetText("Cancel")
                                    .BindIsEnabled(model => model.IsCancelEnabled)
                                    .BindOnClicked(model => model.OnClickCancelContract())
                                    .SetHeight(32f)
                                    .SetWidth(150f);

                                r.AddButton()
                                    .SetText("Claim Deliveries")
                                    .BindOnClicked(model => model.OnClickClaimDeliveries())
                                    .SetHeight(32f)
                                    .SetWidth(150f);

                                r.AddSpacer();
                            });
                        });
                    });
                });

            return _builder.Build();
        }
    }
}
