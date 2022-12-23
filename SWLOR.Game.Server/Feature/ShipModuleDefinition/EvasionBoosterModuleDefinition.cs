using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class EvasionBoosterModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new ShipModuleBuilder();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            AgilityBooster("eva_boost_b", "Basic Evasion Booster", 2, 1);
            AgilityBooster("eva_boost_1", "Evasion Booster I", 4, 2);
            AgilityBooster("eva_boost_2", "Evasion Booster II", 6, 3);
            AgilityBooster("eva_boost_3", "Evasion Booster III", 8, 4);
            AgilityBooster("eva_boost_4", "Evasion Booster IV", 10, 5);

            return _builder.Build();
        }


        private void AgilityBooster(string itemTag, string name, int evasionBoostAmount, int requiredLevel)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(name)
                .Texture("iit_ess_018")
                .Description($"Improves a ship's evasion by {evasionBoostAmount}.")
                .PowerType(ShipModulePowerType.Low)
                .RequirePerk(PerkType.DefensiveModules, requiredLevel)
                .EquippedAction((creature, shipStatus, moduleBonus) =>
                {
                    shipStatus.Evasion += evasionBoostAmount + moduleBonus;
                })
                .UnequippedAction((creature, shipStatus, moduleBonus) =>
                {
                    shipStatus.Evasion -= evasionBoostAmount + moduleBonus;
                });
        }

    }
}