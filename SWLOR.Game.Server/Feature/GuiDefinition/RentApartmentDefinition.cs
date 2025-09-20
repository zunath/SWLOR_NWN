using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Shared.Core.Beamdog;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class RentApartmentDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<RentApartmentViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.RentApartment)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 545f, 295.5f)
                .SetTitle("Rent Apartment")
                
                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetHeight(20f)
                            .BindText(model => model.Instructions)
                            .BindColor(model => model.InstructionsColor)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetVerticalAlign(NuiVerticalAlign.Middle);
                    });

                    col.AddRow(row =>
                    {
                        row.AddColumn(col2 =>
                        {
                            col2.AddRow(row2 =>
                            {
                                row2.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.AddToggleButton()
                                            .BindText(model => model.LayoutNames)
                                            .BindIsToggled(model => model.LayoutToggles)
                                            .BindOnClicked(model => model.OnSelectLayout());
                                    });
                                })
                                    .BindRowCount(model => model.LayoutNames);
                            });
                        });

                        row.AddColumn(col2 =>
                        {
                            col2.AddRow(row2 =>
                            {
                                row2.AddLabel()
                                    .BindText(model => model.Name)
                                    .SetHeight(20f)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetVerticalAlign(NuiVerticalAlign.Middle);
                            });

                            col2.AddRow(row2 =>
                            {
                                row2.AddLabel()
                                    .BindText(model => model.InitialPrice)
                                    .SetHeight(20f)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetVerticalAlign(NuiVerticalAlign.Middle);
                            });

                            col2.AddRow(row2 =>
                            {
                                row2.AddLabel()
                                    .BindText(model => model.PricePerDay)
                                    .SetHeight(20f)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetVerticalAlign(NuiVerticalAlign.Middle);
                            });

                            col2.AddRow(row2 =>
                            {
                                row2.AddLabel()
                                    .BindText(model => model.FurnitureLimit)
                                    .SetHeight(20f)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetVerticalAlign(NuiVerticalAlign.Middle);
                            });

                            col2.AddRow(row2 =>
                            {
                                row2.AddSpacer();
                            });

                            col2.AddRow(row2 =>
                            {
                                row2.AddButton()
                                    .SetText("Preview")
                                    .BindOnClicked(model => model.OnPreviewApartment());

                                row2.AddButton()
                                    .SetText("Buy")
                                    .BindOnClicked(model => model.OnBuyApartment())
                                    .BindIsEnabled(model => model.IsRentApartmentEnabled);
                            });
                        });
                    });

                });


            return _builder.Build();
        }
    }
}
