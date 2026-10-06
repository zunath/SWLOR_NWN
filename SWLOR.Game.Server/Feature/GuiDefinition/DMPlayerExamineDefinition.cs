using System;
using System.Linq.Expressions;
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
            var window = _builder.CreateWindow(GuiWindowType.DMPlayerExamine)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 620f, 600f)
                .BindTitle(model => model.Name)

                .DefinePartialView(DMPlayerExamineViewModel.DetailView, group =>
                {
                    AddContentPanel(group, mainCol =>
                    {
                        AddIdentityRow(mainCol, "Descriptor", model => model.Descriptor);
                        AddIdentityRow(mainCol, "True Name", model => model.Name);
                        AddIdentityRow(mainCol, "Account Name", model => model.AccountName);
                        AddIdentityRow(mainCol, "Public CD Key", model => model.PublicCDKey);

                        mainCol.AddRow(row =>
                        {
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
                    AddContentPanel(group, col => col.AddRow(row => row.AddList(template =>
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
                        .BindRowCount(model => model.SkillNames)
                        .SetHeight(400f)));

                })
                .DefinePartialView(DMPlayerExamineViewModel.PerksView, group =>
                {
                    AddContentPanel(group, col => col.AddRow(row => row.AddList(template =>
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
                        .BindRowCount(model => model.PerkNames)
                        .SetHeight(400f)));
                })
                .DefinePartialView(DMPlayerExamineViewModel.NotesView, group =>
                {
                    AddContentPanel(group, mainCol =>
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
                });

            window.AddStandardLayout(layout =>
            {
                layout.SetTabPanelHeight(40f);
                layout.AddTabRow(row =>
                {
                    row.SetHeight(32f);
                    row.AddToggles()
                        .AddOption("Details")
                        .AddOption("Skills")
                        .AddOption("Perks")
                        .AddOption("Notes")
                        .BindSelectedValue(model => model.TabToggleValue)
                        .SetWidth(560f)
                        .SetHeight(32f);
                });
                layout.SetContentPartialElement(DMPlayerExamineViewModel.PartialView);
            });

            return _builder.Build();
        }

        private static void AddContentPanel(GuiGroup<DMPlayerExamineViewModel> host,
            Action<GuiColumn<DMPlayerExamineViewModel>> buildContent)
        {
            host.AddColumn(col => col.AddRow(row => row.AddGroup(panel =>
            {
                panel.SetShowBorder(false).SetScrollbars(NuiScrollbars.None);
                panel.AddColumn(buildContent);
            }).SetWidth(560f)));
        }

        private static void AddIdentityRow(GuiColumn<DMPlayerExamineViewModel> column, string label,
            Expression<Func<DMPlayerExamineViewModel, string>> binding)
        {
            column.AddRow(row =>
            {
                row.AddLabel()
                    .SetText(label)
                    .SetWidth(120f)
                    .SetHeight(24f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                row.AddLabel()
                    .BindText(binding)
                    .BindTooltip(binding)
                    .SetHeight(24f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });
        }
    }
}
