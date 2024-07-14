using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class TargetingArrayModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            TargetingArray("cap_target1", "Dedicated Targeting Sensor Array", "Target Array", 20);

            return _builder.Build();
        }

        private void TargetingArray(string itemTag,
            string name,
            string shortName,
            int boostAmount)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Texture("iit_ess_247")
                .Description($"A suite of dedicated active sensors for your ship's weapons systems. They improve your accuracy by {boostAmount} and attack by {boostAmount * 2}, but active sensor tech leaves you more visible to enemies, reducing evasion by {boostAmount}.")
                .PowerType(ShipModulePowerType.Low)
                .RequirePerk(PerkType.DefensiveModules, 5)
                .CapitalClassModule()
                .EquippedAction((shipStatus, moduleBonus) =>
                {
                    shipStatus.Accuracy += boostAmount + moduleBonus;
                    shipStatus.ThermalDamage += 2 * (boostAmount + moduleBonus);
                    shipStatus.EMDamage += 2 * (boostAmount + moduleBonus);
                    shipStatus.ExplosiveDamage += 2 * (boostAmount + moduleBonus);
                    shipStatus.Evasion -= boostAmount;
                })
                .UnequippedAction((shipStatus, moduleBonus) =>
                {
                    shipStatus.Accuracy -= boostAmount + moduleBonus;
                    shipStatus.ThermalDamage -= 2 * (boostAmount + moduleBonus);
                    shipStatus.EMDamage -= 2 * (boostAmount + moduleBonus);
                    shipStatus.ExplosiveDamage -= 2 * (boostAmount + moduleBonus);
                    shipStatus.Evasion += boostAmount;
                });
        }
    }
}