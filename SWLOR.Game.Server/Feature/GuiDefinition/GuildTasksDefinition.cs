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
                            r.AddSpacer();
                            r.AddToggleButton()
                                .BindText(model => model.Rank1Text)
                                .BindIsToggled(model => model.IsRank1Toggled)
                                .BindColor(model => model.Rank1Color)
                                .BindIsEnabled(model => model.IsRank1Enabled)
                                .BindOnClicked(model => model.OnClickRankFilter(1))
                                .SetHeight(32f)
                                .SetWidth(80f);
                            r.AddToggleButton()
                                .BindText(model => model.Rank2Text)
                                .BindIsToggled(model => model.IsRank2Toggled)
                                .BindColor(model => model.Rank2Color)
                                .BindIsEnabled(model => model.IsRank2Enabled)
                                .BindOnClicked(model => model.OnClickRankFilter(2))
                                .SetHeight(32f)
                                .SetWidth(80f);
                            r.AddToggleButton()
                                .BindText(model => model.Rank3Text)
                                .BindIsToggled(model => model.IsRank3Toggled)
                                .BindColor(model => model.Rank3Color)
                                .BindIsEnabled(model => model.IsRank3Enabled)
                                .BindOnClicked(model => model.OnClickRankFilter(3))
                                .SetHeight(32f)
                                .SetWidth(80f);
                            r.AddToggleButton()
                                .BindText(model => model.Rank4Text)
                                .BindIsToggled(model => model.IsRank4Toggled)
                                .BindColor(model => model.Rank4Color)
                                .BindIsEnabled(model => model.IsRank4Enabled)
                                .BindOnClicked(model => model.OnClickRankFilter(4))
                                .SetHeight(32f)
                                .SetWidth(80f);
                            r.AddToggleButton()
                                .BindText(model => model.Rank5Text)
                                .BindIsToggled(model => model.IsRank5Toggled)
                                .BindColor(model => model.Rank5Color)
                                .BindIsEnabled(model => model.IsRank5Enabled)
                                .BindOnClicked(model => model.OnClickRankFilter(5))
                                .SetHeight(32f)
                                .SetWidth(80f);
                            r.AddSpacer();
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

                        left.AddRow(r =>
                        {
                            r.AddSpacer();
                            r.AddButton()
                                .SetText("Accept All")
                                .BindOnClicked(model => model.OnClickAcceptAllTasks())
                                .BindIsEnabled(model => model.IsAcceptAllEnabled)
                                .SetHeight(32f)
                                .SetWidth(120f);
                            r.AddSpacer();
                        });
                    });

                    root.AddColumn(right =>
                    {
                        right.SetWidth(300f);
                        right.AddRow(r => r.AddText().BindText(model => model.TaskDetails));
                        right.AddRow(r =>
                        {
                            r.AddSpacer();
                            r.AddButton()
                                .SetText("Accept Task")
                                .BindOnClicked(model => model.OnClickAcceptTask())
                                .BindIsEnabled(model => model.IsAcceptEnabled)
                                .SetHeight(32f)
                                .SetWidth(120f);
                            r.AddButton()
                                .SetText("Give Report")
                                .BindOnClicked(model => model.OnClickGiveReport())
                                .BindIsEnabled(model => model.IsGiveReportEnabled)
                                .SetHeight(32f)
                                .SetWidth(120f);
                            r.AddSpacer();
                        });
                    });
                });

            return _builder.Build();
        }
    }
}
