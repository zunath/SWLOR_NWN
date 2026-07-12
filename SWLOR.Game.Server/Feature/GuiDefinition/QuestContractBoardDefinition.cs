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
                .SetInitialGeometry(0, 0, 860f, 560f)
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
                            left.AddRow(r =>
                            {
                                r.SetHeight(40f);
                                r.BindIsVisible(model => model.IsSearchVisible);

                                r.AddTextEdit()
                                    .SetPlaceholder("Search Title")
                                    .BindValue(model => model.SearchText);

                                r.AddButton()
                                    .SetText("X")
                                    .SetHeight(32f)
                                    .SetWidth(35f)
                                    .BindOnClicked(model => model.OnClickClearSearch());

                                r.AddButton()
                                    .SetText("Search")
                                    .SetHeight(32f)
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
                                r.SetHeight(20f);
                                r.BindIsVisible(model => model.IsContractSelected);

                                r.AddLabel()
                                    .SetText("Objectives");
                            });

                            right.AddRow(r =>
                            {
                                r.SetHeight(84f);
                                r.BindIsVisible(model => model.IsContractSelected);

                                r.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.SetWidth(32f);
                                        cell.SetIsVariable(false);

                                        cell.AddGroup(group =>
                                        {
                                            group.AddImage()
                                                .BindResref(model => model.ObjectiveIconResrefs)
                                                .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                                .SetVerticalAlign(NuiVerticalAlign.Top);
                                        });
                                    });

                                    template.AddCell(cell =>
                                    {
                                        cell.AddText()
                                            .BindText(model => model.ObjectiveLabels)
                                            .SetShowBorder(false)
                                            .SetScrollbars(NuiScrollbars.None);
                                    });

                                    template.AddCell(cell =>
                                    {
                                        cell.SetWidth(80f);
                                        cell.SetIsVariable(false);

                                        cell.AddButton()
                                            .SetText("Examine")
                                            .BindOnClicked(model => model.OnClickExamineObjective());
                                    });
                                })
                                    .BindRowCount(model => model.ObjectiveLabels)
                                    .SetRowHeight(32f);
                            });

                            right.AddRow(r =>
                            {
                                r.SetHeight(20f);
                                r.BindIsVisible(model => model.IsContractSelected);

                                r.AddLabel()
                                    .SetText("Reward Items");
                            });

                            right.AddRow(r =>
                            {
                                r.SetHeight(84f);
                                r.BindIsVisible(model => model.IsContractSelected);

                                r.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.SetWidth(32f);
                                        cell.SetIsVariable(false);

                                        cell.AddGroup(group =>
                                        {
                                            group.AddImage()
                                                .BindResref(model => model.RewardIconResrefs)
                                                .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                                .SetVerticalAlign(NuiVerticalAlign.Top);
                                        });
                                    });

                                    template.AddCell(cell =>
                                    {
                                        cell.AddText()
                                            .BindText(model => model.RewardLabels)
                                            .SetShowBorder(false)
                                            .SetScrollbars(NuiScrollbars.None);
                                    });

                                    template.AddCell(cell =>
                                    {
                                        cell.SetWidth(80f);
                                        cell.SetIsVariable(false);

                                        cell.AddButton()
                                            .SetText("Examine")
                                            .BindOnClicked(model => model.OnClickExamineReward());
                                    });
                                })
                                    .BindRowCount(model => model.RewardLabels)
                                    .SetRowHeight(32f);
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
                                    .SetHeight(32f)
                                    .SetWidth(100f);

                                r.AddButton()
                                    .SetText("Turn In")
                                    .BindIsVisible(model => model.IsTurnInVisible)
                                    .BindOnClicked(model => model.OnClickTurnIn())
                                    .SetHeight(32f)
                                    .SetWidth(100f);

                                r.AddButton()
                                    .SetText("Abandon")
                                    .BindIsVisible(model => model.IsAbandonVisible)
                                    .BindOnClicked(model => model.OnClickAbandon())
                                    .SetHeight(32f)
                                    .SetWidth(100f);

                                r.AddButton()
                                    .SetText("Take Down")
                                    .BindIsVisible(model => model.IsTakeDownVisible)
                                    .BindOnClicked(model => model.OnClickTakeDown())
                                    .SetHeight(32f)
                                    .SetWidth(110f);
                            });

                            right.AddRow(r =>
                            {
                                r.SetHeight(40f);
                                r.BindIsVisible(model => model.IsMyActionsVisible);

                                r.AddButton()
                                    .SetText("New Contract")
                                    .BindIsEnabled(model => model.IsNewContractEnabled)
                                    .BindOnClicked(model => model.OnClickNewContract())
                                    .SetHeight(32f)
                                    .SetWidth(110f);

                                r.AddButton()
                                    .SetText("Edit Draft")
                                    .BindIsEnabled(model => model.IsEditDraftEnabled)
                                    .BindOnClicked(model => model.OnClickEditDraft())
                                    .SetHeight(32f)
                                    .SetWidth(90f);

                                r.AddButton()
                                    .BindText(model => model.CancelButtonText)
                                    .BindIsEnabled(model => model.IsCancelEnabled)
                                    .BindOnClicked(model => model.OnClickCancelContract())
                                    .SetHeight(32f)
                                    .SetWidth(110f);

                                r.AddButton()
                                    .SetText("Claim Deliveries")
                                    .BindIsEnabled(model => model.IsClaimDeliveriesEnabled)
                                    .SetDisabledTooltip("You have no deliveries waiting to be claimed.")
                                    .BindOnClicked(model => model.OnClickClaimDeliveries())
                                    .SetHeight(32f)
                                    .SetWidth(140f);
                            });
                        });
                    });
                });

            return _builder.Build();
        }
    }
}
