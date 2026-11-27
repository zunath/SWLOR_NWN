using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public partial class ManageCityDefinition
    {
        /// <summary>The City tab's street life switches: one button each for the townsfolk and the critters.</summary>
        private static void AddStreetLifeSection(GuiColumn<ManageCityViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddLabel()
                    .SetText("Street Life")
                    .SetHorizontalAlign(NuiHorizontalAlign.Center)
                    .SetVerticalAlign(NuiVerticalAlign.Middle)
                    .SetHeight(25f);
            });

            col.AddRow(row =>
            {
                row.AddButton()
                    .BindText(model => model.TownsfolkSwitchText)
                    .BindTooltip(model => model.TownsfolkSwitchTooltip)
                    .BindOnClicked(model => model.ToggleTownsfolk())
                    .BindIsEnabled(model => model.CanSwitchStreetLife)
                    .SetHeight(35f)
                    .SetWidth(350f);
            });

            col.AddRow(row =>
            {
                row.AddButton()
                    .BindText(model => model.CrittersSwitchText)
                    .BindTooltip(model => model.CrittersSwitchTooltip)
                    .BindOnClicked(model => model.ToggleCritters())
                    .BindIsEnabled(model => model.CanSwitchStreetLife)
                    .SetHeight(35f)
                    .SetWidth(350f);

                row.BindIsVisible(model => model.ShowCrittersSwitch);
            });
        }
    }
}
