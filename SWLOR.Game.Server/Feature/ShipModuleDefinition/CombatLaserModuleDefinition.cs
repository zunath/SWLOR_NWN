﻿using System.Collections.Generic;
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
    public class CombatLaserModuleDefinition: IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new ShipModuleBuilder();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            CombatLaser("com_laser_b", "Basic Combat Laser", "B. Cmbt Laser", "Deals 8 thermal DMG to your target.", 1, 3f, 6, 8);
            CombatLaser("com_laser_1", "Combat Laser I", "Cmbt Laser I", "Deals 12 thermal DMG to your target.", 2, 4f, 9, 12);
            CombatLaser("com_laser_2", "Combat Laser II", "Cmbt Laser II", "Deals 17 thermal DMG to your target.", 3, 5f, 12, 17);
            CombatLaser("com_laser_3", "Combat Laser III", "Cmbt Laser III", "Deals 21 thermal DMG to your target.", 4, 6f, 15, 21);
            CombatLaser("com_laser_4", "Combat Laser IV", "Cmbt Laser IV", "Deals 26 thermal DMG to your target.", 5, 7f, 18, 26);

            return _builder.Build();
        }

        private void CombatLaser(
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
                .Type(ShipModuleType.CombatLaser)
                .Texture("iit_ess_004")
                .Description(description)
                .RequiresTarget()
                .MaxDistance(30f)
                .ValidTargetType(ObjectType.Creature)
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.OffensiveModules, requiredLevel)
                .Recast(recast)
                .Capacitor(capacitor)
                .ActivatedAction((activator, activatorShipStatus, target, targetShipStatus, moduleBonus) =>
                {
                    var attackBonus = moduleBonus * 2 + activatorShipStatus.ThermalDamage;
                    var attackerStat = GetAbilityScore(activator, AbilityType.Willpower);
                    var attack = Stat.GetAttack(activator, AbilityType.Willpower, SkillType.Piloting, attackBonus);

                    var defenseBonus = targetShipStatus.ThermalDefense * 2;
                    var defense = Stat.GetDefense(target, CombatDamageType.Thermal, AbilityType.Vitality, defenseBonus);
                    var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
                    var damage = Combat.CalculateDamage(
                        attack,
                        dmg,
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
