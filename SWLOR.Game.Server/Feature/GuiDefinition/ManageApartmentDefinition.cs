using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Shared.Core.Beamdog;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class ManageApartmentDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<ManageApartmentViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {

            _builder.CreateWindow(GuiWindowType.ManageApartment)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
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
                            row.AddList(template =>
                            {
                                template.AddCell(cell =>
                                {
                                    cell.AddToggleButton()
                                        .BindText(model => model.ApartmentNames)
                                        .BindTooltip(model => model.ApartmentNames)
                                        .BindIsToggled(model => model.ApartmentToggles)
                                        .BindOnClicked(model => model.OnSelectApartment());
                                });
                            })
                                .BindRowCount(model => model.ApartmentNames);
                        });

                        col.AddRow(row =>
                        {
                            row.AddSpacer();
                            row.AddButton()
                                .SetText("Buy Apartment")
                                .SetHeight(35f)
                                .BindOnClicked(model => model.OnBuyApartment())
                                .BindIsVisible(model => model.IsAtTerminal);
                            row.AddSpacer();
                        });
                    });

                    root.AddColumn(col =>
                    {
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

                        col.AddRow(row =>
                        {
                            row.AddButton()
                                .SetText("Enter Apartment")
                                .BindOnClicked(model => model.OnEnterApartment())
                                .BindIsEnabled(model => model.IsEnterEnabled)
                                .SetHeight(35f)
                                .SetWidth(250f)
                                .BindIsVisible(model => model.IsAtTerminal);
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
                                .SetWidth(250f)
                                .BindIsEnabled(model => model.IsManagePermissionsEnabled);
                        });

                        col.AddRow(row =>
                        {
                            row.AddButton()
                                .SetText("Cancel Lease")
                                .SetColor(255, 0, 0)
                                .BindOnClicked(model => model.OnCancelLease())
                                .SetHeight(35f)
                                .SetWidth(250f)
                                .BindIsEnabled(model => model.IsCancelLeaseEnabled)
                                .BindIsVisible(model => model.IsAtTerminal);
                        });
                    });

                    root.AddColumn(col =>
                    {
                        col.AddRow(row =>
                        {
                            row.AddTextEdit()
                                .BindValue(model => model.CustomName)
                                .SetPlaceholder("Apartment Name")
                                .SetMaxLength(ManageApartmentViewModel.MaxNameLength)
                                .BindIsEnabled(model => model.IsPropertyRenameEnabled)
                                .SetWidth(200f);

                        });

                        col.AddRow(row =>
                        {
                            row.AddTextEdit()
                                .BindValue(model => model.CustomDescription)
                                .SetHeight(200f)
                                .SetPlaceholder("Description")
                                .BindIsEnabled(model => model.IsDescriptionEnabled)
                                .SetWidth(200f)
                                .SetIsMultiline(true)
                                .SetMaxLength(ManageApartmentViewModel.MaxDescriptionLength);
                        });

                        col.AddRow(row =>
                        {
                            row.AddButton()
                                .SetText("Save")
                                .SetHeight(35f)
                                .BindOnClicked(model => model.SaveChanges())
                                .BindIsEnabled(model => model.IsSaveEnabled)
                                .SetWidth(200f);
                        });

                    });
                })

                ;



            return _builder.Build();
        }
    }
}
