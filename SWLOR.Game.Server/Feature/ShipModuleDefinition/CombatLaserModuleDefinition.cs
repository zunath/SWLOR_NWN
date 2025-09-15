using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using Random = SWLOR.Game.Server.Service.Random;

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
                .Texture("iit_ess_004")
                .Description(description)
                .MaxDistance(30f)
                .ValidTargetType(ObjectType.Creature)
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.OffensiveModules, requiredLevel)
                .Recast(6f)
                .Capacitor(capacitor)
                .ActivatedAction((activator, activatorShipStatus, target, targetShipStatus, moduleBonus) =>
                {
                    var attackBonus = activatorShipStatus.ThermalDamage;
                    var attackerStat = Space.GetAttackStat(activator);
                    var attack = Space.GetShipAttack(activator, attackBonus);
                    var defenseBonus = targetShipStatus.ThermalDefense * 2;
                    var defense = Space.GetShipDefense(target, defenseBonus);
                    var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
                    var moduleDamage = dmg + moduleBonus / 3;
                    var damage = Combat.CalculateDamage(
                        attack,
                        moduleDamage,
                        attackerStat,
                        defense,
                        defenderStat,
                        0);

                    var chanceToHit = Space.CalculateChanceToHit(activator, target);
                    var roll = Random.D100(1);
                    var isHit = roll <= chanceToHit;
                    var sound = EffectVisualEffect(VisualEffect.Vfx_Ship_Blast);
                    var missile = EffectVisualEffect(VisualEffect.Mirv_StarWars_Bolt2);

                    if (isHit)
                    {
                        AssignCommand(activator, () =>
                        {
                            ApplyEffectToObject(DurationType.Instant, sound, target);
                            ApplyEffectToObject(DurationType.Instant, missile, target);

                            DelayCommand(0.3f, () =>
                            {
                                Space.ApplyShipDamage(activator, target, damage);
                            });
                        });
                    }
                    else
                    {
                        AssignCommand(activator, () =>
                        {
                            ApplyEffectToObject(DurationType.Instant, sound, target);
                            ApplyEffectToObject(DurationType.Instant, missile, target);
                        });
                    }

                    var attackId = isHit ? 1 : 4;
                    var combatLogMessage = Combat.BuildCombatLogMessage(activator, target, attackId, chanceToHit);
                    Messaging.SendMessageNearbyToPlayers(target, combatLogMessage, 60f);

                    Enmity.ModifyEnmity(activator, target, damage);
                    CombatPoint.AddCombatPoint(activator, target, SkillType.Piloting);
                });
        }
    }
}
