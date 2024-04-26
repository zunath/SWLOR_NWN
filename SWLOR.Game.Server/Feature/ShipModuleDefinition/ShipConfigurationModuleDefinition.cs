using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class ShipConfigurationModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new ShipModuleBuilder();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {

            ShipConfig("config_fig1", "Fighter Configuration I", "iit_ess8_121", 1, 10, 10, 0, 10, 3, 10, 0, 10, 2, 2, 0);
            ShipConfig("config_fig1", "Fighter Configuration II", "iit_ess8_121", 2, 25, 25, 0, 25, 8, 25, 0, 25, 5, 5, 0);
            ShipConfig("config_fig1", "Fighter Configuration III", "iit_ess8_121", 3, 40, 40, 0, 60, 14, 60, 0, 60, 10, 10, 0);
            ShipConfig("config_fig1", "Fighter Configuration IV", "iit_ess8_121", 4, 75, 75, 0, 100, 25, 100, 0, 100, 20, 20, 0);
            ShipConfig("config_fig1", "Fighter Configuration V", "iit_ess8_121", 5, 100, 100, 0, 130, 35, 130, 0, 130, 30, 30, 0);
            ShipConfig("config_int1", "Interceptor Configuration I", "iit_ess8_122", 1, 5, 5, 5, 10, 0, 10, 0, 20, 2, 4, 0);
            ShipConfig("config_int1", "Interceptor Configuration II", "iit_ess8_122", 2, 15, 15, 10, 25, 0, 25, 0, 40, 5, 8, 0);
            ShipConfig("config_int1", "Interceptor Configuration III", "iit_ess8_122", 3, 25, 25, 20, 60, 0, 60, 0, 90, 10, 12, 0);
            ShipConfig("config_int1", "Interceptor Configuration IV", "iit_ess8_122", 4, 50, 50, 35, 100, 0, 100, 0, 120, 20, 25, 0);
            ShipConfig("config_int1", "Interceptor Configuration V", "iit_ess8_122", 5, 70, 70, 50, 130, 0, 130, 0, 150, 30, 50, 0);
            ShipConfig("config_bmb1", "Bomber Configuration I", "iit_ess8_123", 1, 10, 10, 0, 20, 0, 10, 5, 7, 4, 0 , 0);
            ShipConfig("config_bmb1", "Bomber Configuration II", "iit_ess8_123", 2, 25, 25, 0, 40, 0, 25, 10, 15, 8, 0, 0);
            ShipConfig("config_bmb1", "Bomber Configuration III", "iit_ess8_123", 3, 40, 40, 0, 90, 0, 60, 20, 40, 12, 5, 0);
            ShipConfig("config_bmb1", "Bomber Configuration IV", "iit_ess8_123", 4, 75, 75, 0, 120, 0, 100, 35, 75, 25, 10, 0);
            ShipConfig("config_bmb1", "Bomber Configuration V", "iit_ess8_123", 5, 100, 100, 0, 150, 0, 130, 50, 100, 40, 20, 0);
            ShipConfig("config_ind1", "Industrial Configuration I", "iit_ess8_124", 1, 20, 20, 0, 7, 0, 7, 0, 7, 0, 0, 2);
            ShipConfig("config_ind2", "Industrial Configuration II", "iit_ess8_124", 2, 50, 50, 0, 15, 0, 15, 0, 15, 0, 0, 4);
            ShipConfig("config_ind3", "Industrial Configuration III", "iit_ess8_124", 3, 80, 80, 0, 40, 0, 40, 0, 40, 0, 0, 6);
            ShipConfig("config_ind4", "Industrial Configuration IV", "iit_ess8_124", 4, 150, 150, 0, 75, 0, 75, 0, 75, 0, 0, 8);
            ShipConfig("config_ind5", "Industrial Configuration V", "iit_ess8_124", 5, 300, 300, 0, 100, 0, 100, 0, 100, 0, 0, 10);

            ShipConfig("npc_cap1", "Boss Config I", "iit_ess8_121", 6, 0, 0, 0, 20, 0, 15, 10, 10, 5, 5, 0);
            ShipConfig("npc_cap1", "Boss Confi II", "iit_ess8_121", 6, 0, 0, 5, 50, 5, 37, 20, 5, 10, 0, 0);
            ShipConfig("npc_cap1", "Boss Confi III", "iit_ess8_121", 6, 0, 0, 10, 100, 10, 75, 10, 50, 15, 0, 0);
            ShipConfig("npc_cap1", "Boss Confi IV", "iit_ess8_121", 6, 0, 0, 20, 150, 20, 100, 20, 75, 20, 0, 0);
            ShipConfig("npc_cap1", "Boss Confin V", "iit_ess8_121", 6, 0, 0, 30, 250, 30, 175, 30, 125, 30, 0, 0);

            return _builder.Build();
        }

        private void ShipConfig(string itemTag, string name, string texture, int requiredLevel, int armor, int shield, int thermalAttack, int thermalDefense, int ionAttack, int ionDefense, int explosiveAttack, int explosiveDefense, int accuracy, int evasion, int industrial)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(name)
                .Texture(texture)
                .Description($"Provides several base stats to a ship to allow it to fulfill a role and operate at full capacity: \n" +
                $"Armor: +{armor} + Module Bonus * 4 \n" +
                $"Shield: +{shield} + Module Bonus * 4 \n" +
                $"Thermal Attack: +{thermalAttack} + Module Bonus * 2 +  \n" +
                $"Thermal Defense: +{thermalDefense} + Module Bonus * 2 \n" +
                $"EM Attack: +{ionAttack} + Module Bonus * 2 \n" +
                $"EM Defense: +{ionDefense} + Module Bonus * 2 \n" +
                $"Explosive Attack: +{explosiveAttack} + Module Bonus * 2 \n" +
                $"Explosive Defense: +{explosiveDefense} + Module Bonus * 2 \n" +
                $"Accuracy: +{accuracy} + Module Bonus * 2 \n" +
                $"Evasion: +{evasion} + Module Bonus * 2 \n" +
                $"Industrial Level: +{industrial} + Module Bonus * 2 (Industrial frames only)")
                .PowerType(ShipModulePowerType.Config)
                .RequirePerk(PerkType.Starships, requiredLevel)
                .EquippedAction((creature, shipStatus, moduleBonus) =>
                {
                    shipStatus.MaxHull +=  armor + moduleBonus * 4;
                    shipStatus.MaxShield += shield + moduleBonus * 4;
                    shipStatus.ThermalDamage += thermalAttack + moduleBonus * 2;
                    shipStatus.ThermalDefense += thermalDefense + moduleBonus * 2;
                    shipStatus.EMDamage += ionAttack + moduleBonus * 2;
                    shipStatus.EMDefense += ionDefense + moduleBonus * 2;
                    shipStatus.ExplosiveDamage += explosiveAttack + moduleBonus * 2;
                    shipStatus.ExplosiveDefense += explosiveDefense + moduleBonus * 2;
                    shipStatus.Accuracy += accuracy + moduleBonus * 2;
                    shipStatus.Evasion += evasion + moduleBonus * 2;
                    if (industrial > 0)
                    {
                        shipStatus.Industrial += industrial + moduleBonus * 2;
                    }
                })
                .UnequippedAction((creature, shipStatus, moduleBonus) =>
                {
                    shipStatus.MaxHull -= armor + moduleBonus * 4;
                    shipStatus.MaxShield -= shield + moduleBonus * 4;
                    shipStatus.ThermalDamage -= thermalAttack + moduleBonus * 2;
                    shipStatus.ThermalDefense -= thermalDefense + moduleBonus * 2;
                    shipStatus.EMDamage -= ionAttack + moduleBonus * 2;
                    shipStatus.EMDefense -= ionDefense + moduleBonus * 2;
                    shipStatus.ExplosiveDamage -= explosiveAttack + moduleBonus * 2;
                    shipStatus.ExplosiveDefense -= explosiveDefense + moduleBonus * 2;
                    shipStatus.Accuracy -= accuracy + moduleBonus * 2;
                    shipStatus.Evasion -= evasion + moduleBonus * 2;
                    if (industrial > 0)
                    {
                        shipStatus.Industrial -= industrial + moduleBonus * 2;
                    }

                    if (shipStatus.Hull > shipStatus.MaxHull)
                        shipStatus.Hull = shipStatus.MaxHull;

                    if (shipStatus.Shield > shipStatus.MaxShield)
                        shipStatus.Shield = shipStatus.MaxShield;
                });
        }
    }
}
