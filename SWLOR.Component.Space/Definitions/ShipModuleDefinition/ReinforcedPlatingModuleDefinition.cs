using SWLOR.Component.Space.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Space.Enums;
using SWLOR.Shared.Domain.Space.ValueObjects;

namespace SWLOR.Component.Space.Definitions.ShipModuleDefinition
{
    public class ReinforcedPlatingModuleDefinition : IShipModuleListDefinition
    {
        private readonly IShipModuleBuilder _builder;

        public ReinforcedPlatingModuleDefinition(IShipModuleBuilder builder)
        {
            _builder = builder;
        }

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
                .Description($"Improves a ship's defenses to Thermal, EM and Explosive by {boostAmount * 3} at the cost of 50 shields.")
                .PowerType(ShipModulePowerType.Low)
                .RequirePerk(PerkType.DefensiveModules, 5)
                .CapitalClassModule()
                .EquippedAction((shipStatus, moduleBonus) =>
                {
                    shipStatus.ThermalDefense += 3 * (boostAmount + moduleBonus);
                    shipStatus.EMDefense += 3 * (boostAmount + moduleBonus);
                    shipStatus.ExplosiveDefense += 3 * (boostAmount + moduleBonus);
                    shipStatus.MaxShield -= 50;
                })
                .UnequippedAction((shipStatus, moduleBonus) =>
                {
                    shipStatus.ThermalDefense -= 3 * (boostAmount + moduleBonus);
                    shipStatus.EMDefense -= 3 * (boostAmount + moduleBonus);
                    shipStatus.ExplosiveDefense -= 3 * (boostAmount + moduleBonus);
                    shipStatus.MaxShield += 50;
                });
        }

    }
}



