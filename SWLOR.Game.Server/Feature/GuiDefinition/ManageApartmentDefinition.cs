using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class ManageApartmentDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<ManageApartmentViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {

            _builder.CreateWindow(GuiWindowType.ManageApartment)
                .SetIsResizable(true)
                .SetInitialGeometry(0, 0, 545f, 295.5f)
                .SetTitle("Manage Apartment")
                
                .AddRow(root =>
                {
                    root.AddColumn(col =>
                    {
                        col.AddRow(row =>
                        {
                            row.AddLabel()
                                .BindText(model => model.Instruction)
                                .BindColor(model => model.InstructionColor)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                .SetVerticalAlign(NuiVerticalAlign.Middle)
                                .SetHeight(20f);
                        });

                        col.AddRow(row =>
                        {
                            row.AddTextEdit()
                                .BindValue(model => model.CustomName)
                                .SetPlaceholder("Apartment Name")
                                .SetMaxLength(32);

                            row.AddButton()
                                .SetText("Save")
                                .SetHeight(35f)
                                .BindOnClicked(model => model.SaveCustomName());
                        });

                        col.AddRow(row =>
                        {
                            row.AddLabel()
                                .BindText(model => model.LayoutName)
                                .SetHeight(20f)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                .SetVerticalAlign(NuiVerticalAlign.Middle);
                        });

                        col.AddRow(row =>
                        {
                            row.AddLabel()
                                .BindText(model => model.InitialPrice)
                                .SetHeight(20f)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                .SetVerticalAlign(NuiVerticalAlign.Middle);
                        });

                        col.AddRow(row =>
                        {
                            row.AddLabel()
                                .BindText(model => model.PricePerDay)
                                .SetHeight(20f)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                .SetVerticalAlign(NuiVerticalAlign.Middle);
                        });

                        col.AddRow(row =>
                        {
                            row.AddLabel()
                                .BindText(model => model.FurnitureLimit)
                                .SetHeight(20f)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                .SetVerticalAlign(NuiVerticalAlign.Middle);
                        });

                        col.AddRow(row =>
                        {
                            row.AddLabel()
                                .BindText(model => model.LeasedUntil)
                                .BindColor(model => model.LeasedUntilColor)
                                .SetHeight(20f)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                .SetVerticalAlign(NuiVerticalAlign.Middle);
                        });
                    });

                    root.AddColumn(col =>
                    {
                        col.AddRow(row =>
                        {
                            row.AddButton()
                                .SetText("Enter Apartment")
                                .BindOnClicked(model => model.OnEnterApartment())
                                .BindIsEnabled(model => model.IsEnterEnabled)
                                .SetHeight(35f)
                                .SetWidth(250f);
                        });

                        col.AddRow(row =>
                        {
                            row.AddButton()
                                .BindText(model => model.ExtendLease1DayText)
                                .BindOnClicked(model => model.OnExtendLease1Day())
                                .BindIsEnabled(model => model.IsExtendLease1DayEnabled)
                                .SetHeight(35f)
                                .SetWidth(250f);
                        });

                        col.AddRow(row =>
                        {
                            row.AddButton()
                                .BindText(model => model.ExtendLease7DaysText)
                                .BindOnClicked(model => model.OnExtendLease7Days())
                                .BindIsEnabled(model => model.IsExtendLease7DaysEnabled)
                                .SetHeight(35f)
                                .SetWidth(250f);
                        });

                        col.AddRow(row =>
                        {
                            row.AddButton()
                                .SetText("Manage Permissions")
                                .BindOnClicked(model => model.OnManagePermissions())
                                .SetHeight(35f)
                                .SetWidth(250f);
                        });

                        col.AddRow(row =>
                        {
                            row.AddButton()
                                .SetText("Cancel Lease")
                                .SetColor(255, 0, 0)
                                .BindOnClicked(model => model.OnCancelLease())
                                .SetHeight(35f)
                                .SetWidth(250f);
                        });

                    });

                })

                ;



            return _builder.Build();
        }
    }
}
