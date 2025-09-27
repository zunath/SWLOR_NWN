using SWLOR.Component.Space.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Space.Enums;
using SWLOR.Shared.Domain.Space.ValueObjects;

namespace SWLOR.Component.Space.Feature.ShipModuleDefinition
{
    public class CapitalPowerDiverterModuleDefinition : IShipModuleListDefinition
    {
        private readonly IShipModuleBuilder _builder;

        public CapitalPowerDiverterModuleDefinition(IShipModuleBuilder builder)
        {
            _builder = builder;
        }

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
