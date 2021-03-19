using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class StatIncreaseShipModuleDefinition: IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new ShipModuleBuilder();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            ShieldBooster("shld_boost_b", "Basic Shield Booster", 5);
            ShieldBooster("shld_boost_1", "Shield Booster I", 8);
            ShieldBooster("shld_boost_2", "Shield Booster II", 11);
            ShieldBooster("shld_boost_3", "Shield Booster III", 14);
            ShieldBooster("shld_boost_4", "Shield Booster IV", 17);

            HullBooster("hull_boost_b", "Basic Hull Booster", 5);
            HullBooster("hull_boost_1", "Hull Booster I", 8);
            HullBooster("hull_boost_2", "Hull Booster II", 11);
            HullBooster("hull_boost_3", "Hull Booster III", 14);
            HullBooster("hull_boost_4", "Hull Booster IV", 17);

            CapacitorBooster("cap_boost_b", "Basic Capacitor Booster", 5);
            CapacitorBooster("cap_boost_1", "Capacitor Booster I", 8);
            CapacitorBooster("cap_boost_2", "Capacitor Booster II", 11);
            CapacitorBooster("cap_boost_3", "Capacitor Booster III", 14);
            CapacitorBooster("cap_boost_4", "Capacitor Booster IV", 17);

            return _builder.Build();
        }

        private void ShieldBooster(string itemTag, string name, int shieldBoostAmount)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(name)
                .Description($"Improves a ship's maximum shields by {shieldBoostAmount}.")
                .PowerType(ShipModulePowerType.Low)
                .EquippedAction((player, item, ship) =>
                {
                    ship.MaxShieldBonus += shieldBoostAmount;
                })
                .UnequippedAction((player, item, ship) =>
                {
                    ship.MaxShieldBonus -= shieldBoostAmount;
                });
        }

        private void HullBooster(string itemTag, string name, int hullBoostAmount)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(name)
                .Description($"Improves a ship's maximum hull by {hullBoostAmount}.")
                .PowerType(ShipModulePowerType.Low)
                .EquippedAction((player, item, ship) =>
                {
                    ship.MaxHullBonus += hullBoostAmount;
                })
                .UnequippedAction((player, item, ship) =>
                {
                    ship.MaxHullBonus -= hullBoostAmount;
                });
        }

        private void CapacitorBooster(string itemTag, string name, int capacitorBoostAmount)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(name)
                .Description($"Improves a ship's maximum capacitor by {capacitorBoostAmount}.")
                .PowerType(ShipModulePowerType.Low)
                .EquippedAction((player, item, ship) =>
                {
                    ship.MaxCapacitorBonus += capacitorBoostAmount;
                })
                .UnequippedAction((player, item, ship) =>
                {
                    ship.MaxCapacitorBonus -= capacitorBoostAmount;
                });
        }
    }
}
