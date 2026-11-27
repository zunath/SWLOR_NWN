using System;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.TownsfolkService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// The City tab's street life switches (Player City Revamp, "Ambient townsfolk"): the mayor shows or hides the wandering
    /// townsfolk and guards, and the street critters, separately.
    /// </summary>
    public partial class ManageCityViewModel
    {
        public string TownsfolkSwitchText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string CrittersSwitchText
        {
            get => Get<string>();
            set => Set(value);
        }

        /// <summary>Whether the player may use the switches (<see cref="TownsfolkRules.SwitchPower"/>).</summary>
        public bool CanSwitchStreetLife
        {
            get => Get<bool>();
            set => Set(value);
        }

        /// <summary>Whether the critters switch is shown. A station has no critters.</summary>
        public bool ShowCrittersSwitch
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string TownsfolkSwitchTooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string CrittersSwitchTooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        private void InitializeStreetLife()
        {
            TownsfolkSwitchTooltip = TownsfolkRules.BuildTooltip(TownsfolkSwitch.Townsfolk);
            CrittersSwitchTooltip = TownsfolkRules.BuildTooltip(TownsfolkSwitch.Critters);
            RefreshStreetLife();
        }

        /// <summary>Reads the city and the player's powers again and rebuilds the switches.</summary>
        private void RefreshStreetLife()
        {
            var city = DB.Get<WorldProperty>(_cityId);
            if (city == null)
            {
                CanSwitchStreetLife = false;
                return;
            }

            CanSwitchStreetLife = CityPowers.Can(city, GetObjectUUID(Player), TownsfolkRules.SwitchPower);
            ShowCrittersSwitch = TownsfolkRules.HasCritters(Station.IsStation(city));
            TownsfolkSwitchText = TownsfolkRules.BuildButtonText(TownsfolkSwitch.Townsfolk, city.TownsfolkHidden);
            CrittersSwitchText = TownsfolkRules.BuildButtonText(TownsfolkSwitch.Critters, city.CrittersHidden);
        }

        public Action ToggleTownsfolk() => () => ToggleStreetLife(TownsfolkSwitch.Townsfolk);

        public Action ToggleCritters() => () => ToggleStreetLife(TownsfolkSwitch.Critters);

        private void ToggleStreetLife(TownsfolkSwitch which)
        {
            // Read the city fresh: another official may have flipped the switch since this window opened.
            var city = DB.Get<WorldProperty>(_cityId);
            var playerId = GetObjectUUID(Player);
            if (city == null || !CityPowers.Can(city, playerId, TownsfolkRules.SwitchPower))
            {
                Instructions = "Insufficient permissions.";
                InstructionsColor = GuiColor.Red;
                RefreshStreetLife();
                return;
            }

            var hidden = !Townsfolk.IsHidden(city, which);
            Townsfolk.SetHidden(city, which, hidden, playerId, CityPowers.GetDelegateTitle(city, playerId));

            RefreshStreetLife();
            Instructions = TownsfolkRules.BuildResultText(which, hidden);
            InstructionsColor = GuiColor.Green;
        }
    }
}
