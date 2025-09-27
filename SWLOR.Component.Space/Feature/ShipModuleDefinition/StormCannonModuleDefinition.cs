using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Space.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Communication.Contracts;
using SWLOR.Shared.Domain.Space.Contracts;
using SWLOR.Shared.Domain.Space.Enums;
using SWLOR.Shared.Domain.Space.ValueObjects;

namespace SWLOR.Component.Space.Feature.ShipModuleDefinition
{
    public class StormCannonModuleDefinition : IShipModuleListDefinition
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IShipModuleBuilder _builder;
        
        // Lazy-loaded services to break circular dependencies
        private IRandomService Random => _serviceProvider.GetRequiredService<IRandomService>();
        private ISpaceService SpaceService => _serviceProvider.GetRequiredService<ISpaceService>();
        private ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IMessagingService MessagingService => _serviceProvider.GetRequiredService<IMessagingService>();

        public StormCannonModuleDefinition(IServiceProvider serviceProvider, IShipModuleBuilder builder)
        {
            _serviceProvider = serviceProvider;
            // Services are now lazy-loaded via IServiceProvider
            _builder = builder;
        }

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
                    var attackerStat = SpaceService.GetAttackStat(activator);
                    var attack = SpaceService.GetShipAttack(activator, attackBonus);

                    var moduleDamage = dmg + moduleBonus;
                    var defenseBonus = targetShipStatus.EMDefense * 2;
                    var defense = SpaceService.GetShipDefense(target, defenseBonus);
                    var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
                    var damage = CombatService.CalculateDamage(
                        attack,
                        moduleDamage,
                        attackerStat,
                        defense,
                        defenderStat,
                        0);

                    var effectBeam = EffectBeam(VisualEffectType.Vfx_Beam_Mind, activator, BodyNodeType.Chest);
                    var effectLightning = EffectBeam(VisualEffectType.Vfx_Beam_Silent_Lightning, activator, BodyNodeType.Chest);

                    for (var i = 0; i < 4; i++)
                    {
                        var delay = i * 0.33f;
                        DelayCommand(delay, () =>
                        {
                            var chanceToHit = SpaceService.CalculateChanceToHit(activator, target);
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
                                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Ion_Shot), activator);
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
                                            var effect = EffectVisualEffect(VisualEffectType.Vfx_Imp_Dispel, false);
                                            ApplyEffectToObject(DurationType.Instant, effect, target);
                                            SpaceService.ApplyShipDamage(activator, target, shieldDamage);
                                            SpaceService.ApplyShipDamage(activator, target, armorDamage);
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
                                            var effect = EffectVisualEffect(VisualEffectType.Vfx_Fnf_Electric_Explosion, true);
                                            ApplyEffectToObject(DurationType.Temporary, effectBeam, target, 1.0f);
                                            ApplyEffectToObject(DurationType.Temporary, effectLightning, target, 1.0f);
                                        });
                                    });
                                }

                                var attackId = isHit ? 1 : 4;
                                var combatLogMessage = CombatService.BuildCombatLogMessage(activator, target, attackId, chanceToHit);
                                MessagingService.SendMessageNearbyToPlayers(target, combatLogMessage, 60f);

                                EnmityService.ModifyEnmity(activator, target, damage);
                                CombatPointService.AddCombatPoint(activator, target, SkillType.Piloting);
                            }
                        });
                    }
                });
        }
    }
}
