using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class ResearchDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<ResearchViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Research)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 545f, 600f)
                .SetTitle("Research Terminal")

                .DefinePartialView(ResearchViewModel.StartStageView, group =>
                {
                    group.AddColumn(col =>
                    {
                        col.AddRow(row =>
                        {
                            row.AddLabel()
                                .BindText(model => model.RecipeName)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                .SetHeight(20f);
                        });
                        col.AddRow(row =>
                        {
                            row.AddLabel()
                                .BindText(model => model.Level)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                .SetHeight(20f);
                        });
                        col.AddRow(row =>
                        {
                            row.AddLabel()
                                .BindText(model => model.CreditReduction)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                .SetHeight(20f);
                        });
                        col.AddRow(row =>
                        {
                            row.AddLabel()
                                .BindText(model => model.EnhancementSlots)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                .SetHeight(20f);
                        });
                        col.AddRow(row =>
                        {
                            row.AddLabel()
                                .BindText(model => model.LicensedRuns)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                .SetHeight(20f);
                        });
                        col.AddRow(row =>
                        {
                            row.AddLabel()
                                .BindText(model => model.TimeReduction)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                .SetHeight(20f);
                        });
                        col.AddRow(row =>
                        {
                            row.AddLabel()
                                .BindText(model => model.ItemBonuses)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                .SetHeight(20f);
                        });
                        col.AddRow(row =>
                        {
                            row.AddLabel()
                                .SetText("Guaranteed Bonuses:")
                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                .SetHeight(20f);
                        });
                        col.AddRow(row =>
                        {
                            row.AddList(template =>
                            {
                                template.AddCell(cell =>
                                {
                                    cell.AddLabel()
                                        .BindText(model => model.GuaranteedBonuses)
                                        .SetHeight(20f);
                                });
                            })
                                .SetHeight(100f)
                                .BindRowCount(model => model.GuaranteedBonuses);
                        });

                        col.AddRow(row =>
                        {
                            row.AddLabel()
                                .BindText(model => model.CreditCost)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                .SetHeight(20f);
                        });

                        col.AddRow(row =>
                        {
                            row.AddLabel()
                                .BindText(model => model.TimeCost)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                .SetHeight(20f);
                        });

                        col.AddRow(row =>
                        {
                            row.AddLabel()
                                .BindText(model => model.NextLevelBonus)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                .SetHeight(20f);
                        });

                        col.AddRow(row =>
                        {
                            row.AddSpacer();
                        });

                        col.AddRow(row =>
                        {
                            row.AddSpacer();
                            row.AddButton()
                                .SetText("Start Job");
                            row.AddSpacer();
                        });
                    });
                })

                .DefinePartialView(ResearchViewModel.InProgressView, group =>
                {

                })

                .DefinePartialView(ResearchViewModel.StageCompleteView, group =>
                {

                })

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddPartialView(ResearchViewModel.PartialView);
                    });
                });


            return _builder.Build();
        }
    }
}
