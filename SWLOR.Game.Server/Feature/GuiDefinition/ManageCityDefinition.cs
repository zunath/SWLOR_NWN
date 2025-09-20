using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Shared.Core.Beamdog;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class ManageCityDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<ManageCityViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.ManageCity)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 545f, 295.5f)
                .SetTitle("Manage City")
                
                .AddColumn(layout =>
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
                                    .BindText(model => model.BankUpgradeLevel)
                                    .BindTooltip(model => model.BankCurrentUpgrade);

                                row.AddButton()
                                    .SetText("Upgrade Banks")
                                    .SetHeight(35f)
                                    .BindOnClicked(model => model.UpgradeBankLevel())
                                    .BindIsEnabled(model => model.CanUpgradeBanks)
                                    .BindTooltip(model => model.BankNextUpgrade);
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
                                    .BindValue(model => model.TransportationTax);
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
                                    .BindValue(model => model.CitizenshipTax);
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
                                    .SetHeight(35f)
                                    .SetWidth(350f);
                            });
                        });
                    });

                });

            return _builder.Build();
        }
    }
}
