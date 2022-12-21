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
    public class MissileLauncherModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new ShipModuleBuilder();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            MissileLauncher("msl_launch_b", "Basic Missile Launcher", "B. Msl Launch", "Deals 6 explosive DMG to your target and nearby ships.", 1, 8f, 10, 6);
            MissileLauncher("msl_launch_1", "Missile Launcher I", "Msl Launch I", "Deals 10 explosive DMG to your target and nearby ships.", 2, 11f, 14, 10);
            MissileLauncher("msl_launch_2", "Missile Launcher II", "Msl Launch II", "Deals 15 explosive DMG to your target and nearby ships.", 3, 14f, 18, 15);
            MissileLauncher("msl_launch_3", "Missile Launcher III", "Msl Launch III", "Deals 19 explosive DMG  to your target and nearby ships.", 4, 17f, 22, 19);
            MissileLauncher("msl_launch_4", "Missile Launcher IV", "Msl Launch IV", "Deals 24 explosive DMG to your target and nearby ships.", 5, 20f, 26, 24);
            MissileLauncher("cap_missile_1", "Concussion Missile I", "CapMissile1", "A 25 damage missile, firing every 2 rounds.", 5, 12f, 30, 25);
            MissileLauncher("cap_missile_2", "Concussion Missile II", "CapMissile2", "A 25 damage missile, firing every 3 rounds.", 5, 18f, 30, 25);
            MissileLauncher("cap_missile_3", "Concussion Missile III", "CapMissile3", "A 35 damage missile, firing every 2 rounds.", 5, 12f, 30, 35);
            MissileLauncher("cap_missile_4", "Concussion Missile IV", "CapMissile4", "A 35 damage missile, firing every 3 rounds.", 5, 18f, 30, 35);
            MissileLauncher("cap_missile_5", "Concussion Missile V", "CapMissile5", "A 45 damage missile, firing every 2 rounds.", 5, 12f, 30, 45);
            MissileLauncher("cap_missile_6", "Concussion Missile VI", "CapMissile6", "A 45 damage missile, firing every 3 rounds.", 5, 18f, 30, 45);
            MissileLauncher("cap_missile_7", "Concussion Missile VII", "CapMissile7", "A 60 damage missile, firing every 3 rounds.", 5, 18f, 30, 60);

            return _builder.Build();
        }

        private void PerformAttack(uint activator, uint target, int dmg, int attackBonus, bool? hitOverride)
        {
            var targetShipStatus = Space.GetShipStatus(target);
            if (targetShipStatus == null)
                return;

            var chanceToHit = Space.CalculateChanceToHit(activator, target);
            var roll = Random.D100(1);
            var isHit = hitOverride ?? roll <= chanceToHit;

            var attackerStat = GetAbilityScore(activator, AbilityType.Willpower);
            var attack = Stat.GetAttack(activator, AbilityType.Willpower, SkillType.Piloting, attackBonus);

            if (isHit)
            {
                var defenseBonus = targetShipStatus.ExplosiveDefense * 2;
                var defense = Stat.GetDefense(target, CombatDamageType.Explosive, AbilityType.Vitality, defenseBonus);
                var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
                var damage = Combat.CalculateDamage(
                    attack,
                    dmg,
                    attackerStat,
                    defense,
                    defenderStat,
                    0);

                Space.ApplyShipDamage(activator, target, damage);
                Enmity.ModifyEnmity(activator, target, damage);
            }

            var attackId = isHit ? 1 : 4;
            var combatLogMessage = Combat.BuildCombatLogMessage(activator, target, attackId, chanceToHit);
            Messaging.SendMessageNearbyToPlayers(target, combatLogMessage, 60f);
            CombatPoint.AddCombatPoint(activator, target, SkillType.Piloting);
        }

        private void MissileLauncher(
            string itemTag,
            string name,
            string shortName,
            string description,
            int requiredLevel,
            float recast,
            int capacitor,
            int dmg)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Texture("iit_ess_089")
                .Type(ShipModuleType.Missile)
                .MaxDistance(55f)
                .Description(description)
                .ValidTargetType(ObjectType.Creature)
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.OffensiveModules, requiredLevel)
                .Recast(recast)
                .Capacitor(capacitor)
                .ActivatedAction((activator, activatorShipStatus, target, targetShipStatus, moduleBonus) =>
                {
                    var targetDistance = GetDistanceBetween(activator, target);
                    var targetLocation = GetLocation(target);
                    var delay = (float)(targetDistance / (3.0 * log(targetDistance) + 2.0));

                    var chanceToHit = Space.CalculateChanceToHit(activator, target);
                    var roll = Random.D100(1);
                    var isHit = roll <= chanceToHit;

                    var attackBonus = moduleBonus * 2 + activatorShipStatus.ExplosiveDamage;

                    // Shoot some missiles out to the target.
                    AssignCommand(activator, () =>
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Ship_Trp), activator);
                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Mirv_Torpedo, !isHit), target);
                    });

                    // Display an explosion at the target location in a few seconds (based on travel distance of the initial missile graphic)
                    // Then apply damage on target and those nearby.
                    DelayCommand(delay, () =>
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Fnf_Fireball, !isHit, 0.5f), target);
                        PerformAttack(activator, target, dmg, attackBonus, isHit);

                        // Iterate over nearby targets, rolling to apply damage to each.
                        var nearby = GetFirstObjectInShape(Shape.Sphere, 3f, targetLocation);
                        while (GetIsObjectValid(nearby))
                        {
                            // Picked up the same creature as our initial target. Move to the next one since we've already processed it.
                            if (nearby == target)
                            {
                                nearby = GetNextObjectInShape(Shape.Sphere, 3f, targetLocation);
                                continue;
                            }

                            // Only targets within 5 meters may be hit by this missile.
                            var nearbyLocation = GetLocation(nearby);
                            if (GetDistanceBetweenLocations(targetLocation, nearbyLocation) > 5f) break;

                            var shipTarget = Space.GetShipStatus(nearby);
                            if (!GetIsObjectValid(nearby))
                            {
                                nearby = GetNextObjectInShape(Shape.Sphere, 3f, targetLocation);
                                continue;
                            }

                            if (shipTarget.Shield <= 0 && shipTarget.Hull <= 0)
                            {
                                nearby = GetNextObjectInShape(Shape.Sphere, 3f, targetLocation);
                                continue;
                            }

                            PerformAttack(activator, nearby, dmg, attackBonus, null);

                            nearby = GetNextObjectInShape(Shape.Sphere, 3f, targetLocation);
                        }
                    });

                });
        }
    }
}
