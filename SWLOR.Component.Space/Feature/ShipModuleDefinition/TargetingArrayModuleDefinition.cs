using SWLOR.Component.Space.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Space.Enums;

namespace SWLOR.Component.Space.Feature.ShipModuleDefinition
{
    public class TargetingArrayModuleDefinition : IShipModuleListDefinition
    {
        private readonly IShipModuleBuilder _builder;

        public TargetingArrayModuleDefinition(IShipModuleBuilder builder)
        {
            _builder = builder;
        }

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
                .Description($"A suite of dedicated passive sensors for your ship's weapons systems. They improve your accuracy by {boostAmount}, but passive sensor tech reduces your Attack by five.")
                .PowerType(ShipModulePowerType.Low)
                .RequirePerk(PerkType.DefensiveModules, 5)
                .CapitalClassModule()
                .EquippedAction((shipStatus, moduleBonus) =>
                {
                    shipStatus.Accuracy += boostAmount + moduleBonus;
                    shipStatus.ThermalDamage -= 5;
                    shipStatus.EMDamage -= 5;
                    shipStatus.ExplosiveDamage -= 5;
                })
                .UnequippedAction((shipStatus, moduleBonus) =>
                {
                    shipStatus.Accuracy -= boostAmount + moduleBonus;
                    shipStatus.ThermalDamage += 5;
                    shipStatus.EMDamage += 5;
                    shipStatus.ExplosiveDamage += 5;
                });
        }
    }
}
