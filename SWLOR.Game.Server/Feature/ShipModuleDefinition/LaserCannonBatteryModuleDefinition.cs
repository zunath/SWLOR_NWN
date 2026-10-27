using SWLOR.Game.Server.Core;
 using System.Collections.Generic;
 using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
 using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class LaserCannonBatteryModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            LaserCannonBattery("lasbattery1", "Laser Cannon Battery I", "Las Cann Bat 1", "Fires at 75% of nearby targets for 30 DMG. Fires once every 2 seconds for 18 seconds.", 30);

            LaserCannonBattery("npcautolas1", "NPC Laser Cannon Battery 1", "npcautolas1", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 12);
            LaserCannonBattery("npcautolas2", "NPC Laser Cannon Battery 2", "npcautolas2", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 17);
            LaserCannonBattery("npcautolas3", "NPC Laser Cannon Battery 3", "npcautolas3", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 22);
            LaserCannonBattery("npcautolas4", "NPC Laser Cannon Battery 4", "npcautolas4", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 25);
            LaserCannonBattery("npcautolas5", "NPC Laser Cannon Battery 5", "npcautolas5", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 30);
            LaserCannonBattery("npcautolas6", "NPC Laser Cannon Battery 6", "npcautolas6", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 33);
            LaserCannonBattery("npcautolas7", "NPC Laser Cannon Battery 7", "npcautolas7", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 36);
            LaserCannonBattery("npcautolas8", "NPC Laser Cannon Battery 8", "npcautolas8", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 39);
            LaserCannonBattery("npcautolas9", "NPC Laser Cannon Battery 9", "npcautolas9", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 41);
            LaserCannonBattery("npcautolas10", "NPC Laser Cannon Battery 10", "npcautolas10", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 43);
            LaserCannonBattery("npcautolas11", "NPC Laser Cannon Battery 11", "npcautolas11", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 45);
            LaserCannonBattery("npcautolas12", "NPC Laser Cannon Battery 12", "npcautolas12", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 50);

            return _builder.Build();
        }

        private void LaserCannonBattery(
            string itemTag,
            string name,
            string shortName,
            string description,
            int dmg)
        {
        _builder.Create(itemTag)
            .Name(name)
            .ShortName(shortName)
            .Type(ShipModuleType.LaserBattery)
            .Texture("iit_smlasbattery")
            .Description(description)
            .ValidTargetType(ObjectType.Creature)
            .MaxDistance(20f)
            .PowerType(ShipModulePowerType.High)
            .RequirePerk(PerkType.OffensiveModules, 5)
            .Recast(18f)
            .Capacitor(15)
            .CapitalClassModule()
            .CanTargetSelf()
            .ActivatedAction((activator, activatorShipStatus, target, _, moduleBonus) =>
            {
                // The player battery is capital-class (rank 40). NPC variants stay at rank 0 so they keep the
                // level-scaled preset until the space enemy presets replace it.
                var rank = itemTag.StartsWith("npc") ? 0 : 40;
                var shot = new SpaceWeaponShot
                {
                    Family = SpaceWeaponFamily.Energy,
                    DamageType = CombatDamageType.Thermal,
                    Dmg = dmg + moduleBonus,
                    Rank = rank,
                    Attack = rank * 2,
                    Accuracy = rank * 2,
                    Size = ShipSize.Capital
                };
                var missile = EffectVisualEffect(VisualEffect.Mirv_StarWars_Bolt2);

                for (var i = 0; i < 9; i++)
                {
                    var delay = i * 2f;
                    DelayCommand(delay, () =>
                    {
                        if (Space.CanShipFire(activator))
                        {
                            foreach (var nearbyTarget in ObjectSnapshot.InShape(Shape.Sphere, 20f, GetLocation(activator), true, ObjectType.Creature))
                            {
                                if (!GetIsObjectValid(nearbyTarget))
                                    continue;

                                if (nearbyTarget != activator &&
                                    Random.D4(1) != 1 &&
                                    GetIsEnemy(nearbyTarget, activator) &&
                                    Space.GetShipStatus(nearbyTarget) != null)
                                {
                                    var nearbyShipStatus = Space.GetShipStatus(nearbyTarget);
                                    ApplyEffectToObject(DurationType.Instant, missile, nearbyTarget);
                                    Space.ResolveWeaponShot(activator, activatorShipStatus, nearbyTarget, nearbyShipStatus, shot);
                                }
                            }
                        }
                    });
                }
            });
        }
    }
}
