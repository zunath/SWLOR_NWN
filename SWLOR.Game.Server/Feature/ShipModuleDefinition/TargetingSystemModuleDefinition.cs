using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class TargetingSystemModuleDefinition: IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            TargetingSystem("tgt_sys_b", "Basic Targeting System", 2, 1);
            TargetingSystem("tgt_sys_1", "Targeting System I", 4, 2);
            TargetingSystem("tgt_sys_2", "Targeting System II", 6, 3);
            TargetingSystem("tgt_sys_3", "Targeting System III", 8, 4);
            TargetingSystem("tgt_sys_4", "Targeting System IV", 10, 5);

            return _builder.Build();
        }


        private void TargetingSystem(string itemTag, string name, int accuracyBoostAmount, int requiredLevel)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(name)
                .Texture("iit_ess_055")
                .Description($"Improves a ship's accuracy by {accuracyBoostAmount}.")
                .PowerType(ShipModulePowerType.Low)
                .RequirePerk(PerkType.DefensiveModules, requiredLevel)
                .EquippedAction((_, shipStatus, moduleBonus) =>
                {
                    shipStatus.Accuracy += accuracyBoostAmount + moduleBonus;
                })
                .UnequippedAction((_, shipStatus, moduleBonus) =>
                {
                    shipStatus.Accuracy -= accuracyBoostAmount + moduleBonus;
                });
        }

    }
}
