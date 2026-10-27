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
    public class IonCannonModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            IonCannon("ion_cann_b", "Basic Ion Cannon", "B. Ion Cann.", "Deals 15 EM DMG to your target. Deals reduced damage to unshielded targets, but imposes debuffs.", 1, 6, 15);
            IonCannon("ion_cann_1", "Ion Cannon I", "Ion Cann. I", "Deals 30 EM DMG to your target. Deals reduced damage to unshielded targets, but imposes debuffs.", 2, 8, 30);
            IonCannon("ion_cann_2", "Ion Cannon II", "Ion Cann. II", "Deals 45 EM DMG to your target. Deals reduced damage to unshielded targets, but imposes debuffs.", 3, 10, 45);
            IonCannon("ion_cann_3", "Ion Cannon III", "Ion Cann. III", "Deals 60 EM DMG to your target. Deals reduced damage to unshielded targets, but imposes debuffs.", 4, 12, 60);
            IonCannon("ion_cann_4", "Ion Cannon IV", "Ion Cann. IV", "Deals 75 EM DMG to your target. Deals reduced damage to unshielded targets, but imposes debuffs.", 5, 14, 75);

            return _builder.Build();
        }

        private void IonCannon(
            string itemTag,
            string name,
            string shortName,
            string description,
            int requiredLevel,
            int capacitor,
            int dmg)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Type(ShipModuleType.IonCannon)
                .Texture("iit_smioncann")
                .Description(description)
                .MaxDistance(40f)
                .ValidTargetType(ObjectType.Creature)
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.OffensiveModules, requiredLevel)
                .Toggle(10f)
                .Capacitor(capacitor)
                .ActivatedAction((activator, activatorShipStatus, target, targetShipStatus, moduleBonus) =>
                {
                    var rank = (requiredLevel - 1) * 10;
                    var shot = new SpaceWeaponShot
                    {
                        Family = SpaceWeaponFamily.Energy,
                        DamageType = CombatDamageType.EM,
                        Dmg = dmg + moduleBonus / 2,
                        Rank = rank,
                        Attack = rank * 2,
                        Accuracy = rank * 2,
                        ShieldDamageMultiplier = 1.5
                    };
                    ShipModuleActions.FireIon(activator, activatorShipStatus, target, targetShipStatus, shot, 50, 2);
                });
        }
    }
}
