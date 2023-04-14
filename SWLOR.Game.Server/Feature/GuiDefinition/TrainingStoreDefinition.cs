using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class TrainingStoreDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<TrainingStoreViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.TrainingStore)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 545f, 295.5f)
                .SetTitle("Training Store")

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.AvailableXP)
                            .SetHeight(32f);
                    });
                    col.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.SetWidth(32f);
                                cell.SetIsVariable(false);

                                cell.AddGroup(group =>
                                {
                                    group.AddImage()
                                        .BindResref(model => model.Icons)
                                        .SetMargin(0f);
                                })
                                    .SetScrollbars(NuiScrollbars.None);
                            });
                            template.AddCell(cell =>
                            {
                                cell.AddLabel()
                                    .BindText(model => model.Names);
                            });
                            template.AddCell(cell =>
                            {
                                cell.AddLabel()
                                    .BindText(model => model.PriceTexts);
                            });
                            template.AddCell(cell =>
                            {
                                cell.SetWidth(75f);
                                cell.SetIsVariable(false);

                                cell.AddButton()
                                    .SetText("Buy")
                                    .BindOnClicked(model => model.BuyItem())
                                    .SetHeight(35f);
                            });
                        })
                            .BindRowCount(model => model.Icons);
                    });
                })
                ;

            return _builder.Build();
        }
    }
}
