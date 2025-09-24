using SWLOR.Component.Space.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Space.Enums;
using SWLOR.Shared.Domain.Space.ValueObjects;

namespace SWLOR.Component.Space.Feature.ShipModuleDefinition
{
    public class RedundantShieldsModuleDefinition : IShipModuleListDefinition
    {
        private readonly IShipModuleBuilder _builder;

        public RedundantShieldsModuleDefinition(IShipModuleBuilder builder)
        {
            _builder = builder;
        }

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            RedundantShields("cap_shields1", "Redundant Shield Generator", "Redun. Shlds.", 50);

            return _builder.Build();
        }

        private void RedundantShields(string itemTag,
            string name,
            string shortName,
            int boostAmount)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Texture("iit_ess2_242")
                .Description($"Improves a ship's max shields by {boostAmount * 3} at the cost of {boostAmount / 5} shield recharge rate.")
                .PowerType(ShipModulePowerType.Low)
                .RequirePerk(PerkType.DefensiveModules, 5)
                .CapitalClassModule()
                .EquippedAction((shipStatus, moduleBonus) =>
                {
                    shipStatus.MaxShield += 3 * (boostAmount + moduleBonus);
                    shipStatus.ShieldRechargeRate += boostAmount / 5;
                })
                .UnequippedAction((shipStatus, moduleBonus) =>
                {
                    shipStatus.MaxShield -= 3 * (boostAmount + moduleBonus);
                    shipStatus.ShieldRechargeRate -= boostAmount / 5;
                });
        }

    }
}
