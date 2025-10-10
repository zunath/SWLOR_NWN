using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Space.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Component.Combat.Contracts;
using SWLOR.Shared.Domain.Communication.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;
using SWLOR.Shared.Domain.Space.Contracts;
using SWLOR.Shared.Domain.Space.Enums;
using SWLOR.Shared.Domain.Space.ValueObjects;

namespace SWLOR.Component.Space.Definitions.ShipModuleDefinition
{
    public class TurboLaserModuleDefinition : IShipModuleListDefinition
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IShipModuleBuilder _builder;
        
        // Lazy-loaded services to break circular dependencies
        private readonly Lazy<IRandomService> _random;
        private readonly Lazy<ISpaceService> _spaceService;
        private readonly Lazy<IEnmityService> _enmityService;
        private readonly Lazy<ICombatPointService> _combatPointService;
        private readonly Lazy<IMessagingService> _messagingService;
        private readonly Lazy<ICombatCalculationService> _combatCalculationService;
        private readonly Lazy<ICombatMessagingService> _combatMessagingService;

        private IRandomService Random => _random.Value;
        private ISpaceService SpaceService => _spaceService.Value;
        private IEnmityService EnmityService => _enmityService.Value;
        private ICombatPointService CombatPointService => _combatPointService.Value;
        private IMessagingService MessagingService => _messagingService.Value;
        private ICombatCalculationService CombatCalculationService => _combatCalculationService.Value;
        private ICombatMessagingService CombatMessagingService => _combatMessagingService.Value;

        public TurboLaserModuleDefinition(IServiceProvider serviceProvider, IShipModuleBuilder builder)
        {
            _serviceProvider = serviceProvider;
            _builder = builder;
            
            // Initialize lazy services
            _random = new Lazy<IRandomService>(() => _serviceProvider.GetRequiredService<IRandomService>());
            _spaceService = new Lazy<ISpaceService>(() => _serviceProvider.GetRequiredService<ISpaceService>());
            _enmityService = new Lazy<IEnmityService>(() => _serviceProvider.GetRequiredService<IEnmityService>());
            _combatPointService = new Lazy<ICombatPointService>(() => _serviceProvider.GetRequiredService<ICombatPointService>());
            _messagingService = new Lazy<IMessagingService>(() => _serviceProvider.GetRequiredService<IMessagingService>());
            _combatCalculationService = new Lazy<ICombatCalculationService>(() => _serviceProvider.GetRequiredService<ICombatCalculationService>());
            _combatMessagingService = new Lazy<ICombatMessagingService>(() => _serviceProvider.GetRequiredService<ICombatMessagingService>());
        }

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            Turbolaser("turbolas1", "Heavy Turbolaser Cannon", "Turbolaser1", "Deals 90 thermal DMG to your target. Designed to target capital ships, it struggles against subcapital ships and suffers a -25% hit chance against them.", 90, 1, 10);
            Turbolaser("turbolas2", "Dual Turbolaser Cannon", "Turbolaser2", "Deals 75 thermal DMG to your target, firing twice over two seconds. Designed to target capital ships, it struggles against subcapital ships and suffers a -25% hit chance against them.", 75, 2, 15);
            Turbolaser("turbolas3", "Quad Turbolaser Cannon", "Turbolaser3", "Deals 65 thermal DMG to your target, firing four times over four seconds. Designed to target capital ships, it struggles against subcapital ships and suffers a -25% hit chance against them.", 65, 4, 30);

            return _builder.Build();
        }

        private void Turbolaser(
            string itemTag,
            string name,
            string shortName,
            string description,
            int dmg,
            int attacks,
            int capacitor)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Type(ShipModuleType.TurboLaser)
                .Texture("iit_ess_079")
                .Description(description)
                .MaxDistance(30f)
                .ValidTargetType(ObjectType.Creature)
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.OffensiveModules, 5)
                .Recast(10f)
                .Capacitor(capacitor)
                .CapitalClassModule()
                .CanTargetSelf()
                .ActivatedAction((activator, activatorShipStatus, target, targetShipStatus, moduleBonus) =>
                {
                    var attackBonus = activatorShipStatus.ThermalDamage;
                    var attackerStat = SpaceService.GetAttackStat(activator);
                    var attack = SpaceService.GetShipAttack(activator, attackBonus);

                    var moduleDamage = dmg + moduleBonus * 3;
                    var defenseBonus = targetShipStatus.ThermalDefense * 2;
                    var defense = SpaceService.GetShipDefense(target, defenseBonus);
                    var defenderStat = GetAbilityScore(target, AbilityType.Vitality);

                    // Determine attacker stat type (Willpower or Perception based on Intuitive Piloting feat)
                    var wil = GetAbilityScore(activator, AbilityType.Willpower);
                    var per = GetAbilityScore(activator, AbilityType.Perception);
                    var attackerStatType = (GetHasFeat(FeatType.IntuitivePiloting, activator) && wil > per)
                        ? AbilityType.Willpower
                        : AbilityType.Perception;

                    var chanceToHit = SpaceService.CalculateChanceToHit(activator, target);

                    // Subcapital ships are harder to hit. Even with very high accuracy, the starting chance to hit is 50% and it caps out at 70% instead of 95%.
                    if (!targetShipStatus.CapitalShip)
                    {
                        chanceToHit -= 25;
                        if (chanceToHit < 20)
                        {
                            chanceToHit = 20;
                        }
                    }
                    var sound = EffectVisualEffect(VisualEffectType.Vfx_Ship_Blast);
                    var missile = EffectVisualEffect(VisualEffectType.Mirv_StarWars_Bolt2);
                    for (var i = 0; i < attacks; i++)
                    {
                        var delay = i * 3f;
                        DelayCommand(delay, () =>
                        {
                            if (!GetIsDead(activator) && !GetIsDead(target))
                            {
                                var roll = Random.D100(1);
                                var isHit = roll <= chanceToHit;
                                var damage = CombatCalculationService.CalculateAbilityDamage(
                        activator,
                        target,
                        moduleDamage,
                        CombatDamageType.Thermal,
                        SkillType.Piloting,
                        attackerStatType,
                        AbilityType.Vitality);
                                if (isHit)
                                {
                                    AssignCommand(activator, () =>
                                    {
                                        ApplyEffectToObject(DurationType.Instant, sound, target);
                                        ApplyEffectToObject(DurationType.Instant, missile, target);
                                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Fnf_Gas_Explosion_Fire), target);

                                        DelayCommand(0.3f, () =>
                                        {
                                            SpaceService.ApplyShipDamage(activator, target, damage);
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
                                var combatLogMessage = CombatMessagingService.BuildCombatLogMessage(activator, target, attackId, chanceToHit);
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




