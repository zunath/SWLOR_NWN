using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// Ship Management: the ship list and its actions in a fixed rail, and the selected ship's details in the standard
    /// scrolling content panel (NuiLayoutRules R5). The default size fits a 1280x720 screen.
    /// </summary>
    public class ShipManagementDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<ShipManagementViewModel> _builder = new();

        private const float ListRailWidth = 300f;
        private const float DetailsPanelWidth = 400f;
        private const float BarLabelWidth = 90f;
        private const float WideButtonWidth = 210f;
        private const float SaleButtonWidth = 130f;
        private const float SlotLabelWidth = 52f;

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.ShipManagement)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 760f, 650f)
                .SetTitle("Ship Management")
                .DefinePartialView(ShipManagementViewModel.DetailsPartial, AddDetails);

            window.AddStandardLayout(layout => layout
                .AddLeadingColumn(AddShipList, ListRailWidth)
                .SetContentPartialElement(ShipManagementViewModel.DetailsElement));

            return _builder.Build();
        }

        private static void AddShipList(GuiColumn<ShipManagementViewModel> col)
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

            col.AddPagination(
                model => model.PageNumbers,
                model => model.SelectedPageIndex,
                model => model.OnPreviousPage(),
                model => model.OnNextPage());

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

            col.AddRow(row =>
            {
                row.AddButton()
                    .SetText("Transfer Ship")
                    .SetTooltip("Give the selected ship, with everything aboard, to a player at this starport.")
                    .BindIsVisible(model => model.IsMyShipsToggled)
                    .BindIsEnabled(model => model.IsTransferEnabled)
                    .BindOnClicked(model => model.OnClickTransferShip())
                    .SetHeight(35f);
            });

            col.AddRow(row =>
            {
                row.AddTextEdit()
                    .BindValue(model => model.SalePrice)
                    .SetPlaceholder("Price")
                    .SetMaxLength(9)
                    .BindIsVisible(model => model.IsMyShipsToggled)
                    .BindIsEnabled(model => model.IsSaleEnabled)
                    .SetHeight(35f);

                row.AddButton()
                    .BindText(model => model.SaleButtonText)
                    .SetTooltip("List the selected ship on the player market at the price typed beside it, or cancel its listing.")
                    .BindIsVisible(model => model.IsMyShipsToggled)
                    .BindIsEnabled(model => model.IsSaleEnabled)
                    .BindOnClicked(model => model.OnClickSellShip())
                    .SetHeight(35f)
                    .SetWidth(SaleButtonWidth);
            });

            col.AddRow(row =>
            {
                row.AddButton()
                    .SetText("Remove My Access")
                    .SetTooltip("Remove your permissions from the selected ship so it no longer appears in this list.")
                    .BindIsVisible(model => model.IsOtherShipsToggled)
                    .BindIsEnabled(model => model.IsRemoveAccessEnabled)
                    .BindOnClicked(model => model.OnClickRemoveAccess())
                    .SetHeight(35f);
            });
        }

        private static void AddDetails(GuiGroup<ShipManagementViewModel> host)
        {
            host.AddColumn(hostCol => hostCol.AddRow(hostRow => hostRow.AddGroup(panel =>
            {
                panel.SetShowBorder(false);
                panel.SetScrollbars(NuiScrollbars.None);
                panel.AddColumn(details =>
                {
                    details.AddRow(row =>
                    {
                        row.AddTextEdit()
                            .BindValue(model => model.ShipName)
                            .SetPlaceholder("Ship Name")
                            .BindIsEnabled(model => model.IsNameEnabled);

                        row.AddButton()
                            .SetText("Save")
                            .BindOnClicked(model => model.OnClickSaveShipName())
                            .SetHeight(35f)
                            .SetWidth(60f)
                            .BindIsEnabled(model => model.IsNameEnabled);
                    });
                    details.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .BindText(model => model.ShipType)
                            .SetHeight(20f);
                    });

                    details.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .BindText(model => model.ShipLocation)
                            .SetHeight(20f);
                    });

                    details.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .BindText(model => model.HullTraitsText)
                            .BindTooltip(model => model.HullTraitsTooltip)
                            .SetHeight(20f);
                    });

                    details.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetText("Shields:")
                            .SetWidth(BarLabelWidth);

                        row.AddProgressBar()
                            .BindValue(model => model.Shields)
                            .BindTooltip(model => model.ShieldsTooltip)
                            .SetHeight(20f)
                            .SetColor(0, 170, 228);
                    });
                    details.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetText("Hull:")
                            .SetWidth(BarLabelWidth);

                        row.AddProgressBar()
                            .BindValue(model => model.Hull)
                            .BindTooltip(model => model.HullTooltip)
                            .SetHeight(20f)
                            .SetColor(139, 0, 0);
                    });
                    details.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetText("Capacitor:")
                            .SetWidth(BarLabelWidth);

                        row.AddProgressBar()
                            .BindValue(model => model.Capacitor)
                            .BindTooltip(model => model.CapacitorTooltip)
                            .SetHeight(20f)
                            .SetColor(255, 165, 0);
                    });

                    details.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddButton()
                            .BindIsEnabled(model => model.IsRepairEnabled)
                            .BindText(model => model.RepairText)
                            .BindOnClicked(model => model.OnClickRepair())
                            .SetHeight(35f)
                            .SetWidth(WideButtonWidth);
                        row.AddSpacer();
                    });

                    details.AddRow(row =>
                    {
                        row.AddComboBox()
                            .BindOptions(model => model.HullDesignOptions)
                            .BindSelectedIndex(model => model.SelectedHullDesignIndex)
                            .BindIsEnabled(model => model.IsHullDesignEnabled)
                            .SetHeight(35f);

                        row.AddButton()
                            .BindIsEnabled(model => model.IsHullDesignEnabled)
                            .BindText(model => model.RefitDesignText)
                            .BindOnClicked(model => model.OnClickRefitDesign())
                            .SetTooltip("Refit the ship to the chosen Hull Design. Design Schematics unlock more designs.")
                            .SetHeight(35f)
                            .SetWidth(WideButtonWidth);
                    });

                    details.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("High")
                            .SetTooltip("High-Powered Slots")
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetWidth(SlotLabelWidth)
                            .SetHeight(20f);
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

                    details.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Mid")
                            .BindIsVisible(model => model.IsMidPowerVisible)
                            .SetTooltip("Mid-Powered Slots")
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetWidth(SlotLabelWidth)
                            .SetHeight(20f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.MidPower1Resref)
                            .BindIsVisible(model => model.MidPower1Visible)
                            .BindIsEnabled(model => model.IsRefitEnabled)
                            .BindOnClicked(model => model.OnClickMidPower1())
                            .BindTooltip(model => model.MidPower1Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.MidPower2Resref)
                            .BindIsVisible(model => model.MidPower2Visible)
                            .BindIsEnabled(model => model.IsRefitEnabled)
                            .BindOnClicked(model => model.OnClickMidPower2())
                            .BindTooltip(model => model.MidPower2Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.MidPower3Resref)
                            .BindIsVisible(model => model.MidPower3Visible)
                            .BindIsEnabled(model => model.IsRefitEnabled)
                            .BindOnClicked(model => model.OnClickMidPower3())
                            .BindTooltip(model => model.MidPower3Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.MidPower4Resref)
                            .BindIsVisible(model => model.MidPower4Visible)
                            .BindIsEnabled(model => model.IsRefitEnabled)
                            .BindOnClicked(model => model.OnClickMidPower4())
                            .BindTooltip(model => model.MidPower4Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.MidPower5Resref)
                            .BindIsVisible(model => model.MidPower5Visible)
                            .BindIsEnabled(model => model.IsRefitEnabled)
                            .BindOnClicked(model => model.OnClickMidPower5())
                            .BindTooltip(model => model.MidPower5Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.MidPower6Resref)
                            .BindIsVisible(model => model.MidPower6Visible)
                            .BindIsEnabled(model => model.IsRefitEnabled)
                            .BindOnClicked(model => model.OnClickMidPower6())
                            .BindTooltip(model => model.MidPower6Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.MidPower7Resref)
                            .BindIsVisible(model => model.MidPower7Visible)
                            .BindIsEnabled(model => model.IsRefitEnabled)
                            .BindOnClicked(model => model.OnClickMidPower7())
                            .BindTooltip(model => model.MidPower7Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.MidPower8Resref)
                            .BindIsVisible(model => model.MidPower8Visible)
                            .BindIsEnabled(model => model.IsRefitEnabled)
                            .BindOnClicked(model => model.OnClickMidPower8())
                            .BindTooltip(model => model.MidPower8Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                    });

                    details.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Low")
                            .SetTooltip("Low-Powered Slots")
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetWidth(SlotLabelWidth)
                            .SetHeight(20f);
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

                    details.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Config")
                            .BindIsVisible(model => model.IsConfigurationVisible)
                            .SetTooltip("Configuration Slot")
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetWidth(SlotLabelWidth)
                            .SetHeight(20f);
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
                });
            }).SetWidth(DetailsPanelWidth)));
        }
    }
}
