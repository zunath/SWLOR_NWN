using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class CapitalPowerDiverterModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            CapitalPowerDiverter("cap_pwdiv", "Capital Power Diverter", "Pwr Diverter", 6);

            return _builder.Build();
        }

        private void CapitalPowerDiverter(string itemTag,
            string name,
            string shortName,
            int boostAmount)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Texture("iit_ess_038")
                .Description($"Improves a ship's shield recharge by {boostAmount} at the cost of 40 max capacitor.")
                .PowerType(ShipModulePowerType.Low)
                .RequirePerk(PerkType.DefensiveModules, 5)
                .CapitalClassModule()
                .EquippedAction((shipStatus, moduleBonus) =>
                {
                    shipStatus.ShieldRechargeRate -= boostAmount + (moduleBonus / 6);
                    shipStatus.MaxCapacitor -= 60;
                })
                .UnequippedAction((shipStatus, moduleBonus) =>
                {
                    shipStatus.ShieldRechargeRate += boostAmount + (moduleBonus / 6);
                    shipStatus.MaxCapacitor += 60;
                });
        }

    }
}
