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
                .SetInitialGeometry(0, 0, 840f, 600f)
                .SetTitle("Quest Contract Editor")

                .AddRow(root =>
                {
                    root.AddColumn(left =>
                    {
                        left.SetWidth(400f);

                        left.AddRow(row =>
                        {
                            row.SetHeight(34f);

                            row.AddTextEdit()
                                .SetPlaceholder("Title")
                                .SetMaxLength(QuestContractBoard.MaxTitleLength)
                                .BindValue(model => model.Title);
                        });

                        left.AddRow(row =>
                        {
                            row.SetHeight(96f);

                            row.AddTextEdit()
                                .SetPlaceholder("Description")
                                .SetIsMultiline(true)
                                .SetMaxLength(QuestContractBoard.MaxDescriptionLength)
                                .BindValue(model => model.Description);
                        });

                        left.AddRow(row =>
                        {
                            row.SetHeight(32f);

                            row.AddLabel()
                                .SetText("Reward Credits (per completion)")
                                .SetWidth(220f);

                            row.AddTextEdit()
                                .SetPlaceholder("Credits")
                                .BindValue(model => model.RewardCreditsText)
                                .SetWidth(120f);
                        });

                        left.AddRow(row =>
                        {
                            row.SetHeight(32f);

                            row.AddLabel()
                                .SetText($"Completions (1-{QuestContractBoard.MaxCompletions})")
                                .SetWidth(220f);

                            row.AddTextEdit()
                                .SetPlaceholder("Completions")
                                .BindValue(model => model.CompletionsText)
                                .SetWidth(120f);
                        });

                        left.AddRow(row =>
                        {
                            row.SetHeight(24f);

                            row.AddLabel()
                                .BindText(model => model.CostSummaryText);
                        });

                        left.AddRow(row =>
                        {
                            row.SetHeight(20f);

                            row.AddLabel()
                                .SetText($"Reward Items (max {QuestContractBoard.MaxRewardItems}, single-completion only)");
                        });

                        left.AddRow(row =>
                        {
                            row.SetHeight(20f);

                            row.AddLabel()
                                .BindText(model => model.RewardItemHint)
                                .SetColor(255, 255, 0);
                        });

                        left.AddRow(row =>
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
                                .SetRowHeight(40f);
                        });

                        left.AddRow(row =>
                        {
                            row.SetHeight(32f);

                            row.AddButton()
                                .SetText("Add Reward Item")
                                .BindIsEnabled(model => model.IsAddRewardItemEnabled)
                                .BindOnClicked(model => model.OnClickAddRewardItem())
                                .SetWidth(180f);
                        });

                        left.AddRow(row =>
                        {
                            row.SetHeight(22f);

                            row.AddLabel()
                                .BindText(model => model.StatusText)
                                .SetColor(255, 0, 0);
                        });

                        left.AddRow(row =>
                        {
                            row.SetHeight(35f);

                            row.AddButton()
                                .SetText("Save Draft")
                                .BindOnClicked(model => model.OnClickSaveDetails())
                                .SetHeight(35f);

                            row.AddButton()
                                .SetText("Publish")
                                .BindOnClicked(model => model.OnClickPublish())
                                .SetHeight(35f);

                            row.AddButton()
                                .SetText("Close")
                                .BindOnClicked(model => model.OnClickClose())
                                .SetHeight(35f);
                        });
                    });

                    root.AddColumn(right =>
                    {
                        right.SetWidth(400f);

                        right.AddRow(row =>
                        {
                            row.SetHeight(20f);

                            row.AddLabel()
                                .SetText("Search for an item to request:");
                        });

                        right.AddRow(row =>
                        {
                            row.SetHeight(34f);

                            row.AddTextEdit()
                                .SetPlaceholder("Item name")
                                .BindValue(model => model.ItemSearchText);

                            row.AddButton()
                                .SetText("Search")
                                .SetHeight(34f)
                                .SetWidth(90f)
                                .BindOnClicked(model => model.OnClickSearchItems());
                        });

                        right.AddRow(row =>
                        {
                            row.AddList(template =>
                            {
                                template.AddCell(cell =>
                                {
                                    cell.AddToggleButton()
                                        .BindText(model => model.SearchResultLabels)
                                        .BindIsToggled(model => model.SearchResultToggles)
                                        .BindOnClicked(model => model.OnClickSelectSearchResult());
                                });
                            })
                                .BindRowCount(model => model.SearchResultLabels)
                                .SetRowHeight(28f);
                        });

                        right.AddRow(row =>
                        {
                            row.SetHeight(32f);

                            row.AddButton()
                                .SetText("Add as Objective")
                                .BindIsEnabled(model => model.IsAddObjectiveEnabled)
                                .BindOnClicked(model => model.OnClickAddObjective())
                                .SetWidth(180f);
                        });

                        right.AddRow(row =>
                        {
                            row.SetHeight(20f);

                            row.AddLabel()
                                .SetText($"Current Objectives (max {QuestContractBoard.MaxObjectives})");
                        });

                        right.AddRow(row =>
                        {
                            row.SetHeight(120f);

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
                                .SetRowHeight(32f);
                        });

                        right.AddRow(row =>
                        {
                            row.SetHeight(34f);
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
                    });
                });

            return _builder.Build();
        }
    }
}
