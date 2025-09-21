using System.Collections.Generic;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class WeaponsComputerModuleDefinition : IShipModuleListDefinition
    {
        private readonly IShipModuleBuilder _builder;

        public WeaponsComputerModuleDefinition(IShipModuleBuilder builder)
        {
            _builder = builder;
        }

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            WeaponsComputer("cap_wcomp1", "Dedicated Weapons System Computer", "Weapons Computer", 20);

            return _builder.Build();
        }

        private void WeaponsComputer(string itemTag,
            string name,
            string shortName,
            int boostAmount)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Texture("iit_ess_252")
                .Description("This weapons computer increases all Attack by 20 but reduces Accuracy by 10.")
                .PowerType(ShipModulePowerType.Low)
                .RequirePerk(PerkType.DefensiveModules, 5)
                .CapitalClassModule()
                .EquippedAction((shipStatus, moduleBonus) =>
                {
                    shipStatus.ThermalDamage += boostAmount + moduleBonus;
                    shipStatus.EMDamage += boostAmount + moduleBonus;
                    shipStatus.ExplosiveDamage += boostAmount + moduleBonus;
                    shipStatus.Accuracy -= 10;
                })
                .UnequippedAction((shipStatus, moduleBonus) =>
                {

                    shipStatus.ThermalDamage -= boostAmount + moduleBonus;
                    shipStatus.EMDamage -= boostAmount + moduleBonus;
                    shipStatus.ExplosiveDamage -= boostAmount + moduleBonus;
                    shipStatus.Accuracy += 10;
                });
        }
    }
}
