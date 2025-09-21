using System.Collections.Generic;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class AdvancedThrustersModuleDefinition : IShipModuleListDefinition
    {
        private readonly IShipModuleBuilder _builder = new();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            AdvancedThrusters("cap_thrust1", "Advanced Maneuvering Thrusters", "Adv Thrust", 20);

            return _builder.Build();
        }

        private void AdvancedThrusters(string itemTag, 
            string name, 
            string shortName,
            int boostAmount)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Texture("iit_ess_098")
                .Description($"Improves a ship's evasion by {boostAmount} at the cost of 30 max capacitor.")
                .PowerType(ShipModulePowerType.Low)
                .RequirePerk(PerkType.DefensiveModules, 5)
                .CapitalClassModule()
                .EquippedAction((shipStatus, moduleBonus) =>
                {
                    shipStatus.Evasion += boostAmount + moduleBonus;
                    shipStatus.MaxCapacitor -= 30;
                })
                .UnequippedAction((shipStatus, moduleBonus) =>
                {
                    shipStatus.Evasion -= boostAmount + moduleBonus;
                    shipStatus.MaxCapacitor += 30;
                });
        }

    }
}
