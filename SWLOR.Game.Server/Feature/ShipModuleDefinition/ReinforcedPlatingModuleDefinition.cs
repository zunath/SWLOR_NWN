using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class ReinforcedPlatingModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            ReinforcedPlating("cap_armor1", "Reinforced Durasteel Plating", "Rein Plating", 15);

            return _builder.Build();
        }

        private void ReinforcedPlating(string itemTag,
            string name,
            string shortName,
            int boostAmount)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Texture("iit_ess2_114")
                .Description($"Improves a ship's defenses to Thermal, EM and Explosive by {boostAmount * 3} at the cost of {boostAmount} evasion.")
                .PowerType(ShipModulePowerType.Low)
                .RequirePerk(PerkType.DefensiveModules, 5)
                .CapitalClassModule()
                .EquippedAction((shipStatus, moduleBonus) =>
                {
                    shipStatus.ThermalDefense += 3 * (boostAmount + moduleBonus);
                    shipStatus.EMDefense += 3 * (boostAmount + moduleBonus);
                    shipStatus.ExplosiveDefense += 3 * (boostAmount + moduleBonus);
                    shipStatus.Evasion -= boostAmount;
                })
                .UnequippedAction((shipStatus, moduleBonus) =>
                {
                    shipStatus.ThermalDefense -= 3 * (boostAmount + moduleBonus);
                    shipStatus.EMDefense -= 3 * (boostAmount + moduleBonus);
                    shipStatus.ExplosiveDefense -= 3 * (boostAmount + moduleBonus);
                    shipStatus.Evasion += boostAmount;
                });
        }

    }
}
