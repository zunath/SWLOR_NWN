using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SpaceService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class MissileModuleDefinition: IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new ShipModuleBuilder();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            MissileLauncher("msl_launch_b", "Basic Missile Launcher", "B. Msl Launch", "Deals explosive damage to your target and nearby ships.", 1, 8f, 10, 7);
            MissileLauncher("msl_launch_1", "Missile Launcher I", "Msl Launch I", "Deals explosive damage to your target and nearby ships.", 2, 11f, 14, 10);
            MissileLauncher("msl_launch_2", "Missile Launcher II", "Msl Launch II", "Deals explosive damage to your target and nearby ships.", 3, 14f, 18, 13);
            MissileLauncher("msl_launch_3", "Missile Launcher III", "Msl Launch III", "Deals explosive damage to your target and nearby ships.", 4, 17f, 22, 16);
            MissileLauncher("msl_launch_4", "Missile Launcher IV", "Msl Launch IV", "Deals explosive damage to your target and nearby ships.", 5, 20f, 26, 19);

            return _builder.Build();
        }

        private void MissileLauncher(string itemTag, string name, string shortName, string description, int requiredLevel, float recast, int capacitor, int baseDamage)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Texture("iit_ess_089")
                .Type(ShipModuleType.Missile)
                .Description(description)
                .RequiresTarget()
                .ValidTargetType(ObjectType.Creature)
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.OffensiveModules, requiredLevel)
                .Recast(recast)
                .Capacitor(capacitor)
                .ActivatedAction((activator, activatorShipStatus, target, targetShipStatus) =>
                {
                    var chanceToHit = Space.CalculateChanceToHit(activator, target);
                    var roll = Random.D100(1);
                    var isHit = roll <= chanceToHit;
                    var targetDistance = GetDistanceBetween(activator, target);
                    var targetLocation = GetLocation(target);
                    var targetDefense = targetShipStatus.ExplosiveDefense;
                    var attackerDamage = baseDamage + activatorShipStatus.ExplosiveDamage;
                    var damage = attackerDamage - targetDefense;
                    if (damage < 0) damage = 0;
                    var delay = (float)(targetDistance / (3.0 * log(targetDistance) + 2.0));

                    // Shoot some missiles out to the target.
                    var isHitCopy = isHit;
                    AssignCommand(activator, () =>
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Mirv, !isHitCopy), target);
                    });
                    
                    // Display an explosion at the target location in a few seconds (based on travel distance of the initial missile graphic)
                    // Then apply damage on target and those nearby.
                    DelayCommand(delay, () =>
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Fnf_Fireball, !isHitCopy), target);

                        if (isHit)
                        {
                            Space.ApplyShipDamage(activator, target, damage);
                            Enmity.ModifyEnmity(activator, target, damage);
                        }
                        else
                        {
                            Messaging.SendMessageNearbyToPlayers(activator, $"{GetName(activator)} misses their intended target.");
                        }

                        CombatPoint.AddCombatPoint(activator, target, SkillType.Piloting);

                        // Iterate over nearby targets, rolling to apply damage to each.
                        var nearby = GetFirstObjectInShape(Shape.Sphere, 3f, targetLocation, false, ObjectType.Creature);
                        while (GetIsObjectValid(nearby))
                        {
                            // Picked up the same creature as our initial target. Move to the next one since we've already processed it.
                            if (nearby == target)
                            {
                                nearby = GetNextObjectInShape(Shape.Sphere, 3f, targetLocation, false, ObjectType.Creature);
                                continue;
                            }

                            // Only targets within 5 meters may be hit by this missile.
                            var nearbyLocation = GetLocation(nearby);
                            if (GetDistanceBetweenLocations(targetLocation, nearbyLocation) > 5f) break;

                            var shipTarget = Space.GetShipStatus(nearby);
                            if (!GetIsObjectValid(nearby))
                            {
                                nearby = GetNextObjectInShape(Shape.Sphere, 3f, targetLocation, false, ObjectType.Creature);
                                continue;
                            }

                            if (shipTarget.Shield <= 0 && shipTarget.Hull <= 0)
                            {
                                nearby = GetNextObjectInShape(Shape.Sphere, 3f, targetLocation, false, ObjectType.Creature);
                                continue;
                            }

                            targetDefense = shipTarget.ExplosiveDefense;
                            attackerDamage = baseDamage + activatorShipStatus.ExplosiveDamage;

                            damage = attackerDamage - targetDefense;
                            if (damage < 0) damage = 0;
                            chanceToHit = Space.CalculateChanceToHit(activator, nearby);
                            roll = Random.D100(1);
                            isHit = roll <= chanceToHit;

                            if (isHit)
                            {
                                Space.ApplyShipDamage(activator, nearby, damage / 2);
                            }

                            Enmity.ModifyEnmity(activator, nearby, damage);
                            CombatPoint.AddCombatPoint(activator, nearby, SkillType.Piloting);

                            nearby = GetNextObjectInShape(Shape.Sphere, 3f, targetLocation, false, ObjectType.Creature);
                        }
                    });

                });
        }
    }
}
