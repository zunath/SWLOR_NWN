using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class TestShipModuleDefinition: IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new ShipModuleBuilder();

        public Dictionary<ShipModuleType, ShipModuleDetail> BuildShipModules()
        {
            _builder.Create(ShipModuleType.TestModule)
                .Name("Test Module")
                .ShortName("Tst Mod")
                .IsActiveModule()
                .PowerType(ShipModulePowerType.High)
                .Capacitor(5)
                .Recast(3f)
                .EquippedAction((player, item, ship) =>
                {
                })
                .UnequippedAction((player, item, ship) =>
                {
                })
                .ActivatedAction((player, item, ship) =>
                {
                });

            return _builder.Build();
        }
    }
}
