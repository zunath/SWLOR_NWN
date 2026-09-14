using System.Linq;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class SkillsDefinition: IGuiWindowDefinition
    {
        private const float CategoryTabWidth = 90f;

        private readonly GuiWindowBuilder<SkillsViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            var activeCategories = Skill.GetAllActiveSkillCategories()
                .OrderBy(category => category.Value.Sequence)
                .ToList();

            _builder.CreateWindow(GuiWindowType.Skills)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 545f, 295.5f)
                .SetTitle("Skills")
                .AddColumn(column =>
                {
                    column.AddRow(row =>
                    {
                        row.AddGroup(group =>
                        {
                            group.SetShowBorder(false);
                            group.SetScrollbars(NuiScrollbars.Auto);
                            group.AddColumn(tabColumn =>
                            {
                                tabColumn.AddRow(tabRow =>
                                {
                                    var tabs = tabRow.AddToggles()
                                        .BindSelectedValue(model => model.SelectedCategoryId)
                                        .SetHeight(32f)
                                        .SetWidth(CategoryTabWidth * (activeCategories.Count + 1));

                                    tabs.AddOption("All");
                                    foreach (var (_, detail) in activeCategories)
                                    {
                                        tabs.AddOption(detail.Name);
                                    }
                                });
                            });
                        })
                            .SetHeight(48f);
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
