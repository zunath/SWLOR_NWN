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
    public class CapitalWeaponsModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new ShipModuleBuilder();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            CapitalWeapons("cap_weps1", "Tier 1 Capship Weapons", "T1CapWeapons", "Weapons assigned to Tier 1 Capital Ships.", 20);
            CapitalWeapons("cap_weps2", "Tier 2 Capship Weapons", "T2CapWeapons", "Weapons assigned to Tier 2 Capital Ships.", 26);
            CapitalWeapons("cap_weps3", "Tier 3 Capship Weapons", "T3CapWeapons", "Weapons assigned to Tier 3 Capital Ships.", 32);
            CapitalWeapons("cap_weps4", "Tier 4 Capship Weapons", "T4CapWeapons", "Weapons assigned to Tier 4 Capital Ships.", 38);
            CapitalWeapons("cap_weps5", "Tier 5 Capship Weapons", "T5CapWeapons", "Weapons assigned to Tier 5 Capital Ships.", 44);
            CapitalWeapons("cap_weps6", "Tier 6 Capship Weapons", "T6CapWeapons", "Weapons assigned to Corvettes.", 50);
            CapitalWeapons("cap_weps7", "Tier 7 Capship Weapons", "T7CapWeapons", "Weapons assigned to Frigates.", 56);
            CapitalWeapons("cap_weps8", "Tier 8 Capship Weapons", "T8CapWeapons", "Weapons assigned to Cruisers.", 60);
            CapitalWeapons("cap_weps9", "Tier 9 Capship Weapons", "T9CapWeapons", "Weapons assigned to Heavy Cruisers.", 64);
            CapitalWeapons("cap_weps10", "Tier 10 Capship Weapons", "T10CapWeapons", "Weapons assigned to Battlecruisers.", 67);
            CapitalWeapons("cap_weps11", "Tier 11 Capship Weapons", "T11CapWeapons", "Weapons assigned to Battleships.", 70);
            CapitalWeapons("cap_weps12", "Tier 12 Capship Weapons", "T12CapWeapons", "Weapons assigned to Dreadnoughts.", 75);

            return _builder.Build();
        }

        private void CapitalWeapons(
            string itemTag,
            string name,
            string shortName,
            string description,
            int dmg)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Description(description)
                .Type(ShipModuleType.CapitalWeapons)
                .Texture("iit_ess_079")
                .MaxDistance(50f)
                .ValidTargetType(ObjectType.Creature)
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.OffensiveModules, 1)
                .Recast(2f)
                .Capacitor(5)
                .ActivatedAction((activator, activatorShipStatus, target, targetShipStatus, moduleBonus) =>
                {
                    var attackRoll = Random.D10(1);
                    var attackType = "";
                    var attackBonus = activatorShipStatus.ThermalDamage;
                    var attackerStat = GetAbilityScore(activator, AbilityType.Perception);
                    var attack = Stat.GetAttack(activator, AbilityType.Perception, SkillType.Piloting, attackBonus);
                    var defenseBonus = targetShipStatus.ThermalDefense * 2;
                    var defense = Stat.GetDefense(target, CombatDamageType.Thermal, AbilityType.Vitality, defenseBonus);
                    if (attackRoll < 7)
                    {
                        attackType = "laser";
                    }
                    else if (attackRoll < 9)
                    {
                        attackType = "ion";
                        dmg += dmg / 3;
                        attackBonus = activatorShipStatus.EMDamage;
                        defenseBonus = targetShipStatus.EMDefense * 2;
                        defense = Stat.GetDefense(target, CombatDamageType.EM, AbilityType.Vitality, defenseBonus);
                    }
                    else
                    {
                        attackBonus = activatorShipStatus.ExplosiveDamage;
                        dmg += dmg / 4;
                        defenseBonus = targetShipStatus.EMDefense * 2;
                        defense = Stat.GetDefense(target, CombatDamageType.Explosive, AbilityType.Vitality, defenseBonus);
                        if (targetShipStatus.Shield <= 4)
                        {
                            dmg += dmg / 4;
                        }
                    }
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

                    if (attackType == "laser")
                    {
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
                    }
                    else if (attackType == "ion")
                    {
                        if (isHit)
                        {
                            AssignCommand(activator, () =>
                            {
                                var effect = EffectBeam(VisualEffect.Vfx_Beam_Mind, activator, BodyNode.Chest);
                                ApplyEffectToObject(DurationType.Temporary, effect, target, 1.0f);
                                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Ion_Shot), activator);
                            });

                            DelayCommand(0.1f, () =>
                            {
                                AssignCommand(activator, () =>
                                {
                                    var shieldDamage = int.Min(damage, targetShipStatus.Shield);
                                    var armorDamage = (damage - shieldDamage) / 2;
                                    if (armorDamage < 0)
                                    {
                                        armorDamage = 0;
                                    }
                                    var effect = EffectVisualEffect(VisualEffect.Vfx_Imp_Dispel, false, 0.5f);
                                    ApplyEffectToObject(DurationType.Instant, effect, target);
                                    Space.ApplyShipDamage(activator, target, shieldDamage);
                                    Space.ApplyShipDamage(activator, target, armorDamage);
                                    if (armorDamage > 0)
                                    {
                                        ApplyEffectToObject(DurationType.Temporary, EffectMovementSpeedDecrease(50), target, 6f);
                                        ApplyEffectToObject(DurationType.Temporary, EffectAbilityDecrease(AbilityType.Agility, 1), target, 6f);
                                    }
                                });
                            });
                        }
                        else
                        {
                            AssignCommand(activator, () =>
                            {
                                var effect = EffectBeam(VisualEffect.Vfx_Beam_Cold, activator, BodyNode.Chest, true);
                                ApplyEffectToObject(DurationType.Temporary, effect, target, 1.0f);
                            });

                            DelayCommand(0.1f, () =>
                            {
                                AssignCommand(activator, () =>
                                {
                                    var effect = EffectVisualEffect(VisualEffect.Vfx_Imp_Dispel, true);
                                    ApplyEffectToObject(DurationType.Instant, effect, target);
                                });
                            });
                        }

                        var attackId = isHit ? 1 : 4;
                        var combatLogMessage = Combat.BuildCombatLogMessage(activator, target, attackId, chanceToHit);
                        Messaging.SendMessageNearbyToPlayers(target, combatLogMessage, 60f);

                        Enmity.ModifyEnmity(activator, target, damage);
                        CombatPoint.AddCombatPoint(activator, target, SkillType.Piloting);
                    }
                    else
                    {
                        var targetDistance = GetDistanceBetween(activator, target);
                        var targetLocation = GetLocation(target);
                        var delay = (float)(targetDistance / (3.0 * log(targetDistance) + 2.0));

                        // Shoot some missiles out to the target.
                        AssignCommand(activator, () =>
                        {
                            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Ship_Trp), activator);
                            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Mirv_Torpedo, !isHit), target);
                        });

                        DelayCommand(delay, () =>
                        {
                            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Fnf_Fireball, !isHit, 0.5f), target);
                            if (isHit)
                            {
                                Space.ApplyShipDamage(activator, target, damage);
                                Enmity.ModifyEnmity(activator, target, damage);
                            }

                            var attackId = isHit ? 1 : 4;
                            var combatLogMessage = Combat.BuildCombatLogMessage(activator, target, attackId, chanceToHit);
                            Messaging.SendMessageNearbyToPlayers(target, combatLogMessage, 60f);
                            CombatPoint.AddCombatPoint(activator, target, SkillType.Piloting);
                        });
                    }

                    var count = 0;
                    var nearbyTarget = GetFirstObjectInShape(Shape.Sphere, RadiusSize.Gargantuan, GetLocation(activator), true);
                    while (GetIsObjectValid(nearbyTarget) && count < 5)
                    {
                        if (GetIsReactionTypeHostile(nearbyTarget, activator))
                        {
                            chanceToHit = Space.CalculateChanceToHit(activator, nearbyTarget);
                            roll = Random.D100(1);
                            isHit = roll <= chanceToHit;
                            if (isHit == true && nearbyTarget != target)
                            {
                                nearbyTarget = target;
                                attackBonus = activatorShipStatus.ThermalDamage;
                                defenseBonus = targetShipStatus.ThermalDefense * 2;
                                defense = Stat.GetDefense(nearbyTarget, CombatDamageType.Thermal, AbilityType.Vitality, defenseBonus);
                                damage = Combat.CalculateDamage(attack, dmg, attackerStat, defense, defenderStat, 0) / 3;
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
                            else if (isHit == false)
                            {
                                AssignCommand(activator, () =>
                                {
                                    ApplyEffectToObject(DurationType.Instant, sound, target);
                                    ApplyEffectToObject(DurationType.Instant, missile, target);
                                });
                                Enmity.ModifyEnmity(activator, nearbyTarget, 100 * damage);
                            }

                            var attackId = isHit ? 1 : 4;
                            var combatLogMessage = Combat.BuildCombatLogMessage(activator, target, attackId, chanceToHit);
                            Messaging.SendMessageNearbyToPlayers(target, combatLogMessage, 60f);

                            Enmity.ModifyEnmity(activator, nearbyTarget, damage);
                            CombatPoint.AddCombatPoint(activator, target, SkillType.Piloting);
                            count++;
                        }
                        nearbyTarget = GetNextObjectInShape(Shape.Sphere, RadiusSize.Gargantuan, GetLocation(activator), true);
                    }
                });
        }
    }
}