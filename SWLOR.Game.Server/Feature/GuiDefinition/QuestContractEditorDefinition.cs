using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class QuestContractEditorDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<QuestContractEditorViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.QuestContractEditor)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 700f, 700f)
                .SetTitle("Quest Contract Editor")

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddTextEdit()
                            .SetPlaceholder("Title")
                            .SetMaxLength(QuestContractBoard.MaxTitleLength)
                            .BindValue(model => model.Title);
                    });

                    col.AddRow(row =>
                    {
                        row.AddTextEdit()
                            .SetPlaceholder("Description")
                            .SetIsMultiline(true)
                            .SetMaxLength(QuestContractBoard.MaxDescriptionLength)
                            .SetHeight(120f)
                            .BindValue(model => model.Description);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("Save Details")
                            .BindOnClicked(model => model.OnClickSaveDetails())
                            .SetHeight(32f)
                            .SetWidth(150f);

                        row.AddLabel()
                            .BindText(model => model.StatusText)
                            .SetColor(255, 0, 0)
                            .SetHeight(24f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText($"Objectives (max {QuestContractBoard.MaxObjectives})")
                            .SetHeight(20f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddToggleButton()
                                    .BindText(model => model.ObjectiveLabels)
                                    .BindIsToggled(model => model.ObjectiveToggles)
                                    .BindOnClicked(model => model.OnClickSelectObjective());
                            });

                            template.AddCell(cell =>
                            {
                                cell.SetWidth(80f);
                                cell.SetIsVariable(false);

                                cell.AddButton()
                                    .SetText("Remove")
                                    .BindOnClicked(model => model.OnClickRemoveObjective());
                            });
                        })
                            .BindRowCount(model => model.ObjectiveLabels)
                            .SetRowHeight(32f)
                            .SetHeight(110f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("Add Objective")
                            .BindIsEnabled(model => model.IsAddObjectiveEnabled)
                            .BindOnClicked(model => model.OnClickAddObjective())
                            .SetHeight(32f)
                            .SetWidth(150f);
                    });

                    col.AddRow(row =>
                    {
                        row.BindIsVisible(model => model.IsObjectiveDetailVisible);

                        row.AddTextEdit()
                            .SetPlaceholder("Quantity")
                            .BindValue(model => model.ObjectiveQuantityText)
                            .SetWidth(100f);

                        row.AddCheckBox()
                            .SetText("Must be player-crafted")
                            .BindIsChecked(model => model.ObjectiveIsPlayerCrafted);

                        row.AddButton()
                            .SetText("Apply")
                            .BindOnClicked(model => model.OnClickApplyObjective())
                            .SetHeight(32f)
                            .SetWidth(100f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Reward Credits (per completion)")
                            .SetWidth(220f)
                            .SetHeight(28f);

                        row.AddTextEdit()
                            .SetPlaceholder("Credits")
                            .BindValue(model => model.RewardCreditsText)
                            .SetWidth(120f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText($"Completions (1-{QuestContractBoard.MaxCompletions})")
                            .SetWidth(220f)
                            .SetHeight(28f);

                        row.AddTextEdit()
                            .SetPlaceholder("Completions")
                            .BindValue(model => model.CompletionsText)
                            .SetWidth(120f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText($"Reward Items (max {QuestContractBoard.MaxRewardItems}, single-completion contracts only)")
                            .SetHeight(20f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.RewardItemHint)
                            .SetColor(255, 255, 0)
                            .SetHeight(20f);
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
                                        .BindResref(model => model.RewardItemIconResrefs)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                        .SetVerticalAlign(NuiVerticalAlign.Top);
                                });
                            });

                            template.AddCell(cell =>
                            {
                                cell.AddText()
                                    .BindText(model => model.RewardItemLabels)
                                    .SetShowBorder(false)
                                    .SetScrollbars(NuiScrollbars.None);
                            });

                            template.AddCell(cell =>
                            {
                                cell.SetWidth(80f);
                                cell.SetIsVariable(false);

                                cell.AddButton()
                                    .SetText("Remove")
                                    .BindOnClicked(model => model.OnClickRemoveRewardItem());
                            });
                        })
                            .BindRowCount(model => model.RewardItemLabels)
                            .SetRowHeight(40f)
                            .SetHeight(160f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("Add Reward Item")
                            .BindIsEnabled(model => model.IsAddRewardItemEnabled)
                            .BindOnClicked(model => model.OnClickAddRewardItem())
                            .SetHeight(32f)
                            .SetWidth(150f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.CostSummaryText)
                            .SetHeight(24f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddButton()
                            .SetText("Publish")
                            .BindOnClicked(model => model.OnClickPublish())
                            .SetHeight(35f)
                            .SetWidth(150f);

                        row.AddButton()
                            .SetText("Close")
                            .BindOnClicked(model => model.OnClickClose())
                            .SetHeight(35f)
                            .SetWidth(150f);

                        row.AddSpacer();
                    });
                });

            return _builder.Build();
        }
    }
}
