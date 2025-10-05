using SWLOR.Component.Space.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Space.Enums;
using SWLOR.Shared.Domain.Space.ValueObjects;

namespace SWLOR.Component.Space.Definitions.ShipModuleDefinition
{
    public class TargetingSystemModuleDefinition: IShipModuleListDefinition
    {
        private readonly IShipModuleBuilder _builder;

        public TargetingSystemModuleDefinition(IShipModuleBuilder builder)
        {
            _builder = builder;
        }

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
                .EquippedAction((shipStatus, moduleBonus) =>
                {
                    shipStatus.Accuracy += accuracyBoostAmount + moduleBonus;
                })
                .UnequippedAction((shipStatus, moduleBonus) =>
                {
                    shipStatus.Accuracy -= accuracyBoostAmount + moduleBonus;
                });
        }

    }
}
