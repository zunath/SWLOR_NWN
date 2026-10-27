using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class StripMinerModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            StripMiner("cap_minlas", "Strip Miner", "Strip Miner");

            return _builder.Build();
        }

        private void StripMiner(string itemTag, string name, string shortName)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Texture("iit_smstripminer")
                .Type(ShipModuleType.StripMiner)
                .MaxDistance(10f)
                .ValidTargetType(ObjectType.Placeable)
                .Description("Mines a target asteroid over 18 seconds, retrieving an extremely large number of ores up to the asteroid's tier. Your ship is immobile while this happens.")
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.MiningModules, 5)
                .Capacitor(30)
                .Toggle(18)
                .CapitalClassModule()
                .ValidationAction((_, _, target, _, _) => ShipModuleActions.StripMinerError(target, null))
                .ActivatedAction((activator, _, target, _, moduleBonus) =>
                {
                    ShipModuleActions.StripAsteroid(activator, target, 18f, moduleBonus);
                });
        }
    }
}
