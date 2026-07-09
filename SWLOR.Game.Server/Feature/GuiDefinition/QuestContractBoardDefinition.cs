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
                .SetInitialGeometry(0, 0, 760f, 470f)
                .SetTitle("Quest Contract Board")

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.SetHeight(40f);

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
                        row.AddColumn(left =>
                        {
                            left.SetWidth(360f);

                            left.AddRow(r =>
                            {
                                r.SetHeight(36f);
                                r.BindIsVisible(model => model.IsSearchVisible);

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
                                            .BindText(model => model.RowLabels)
                                            .BindIsToggled(model => model.RowToggles)
                                            .BindColor(model => model.RowColors)
                                            .BindOnClicked(model => model.OnClickSelectRow());
                                    });
                                })
                                    .BindRowCount(model => model.RowLabels)
                                    .SetRowHeight(32f);
                            });
                        });

                        row.AddColumn(right =>
                        {
                            right.AddRow(r =>
                            {
                                r.AddText()
                                    .BindText(model => model.DetailText);
                            });

                            right.AddRow(r =>
                            {
                                r.SetHeight(24f);

                                r.AddLabel()
                                    .BindText(model => model.StatusText)
                                    .SetColor(255, 0, 0);
                            });

                            right.AddRow(r =>
                            {
                                r.SetHeight(40f);
                                r.BindIsVisible(model => model.IsBrowseActionsVisible);

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

                            right.AddRow(r =>
                            {
                                r.SetHeight(40f);
                                r.BindIsVisible(model => model.IsMyActionsVisible);

                                r.AddButton()
                                    .SetText("New/Edit Draft")
                                    .BindOnClicked(model => model.OnClickEditDraft())
                                    .SetHeight(32f);

                                r.AddButton()
                                    .SetText("Cancel")
                                    .BindIsEnabled(model => model.IsCancelEnabled)
                                    .BindOnClicked(model => model.OnClickCancelContract())
                                    .SetHeight(32f);

                                r.AddButton()
                                    .SetText("Claim Deliveries")
                                    .BindOnClicked(model => model.OnClickClaimDeliveries())
                                    .SetHeight(32f);
                            });
                        });
                    });
                });

            return _builder.Build();
        }
    }
}
