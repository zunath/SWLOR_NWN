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
    public class CombatLaserModuleDefinition: IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new ShipModuleBuilder();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            CombatLaser("com_laser_b", "Basic Combat Laser", "B. Cmbt Laser", "Deals light thermal damage to your target.", 1, 3f, 6, 8);
            CombatLaser("com_laser_1", "Combat Laser I", "Cmbt Laser I", "Deals light thermal damage to your target.", 2, 4f, 9, 12);
            CombatLaser("com_laser_2", "Combat Laser II", "Cmbt Laser II", "Deals light thermal damage to your target.", 3, 5f, 12, 17);
            CombatLaser("com_laser_3", "Combat Laser III", "Cmbt Laser III", "Deals light thermal damage to your target.", 4, 6f, 15, 21);
            CombatLaser("com_laser_4", "Combat Laser IV", "Cmbt Laser IV", "Deals light thermal damage to your target.", 5, 7f, 18, 26);

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
                    var targetLocation = GetLocation(target);
                    var sound = GetMissileSfx(activator);
                    var missile = GetMissileVfx(activator);

                    if (isHit)
                    {
                        AssignCommand(activator, () =>
                        {
                            ApplyEffectToObject(DurationType.Instant, sound, target);
                            ApplyEffectToObject(DurationType.Instant, missile, target);
    
                            DelayCommand(0.3f, () =>
                            {
                                PlayShipHitSfx(target, damage);
                                Space.ApplyShipDamage(activator, target, damage);
                            });
                        });
                    }
                    else
                    {
                        AssignCommand(activator, () =>
                        {
                            var sound = EffectVisualEffect(VisualEffect.Vfx_Ship_Blast);
                            var missile = EffectVisualEffect(VisualEffect.Mirv_StarWars_Bolt2);
                            ApplyEffectToObject(DurationType.Instant, sound, target);
                            ApplyEffectToObject(DurationType.Instant, missile, target);
                        });
                    }

                    var attackId = isHit ? 1 : 4;
                    var combatLogMessage = Combat.BuildCombatLogMessage(GetName(activator), GetName(target), attackId, chanceToHit);
                    Messaging.SendMessageNearbyToPlayers(target, combatLogMessage, 60f);

                    Enmity.ModifyEnmity(activator, target, damage);
                    CombatPoint.AddCombatPoint(activator, target, SkillType.Piloting);
                });
        }
        private static Core.Effect GetMissileVfx(uint activator)
        {
            var appearance = GetAppearanceType(activator);
            var missile = EffectVisualEffect(VisualEffect.Mirv_StarWars_Bolt2); ;

            if (appearance == AppearanceType.RepublicForay || appearance == AppearanceType.RepublicHammerhead)
            {
                missile = EffectVisualEffect(VisualEffect.Mirv_StarWars_Bolt3);
            }
            else if (appearance == AppearanceType.SithScoutA || appearance == AppearanceType.SithScoutC || appearance == AppearanceType.SithGunshipC)
            {
                missile = EffectVisualEffect(VisualEffect.Mirv_StarWars_Bolt3);
            }
            else
            {
                missile = EffectVisualEffect(VisualEffect.Mirv_StarWars_Bolt2);
            }
            return missile;
        }
        private static Core.Effect GetMissileSfx(uint activator)
        {
            var appearance = GetAppearanceType(activator);
            var sound = EffectVisualEffect(VisualEffect.Vfx_Ship_Blast3); ;

            if (appearance == AppearanceType.RepublicForay || appearance == AppearanceType.RepublicHammerhead)
            {
                sound = EffectVisualEffect(VisualEffect.Vfx_Ship_Blast3);
            }
            else if (appearance == AppearanceType.SithScoutA || appearance == AppearanceType.SithScoutC || appearance == AppearanceType.SithGunshipC)
            {
                sound = EffectVisualEffect(VisualEffect.Vfx_Ship_Blast2);
            }
            else
            {
                sound = EffectVisualEffect(VisualEffect.Vfx_Ship_Blast);
            }
            return sound;
        }
        private static void PlayShipHitSfx(uint target, int amount)
        {
            if (amount < 0) return;

            var targetShipStatus = Space.GetShipStatus(target);

            var remainingDamage = amount;
            // First deal damage to target's shields.
            if (remainingDamage <= targetShipStatus.Shield)
            {
                // Shields have enough to cover the attack.
                targetShipStatus.Shield -= remainingDamage;
                remainingDamage = 0;
                ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Cyan_Blue), target, 1.0f);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Ship_Deflect), target);
            }
            else
            {
                remainingDamage -= targetShipStatus.Shield;
                targetShipStatus.Shield = 0;
            }
            if (remainingDamage > 0)
            {
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Ship_Explosion), target);
            }
        }
    }
}
