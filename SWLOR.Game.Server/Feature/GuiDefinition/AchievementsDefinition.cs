using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class AchievementsDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<AchievementsViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Achievements)
                .SetIsResizable(true)
                .SetInitialGeometry(0, 0, 545f, 295.5f)
                .SetTitle("Achievements")
                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddCheckBox()
                            .BindIsChecked(model => model.ShowAll)
                            .SetText("Show All");

                        row.AddSpacer();
                    });

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
