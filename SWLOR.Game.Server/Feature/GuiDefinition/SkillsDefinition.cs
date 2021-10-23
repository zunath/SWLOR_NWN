using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class SkillsDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<SkillsViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {

            _builder.CreateWindow(GuiWindowType.Skills)
                .BindOnOpened(model => model.OnLoadWindow())
                .SetIsResizable(true)
                .SetInitialGeometry(0, 0, 545f, 295.5f)
                .SetTitle("Skills")
                .AddColumn(column =>
                {
                    column.AddRow(row =>
                    {
                        row.AddSpacer();
                        var comboBox = row.AddComboBox()
                            .BindSelectedIndex(model => model.SelectedCategoryId);

                        comboBox.AddOption("<All Skills>", 0);
                        foreach (var (type, detail) in Skill.GetAllActiveSkillCategories())
                        {
                            comboBox.AddOption(detail.Name, (int) type);
                        }

                        row.AddSpacer();
                    });

                    column.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Skill")
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetVerticalAlign(NuiVerticalAlign.Top);

                        row.AddLabel()
                            .SetText("Level")
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetVerticalAlign(NuiVerticalAlign.Top);

                        row.AddLabel()
                            .SetText("Title")
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetVerticalAlign(NuiVerticalAlign.Top);

                        row.AddLabel()
                            .SetText("XP")
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetVerticalAlign(NuiVerticalAlign.Top);

                        row.AddLabel()
                            .SetText("Decay Lock")
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetVerticalAlign(NuiVerticalAlign.Top);

                        row.SetHeight(20f);
                    });

                    column.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddLabel()
                                    .BindText(model => model.SkillNames);
                            });
                            template.AddCell(cell =>
                            {
                                cell.AddLabel()
                                    .BindText(model => model.Levels);
                            });
                            template.AddCell(cell =>
                            {
                                cell.AddLabel()
                                    .BindText(model => model.Titles);
                            });
                            template.AddCell(cell =>
                            {
                                cell.AddProgressBar()
                                    .BindValue(model => model.Progresses);
                            });
                            template.AddCell(cell =>
                            {
                                cell.AddButton()
                                    .BindText(model => model.DecayLockTexts)
                                    .BindColor(model => model.DecayLockColors)
                                    .BindOnClicked(model => model.ToggleDecayLock())
                                    .BindIsEnabled(model => model.DecayLockButtonEnabled);
                            });
                        })
                            .BindRowCount(model => model.SkillNames);
                    });
                });

            return _builder.Build();
        }
    }
}
