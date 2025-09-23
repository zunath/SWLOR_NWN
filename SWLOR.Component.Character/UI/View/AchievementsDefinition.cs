using SWLOR.Component.Character.UI.ViewModel;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Character.UI.View
{
    public class AchievementsDefinition: IGuiWindowDefinition
    {
        private readonly IGuiService _guiService;
        private readonly GuiWindowBuilder<AchievementsViewModel> _builder;

        public AchievementsDefinition(IGuiService guiService)
        {
            _guiService = guiService;
            _builder = new GuiWindowBuilder<AchievementsViewModel>(_guiService);
        }

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Achievements)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 545f, 295.5f)
                .SetTitle("Achievements")
                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddToggleButton()
                                    .BindText(model => model.Names)
                                    .BindIsToggled(model => model.Toggles)
                                    .BindOnClicked(model => model.OnClickAchievement())
                                    .BindColor(model => model.Colors);
                            });
                        })
                            .BindRowCount(model => model.Names);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddButton()
                            .SetText("<")
                            .SetWidth(32f)
                            .SetHeight(35f)
                            .BindOnClicked(model => model.OnClickPreviousPage());

                        row.AddComboBox()
                            .BindOptions(model => model.PageNumbers)
                            .BindSelectedIndex(model => model.SelectedPageIndex);

                        row.AddButton()
                            .SetText(">")
                            .SetWidth(32f)
                            .SetHeight(35f)
                            .BindOnClicked(model => model.OnClickNextPage());

                        row.AddSpacer();
                    });
                })
                
                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.Name)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHeight(20f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddText()
                            .BindText(model => model.Description);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.AcquiredDate)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetVerticalAlign(NuiVerticalAlign.Middle)
                            .SetHeight(20f);
                    });
                })
                ;

            return _builder.Build();
        }
    }
}
