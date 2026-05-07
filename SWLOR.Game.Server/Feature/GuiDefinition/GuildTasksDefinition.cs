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
                            r.AddButton().BindText(model => model.Rank1Text)
                                .BindColor(model => model.Rank1Color)
                                .BindIsEnabled(model => model.IsRank1Enabled)
                                .BindOnClicked(model => model.OnClickRankFilter(1));
                            r.AddButton().BindText(model => model.Rank2Text)
                                .BindColor(model => model.Rank2Color)
                                .BindIsEnabled(model => model.IsRank2Enabled)
                                .BindOnClicked(model => model.OnClickRankFilter(2));
                            r.AddButton().BindText(model => model.Rank3Text)
                                .BindColor(model => model.Rank3Color)
                                .BindIsEnabled(model => model.IsRank3Enabled)
                                .BindOnClicked(model => model.OnClickRankFilter(3));
                            r.AddButton().BindText(model => model.Rank4Text)
                                .BindColor(model => model.Rank4Color)
                                .BindIsEnabled(model => model.IsRank4Enabled)
                                .BindOnClicked(model => model.OnClickRankFilter(4));
                            r.AddButton().BindText(model => model.Rank5Text)
                                .BindColor(model => model.Rank5Color)
                                .BindIsEnabled(model => model.IsRank5Enabled)
                                .BindOnClicked(model => model.OnClickRankFilter(5));
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
                                        .BindOnClicked(model => model.OnClickTask())
                                        .BindColor(model => model.TaskColors);
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
