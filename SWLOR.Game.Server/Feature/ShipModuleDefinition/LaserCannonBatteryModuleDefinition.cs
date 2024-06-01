 using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class LaserCannonBatteryModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new ShipModuleBuilder();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            LaserCannonBattery("lasbattery1", "Laser Cannon Battery I", "Las Cann Bat 1", "Fires at 75% of nearby targets for 20 DMG. Fires once every 2 seconds for 18 seconds.", 30);

            LaserCannonBattery("npcautolas1", "NPC Laser Cannon Battery 1", "npcautolas1", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 10);
            LaserCannonBattery("npcautolas2", "NPC Laser Cannon Battery 2", "npcautolas2", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 15);
            LaserCannonBattery("npcautolas3", "NPC Laser Cannon Battery 3", "npcautolas3", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 20);
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
            .Texture("iit_ess8_088")
            .Description(description)
            .ValidTargetType(ObjectType.Creature)
            .MaxDistance(20f)
            .PowerType(ShipModulePowerType.High)
            .RequirePerk(PerkType.OffensiveModules, 5)
            .Recast(18f)
            .Capacitor(15)
            .CapitalClassModule()
            .CanTargetSelf()
            .ActivatedAction((activator, activatorShipStatus, target, targetShipStatus, moduleBonus) =>
            {
                var attackBonus = activatorShipStatus.ThermalDamage;
                var attackerStat = GetAbilityScore(activator, AbilityType.Perception);
                var attack = Stat.GetAttack(activator, AbilityType.Perception, SkillType.Piloting, attackBonus);
                if (GetHasFeat(FeatType.IntuitivePiloting, activator) && GetAbilityScore(activator, AbilityType.Willpower) > GetAbilityScore(activator, AbilityType.Perception))
                {
                    attackerStat = GetAbilityScore(activator, AbilityType.Willpower);
                    attack = Stat.GetAttack(activator, AbilityType.Willpower, SkillType.Piloting, attackBonus);
                }

                var moduleDMG = dmg + moduleBonus / 2;
                var sound = EffectVisualEffect(VisualEffect.Vfx_Ship_Blast);
                var missile = EffectVisualEffect(VisualEffect.Mirv_StarWars_Bolt2);

                for (int i = 0; i < 9; i++)
                {
                    float delay = i * 2f;
                    DelayCommand(delay, () =>
                    {
                        if (!GetIsDead(activator))
                        {
                            var nearbyTarget = GetFirstObjectInShape(Shape.Sphere, 20f, GetLocation(activator), true, ObjectType.Creature);
                            while (GetIsObjectValid(nearbyTarget))
                            {
                                if (nearbyTarget != activator && Random.D4(1) != 1 && GetIsEnemy(nearbyTarget, activator) && Space.GetShipStatus(nearbyTarget) != null)
                                {
                                    var nearbyShipStatus = Space.GetShipStatus(nearbyTarget);
                                    var nearbyDefenseBonus = nearbyShipStatus.ThermalDefense * 2;
                                    var nearbyDefense = Stat.GetDefense(target, CombatDamageType.Thermal, AbilityType.Vitality, nearbyDefenseBonus);
                                    var nearbyDefenderStat = GetAbilityScore(target, AbilityType.Vitality);
                                    var damage = Combat.CalculateDamage(
                                        attack,
                                        moduleDMG,
                                        attackerStat,
                                        nearbyDefense,
                                        nearbyDefenderStat,
                                        0);
                                    var sound = EffectVisualEffect(VisualEffect.Vfx_Ship_Blast);
                                    var chanceToHit = Space.CalculateChanceToHit(activator, nearbyTarget);
                                    var roll = Random.D100(1);
                                    var isHit = roll <= chanceToHit;
                                    ApplyEffectToObject(DurationType.Instant, missile, nearbyTarget);
                                    if (isHit)
                                    {
                                        Space.ApplyShipDamage(activator, nearbyTarget, damage);
                                    }

                                    var attackId = isHit ? 1 : 4;
                                    var combatLogMessage = Combat.BuildCombatLogMessage(activator, target, attackId, chanceToHit);
                                    Messaging.SendMessageNearbyToPlayers(nearbyTarget, combatLogMessage, 60f);

                                    Enmity.ModifyEnmity(activator, nearbyTarget, damage);
                                    CombatPoint.AddCombatPoint(activator, nearbyTarget, SkillType.Piloting);
                                }
                                nearbyTarget = GetNextObjectInShape(Shape.Sphere, 20f, GetLocation(activator), true, ObjectType.Creature);
                            }
                        }
                    });
                }
            });
        }
    }
}
