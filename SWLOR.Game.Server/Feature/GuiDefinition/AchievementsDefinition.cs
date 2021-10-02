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
                .BindOnOpened(model => model.OnLoadWindow())
                .SetIsResizable(true)
                .SetInitialGeometry(0, 0, 545f, 295.5f)
                .SetTitle("Achievements")
                .AddColumn(column =>
                {
                    column.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddCheckBox()
                            .BindIsChecked(model => model.ShowAll)
                            .SetText("Show All");

                        row.AddSpacer();
                    });

                    column.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Name")
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetVerticalAlign(NuiVerticalAlign.Top);

                        row.AddLabel()
                            .SetText("Description")
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetVerticalAlign(NuiVerticalAlign.Top);

                        row.AddLabel()
                            .SetText("Date Unlocked")
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetVerticalAlign(NuiVerticalAlign.Top);

                        row.SetHeight(20f);
                    });

                    column.AddRow(row =>
                    {
                        row.AddList(template =>
                            {
                                template.AddLabel()
                                    .BindText(model => model.Names)
                                    .BindColor(model => model.Colors);

                                template.AddLabel()
                                    .BindText(model => model.Descriptions)
                                    .BindColor(model => model.Colors);

                                template.AddLabel()
                                    .BindText(model => model.AcquiredDates)
                                    .BindColor(model => model.Colors);

                            })
                            .BindRowCount(model => model.Names);
                    });
                });

            return _builder.Build();
        }
    }
}
