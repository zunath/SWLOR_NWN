using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Space.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Communication.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;
using SWLOR.Shared.Domain.Space.Contracts;
using SWLOR.Shared.Domain.Space.Enums;
using SWLOR.Shared.Domain.Space.ValueObjects;

namespace SWLOR.Component.Space.Definitions.ShipModuleDefinition
{
    public class StormCannonModuleDefinition : IShipModuleListDefinition
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IShipModuleBuilder _builder;
        
        // Lazy-loaded services to break circular dependencies
        private readonly Lazy<IRandomService> _random;
        private readonly Lazy<ISpaceService> _spaceService;
        private readonly Lazy<ICombatService> _combatService;
        private readonly Lazy<IEnmityService> _enmityService;
        private readonly Lazy<ICombatPointService> _combatPointService;
        private readonly Lazy<IMessagingService> _messagingService;
        
        private IRandomService Random => _random.Value;
        private ISpaceService SpaceService => _spaceService.Value;
        private ICombatService CombatService => _combatService.Value;
        private IEnmityService EnmityService => _enmityService.Value;
        private ICombatPointService CombatPointService => _combatPointService.Value;
        private IMessagingService MessagingService => _messagingService.Value;

        public StormCannonModuleDefinition(IServiceProvider serviceProvider, IShipModuleBuilder builder)
        {
            _serviceProvider = serviceProvider;
            _builder = builder;
            
            // Initialize lazy services
            _random = new Lazy<IRandomService>(() => _serviceProvider.GetRequiredService<IRandomService>());
            _spaceService = new Lazy<ISpaceService>(() => _serviceProvider.GetRequiredService<ISpaceService>());
            _combatService = new Lazy<ICombatService>(() => _serviceProvider.GetRequiredService<ICombatService>());
            _enmityService = new Lazy<IEnmityService>(() => _serviceProvider.GetRequiredService<IEnmityService>());
            _combatPointService = new Lazy<ICombatPointService>(() => _serviceProvider.GetRequiredService<ICombatPointService>());
            _messagingService = new Lazy<IMessagingService>(() => _serviceProvider.GetRequiredService<IMessagingService>());
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
