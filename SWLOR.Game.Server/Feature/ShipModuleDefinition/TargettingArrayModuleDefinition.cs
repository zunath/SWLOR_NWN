using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class TargettingArrayModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new ShipModuleBuilder();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            TargettingArray("cap_target1", "Dedicated Targetting Sensor Array", "Target Array", 20);

            return _builder.Build();
        }

        private void TargettingArray(string itemTag,
            string name,
            string shortName,
            int boostAmount)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Texture("iit_ess_247")
                .Description($"A suite of dedicated active sensors for your ship's weapons systems. They improve your accuracy by {boostAmount} and attack by {boostAmount}, but active sensor tech leaves you more visible to enemies, reducing evasion by {boostAmount}.")
                .PowerType(ShipModulePowerType.Low)
                .RequirePerk(PerkType.DefensiveModules, 5)
                .CapitalClassModule()
                .EquippedAction((creature, shipStatus, moduleBonus) =>
                {
                    shipStatus.Accuracy += boostAmount + moduleBonus;
                    shipStatus.ThermalDamage += boostAmount + moduleBonus;
                    shipStatus.EMDamage += boostAmount + moduleBonus;
                    shipStatus.ExplosiveDamage += boostAmount + moduleBonus;
                    shipStatus.Evasion -= boostAmount;
                })
                .UnequippedAction((creature, shipStatus, moduleBonus) =>
                {
                    shipStatus.Accuracy -= boostAmount + moduleBonus;
                    shipStatus.ThermalDamage -= boostAmount + moduleBonus;
                    shipStatus.EMDamage -= boostAmount + moduleBonus;
                    shipStatus.ExplosiveDamage -= boostAmount + moduleBonus;
                    shipStatus.Evasion += boostAmount;
                });
        }
    }
}