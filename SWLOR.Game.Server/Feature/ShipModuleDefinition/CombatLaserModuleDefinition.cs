using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class CombatLaserModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            CombatLaser("com_laser_b", "Basic Combat Laser", "B. Cmbt Laser", "Deals 12 thermal DMG to your target.", 1, 12, 2);
            CombatLaser("com_laser_1", "Combat Laser I", "Cmbt Laser I", "Deals 18 thermal DMG to your target.", 2, 18, 3);
            CombatLaser("com_laser_2", "Combat Laser II", "Cmbt Laser II", "Deals 24 thermal DMG to your target.", 3, 24, 4);
            CombatLaser("com_laser_3", "Combat Laser III", "Cmbt Laser III", "Deals 30 thermal DMG to your target.", 4, 30, 5);
            CombatLaser("com_laser_4", "Combat Laser IV", "Cmbt Laser IV", "Deals 36 thermal DMG to your target.", 5, 36, 6);

            return _builder.Build();
        }

        private void CombatLaser(
            string itemTag,
            string name,
            string shortName,
            string description,
            int requiredLevel,
            int dmg,
            int capacitor)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Type(ShipModuleType.CombatLaser)
                .Texture("iit_smcmblaser")
                .Description(description)
                .MaxDistance(30f)
                .ValidTargetType(ObjectType.Creature)
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.OffensiveModules, requiredLevel)
                .Toggle(6f)
                .Capacitor(capacitor)
                .ActivatedAction((activator, activatorShipStatus, target, targetShipStatus, moduleBonus) =>
                {
                    var rank = (requiredLevel - 1) * 10;
                    var shot = new SpaceWeaponShot
                    {
                        Family = SpaceWeaponFamily.Energy,
                        DamageType = CombatDamageType.Thermal,
                        Dmg = dmg + moduleBonus / 3,
                        Rank = rank,
                        Attack = rank * 2,
                        Accuracy = rank * 2
                    };
                    ShipModuleActions.FireBolt(activator, activatorShipStatus, target, targetShipStatus, shot);
                });
        }
    }
}
