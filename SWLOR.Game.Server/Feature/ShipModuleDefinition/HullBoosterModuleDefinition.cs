using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class HullBoosterModuleDefinition: IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            HullBooster("hull_boost_b", "Basic Hull Booster", 8, 1);
            HullBooster("hull_boost_1", "Hull Booster I", 16, 2);
            HullBooster("hull_boost_2", "Hull Booster II", 24, 3);
            HullBooster("hull_boost_3", "Hull Booster III", 32, 4);
            HullBooster("hull_boost_4", "Hull Booster IV", 40, 5);

            return _builder.Build();
        }

        private void HullBooster(string itemTag, string name, int hullBoostAmount, int requiredLevel)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(name)
                .Texture("iit_ess_026")
                .Description($"Improves a ship's maximum hull by {hullBoostAmount}.")
                .PowerType(ShipModulePowerType.Low)
                .RequirePerk(PerkType.DefensiveModules, requiredLevel)
                .EquippedAction((_, shipStatus, moduleBonus) =>
                {
                    shipStatus.MaxHull += hullBoostAmount + moduleBonus * 2;
                })
                .UnequippedAction((_, shipStatus, moduleBonus) =>
                {
                    shipStatus.MaxHull -= hullBoostAmount + moduleBonus * 2;

                    if (shipStatus.Hull > shipStatus.MaxHull)
                        shipStatus.Hull = shipStatus.MaxHull;
                });
        }
    }
}
