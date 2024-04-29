using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class ShipArmorModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new ShipModuleBuilder();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            ThermalArmor("las_armor_1", "Mirrored Plating I", "Therm Armor 1", 2, 1);
            ThermalArmor("las_armor_2", "Mirrored Plating II", "Therm Armor 1", 4, 2);
            ThermalArmor("las_armor_3", "Mirrored Plating III", "Therm Armor 1", 6, 3);
            ThermalArmor("las_armor_4", "Mirrored Plating IV", "Therm Armor 1", 8, 4);
            ThermalArmor("las_armor_5", "Mirrored Plating V", "Therm Armor 1", 10, 5);

            IonArmor("em_armor_1", "Supplemental Ion Shielding I", "EM Armor 1", 2, 1);
            IonArmor("em_armor_2", "Supplemental Ion Shielding II", "EM Armor 1", 4, 2);
            IonArmor("em_armor_3", "Supplemental Ion Shielding III", "EM Armor 1", 6, 3);
            IonArmor("em_armor_4", "Supplemental Ion Shielding IV", "EM Armor 1", 8, 4);
            IonArmor("em_armor_5", "Supplemental Ion Shielding V", "EM Armor 1", 10, 5);

            ExplosiveArmor("exp_armor_1", "Reactive Armor I", "Exp Armor 1", 2, 1);
            ExplosiveArmor("exp_armor_2", "Reactive Armor II", "Exp Armor 1", 4, 2);
            ExplosiveArmor("exp_armor_3", "Reactive Armor III", "Exp Armor 1", 6, 3);
            ExplosiveArmor("exp_armor_4", "Reactive Armor IV", "Exp Armor 1", 8, 4);
            ExplosiveArmor("exp_armor_5", "Reactive Armor V", "Exp Armor 1", 10, 5);

            HeavyArmor("hvy_armor_1", "Durasteel Plating I", "Hvy Armor 1", 2, 1);
            HeavyArmor("hvy_armor_1", "Durasteel Plating II", "Hvy Armor 1", 4, 2);
            HeavyArmor("hvy_armor_1", "Durasteel Plating III", "Hvy Armor 1", 6, 3);
            HeavyArmor("hvy_armor_1", "Durasteel Plating IV", "Hvy Armor 1", 8, 4);
            HeavyArmor("hvy_armor_1", "Durasteel Plating V", "Hvy Armor 1", 10, 5);

            return _builder.Build();
        }

        private void ThermalArmor(string itemTag, string name, string shortName, int armorBoost, int requiredLevel)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Texture("iit_ess8_145")
                .Description($"Improves a ship's thermal defense by {armorBoost*2}.")
                .PowerType(ShipModulePowerType.Low)
                .RequirePerk(PerkType.DefensiveModules, requiredLevel)
                .EquippedAction((creature, shipStatus, moduleBonus) =>
                {
                    shipStatus.ThermalDefense += 2 * armorBoost + moduleBonus;
                })
                .UnequippedAction((creature, shipStatus, moduleBonus) =>
                {
                    shipStatus.ThermalDefense -= 2 * armorBoost + moduleBonus;
                });
        }

        private void IonArmor(string itemTag, string name, string shortName, int armorBoost, int requiredLevel)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(name)
                .Texture("iit_ess8_145")
                .Description($"Improves a ship's EM defense by {armorBoost*2}.")
                .PowerType(ShipModulePowerType.Low)
                .RequirePerk(PerkType.DefensiveModules, requiredLevel)
                .EquippedAction((creature, shipStatus, moduleBonus) =>
                {
                    shipStatus.EMDefense += 2 * armorBoost + moduleBonus;
                })
                .UnequippedAction((creature, shipStatus, moduleBonus) =>
                {
                    shipStatus.EMDefense -= 2 * armorBoost + moduleBonus;
                });
        }

        private void ExplosiveArmor(string itemTag, string name, string shortName, int armorBoost, int requiredLevel)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(name)
                .Texture("iit_ess8_145")
                .Description($"Improves a ship's explosive defense by {armorBoost*2}.")
                .PowerType(ShipModulePowerType.Low)
                .RequirePerk(PerkType.DefensiveModules, requiredLevel)
                .EquippedAction((creature, shipStatus, moduleBonus) =>
                {
                    shipStatus.ExplosiveDefense += 2 * armorBoost + moduleBonus;
                })
                .UnequippedAction((creature, shipStatus, moduleBonus) =>
                {
                    shipStatus.ExplosiveDefense -= 2 * armorBoost + moduleBonus;
                });
        }

        private void HeavyArmor(string itemTag, string name, string shortName, int armorBoost, int requiredLevel)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(name)
                .Texture("iit_ess8_145")
                .Description($"Improves a ship's overall defenses by {armorBoost} at the cost of {armorBoost} evasion.")
                .PowerType(ShipModulePowerType.Low)
                .RequirePerk(PerkType.DefensiveModules, requiredLevel)
                .EquippedAction((creature, shipStatus, moduleBonus) =>
                {
                    shipStatus.ThermalDefense += armorBoost + moduleBonus;
                    shipStatus.EMDefense+= armorBoost + moduleBonus;
                    shipStatus.ExplosiveDefense += armorBoost + moduleBonus;
                    shipStatus.Evasion -= armorBoost;
                })
                .UnequippedAction((creature, shipStatus, moduleBonus) =>
                {
                    shipStatus.ThermalDefense -= armorBoost + moduleBonus;
                    shipStatus.EMDefense -= armorBoost + moduleBonus;
                    shipStatus.ExplosiveDefense -= armorBoost + moduleBonus;
                    shipStatus.Evasion += armorBoost;
                });
        }

    }
}