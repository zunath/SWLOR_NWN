using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class AdvancedThrustersModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new();

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
                .Description($"Improves a ship's evasion by {boostAmount} at the cost of {boostAmount * 3} max capacitor.")
                .PowerType(ShipModulePowerType.Low)
                .RequirePerk(PerkType.DefensiveModules, 5)
                .CapitalClassModule()
                .EquippedAction((shipStatus, moduleBonus) =>
                {
                    shipStatus.Evasion += boostAmount + moduleBonus;
                    shipStatus.MaxCapacitor -= boostAmount;
                })
                .UnequippedAction((shipStatus, moduleBonus) =>
                {
                    shipStatus.Evasion -= boostAmount + moduleBonus;
                    shipStatus.MaxCapacitor += boostAmount;
                });
        }

    }
}
