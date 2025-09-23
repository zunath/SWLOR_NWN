using SWLOR.Component.Inventory.UI.ViewModel;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Inventory.UI.View
{
    public class TrainingStoreDefinition: IGuiWindowDefinition
    {
        private readonly IGuiService _guiService;
        private readonly GuiWindowBuilder<TrainingStoreViewModel> _builder;

        public TrainingStoreDefinition(IGuiService guiService)
        {
            _guiService = guiService;
            _builder = new GuiWindowBuilder<TrainingStoreViewModel>(_guiService);
        }

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
