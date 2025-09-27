using SWLOR.Component.Skill.UI.ViewModel;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Domain.Skill.Contracts;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Skill.UI.View
{
    public class SkillsDefinition: IGuiWindowDefinition
    {
        private readonly IGuiService _guiService;
        private readonly ISkillService _skillService;
        private readonly GuiWindowBuilder<SkillsViewModel> _builder;

        public SkillsDefinition(
            IGuiService guiService,
            ISkillService skillService)
        {
            _guiService = guiService;
            _skillService = skillService;
            _builder = new GuiWindowBuilder<SkillsViewModel>(_guiService);
        }

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
                        foreach (var (type, detail) in _skillService.GetAllActiveSkillCategories())
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
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                            .SetVerticalAlign(NuiVerticalAlignType.Top);
                    });

                    column.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.XPDebt)
                            .SetHeight(20f)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                            .SetVerticalAlign(NuiVerticalAlignType.Top);
                    });

                    column.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Skill")
                            .SetHorizontalAlign(NuiHorizontalAlignType.Center)
                            .SetVerticalAlign(NuiVerticalAlignType.Top);

                        row.AddLabel()
                            .SetText("Level")
                            .SetHorizontalAlign(NuiHorizontalAlignType.Center)
                            .SetVerticalAlign(NuiVerticalAlignType.Top);

                        row.AddLabel()
                            .SetText("Title")
                            .SetHorizontalAlign(NuiHorizontalAlignType.Center)
                            .SetVerticalAlign(NuiVerticalAlignType.Top);

                        row.AddLabel()
                            .SetText("XP")
                            .SetHorizontalAlign(NuiHorizontalAlignType.Center)
                            .SetVerticalAlign(NuiVerticalAlignType.Top);

                        row.AddLabel()
                            .SetText("Decay Lock")
                            .SetHorizontalAlign(NuiHorizontalAlignType.Center)
                            .SetVerticalAlign(NuiVerticalAlignType.Top);

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
