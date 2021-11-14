using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class ApartmentManagementDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<ApartmentManagementViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.ApartmentManagement)
                .SetIsResizable(true)
                .SetInitialGeometry(0, 0, 545f, 295.5f)
                .SetTitle("Apartments Terminal")
                
                .DefinePartialView("NoApartmentPartial", group =>
                {
                    group.AddColumn(col =>
                    {
                        col.AddRow(row =>
                        {
                            row.AddList(template =>
                            {
                                template.AddCell(cell =>
                                {
                                    cell.AddToggleButton()
                                        .BindText(model => model.LayoutNames)
                                        .BindIsToggled(model => model.LayoutToggles)
                                        .BindOnClicked(model => model.OnSelectLayout());
                                });
                            });
                        });
                    });

                    group.AddColumn(col =>
                    {
                        col.AddRow(row =>
                        {
                            row.AddButton()
                                .SetText("Buy")
                                .BindOnClicked(model => model.OnBuyApartment());

                            row.AddButton()
                                .SetText("Preview")
                                .BindOnClicked(model => model.OnPreviewApartment());
                        });
                    });
                })

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetHeight(20f)
                            .BindText(model => model.Instructions)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetVerticalAlign(NuiVerticalAlign.Middle);
                    });

                    col.AddRow(row =>
                    {
                        row.AddPartialView("MainView");
                    });

                });


            return _builder.Build();
        }
    }
}
