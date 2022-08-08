using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

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
                .SetInitialGeometry(0, 0, 829f, 453f)
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
                                        .BindText(model => model.Name);
                                });
                            });

                            row.AddColumn(col2 =>
                            {
                                col2.AddRow(row2 =>
                                {
                                    row2.AddLabel()
                                        .BindText(model => model.CharacterType);
                                });
                            });
                        });

                        mainCol.AddRow(row =>
                        {
                            row.AddTextEdit()
                                .BindValue(model => model.Description)
                                .SetIsMultiline(true)
                                .SetHeight(400f);
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
                    group.AddLabel()
                        .SetText("notes");
                })

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
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
