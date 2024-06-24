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
    public class StormCannonModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            StormCannon("storm_cann", "Storm Cannon", "Storm Cann.", "A stream of high energy particles deals 140 Damage over 1 second. Deals reduced damage to unshielded targets, but imposes debuffs.", 35);

            return _builder.Build();
        }

        private void StormCannon(
            string itemTag,
            string name,
            string shortName,
            string description,
            int dmg)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Type(ShipModuleType.IonCannon)
                .Texture("iit_ess2_062")
                .Description(description)
                .MaxDistance(40f)
                .ValidTargetType(ObjectType.Creature)
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.OffensiveModules, 5)
                .Recast(8f)
                .Capacitor(30)
                .CapitalClassModule()
                .ActivatedAction((activator, activatorShipStatus, target, targetShipStatus, moduleBonus) =>
                {
                    var attackBonus = activatorShipStatus.EMDamage;
                    var attackerStat = GetAbilityScore(activator, AbilityType.Perception);
                    var attack = Stat.GetAttack(activator, AbilityType.Perception, SkillType.Piloting, attackBonus);

                    if (GetHasFeat(FeatType.IntuitivePiloting, activator) && GetAbilityScore(activator, AbilityType.Willpower) > GetAbilityScore(activator, AbilityType.Perception))
                    {
                        attackerStat = GetAbilityScore(activator, AbilityType.Willpower);
                        attack = Stat.GetAttack(activator, AbilityType.Willpower, SkillType.Piloting, attackBonus);
                    }

                    var moduleDamage = dmg + moduleBonus;
                    var defenseBonus = targetShipStatus.EMDefense * 2;
                    var defense = Stat.GetDefense(target, CombatDamageType.EM, AbilityType.Vitality, defenseBonus);
                    var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
                    var damage = Combat.CalculateDamage(
                        attack,
                        moduleDamage,
                        attackerStat,
                        defense,
                        defenderStat,
                        0);

                    var effectBeam = EffectBeam(VisualEffect.Vfx_Beam_Mind, activator, BodyNode.Chest);
                    var effectLightning = EffectBeam(VisualEffect.Vfx_Beam_Silent_Lightning, activator, BodyNode.Chest);

                    for (var i = 0; i < 4; i++)
                    {
                        var delay = i * 0.33f;
                        DelayCommand(delay, () =>
                        {
                            var chanceToHit = Space.CalculateChanceToHit(activator, target);
                            var roll = Random.D100(1);
                            var isHit = roll <= chanceToHit;

                            if (!GetIsDead(activator) && !GetIsDead(target))
                            {
                                if (isHit)
                                {
                                    AssignCommand(activator, () =>
                                    {
                                        ApplyEffectToObject(DurationType.Temporary, effectBeam, target, 1.0f);
                                        ApplyEffectToObject(DurationType.Temporary, effectLightning, target, 1.0f);
                                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Ion_Shot), activator);
                                    });

                                    DelayCommand(0.1f, () =>
                                    {
                                        AssignCommand(activator, () =>
                                        {
                                            var shieldDamage = int.Min(damage, targetShipStatus.Shield);
                                            var armorDamage = (damage - shieldDamage) / 4;
                                            if (armorDamage < 0)
                                            {
                                                armorDamage = 0;
                                            }
                                            var effect = EffectVisualEffect(VisualEffect.Vfx_Imp_Dispel, false);
                                            ApplyEffectToObject(DurationType.Instant, effect, target);
                                            Space.ApplyShipDamage(activator, target, shieldDamage);
                                            Space.ApplyShipDamage(activator, target, armorDamage);
                                            if (armorDamage > 0)
                                            {
                                                ApplyEffectToObject(DurationType.Temporary, EffectMovementSpeedDecrease(75), target, 6f);
                                                ApplyEffectToObject(DurationType.Temporary, EffectAbilityDecrease(AbilityType.Agility, 4), target, 12f);
                                            }
                                        });
                                    });
                                }
                                else
                                {
                                    AssignCommand(activator, () =>
                                    {
                                        ApplyEffectToObject(DurationType.Temporary, effectBeam, target, 1.0f);
                                        ApplyEffectToObject(DurationType.Temporary, effectLightning, target, 1.0f);
                                    });

                                    DelayCommand(0.1f, () =>
                                    {
                                        AssignCommand(activator, () =>
                                        {
                                            var effect = EffectVisualEffect(VisualEffect.Vfx_Fnf_Electric_Explosion, true);
                                            ApplyEffectToObject(DurationType.Temporary, effectBeam, target, 1.0f);
                                            ApplyEffectToObject(DurationType.Temporary, effectLightning, target, 1.0f);
                                        });
                                    });
                                }

                                var attackId = isHit ? 1 : 4;
                                var combatLogMessage = Combat.BuildCombatLogMessage(activator, target, attackId, chanceToHit);
                                Messaging.SendMessageNearbyToPlayers(target, combatLogMessage, 60f);

                                Enmity.ModifyEnmity(activator, target, damage);
                                CombatPoint.AddCombatPoint(activator, target, SkillType.Piloting);
                            }
                        });
                    }
                });
        }
    }
}