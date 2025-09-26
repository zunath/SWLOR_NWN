using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Space.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Communication.Contracts;
using SWLOR.Shared.Domain.Space.Contracts;
using SWLOR.Shared.Domain.Space.Enums;
using SWLOR.Shared.Domain.Space.ValueObjects;

namespace SWLOR.Component.Space.Feature.ShipModuleDefinition
{
    public class TurboLaserModuleDefinition : IShipModuleListDefinition
    {
        private readonly IRandomService _random;
        private readonly IServiceProvider _serviceProvider;
        private readonly IShipModuleBuilder _builder;
        
        // Lazy-loaded services to break circular dependencies
        private ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();
        private ISpaceService SpaceService => _serviceProvider.GetRequiredService<ISpaceService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IMessagingService MessagingService => _serviceProvider.GetRequiredService<IMessagingService>();

        public TurboLaserModuleDefinition(IRandomService random, IServiceProvider serviceProvider, IShipModuleBuilder builder)
        {
            _random = random;
            // Services are now lazy-loaded via IServiceProvider
            _builder = builder;
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
                    var sound = EffectVisualEffect(VisualEffect.Vfx_Ship_Blast);
                    var missile = EffectVisualEffect(VisualEffect.Mirv_StarWars_Bolt2);
                    for (var i = 0; i < attacks; i++)
                    {
                        var delay = i * 3f;
                        DelayCommand(delay, () =>
                        {
                            if (!GetIsDead(activator) && !GetIsDead(target))
                            {
                                var roll = _random.D100(1);
                                var isHit = roll <= chanceToHit;
                                var damage = CombatService.CalculateDamage(
                                    attack,
                                    moduleDamage,
                                    attackerStat,
                                    defense,
                                    defenderStat,
                                    0);
                                if (isHit)
                                {
                                    AssignCommand(activator, () =>
                                    {
                                        ApplyEffectToObject(DurationType.Instant, sound, target);
                                        ApplyEffectToObject(DurationType.Instant, missile, target);
                                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Gas_Explosion_Fire), target);

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
