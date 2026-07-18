using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class DMPlayerExamineDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<DMPlayerExamineViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.DMPlayerExamine)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 829f, 520f)
                .BindTitle(model => model.Name)

                .DefinePartialView(DMPlayerExamineViewModel.DetailView, group =>
                {
                    group.AddColumn(mainCol =>
                    {
                        mainCol.AddRow(row =>
                        {
                            row.AddColumn(col1 =>
                            {
                                col1.AddRow(row1 =>
                                {
                                    row1.AddLabel()
                                        .BindText(model => model.Name)
                                        .SetHeight(20f);
                                });
                            });

                            row.AddColumn(col2 =>
                            {
                                col2.AddRow(row2 =>
                                {
                                    row2.AddLabel()
                                        .BindText(model => model.CharacterType)
                                        .SetHeight(20f);
                                });
                            });

                            row.AddColumn(col3 =>
                            {
                                col3.AddRow(row3 =>
                                {
                                    row3.AddLabel()
                                        .BindText(model => model.Credits)
                                        .SetHeight(20f);
                                });
                            });
                        });

                        mainCol.AddRow(row =>
                        {
                            row.AddTextEdit()
                                .BindValue(model => model.Description)
                                .SetIsMultiline(true)
                                .SetHeight(350f)
                                .SetMaxLength(5000);
                        });
                    });


                })
                .DefinePartialView(DMPlayerExamineViewModel.SkillsView, group =>
                {
                    group.AddList(template =>
                    {
                        template.AddCell(cell =>
                        {
                            cell.AddLabel()
                                .BindText(model => model.SkillNames);
                        });

                        template.AddCell(cell =>
                        {
                            cell.AddLabel()
                                .BindText(model => model.SkillLevels);
                        });
                    })
                        .BindRowCount(model => model.SkillNames);

                })
                .DefinePartialView(DMPlayerExamineViewModel.PerksView, group =>
                {
                    group.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddLabel()
                                    .BindText(model => model.PerkNames);
                            });

                            template.AddCell(cell =>
                            {
                                cell.AddLabel()
                                    .BindText(model => model.PerkLevels);
                            });
                        })
                        .BindRowCount(model => model.PerkNames);
                })
                .DefinePartialView(DMPlayerExamineViewModel.NotesView, group =>
                {
                    group.AddColumn(mainCol =>
                    {
                        mainCol.AddRow(mainRow =>
                        {
                            mainRow.AddColumn(col =>
                            {
                                col.AddRow(row =>
                                {
                                    row.AddList(template =>
                                        {
                                            template.AddCell(cell =>
                                            {
                                                cell.AddToggleButton()
                                                    .BindText(model => model.NoteNames)
                                                    .BindIsToggled(model => model.NoteToggles)
                                                    .BindOnClicked(model => model.OnClickNote());
                                            });
                                        })
                                        .BindRowCount(model => model.NoteNames);
                                });

                                col.AddRow(row =>
                                {
                                    row.AddButton()
                                        .SetHeight(32f)
                                        .SetText("New Note")
                                        .BindOnClicked(model => model.OnClickNewNote());

                                    row.AddButton()
                                        .SetHeight(32f)
                                        .SetText("Delete")
                                        .BindOnClicked(model => model.OnClickDeleteNote());
                                });
                            });

                            mainRow.AddColumn(col =>
                            {
                                col.AddRow(row =>
                                {
                                    row.AddTextEdit()
                                        .BindValue(model => model.ActiveNoteName)
                                        .SetPlaceholder("Note Name")
                                        .SetMaxLength(50)
                                        .BindIsEnabled(model => model.IsNoteSelected);
                                });

                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.ActiveNoteCreator);
                                });

                                col.AddRow(row =>
                                {
                                    row.AddTextEdit()
                                        .SetIsMultiline(true)
                                        .BindValue(model => model.ActiveNoteDetail)
                                        .SetHeight(350f)
                                        .SetMaxLength(3000)
                                        .BindIsEnabled(model => model.IsNoteSelected);
                                });

                                col.AddRow(row =>
                                {
                                    row.AddSpacer();
                                    row.AddButton()
                                        .SetText("Save Changes")
                                        .SetHeight(32f)
                                        .BindOnClicked(model => model.OnClickSaveChanges())
                                        .BindIsEnabled(model => model.IsNoteSelected);
                                    row.AddSpacer();
                                });
                            });
                        });
                    });
                })
                .DefinePartialView(DMPlayerExamineViewModel.MasteriesView, group =>
                {
                    group.AddColumn(mainCol =>
                    {
                        mainCol.AddRow(row =>
                        {
                            row.SetHeight(20f);
                            row.AddLabel()
                                .BindText(model => model.MasteryTotalsText);
                        });

                        mainCol.AddRow(mainRow =>
                        {
                            mainRow.AddColumn(col =>
                            {
                                col.SetWidth(430f);

                                col.AddRow(row =>
                                {
                                    row.AddList(template =>
                                    {
                                        template.AddCell(cell =>
                                        {
                                            cell.AddToggleButton()
                                                .BindText(model => model.MasteryRowLabels)
                                                .BindIsToggled(model => model.MasteryRowToggles)
                                                .BindOnClicked(model => model.OnClickSelectMasteryRow());
                                        });
                                    })
                                        .BindRowCount(model => model.MasteryRowLabels)
                                        .SetRowHeight(28f);
                                });

                                col.AddRow(row =>
                                {
                                    row.SetHeight(22f);
                                    row.AddLabel()
                                        .BindText(model => model.MasterySelectedSummary);
                                });

                                col.AddRow(row =>
                                {
                                    row.SetHeight(32f);
                                    row.BindIsVisible(model => model.IsMasteryRowSelected);

                                    row.AddTextEdit()
                                        .SetPlaceholder("Reason (required)")
                                        .SetMaxLength(300)
                                        .BindValue(model => model.MasteryActionReason);
                                });

                                col.AddRow(row =>
                                {
                                    row.SetHeight(32f);
                                    row.BindIsVisible(model => model.IsMasteryTrainingActionsVisible);

                                    row.AddLabel()
                                        .SetText("Reduce (days)")
                                        .SetWidth(100f);

                                    row.AddTextEdit()
                                        .SetPlaceholder("Days")
                                        .SetMaxLength(3)
                                        .BindValue(model => model.MasteryReduceDaysText);
                                });

                                col.AddRow(row =>
                                {
                                    row.SetHeight(32f);

                                    row.AddButton()
                                        .SetText("Increase Tier")
                                        .BindIsVisible(model => model.IsMasteryEarnedActionsVisible)
                                        .BindIsEnabled(model => model.IsMasteryIncreaseEnabled)
                                        .BindOnClicked(model => model.OnClickIncreaseTier());

                                    row.AddButton()
                                        .SetText("Revoke Tier")
                                        .BindIsVisible(model => model.IsMasteryEarnedActionsVisible)
                                        .BindOnClicked(model => model.OnClickRevokeTier());

                                    row.AddButton()
                                        .SetText("Reduce")
                                        .BindIsVisible(model => model.IsMasteryTrainingActionsVisible)
                                        .BindOnClicked(model => model.OnClickReduceTraining());

                                    row.AddButton()
                                        .SetText("Abandon Training")
                                        .BindIsVisible(model => model.IsMasteryTrainingActionsVisible)
                                        .BindOnClicked(model => model.OnClickAbandonTraining());
                                });

                                col.AddRow(row =>
                                {
                                    row.SetHeight(32f);
                                    row.BindIsVisible(model => model.IsMasteryQueuedActionsVisible);

                                    row.AddButton()
                                        .SetText("Move Up")
                                        .BindIsEnabled(model => model.IsMasteryMoveUpEnabled)
                                        .BindOnClicked(model => model.OnClickMoveMasteryUp());

                                    row.AddButton()
                                        .SetText("Move Down")
                                        .BindIsEnabled(model => model.IsMasteryMoveDownEnabled)
                                        .BindOnClicked(model => model.OnClickMoveMasteryDown());

                                    row.AddButton()
                                        .SetText("Abandon Training")
                                        .BindOnClicked(model => model.OnClickAbandonTraining());
                                });

                                col.AddRow(row =>
                                {
                                    row.SetHeight(20f);
                                    row.AddLabel()
                                        .BindText(model => model.MasteryActionStatusText)
                                        .SetColor(255, 220, 0);
                                });
                            });

                            mainRow.AddColumn(col =>
                            {
                                col.AddRow(row =>
                                {
                                    row.SetHeight(20f);
                                    row.AddLabel()
                                        .SetText("Grant Mastery");
                                });

                                col.AddRow(row =>
                                {
                                    row.SetHeight(32f);

                                    row.AddComboBox()
                                        .BindOptions(model => model.GrantMasteryOptions)
                                        .BindSelectedIndex(model => model.SelectedGrantMasteryIndex);

                                    var tierCombo = row.AddComboBox()
                                        .BindSelectedIndex(model => model.SelectedGrantTier)
                                        .SetWidth(90f);

                                    for (var tier = 1; tier <= 5; tier++)
                                        tierCombo.AddOption($"Tier {tier}", tier);
                                });

                                col.AddRow(row =>
                                {
                                    row.SetHeight(32f);
                                    row.AddTextEdit()
                                        .SetPlaceholder("Reason (required)")
                                        .SetMaxLength(300)
                                        .BindValue(model => model.GrantReason);
                                });

                                col.AddRow(row =>
                                {
                                    row.SetHeight(32f);
                                    row.AddButton()
                                        .SetText("Grant Mastery")
                                        .BindOnClicked(model => model.OnClickGrantMastery());
                                });

                                col.AddRow(row =>
                                {
                                    row.SetHeight(20f);
                                    row.AddLabel()
                                        .SetText("Award Quick Slot");
                                });

                                col.AddRow(row =>
                                {
                                    row.SetHeight(32f);
                                    row.AddTextEdit()
                                        .SetPlaceholder("Reason (required)")
                                        .SetMaxLength(300)
                                        .BindValue(model => model.QuickSlotReason);
                                });

                                col.AddRow(row =>
                                {
                                    row.SetHeight(32f);
                                    row.AddButton()
                                        .SetText("Award Quick Slot")
                                        .BindOnClicked(model => model.OnClickAwardQuickSlot());
                                });

                                col.AddRow(row =>
                                {
                                    row.SetHeight(20f);
                                    row.AddLabel()
                                        .SetText("Audit Log (most recent first)");
                                });

                                col.AddRow(row =>
                                {
                                    row.AddList(template =>
                                    {
                                        template.AddCell(cell =>
                                        {
                                            cell.AddText()
                                                .BindText(model => model.MasteryAuditLines)
                                                .SetShowBorder(false)
                                                .SetScrollbars(NuiScrollbars.None);
                                        });
                                    })
                                        .BindRowCount(model => model.MasteryAuditLines)
                                        .SetRowHeight(20f);
                                });
                            });
                        });
                    });
                })

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddToggleButton()
                            .SetText("Details")
                            .SetHeight(32f)
                            .BindIsToggled(model => model.IsDetailsToggled)
                            .BindOnClicked(model => model.OnClickDetails());

                        row.AddToggleButton()
                            .SetText("Skills")
                            .SetHeight(32f)
                            .BindIsToggled(model => model.IsSkillsToggled)
                            .BindOnClicked(model => model.OnClickSkills());

                        row.AddToggleButton()
                            .SetText("Perks")
                            .SetHeight(32f)
                            .BindIsToggled(model => model.IsPerksToggled)
                            .BindOnClicked(model => model.OnClickPerks());

                        row.AddToggleButton()
                            .SetText("Notes")
                            .SetHeight(32f)
                            .BindIsToggled(model => model.IsNotesToggled)
                            .BindOnClicked(model => model.OnClickNotes());

                        row.AddToggleButton()
                            .SetText("Masteries")
                            .SetHeight(32f)
                            .BindIsToggled(model => model.IsMasteriesToggled)
                            .BindOnClicked(model => model.OnClickMasteries());

                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddPartialView(DMPlayerExamineViewModel.PartialView);
                    });

                });

            return _builder.Build();
        }
    }
}
