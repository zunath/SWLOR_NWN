using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public partial class ManageCityDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<ManageCityViewModel> _builder = new();

        private const float TabRowHeight = 28f;
        private const float TabPanelHeight = 48f;
        private const float ContentPanelWidth = 560f;

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.ManageCity)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 610f, 560f)
                .SetTitle("Manage City")
                .DefinePartialView(ManageCityViewModel.CityTabPartial, AddCityTab)
                .DefinePartialView(ManageCityViewModel.EdictsTabPartial, AddEdictsTab)
                .DefinePartialView(ManageCityViewModel.SuccessionTabPartial, AddSuccessionTab);

            window.AddStandardLayout(layout =>
            {
                layout.SetTabPanelHeight(TabPanelHeight);
                layout.AddTabRow(row =>
                {
                    row.SetHeight(TabRowHeight);
                    row.AddToggles()
                        .AddOption("City")
                        .AddOption("Edicts")
                        .AddOption("Succession")
                        .BindSelectedValue(model => model.TabToggleValue)
                        .SetWidth(348f)
                        .SetHeight(TabRowHeight);
                });
                layout.SetContentPartialElement(ManageCityViewModel.TabContentElement);
            });

            return _builder.Build();
        }

        /// <summary>One tab's content in a fixed-width borderless panel that scrolls itself.</summary>
        private static void AddPanel(GuiGroup<ManageCityViewModel> host, System.Action<GuiColumn<ManageCityViewModel>> content)
        {
            host.AddColumn(col => col.AddRow(row => row.AddGroup(panel =>
            {
                panel.SetShowBorder(false);
                panel.SetScrollbars(NuiScrollbars.Auto);
                panel.AddColumn(content);
            }).SetWidth(ContentPanelWidth)));
        }

        private static void AddCityTab(GuiGroup<ManageCityViewModel> host)
        {
            AddPanel(host, layout =>
            {
                layout.AddRow(row =>
                {
                    row.AddLabel()
                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                        .SetVerticalAlign(NuiVerticalAlign.Middle)
                        .SetHeight(25f)
                        .BindText(model => model.Instructions)
                        .BindColor(model => model.InstructionsColor);
                });

                layout.AddRow(layoutRow =>
                {
                    layoutRow.AddColumn(col =>
                    {
                        col.AddRow(row =>
                        {
                            row.AddTextEdit()
                                .BindValue(model => model.CityName)
                                .SetPlaceholder("Name");
                        });

                        col.AddRow(row =>
                        {
                            row.AddLabel()
                                .BindText(model => model.CityLevel)
                                .SetHeight(25f)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                .SetVerticalAlign(NuiVerticalAlign.Middle);
                        });

                        col.AddRow(row =>
                        {
                            row.AddLabel()
                                .SetText("Upgrades")
                                .SetHeight(25f)
                                .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                .SetVerticalAlign(NuiVerticalAlign.Middle);
                        });

                        col.AddRow(row =>
                        {
                            row.AddLabel()
                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                .SetVerticalAlign(NuiVerticalAlign.Middle)
                                .SetHeight(25f)
                                .BindText(model => model.MedicalCenterLevel)
                                .BindTooltip(model => model.MedicalCenterCurrentUpgrade);

                            row.AddButton()
                                .SetText("Upgrade Med. Centers")
                                .SetHeight(35f)
                                .BindOnClicked(model => model.UpgradeMedicalCenterLevel())
                                .BindIsEnabled(model => model.CanUpgradeMedicalCenters)
                                .BindTooltip(model => model.MedicalCenterNextUpgrade);
                        });

                        col.AddRow(row =>
                        {
                            row.AddLabel()
                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                .SetVerticalAlign(NuiVerticalAlign.Middle)
                                .SetHeight(25f)
                                .BindText(model => model.StarportLevel)
                                .BindTooltip(model => model.StarportCurrentUpgrade);

                            row.AddButton()
                                .SetText("Upgrade Starports")
                                .SetHeight(35f)
                                .BindOnClicked(model => model.UpgradeStarportLevel())
                                .BindIsEnabled(model => model.CanUpgradeStarports)
                                .BindTooltip(model => model.StarportNextUpgrade);
                        });

                        col.AddRow(row =>
                        {
                            row.AddLabel()
                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                .SetVerticalAlign(NuiVerticalAlign.Middle)
                                .SetHeight(25f)
                                .BindText(model => model.CantinaLevel)
                                .BindTooltip(model => model.CantinaCurrentUpgrade);

                            row.AddButton()
                                .SetText("Upgrade Cantinas")
                                .SetHeight(35f)
                                .BindOnClicked(model => model.UpgradeCantinaLevel())
                                .BindIsEnabled(model => model.CanUpgradeCantinas)
                                .BindTooltip(model => model.CantinaNextUpgrade);
                        });

                        col.AddRow(row =>
                        {
                            row.AddLabel()
                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                .SetVerticalAlign(NuiVerticalAlign.Middle)
                                .SetHeight(25f)
                                .BindText(model => model.JumpBeaconLevel)
                                .BindTooltip(model => model.JumpBeaconCurrentUpgrade);

                            row.AddButton()
                                .SetText("Upgrade Jump Beacon")
                                .SetHeight(35f)
                                .BindOnClicked(model => model.UpgradeJumpBeaconLevel())
                                .BindIsEnabled(model => model.CanUpgradeJumpBeacons)
                                .BindTooltip(model => model.JumpBeaconNextUpgrade);

                            row.BindIsVisible(model => model.IsStation);
                        });

                        col.AddRow(row =>
                        {
                            row.AddLabel()
                                .SetText("Taxes & Fees")
                                .SetHeight(25f)
                                .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                .SetVerticalAlign(NuiVerticalAlign.Middle);
                        });

                        col.AddRow(row =>
                        {
                            row.AddLabel()
                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                .SetVerticalAlign(NuiVerticalAlign.Middle)
                                .SetText("Transportation:")
                                .SetHeight(25f)
                                .SetTooltip("A percentage rate added to all transportation fees. (Range: 0%-25%)");
                        });

                        col.AddRow(row =>
                        {
                            row.AddTextEdit()
                                .BindValue(model => model.TransportationTax)
                                .BindIsEnabled(model => model.CanEditTaxes);
                        });

                        col.AddRow(row =>
                        {
                            row.AddLabel()
                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                .SetVerticalAlign(NuiVerticalAlign.Middle)
                                .SetText("Citizenship:")
                                .SetHeight(25f)
                                .SetTooltip("A flat rate of credits charged to each citizen once a week. (Range: 0-50000)");
                        });

                        col.AddRow(row =>
                        {
                            row.AddTextEdit()
                                .BindValue(model => model.CitizenshipTax)
                                .BindIsEnabled(model => model.CanEditTaxes);
                        });

                        col.AddRow(row =>
                        {
                            row.AddLabel()
                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                .SetVerticalAlign(NuiVerticalAlign.Middle)
                                .SetText("Exchange Tariff:")
                                .SetHeight(25f)
                                .SetTooltip("A percentage added to purchases and taken from sales at your starport's Trade Exchange, paid into the treasury. The exchange opens at starport level 2. (Range: 0%-10%)");
                        });

                        col.AddRow(row =>
                        {
                            row.AddTextEdit()
                                .BindValue(model => model.ExchangeTariff);
                        });

                        col.AddRow(row =>
                        {
                            row.AddLabel()
                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                .SetVerticalAlign(NuiVerticalAlign.Middle)
                                .SetText("Shipyard Fee:")
                                .SetHeight(25f)
                                .SetTooltip("Flat credits charged for each Capital hull built at your station's Shipyard, paid into the treasury. (Range: 0-50000)");

                            row.BindIsVisible(model => model.IsStation);
                        });

                        col.AddRow(row =>
                        {
                            row.AddTextEdit()
                                .BindValue(model => model.ShipyardFee);

                            row.BindIsVisible(model => model.IsStation);
                        });

                        col.AddRow(row =>
                        {
                            row.AddSpacer();

                            row.AddLabel()
                                .BindText(model => model.Treasury)
                                .SetHeight(25f);

                            row.AddSpacer();
                        });

                        col.AddRow(row =>
                        {
                            row.AddButton()
                                .BindOnClicked(model => model.Withdraw())
                                .SetHeight(35f)
                                .SetText("Withdraw")
                                .BindIsEnabled(model => model.CanAccessTreasury);

                            row.AddButton()
                                .BindOnClicked(model => model.Deposit())
                                .SetHeight(35f)
                                .SetText("Deposit")
                                .BindIsEnabled(model => model.CanAccessTreasury);
                        });

                        col.AddRow(row =>
                        {
                            row.AddSpacer();
                        });

                        col.AddRow(row =>
                        {
                            row.AddButton()
                                .SetText("Save Changes")
                                .BindOnClicked(model => model.SaveChanges())
                                .SetHeight(35f);

                            row.AddButton()
                                .SetText("Reset")
                                .BindOnClicked(model => model.ResetChanges())
                                .SetHeight(35f);
                        });
                    });

                    layoutRow.AddColumn(col =>
                    {
                        col.AddRow(row =>
                        {
                            row.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.AddToggleButton()
                                            .BindText(model => model.CitizenNames)
                                            .BindTooltip(model => model.CitizenNames);
                                    });

                                    template.AddCell(cell =>
                                    {
                                        cell.AddLabel()
                                            .BindText(model => model.CitizenCreditsOwed)
                                            .BindTooltip(model => model.CitizenCreditsOwed);
                                    });
                                })
                                .BindRowCount(model => model.CitizenNames);
                        });

                        col.AddRow(row =>
                        {
                            row.AddLabel()
                                .SetText("Upkeep")
                                .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                .SetVerticalAlign(NuiVerticalAlign.Middle)
                                .SetHeight(25f);
                        });

                        col.AddRow(row =>
                        {
                            row.AddButton()
                                .BindText(model => model.UpkeepText)
                                .BindOnClicked(model => model.PayUpkeep())
                                .BindIsEnabled(model => model.CanManageUpkeep)
                                .SetHeight(35f)
                                .SetWidth(350f);
                        });

                        col.AddRow(row =>
                        {
                            row.AddLabel()
                                .SetText("City Affairs")
                                .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                .SetVerticalAlign(NuiVerticalAlign.Middle)
                                .SetHeight(25f);
                        });

                        col.AddRow(row =>
                        {
                            row.AddButton()
                                .SetText("Open City Affairs")
                                .SetTooltip("Decide the city's open situations and appoint officers.")
                                .BindOnClicked(model => model.OpenCityAffairs())
                                .SetHeight(35f)
                                .SetWidth(350f);
                        });

                        AddStreetLifeSection(col);
                    });
                });
            });
        }
    }
}
