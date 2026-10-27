using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SpaceService;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class HypermatterInjectorModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            HypermatterInjector("cap_inject1", "Basic Hypermatter Injector", "Basic Fuel Inj", "Consumes a fuel capsule to restore 8 capacitor.", 1, 8);
            HypermatterInjector("cap_inject2", "Hypermatter Injector I", "Fuel Inj 1", "Consumes a fuel capsule to restore 14 capacitor.", 2, 14);
            HypermatterInjector("cap_inject3", "Hypermatter Injector II", "Fuel Inj 2", "Consumes a fuel capsules to restore 20 capacitor.", 3, 20);
            HypermatterInjector("cap_inject4", "Hypermatter Injector III", "Fuel Inj 3", "Consumes a fuel capsules to restore 26 capacitor.", 4, 26);
            HypermatterInjector("cap_inject5", "Hypermatter Injector IV", "Fuel Inj 4", "Consumes a fuel capsules to restore 32 capacitor.", 5, 32);

            return _builder.Build();
        }

        private void HypermatterInjector(string tag, string name, string shortName, string description, int requiredLevel, int capRestore)
        {
            _builder.Create(tag)
                .Name(name)
                .ShortName(shortName)
                .Description(description)
                .Type(ShipModuleType.HypermatterInjector)
                .CanTargetSelf()
                .ValidTargetType(ObjectType.Creature)
                .PowerType(ShipModulePowerType.High)
                .Texture("iit_smhminject")
                .RequirePerk(PerkType.DefensiveModules, requiredLevel)
                .Recast(60f)
                .OnePress()
                .ValidationAction((activator, _, _, _, _) =>
                    ShipModuleActions.AmmoError(activator, ShipModuleActions.FuelCapsuleResref, "You need a fuel capsule to activate this module."))
                .ActivatedAction((activator, activatorShipStatus, target, targetShipStatus, moduleBonus) =>
                {
                    var recovery = capRestore + (moduleBonus + activatorShipStatus.Industrial) * 2;
                    ShipModuleActions.InjectCapacitor(activator, activatorShipStatus, target, targetShipStatus, recovery);
                });
        }
    }
}
