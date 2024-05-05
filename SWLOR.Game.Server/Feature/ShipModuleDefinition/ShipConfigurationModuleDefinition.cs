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
            ShipConfig("config_fig2", "Fighter Configuration II", "iit_ess8_121", 2, 25, 25, 0, 25, 8, 25, 0, 25, 5, 5, 0);
            ShipConfig("config_fig3", "Fighter Configuration III", "iit_ess8_121", 3, 40, 40, 0, 60, 14, 60, 0, 60, 10, 10, 0);
            ShipConfig("config_fig4", "Fighter Configuration IV", "iit_ess8_121", 4, 75, 75, 0, 100, 25, 100, 0, 100, 20, 20, 0);
            ShipConfig("config_fig5", "Fighter Configuration V", "iit_ess8_121", 5, 100, 100, 0, 130, 35, 130, 0, 130, 30, 30, 0);
            ShipConfig("config_int1", "Interceptor Configuration I", "iit_ess8_122", 1, 5, 5, 5, 10, 0, 10, 0, 20, 2, 4, 0);
            ShipConfig("config_int2", "Interceptor Configuration II", "iit_ess8_122", 2, 15, 15, 10, 25, 0, 25, 0, 40, 5, 8, 0);
            ShipConfig("config_int3", "Interceptor Configuration III", "iit_ess8_122", 3, 25, 25, 20, 60, 0, 60, 0, 90, 10, 12, 0);
            ShipConfig("config_int4", "Interceptor Configuration IV", "iit_ess8_122", 4, 50, 50, 35, 100, 0, 100, 0, 120, 20, 25, 0);
            ShipConfig("config_int5", "Interceptor Configuration V", "iit_ess8_122", 5, 70, 70, 50, 130, 0, 130, 0, 150, 30, 50, 0);
            ShipConfig("config_bmb1", "Bomber Configuration I", "iit_ess8_123", 1, 10, 10, 0, 20, 0, 10, 5, 7, 4, 0 , 0);
            ShipConfig("config_bmb2", "Bomber Configuration II", "iit_ess8_123", 2, 25, 25, 0, 40, 0, 25, 10, 15, 8, 0, 0);
            ShipConfig("config_bmb3", "Bomber Configuration III", "iit_ess8_123", 3, 40, 40, 0, 90, 0, 60, 20, 40, 12, 5, 0);
            ShipConfig("config_bmb4", "Bomber Configuration IV", "iit_ess8_123", 4, 75, 75, 0, 120, 0, 100, 35, 75, 25, 10, 0);
            ShipConfig("config_bmb5", "Bomber Configuration V", "iit_ess8_123", 5, 100, 100, 0, 150, 0, 130, 50, 100, 40, 20, 0);
            ShipConfig("config_ind1", "Industrial Configuration I", "iit_ess8_124", 1, 20, 20, 0, 7, 0, 7, 0, 7, 0, 0, 2);
            ShipConfig("config_ind2", "Industrial Configuration II", "iit_ess8_124", 2, 50, 50, 0, 15, 0, 15, 0, 15, 0, 0, 4);
            ShipConfig("config_ind3", "Industrial Configuration III", "iit_ess8_124", 3, 80, 80, 0, 40, 0, 40, 0, 40, 0, 0, 6);
            ShipConfig("config_ind4", "Industrial Configuration IV", "iit_ess8_124", 4, 150, 150, 0, 75, 0, 75, 0, 75, 0, 0, 8);
            ShipConfig("config_ind5", "Industrial Configuration V", "iit_ess8_124", 5, 300, 300, 0, 100, 0, 100, 0, 100, 0, 0, 10);

            ShipConfig("con_fig1", "NPC Fig Conf 1", "iit_ess8_121", 1, 0, 0, 0, 10, 0, 10, 0, 10, 2, 2, 0);
            ShipConfig("con_fig2", "NPC Fig Conf 2", "iit_ess8_121", 1, 0, 0, 0, 25, 0, 25, 0, 25, 5, 2, 0);
            ShipConfig("con_fig3", "NPC Fig Conf 3", "iit_ess8_121", 1, 0, 0, 0, 40, 0, 40, 0, 40, 10, 2, 0);
            ShipConfig("con_fig4", "NPC Fig Conf 4", "iit_ess8_121", 1, 0, 0, 0, 75, 0, 75, 0, 75, 20, 2, 0);
            ShipConfig("con_fig5", "NPC Fig Conf 5", "iit_ess8_121", 1, 0, 0, 0, 100, 0, 100, 0, 100, 30, 30, 0);
            ShipConfig("con_fig6", "NPC Fig Conf 6", "iit_ess8_121", 1, 0, 0, 0, 125, 0, 125, 0, 125, 30, 30, 0);

            ShipConfig("con_int1", "NPC Int Conf 1", "iit_ess8_121", 1, 0, 0, 0, 8, 0, 10, 0, 20, 2, 2, 0);
            ShipConfig("con_int2", "NPC Int Conf 2", "iit_ess8_121", 1, 0, 0, 0, 20, 0, 25, 0, 35, 5, 2, 0);
            ShipConfig("con_int3", "NPC Int Conf 3", "iit_ess8_121", 1, 0, 0, 0, 35, 0, 40, 0, 65, 10, 2, 0);
            ShipConfig("con_int4", "NPC Int Conf 4", "iit_ess8_121", 1, 0, 0, 0, 65, 0, 75, 0, 90, 20, 2, 0);
            ShipConfig("con_int5", "NPC Int Conf 5", "iit_ess8_121", 1, 0, 0, 0, 90, 0, 100, 0, 110, 30, 30, 0);
            ShipConfig("con_int6", "NPC Int Conf 6", "iit_ess8_121", 1, 0, 0, 0, 110, 0, 125, 0, 160, 30, 30, 0);

            ShipConfig("con_bmb1", "NPC Bmb Conf 1", "iit_ess8_121", 1, 0, 0, 0, 20, 0, 10, 5, 8, 2, 2, 0);
            ShipConfig("con_bmb2", "NPC Bmb Conf 2", "iit_ess8_121", 1, 0, 0, 0, 35, 0, 25, 10, 20, 5, 2, 0);
            ShipConfig("con_bmb3", "NPC Bmb Conf 3", "iit_ess8_121", 1, 0, 0, 0, 65, 0, 40, 15, 35, 10, 2, 0);
            ShipConfig("con_bmb4", "NPC Bmb Conf 4", "iit_ess8_121", 1, 0, 0, 0, 90, 0, 75, 20, 65, 20, 2, 0);
            ShipConfig("con_bmb5", "NPC Bmb Conf 5", "iit_ess8_121", 1, 0, 0, 0, 110, 0, 100, 25, 90, 30, 30, 0);
            ShipConfig("con_bmb6", "NPC Bmb Conf 6", "iit_ess8_121", 1, 0, 0, 0, 160, 0, 125, 30, 110, 30, 30, 0);

            ShipConfig("con_hvy1", "NPC Hvy Conf 1", "iit_ess8_121", 1, 0, 0, 0, 15, 0, 15, 0, 8, 2, 2, 0);
            ShipConfig("con_hvy2", "NPC Hvy Conf 2", "iit_ess8_121", 1, 0, 0, 0, 30, 0, 30, 0, 20, 5, 2, 0);
            ShipConfig("con_hvy3", "NPC Hvy Conf 3", "iit_ess8_121", 1, 0, 0, 0, 45, 0, 45, 0, 35, 10, 2, 0);
            ShipConfig("con_hvy4", "NPC Hvy Conf 4", "iit_ess8_121", 1, 0, 0, 0, 80, 0, 80, 0, 65, 20, 2, 0);
            ShipConfig("con_hvy5", "NPC Hvy Conf 5", "iit_ess8_121", 1, 0, 0, 0, 110, 0, 110, 0, 90, 30, 30, 0);
            ShipConfig("con_hvy6", "NPC Hvy Conf 6", "iit_ess8_121", 1, 0, 0, 0, 140, 0, 140, 0, 110, 30, 30, 0);

            CapShipConfig("cap_indus", "Industrial Nexus Configuration", "iit_ess8_124", 5, 500, 0, 0, 100, 0, 100, 0, 100, 0, 0, 10, 0);
            CapShipConfig("cap_skirm", "Skirmisher Configuration", "iit_ess8_121", 5, 100, 250, 0, 75, 0, 75, 0, 75, 0, 0, 0, -10);
            CapShipConfig("cap_warship", "Warship Configuration", "iit_ess8_123", 5, 300, 300, 0, 100, 0, 100, 0, 100, 0, 0, 0, 0);

            CapShipConfig("npc_cap1", "Boss Conf 1", "iit_ess8_121", 1, 0, 0, 0, 20, 0, 15, 10, 10, 5, 5, 0, 0);
            CapShipConfig("npc_cap2", "Boss Conf 2", "iit_ess8_121", 1, 0, 0, 5, 50, 5, 37, 20, 5, 10, 0, 0, 0);
            CapShipConfig("npc_cap3", "Boss Conf 3", "iit_ess8_121", 1, 0, 0, 10, 100, 10, 75, 10, 50, 15, 0, 0, 0);
            CapShipConfig("npc_cap4", "Boss Conf 4", "iit_ess8_121", 1, 0, 0, 20, 150, 20, 100, 20, 75, 20, 0, 0, 0);
            CapShipConfig("npc_cap5", "Boss Conf 5", "iit_ess8_121", 1, 0, 0, 30, 250, 30, 175, 30, 125, 30, 0, 0, 0);

            CapShipConfig("con_cap1", "NPC Cap Conf 1", "iit_ess8_121", 1, 0, 0, 30, 250, 30, 175, 30, 125, 30, 0, 0, 0);
            CapShipConfig("con_cap2", "NPC Cap Conf 2", "iit_ess8_121", 1, 0, 0, 30, 275, 30, 190, 30, 140, 30, 0, 0, 0);
            CapShipConfig("con_cap3", "NPC Cap Conf 3", "iit_ess8_121", 1, 0, 0, 30, 300, 30, 205, 30, 155, 30, 0, 0, 0);
            CapShipConfig("con_cap4", "NPC Cap Conf 4", "iit_ess8_121", 1, 0, 0, 30, 325, 30, 220, 30, 170, 30, 0, 0, 0);
            CapShipConfig("con_cap5", "NPC Cap Conf 5", "iit_ess8_121", 1, 0, 0, 30, 350, 30, 235, 30, 195, 30, 0, 0, 0);
            CapShipConfig("con_cap6", "NPC Cap Conf 6", "iit_ess8_121", 1, 0, 0, 30, 375, 30, 250, 30, 210, 30, 0, 0, 0);
            CapShipConfig("con_cap7", "NPC Cap Conf 7", "iit_ess8_121", 1, 0, 0, 30, 400, 30, 275, 30, 225, 30, 0, 0, 0);

            return _builder.Build();
        }

        private void ShipConfig(string itemTag,
            string name,
            string texture,
            int requiredLevel,
            int armor,
            int shield,
            int thermalAttack,
            int thermalDefense,
            int ionAttack,
            int ionDefense,
            int explosiveAttack,
            int explosiveDefense,
            int accuracy,
            int evasion,
            int industrial)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(name)
                .Texture(texture)
                .Description($"Provides several base stats to a ship to allow it to fulfill a role and operate at full capacity: \n" +
                $"Armor: +{armor} + Module Bonus x 4 \n" +
                $"Shield: +{shield} + Module Bonus * 4 \n" +
                $"Thermal Attack: +{thermalAttack} + Module Bonus x 2 +  \n" +
                $"Thermal Defense: +{thermalDefense} + Module Bonus x 2 \n" +
                $"EM Attack: +{ionAttack} + Module Bonus x 2 \n" +
                $"EM Defense: +{ionDefense} + Module Bonus x 2 \n" +
                $"Explosive Attack: +{explosiveAttack} + Module Bonus x 2 \n" +
                $"Explosive Defense: +{explosiveDefense} + Module Bonus x 2 \n" +
                $"Accuracy: +{accuracy} + Module Bonus x 2 \n" +
                $"Evasion: +{evasion} + Module Bonus x 2 \n" +
                $"Industrial Level: +{industrial} + Module Bonus x 2 (Industrial frames only)")
                .PowerType(ShipModulePowerType.Config)
                .RequirePerk(PerkType.Starships, requiredLevel)
                .EquippedAction((creature, shipStatus, moduleBonus) =>
                {
                    shipStatus.MaxHull += armor + moduleBonus * 4;
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
                        shipStatus.Industrial += industrial + moduleBonus / 2;
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
                        shipStatus.Industrial -= industrial + moduleBonus / 2;
                    }

                    if (shipStatus.Hull > shipStatus.MaxHull)
                        shipStatus.Hull = shipStatus.MaxHull;

                    if (shipStatus.Shield > shipStatus.MaxShield)
                        shipStatus.Shield = shipStatus.MaxShield;
                });
        }

        private void CapShipConfig(string itemTag,
            string name,
            string texture,
            int requiredLevel,
            int armor,
            int shield,
            int thermalAttack,
            int thermalDefense,
            int ionAttack,
            int ionDefense,
            int explosiveAttack,
            int explosiveDefense,
            int accuracy,
            int evasion,
            int industrial,
            int shieldRecharge)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(name)
                .Texture(texture)
                .Description($"Provides several base stats to a ship to allow it to fulfill a role and operate at full capacity: \n" +
                $"Armor: +{armor} + Module Bonus x 4 \n" +
                $"Shield: +{shield} + Module Bonus * 4 \n" +
                $"Thermal Attack: +{thermalAttack} + Module Bonus x 2 +  \n" +
                $"Thermal Defense: +{thermalDefense} + Module Bonus x 2 \n" +
                $"EM Attack: +{ionAttack} + Module Bonus x 2 \n" +
                $"EM Defense: +{ionDefense} + Module Bonus x 2 \n" +
                $"Explosive Attack: +{explosiveAttack} + Module Bonus x 2 \n" +
                $"Explosive Defense: +{explosiveDefense} + Module Bonus x 2 \n" +
                $"Accuracy: +{accuracy} + Module Bonus x 2 \n" +
                $"Evasion: +{evasion} + Module Bonus x 2 \n" +
                $"Industrial Level: +{industrial} + Module Bonus x 2 (Industrial frames only) \n" +
                $"Shield Recharge Rate Adjustment: -{shieldRecharge} - Module Bonus / 2 seconds per point adjustment.")
                .PowerType(ShipModulePowerType.Config)
                .RequirePerk(PerkType.Starships, requiredLevel)
                .CapitalClassModule()
                .EquippedAction((creature, shipStatus, moduleBonus) =>
                {
                    shipStatus.MaxHull += armor + moduleBonus * 4;
                    shipStatus.MaxShield += shield + moduleBonus * 4;
                    shipStatus.ThermalDamage += thermalAttack + moduleBonus * 2;
                    shipStatus.ThermalDefense += thermalDefense + moduleBonus * 2;
                    shipStatus.EMDamage += ionAttack + moduleBonus * 2;
                    shipStatus.EMDefense += ionDefense + moduleBonus * 2;
                    shipStatus.ExplosiveDamage += explosiveAttack + moduleBonus * 2;
                    shipStatus.ExplosiveDefense += explosiveDefense + moduleBonus * 2;
                    shipStatus.Accuracy += accuracy + moduleBonus * 2;
                    shipStatus.Evasion += evasion + moduleBonus * 2;
                    shipStatus.ShieldRechargeRate += shieldRecharge - moduleBonus / 2;
                    if (industrial > 0)
                    {
                        shipStatus.Industrial += industrial + moduleBonus / 2;
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
                    shipStatus.ShieldRechargeRate -= shieldRecharge - moduleBonus / 2;
                    if (industrial > 0)
                    {
                        shipStatus.Industrial -= industrial + moduleBonus / 2;
                    }

                    if (shipStatus.Hull > shipStatus.MaxHull)
                        shipStatus.Hull = shipStatus.MaxHull;

                    if (shipStatus.Shield > shipStatus.MaxShield)
                        shipStatus.Shield = shipStatus.MaxShield;
                });
        }
    }
}
