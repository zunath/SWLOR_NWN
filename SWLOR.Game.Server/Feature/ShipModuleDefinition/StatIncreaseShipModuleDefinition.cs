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
            ShieldBooster("shld_boost_b", "Basic Shield Booster", 5, 1);
            ShieldBooster("shld_boost_1", "Shield Booster I", 8, 2);
            ShieldBooster("shld_boost_2", "Shield Booster II", 11, 3);
            ShieldBooster("shld_boost_3", "Shield Booster III", 14, 4);
            ShieldBooster("shld_boost_4", "Shield Booster IV", 17, 5);

            HullBooster("hull_boost_b", "Basic Hull Booster", 5, 1);
            HullBooster("hull_boost_1", "Hull Booster I", 8, 2);
            HullBooster("hull_boost_2", "Hull Booster II", 11, 3);
            HullBooster("hull_boost_3", "Hull Booster III", 14, 4);
            HullBooster("hull_boost_4", "Hull Booster IV", 17, 5);

            CapacitorBooster("cap_boost_b", "Basic Capacitor Booster", 5, 1);
            CapacitorBooster("cap_boost_1", "Capacitor Booster I", 8, 2);
            CapacitorBooster("cap_boost_2", "Capacitor Booster II", 11, 3);
            CapacitorBooster("cap_boost_3", "Capacitor Booster III", 14, 4);
            CapacitorBooster("cap_boost_4", "Capacitor Booster IV", 17, 5);

            return _builder.Build();
        }

        private void ShieldBooster(string itemTag, string name, int shieldBoostAmount, int requiredLevel)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(name)
                .Description($"Improves a ship's maximum shields by {shieldBoostAmount}.")
                .PowerType(ShipModulePowerType.Low)
                .RequirePerk(PerkType.DefensiveModules, requiredLevel)
                .EquippedAction((player, item, ship) =>
                {
                    ship.MaxShieldBonus += shieldBoostAmount;
                })
                .UnequippedAction((player, item, ship) =>
                {
                    ship.MaxShieldBonus -= shieldBoostAmount;
                });
        }

        private void HullBooster(string itemTag, string name, int hullBoostAmount, int requiredLevel)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(name)
                .Description($"Improves a ship's maximum hull by {hullBoostAmount}.")
                .PowerType(ShipModulePowerType.Low)
                .RequirePerk(PerkType.DefensiveModules, requiredLevel)
                .EquippedAction((player, item, ship) =>
                {
                    ship.MaxHullBonus += hullBoostAmount;
                })
                .UnequippedAction((player, item, ship) =>
                {
                    ship.MaxHullBonus -= hullBoostAmount;
                });
        }

        private void CapacitorBooster(string itemTag, string name, int capacitorBoostAmount, int requiredLevel)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(name)
                .Description($"Improves a ship's maximum capacitor by {capacitorBoostAmount}.")
                .PowerType(ShipModulePowerType.Low)
                .RequirePerk(PerkType.DefensiveModules, requiredLevel)
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
