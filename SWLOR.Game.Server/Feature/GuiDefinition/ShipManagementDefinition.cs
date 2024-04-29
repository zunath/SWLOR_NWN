using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class ShipManagementDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<ShipManagementViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.ShipManagement)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 545f, 350f)
                .SetTitle("Ship Management")

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddToggleButton()
                            .SetText("My Ships")
                            .SetHeight(35f)
                            .BindOnClicked(model => model.OnClickMyShips())
                            .BindIsToggled(model => model.IsMyShipsToggled);

                        row.AddToggleButton()
                            .SetText("Other Ships")
                            .SetHeight(35f)
                            .BindOnClicked(model => model.OnClickOtherShips())
                            .BindIsToggled(model => model.IsOtherShipsToggled);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddLabel()
                            .BindText(model => model.ShipCountRegistered)
                            .BindIsVisible(model => model.IsMyShipsToggled)
                            .SetHeight(20f);
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddToggleButton()
                                    .BindOnClicked(model => model.OnClickShip())
                                    .BindText(model => model.ShipNames)
                                    .BindTooltip(model => model.ShipNames)
                                    .BindIsToggled(model => model.ShipToggles);
                            });
                        })
                            .BindRowCount(model => model.ShipNames);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("Register Ship")
                            .BindIsEnabled(model => model.IsRegisterEnabled)
                            .BindOnClicked(model => model.OnClickRegisterShip())
                            .SetHeight(35f);

                        row.AddButton()
                            .SetText("Unregister Ship")
                            .BindIsEnabled(model => model.IsUnregisterEnabled)
                            .BindOnClicked(model => model.OnClickUnregisterShip())
                            .SetHeight(35f);
                    });
                })

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddTextEdit()
                            .BindValue(model => model.ShipName)
                            .SetPlaceholder("Ship Name")
                            .BindIsEnabled(model => model.IsNameEnabled);

                        row.AddButton()
                            .SetText("Save")
                            .BindOnClicked(model => model.OnClickSaveShipName())
                            .SetHeight(35f)
                            .SetWidth(40f)
                            .BindIsEnabled(model => model.IsNameEnabled);
                    });
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .BindText(model => model.ShipType)
                            .SetHeight(20f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .BindText(model => model.ShipLocation)
                            .SetHeight(20f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetText("Shields:");

                        row.AddProgressBar()
                            .BindValue(model => model.Shields)
                            .BindTooltip(model => model.ShieldsTooltip)
                            .SetHeight(20f)
                            .SetColor(0, 170, 228);
                    });
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetText("Hull:");

                        row.AddProgressBar()
                            .BindValue(model => model.Hull)
                            .BindTooltip(model => model.HullTooltip)
                            .SetHeight(20f)
                            .SetColor(139, 0, 0);
                    });
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetText("Capacitor:");

                        row.AddProgressBar()
                            .BindValue(model => model.Capacitor)
                            .BindTooltip(model => model.CapacitorTooltip)
                            .SetHeight(20f)
                            .SetColor(255, 165, 0);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddButton()
                            .BindIsEnabled(model => model.IsRepairEnabled)
                            .BindText(model => model.RepairText)
                            .BindOnClicked(model => model.OnClickRepair())
                            .SetHeight(35f);
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("High-Powered Slots")
                            .SetHeight(20f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButtonImage()
                            .BindImageResref(model => model.HighPower1Resref)
                            .BindIsVisible(model => model.HighPower1Visible)
                            .BindIsEnabled(model => model.IsRefitEnabled)
                            .BindOnClicked(model => model.OnClickHighPower1())
                            .BindTooltip(model => model.HighPower1Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.HighPower2Resref)
                            .BindIsVisible(model => model.HighPower2Visible)
                            .BindIsEnabled(model => model.IsRefitEnabled)
                            .BindOnClicked(model => model.OnClickHighPower2())
                            .BindTooltip(model => model.HighPower2Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.HighPower3Resref)
                            .BindIsVisible(model => model.HighPower3Visible)
                            .BindIsEnabled(model => model.IsRefitEnabled)
                            .BindOnClicked(model => model.OnClickHighPower3())
                            .BindTooltip(model => model.HighPower3Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.HighPower4Resref)
                            .BindIsVisible(model => model.HighPower4Visible)
                            .BindIsEnabled(model => model.IsRefitEnabled)
                            .BindOnClicked(model => model.OnClickHighPower4())
                            .BindTooltip(model => model.HighPower4Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.HighPower5Resref)
                            .BindIsVisible(model => model.HighPower5Visible)
                            .BindIsEnabled(model => model.IsRefitEnabled)
                            .BindOnClicked(model => model.OnClickHighPower5())
                            .BindTooltip(model => model.HighPower5Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.HighPower6Resref)
                            .BindIsVisible(model => model.HighPower6Visible)
                            .BindIsEnabled(model => model.IsRefitEnabled)
                            .BindOnClicked(model => model.OnClickHighPower6())
                            .BindTooltip(model => model.HighPower6Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.HighPower7Resref)
                            .BindIsVisible(model => model.HighPower7Visible)
                            .BindIsEnabled(model => model.IsRefitEnabled)
                            .BindOnClicked(model => model.OnClickHighPower7())
                            .BindTooltip(model => model.HighPower7Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.HighPower8Resref)
                            .BindIsVisible(model => model.HighPower8Visible)
                            .BindIsEnabled(model => model.IsRefitEnabled)
                            .BindOnClicked(model => model.OnClickHighPower8())
                            .BindTooltip(model => model.HighPower8Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Low-Powered Slots")
                            .SetHeight(20f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButtonImage()
                            .BindImageResref(model => model.LowPower1Resref)
                            .BindIsVisible(model => model.LowPower1Visible)
                            .BindIsEnabled(model => model.IsRefitEnabled)
                            .BindOnClicked(model => model.OnClickLowPower1())
                            .BindTooltip(model => model.LowPower1Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.LowPower2Resref)
                            .BindIsVisible(model => model.LowPower2Visible)
                            .BindIsEnabled(model => model.IsRefitEnabled)
                            .BindOnClicked(model => model.OnClickLowPower2())
                            .BindTooltip(model => model.LowPower2Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.LowPower3Resref)
                            .BindIsVisible(model => model.LowPower3Visible)
                            .BindIsEnabled(model => model.IsRefitEnabled)
                            .BindOnClicked(model => model.OnClickLowPower3())
                            .BindTooltip(model => model.LowPower3Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.LowPower4Resref)
                            .BindIsVisible(model => model.LowPower4Visible)
                            .BindIsEnabled(model => model.IsRefitEnabled)
                            .BindOnClicked(model => model.OnClickLowPower4())
                            .BindTooltip(model => model.LowPower4Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.LowPower5Resref)
                            .BindIsVisible(model => model.LowPower5Visible)
                            .BindIsEnabled(model => model.IsRefitEnabled)
                            .BindOnClicked(model => model.OnClickLowPower5())
                            .BindTooltip(model => model.LowPower5Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.LowPower6Resref)
                            .BindIsVisible(model => model.LowPower6Visible)
                            .BindIsEnabled(model => model.IsRefitEnabled)
                            .BindOnClicked(model => model.OnClickLowPower6())
                            .BindTooltip(model => model.LowPower6Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.LowPower7Resref)
                            .BindIsVisible(model => model.LowPower7Visible)
                            .BindIsEnabled(model => model.IsRefitEnabled)
                            .BindOnClicked(model => model.OnClickLowPower7())
                            .BindTooltip(model => model.LowPower7Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.LowPower8Resref)
                            .BindIsVisible(model => model.LowPower8Visible)
                            .BindIsEnabled(model => model.IsRefitEnabled)
                            .BindOnClicked(model => model.OnClickLowPower8())
                            .BindTooltip(model => model.LowPower8Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Configuration Slot")
                            .SetHeight(20f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButtonImage()
                         .BindImageResref(model => model.Configuration1Resref)
                         .BindIsVisible(model => model.Configuration1Visible)
                         .BindIsEnabled(model => model.IsRefitEnabled)
                         .BindOnClicked(model => model.OnClickConfiguration1())
                         .BindTooltip(model => model.Configuration1Tooltip)
                         .SetHeight(32f)
                         .SetWidth(32f);
                        row.AddButtonImage()
                         .BindImageResref(model => model.Configuration2Resref)
                         .BindIsVisible(model => model.Configuration2Visible)
                         .BindIsEnabled(model => model.IsRefitEnabled)
                         .BindOnClicked(model => model.OnClickConfiguration2())
                         .BindTooltip(model => model.Configuration2Tooltip)
                         .SetHeight(32f)
                         .SetWidth(32f);
                        row.AddButtonImage()
                         .BindImageResref(model => model.Configuration3Resref)
                         .BindIsVisible(model => model.Configuration3Visible)
                         .BindIsEnabled(model => model.IsRefitEnabled)
                         .BindOnClicked(model => model.OnClickConfiguration3())
                         .BindTooltip(model => model.Configuration3Tooltip)
                         .SetHeight(32f)
                         .SetWidth(32f);
                        row.AddButtonImage()
                         .BindImageResref(model => model.Configuration4Resref)
                         .BindIsVisible(model => model.Configuration4Visible)
                         .BindIsEnabled(model => model.IsRefitEnabled)
                         .BindOnClicked(model => model.OnClickConfiguration4())
                         .BindTooltip(model => model.Configuration4Tooltip)
                         .SetHeight(32f)
                         .SetWidth(32f);
                        row.AddButtonImage()
                         .BindImageResref(model => model.Configuration5Resref)
                         .BindIsVisible(model => model.Configuration5Visible)
                         .BindIsEnabled(model => model.IsRefitEnabled)
                         .BindOnClicked(model => model.OnClickConfiguration5())
                         .BindTooltip(model => model.Configuration5Tooltip)
                         .SetHeight(32f)
                         .SetWidth(32f);
                        row.AddButtonImage()
                         .BindImageResref(model => model.Configuration6Resref)
                         .BindIsVisible(model => model.Configuration6Visible)
                         .BindIsEnabled(model => model.IsRefitEnabled)
                         .BindOnClicked(model => model.OnClickConfiguration6())
                         .BindTooltip(model => model.Configuration6Tooltip)
                         .SetHeight(32f)
                         .SetWidth(32f);
                        row.AddButtonImage()
                         .BindImageResref(model => model.Configuration7Resref)
                         .BindIsVisible(model => model.Configuration7Visible)
                         .BindIsEnabled(model => model.IsRefitEnabled)
                         .BindOnClicked(model => model.OnClickConfiguration7())
                         .BindTooltip(model => model.Configuration7Tooltip)
                         .SetHeight(32f)
                         .SetWidth(32f);
                        row.AddButtonImage()
                         .BindImageResref(model => model.Configuration8Resref)
                         .BindIsVisible(model => model.Configuration8Visible)
                         .BindIsEnabled(model => model.IsRefitEnabled)
                         .BindOnClicked(model => model.OnClickConfiguration8())
                         .BindTooltip(model => model.Configuration8Tooltip)
                         .SetHeight(32f)
                         .SetWidth(32f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("Board Ship")
                            .BindIsEnabled(model => model.IsBoardShipEnabled)
                            .BindOnClicked(model => model.OnClickBoardShip())
                            .SetHeight(35f);

                        row.AddButton()
                            .SetText("Permissions")
                            .BindIsEnabled(model => model.IsPermissionsEnabled)
                            .BindOnClicked(model => model.OnClickPermissions())
                            .SetHeight(35f);
                    });
                })

                ;

            return _builder.Build();
        }
    }
}
