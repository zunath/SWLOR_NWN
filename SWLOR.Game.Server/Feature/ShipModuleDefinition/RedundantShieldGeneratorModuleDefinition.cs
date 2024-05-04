using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class RedundantShieldsModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new ShipModuleBuilder();

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
                .Texture("iit_ess_055")
                .Description($"Improves a ship's max shields by {boostAmount * 3} at the cost of {boostAmount / 5} shield recharge rate.")
                .PowerType(ShipModulePowerType.Low)
                .RequirePerk(PerkType.DefensiveModules, 5)
                .CapitalClassModule()
                .EquippedAction((creature, shipStatus, moduleBonus) =>
                {
                    shipStatus.MaxShield += boostAmount * 3 + moduleBonus;
                    shipStatus.ShieldRechargeRate += boostAmount / 5;
                })
                .UnequippedAction((creature, shipStatus, moduleBonus) =>
                {
                    shipStatus.MaxShield -= boostAmount * 3 + moduleBonus;
                    shipStatus.ShieldRechargeRate -= boostAmount / 5;
                });
        }

    }
}
