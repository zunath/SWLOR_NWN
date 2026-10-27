using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class MiningLaserModuleDefinition: IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            MiningLaser("min_laser_b", "Basic Mining Laser", "B. Min. Laser", 1, 5, 6f);
            MiningLaser("min_laser_1", "Mining Laser I", "Min. Laser I", 2, 7, 7f);
            MiningLaser("min_laser_2", "Mining Laser II", "Min. Laser II", 3, 9, 8f);
            MiningLaser("min_laser_3", "Mining Laser III", "Min. Laser III", 4, 11, 9f);
            MiningLaser("min_laser_4", "Mining Laser IV", "Min. Laser IV", 5, 13, 10f);

            return _builder.Build();
        }

        private void MiningLaser(string itemTag, string name, string shortName, int requiredLevel, int capacitor, float recast)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Texture("iit_smminelaser")
                .Type(ShipModuleType.MiningLaser)
                .MaxDistance(20f)
                .ValidTargetType(ObjectType.Placeable)
                .Description($"Mines targets up to tier {requiredLevel}.")
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.MiningModules, requiredLevel)
                .Capacitor(capacitor)
                .Toggle(recast)
                .Recast((activator, _, _) => ShipModuleActions.MiningCycleSeconds(activator, recast))
                .ValidationAction((activator, _, target, _, _) => ShipModuleActions.MiningLaserError(activator, target, requiredLevel))
                .ActivatedAction((activator, _, target, _, moduleBonus) =>
                {
                    ShipModuleActions.MineAsteroid(activator, target, recast, moduleBonus);
                });
        }
    }
}
