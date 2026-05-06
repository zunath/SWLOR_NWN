using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class GuildTasksDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<GuildTasksViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.GuildTasks)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 700, 420)
                .SetTitle("Guild Tasks")
                .AddRow(root =>
                {
                    root.AddColumn(left =>
                    {
                        left.AddRow(r =>
                        {
                            r.AddText().BindText(model => model.HeaderText);
                        });

                        left.AddRow(r =>
                        {
                            r.AddList(template =>
                            {
                                template.AddCell(cell =>
                                {
                                    cell.AddToggleButton()
                                        .BindText(model => model.TaskNames)
                                        .BindIsToggled(model => model.TaskToggles)
                                        .BindOnClicked(model => model.OnClickTask());
                                });
                            }).BindRowCount(model => model.TaskNames);
                        });
                    });

                    root.AddColumn(right =>
                    {
                        right.AddRow(r => r.AddText().BindText(model => model.TaskDetails));
                        right.AddRow(r =>
                        {
                            r.AddSpacer();
                            r.AddButton().SetText("Accept Task")
                                .BindOnClicked(model => model.OnClickAcceptTask())
                                .BindIsEnabled(model => model.IsAcceptEnabled);
                            r.AddButton().SetText("Give Report")
                                .BindOnClicked(model => model.OnClickGiveReport())
                                .BindIsEnabled(model => model.IsGiveReportEnabled);
                            r.AddSpacer();
                        });
                    });
                });

            return _builder.Build();
        }
    }
}
