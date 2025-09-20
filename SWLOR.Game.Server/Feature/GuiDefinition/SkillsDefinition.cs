using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Shared.Core.Beamdog;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class SkillsDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<SkillsViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {

            _builder.CreateWindow(GuiWindowType.Skills)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
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
                            .BindText(model => model.AvailableXP)
                            .SetHeight(20f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Top);
                    });

                    column.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.XPDebt)
                            .SetHeight(20f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Top);
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

                        row.AddLabel()
                            .SetText("")
                            .SetWidth(32f);

                        row.SetHeight(20f);
                    });

                    column.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddLabel()
                                    .BindText(model => model.SkillNames)
                                    .BindTooltip(model => model.Descriptions);
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
                                    .BindValue(model => model.Progresses)
                                    .BindTooltip(model => model.RawXPAmounts);
                            });
                            template.AddCell(cell =>
                            {
                                cell.AddButton()
                                    .BindText(model => model.DecayLockTexts)
                                    .BindColor(model => model.DecayLockColors)
                                    .BindOnClicked(model => model.ToggleDecayLock())
                                    .BindIsEnabled(model => model.DecayLockButtonEnabled);
                            });

                            template.AddCell(cell =>
                            {
                                cell.SetIsVariable(false);
                                cell.SetWidth(32f);
                                cell.AddButton()
                                    .SetText("+")
                                    .BindOnClicked(model => model.OnClickDistributeRPXP())
                                    .BindIsEnabled(model => model.DistributeRPXPButtonEnabled)
                                    .BindTooltip(model => model.DistributeRPXPButtonTooltips);
                            });
                        })
                            .BindRowCount(model => model.SkillNames);
                    });
                });

            return _builder.Build();
        }
    }
}
