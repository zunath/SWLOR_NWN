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
                .SetInitialGeometry(0, 0, 545f, 295.5f)
                .SetTitle("Ship Management")

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddLabel()
                            .BindText(model => model.ShipCountRegistered)
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
                                    .BindIsToggled(model => model.ShipToggles)
                                    .BindColor(model => model.ShipColors);
                            });
                        })
                            .BindRowCount(model => model.ShipNames);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("Register Ship")
                            .BindIsEnabled(model => model.IsRegisterEnabled)
                            .BindOnClicked(model => model.OnClickRegisterShip());

                        row.AddButton()
                            .SetText("Unregister Ship")
                            .BindIsEnabled(model => model.IsUnregisterEnabled)
                            .BindOnClicked(model => model.OnClickUnregisterShip());
                    });
                })

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddTextEdit()
                            .BindValue(model => model.ShipName)
                            .SetPlaceholder("Ship Name");

                        row.AddButton()
                            .SetText("Save")
                            .BindOnClicked(model => model.OnClickSaveShipName())
                            .SetHeight(35f)
                            .SetWidth(40f);
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
                        row.AddLabel()
                            .SetText("High-Powered Slots")
                            .SetHeight(20f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButtonImage()
                            .BindImageResref(model => model.HighPower1Resref)
                            .BindIsVisible(model => model.HighPower1Visible)
                            .BindOnClicked(model => model.OnClickHighPower1())
                            .BindTooltip(model => model.HighPower1Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.HighPower2Resref)
                            .BindIsVisible(model => model.HighPower2Visible)
                            .BindOnClicked(model => model.OnClickHighPower2())
                            .BindTooltip(model => model.HighPower2Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.HighPower3Resref)
                            .BindIsVisible(model => model.HighPower3Visible)
                            .BindOnClicked(model => model.OnClickHighPower3())
                            .BindTooltip(model => model.HighPower3Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.HighPower4Resref)
                            .BindIsVisible(model => model.HighPower4Visible)
                            .BindOnClicked(model => model.OnClickHighPower4())
                            .BindTooltip(model => model.HighPower4Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.HighPower5Resref)
                            .BindIsVisible(model => model.HighPower5Visible)
                            .BindOnClicked(model => model.OnClickHighPower5())
                            .BindTooltip(model => model.HighPower5Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.HighPower6Resref)
                            .BindIsVisible(model => model.HighPower6Visible)
                            .BindOnClicked(model => model.OnClickHighPower6())
                            .BindTooltip(model => model.HighPower6Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.HighPower7Resref)
                            .BindIsVisible(model => model.HighPower7Visible)
                            .BindOnClicked(model => model.OnClickHighPower7())
                            .BindTooltip(model => model.HighPower7Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.HighPower8Resref)
                            .BindIsVisible(model => model.HighPower8Visible)
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
                            .BindOnClicked(model => model.OnClickLowPower1())
                            .BindTooltip(model => model.LowPower1Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.LowPower2Resref)
                            .BindIsVisible(model => model.LowPower2Visible)
                            .BindOnClicked(model => model.OnClickLowPower2())
                            .BindTooltip(model => model.LowPower2Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.LowPower3Resref)
                            .BindIsVisible(model => model.LowPower3Visible)
                            .BindOnClicked(model => model.OnClickLowPower3())
                            .BindTooltip(model => model.LowPower3Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.LowPower4Resref)
                            .BindIsVisible(model => model.LowPower4Visible)
                            .BindOnClicked(model => model.OnClickLowPower4())
                            .BindTooltip(model => model.LowPower4Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.LowPower5Resref)
                            .BindIsVisible(model => model.LowPower5Visible)
                            .BindOnClicked(model => model.OnClickLowPower5())
                            .BindTooltip(model => model.LowPower5Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.LowPower6Resref)
                            .BindIsVisible(model => model.LowPower6Visible)
                            .BindOnClicked(model => model.OnClickLowPower6())
                            .BindTooltip(model => model.LowPower6Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.LowPower7Resref)
                            .BindIsVisible(model => model.LowPower7Visible)
                            .BindOnClicked(model => model.OnClickLowPower7())
                            .BindTooltip(model => model.LowPower7Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.LowPower8Resref)
                            .BindIsVisible(model => model.LowPower8Visible)
                            .BindOnClicked(model => model.OnClickLowPower8())
                            .BindTooltip(model => model.LowPower8Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddButton()
                            .SetText("Make Active")
                            .BindIsEnabled(model => model.IsMakeActiveEnabled)
                            .BindOnClicked(model => model.OnClickMakeActive());
                        row.AddSpacer();
                    });
                })

                ;

            return _builder.Build();
        }
    }
}
