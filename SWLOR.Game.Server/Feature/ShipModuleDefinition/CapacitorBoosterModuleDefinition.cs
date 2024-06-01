using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class CapacitorBoosterModuleDefinition: IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {

            CapacitorBooster("cap_boost_b", "Basic Capacitor Booster", 5, 1);
            CapacitorBooster("cap_boost_1", "Capacitor Booster I", 8, 2);
            CapacitorBooster("cap_boost_2", "Capacitor Booster II", 11, 3);
            CapacitorBooster("cap_boost_3", "Capacitor Booster III", 14, 4);
            CapacitorBooster("cap_boost_4", "Capacitor Booster IV", 17, 5);

            return _builder.Build();
        }

        private void CapacitorBooster(string itemTag, string name, int capacitorBoostAmount, int requiredLevel)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(name)
                .Texture("iit_ess_035")
                .Description($"Improves a ship's maximum capacitor by {capacitorBoostAmount}.")
                .PowerType(ShipModulePowerType.Low)
                .RequirePerk(PerkType.DefensiveModules, requiredLevel)
                .EquippedAction((_, shipStatus, moduleBonus) =>
                {
                    shipStatus.MaxCapacitor += capacitorBoostAmount + moduleBonus * 2;
                })
                .UnequippedAction((_, shipStatus, moduleBonus) =>
                {
                    shipStatus.MaxCapacitor -= capacitorBoostAmount + moduleBonus * 2;
                });
        }
    }
}
