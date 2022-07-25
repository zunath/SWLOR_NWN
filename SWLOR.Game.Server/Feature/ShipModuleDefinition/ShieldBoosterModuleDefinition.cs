using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class ShieldBoosterModuleDefinition: IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new ShipModuleBuilder();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            ShieldBooster("shld_boost_b", "Basic Shield Booster", 5, 1);
            ShieldBooster("shld_boost_1", "Shield Booster I", 8, 2);
            ShieldBooster("shld_boost_2", "Shield Booster II", 11, 3);
            ShieldBooster("shld_boost_3", "Shield Booster III", 14, 4);
            ShieldBooster("shld_boost_4", "Shield Booster IV", 17, 5);

            return _builder.Build();
        }

        private void ShieldBooster(string itemTag, string name, int shieldBoostAmount, int requiredLevel)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(name)
                .Texture("iit_ess_027")
                .Description($"Improves a ship's maximum shields by {shieldBoostAmount}.")
                .PowerType(ShipModulePowerType.Low)
                .RequirePerk(PerkType.DefensiveModules, requiredLevel)
                .EquippedAction((creature, shipStatus, moduleBonus) =>
                {
                    shipStatus.MaxShield += shieldBoostAmount + moduleBonus * 2;
                })
                .UnequippedAction((creature, shipStatus, moduleBonus) =>
                {
                    shipStatus.MaxShield -= shieldBoostAmount + moduleBonus * 2;

                    if (shipStatus.Shield > shipStatus.MaxShield)
                        shipStatus.Shield = shipStatus.MaxShield;
                });
        }
    }
}
