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
    public class IonCannonModuleDefinition : IShipModuleListDefinition
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

        public IonCannonModuleDefinition(IRandomService random, IServiceProvider serviceProvider, IShipModuleBuilder builder)
        {
            _random = random;
            _serviceProvider = serviceProvider;
            // Services are now lazy-loaded via IServiceProvider
            _builder = builder;
        }

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            IonCannon("ion_cann_b", "Basic Ion Cannon", "B. Ion Cann.", "Deals 15 EM DMG to your target. Deals reduced damage to unshielded targets, but imposes debuffs.", 1, 6, 15);
            IonCannon("ion_cann_1", "Ion Cannon I", "Ion Cann. I", "Deals 30 EM DMG to your target. Deals reduced damage to unshielded targets, but imposes debuffs.", 2, 8, 30);
            IonCannon("ion_cann_2", "Ion Cannon II", "Ion Cann. II", "Deals 45 EM DMG to your target. Deals reduced damage to unshielded targets, but imposes debuffs.", 3, 10, 45);
            IonCannon("ion_cann_3", "Ion Cannon III", "Ion Cann. III", "Deals 60 EM DMG to your target. Deals reduced damage to unshielded targets, but imposes debuffs.", 4, 12, 60);
            IonCannon("ion_cann_4", "Ion Cannon IV", "Ion Cann. IV", "Deals 75 EM DMG to your target. Deals reduced damage to unshielded targets, but imposes debuffs.", 5, 14, 75);

            return _builder.Build();
        }

        private void IonCannon(
            string itemTag,
            string name,
            string shortName,
            string description,
            int requiredLevel,
            int capacitor,
            int dmg)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Type(ShipModuleType.IonCannon)
                .Texture("iit_ess_050")
                .Description(description)
                .MaxDistance(40f)
                .ValidTargetType(ObjectType.Creature)
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.OffensiveModules, requiredLevel)
                .Recast(10f)
                .Capacitor(capacitor)
                .ActivatedAction((activator, activatorShipStatus, target, targetShipStatus, moduleBonus) =>
                {
                    var attackBonus = activatorShipStatus.EMDamage;
                    var attackerStat = SpaceService.GetAttackStat(activator);
                    var attack = SpaceService.GetShipAttack(activator, attackBonus);
                    var moduleDamage = dmg + moduleBonus / 2;

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

                    var chanceToHit = SpaceService.CalculateChanceToHit(activator, target);
                    var roll = _random.D100(1);
                    var isHit = roll <= chanceToHit;

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
                                var armorDamage = (damage-shieldDamage)/2;
                                if (armorDamage < 0)
                                {
                                    armorDamage = 0;
                                }
                                var effect = EffectVisualEffect(VisualEffect.Vfx_Imp_Dispel, false, 0.5f);
                                ApplyEffectToObject(DurationType.Instant, effect, target);
                                SpaceService.ApplyShipDamage(activator, target, shieldDamage);
                                SpaceService.ApplyShipDamage(activator, target, armorDamage);
                                if (armorDamage > 0)
                                {
                                    ApplyEffectToObject(DurationType.Temporary, EffectMovementSpeedDecrease(50), target, 6f);
                                    ApplyEffectToObject(DurationType.Temporary, EffectAbilityDecrease(AbilityType.Agility, 2), target, 12f);
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
                                var effect = EffectVisualEffect(VisualEffect.Vfx_Fnf_Electric_Explosion, true);
                                ApplyEffectToObject(DurationType.Instant, effect, target);
                            });
                        });
                    }

                    var attackId = isHit ? 1 : 4;
                    var combatLogMessage = CombatService.BuildCombatLogMessage(activator, target, attackId, chanceToHit);
                    MessagingService.SendMessageNearbyToPlayers(target, combatLogMessage, 60f);

                    EnmityService.ModifyEnmity(activator, target, damage);
                    CombatPointService.AddCombatPoint(activator, target, SkillType.Piloting);
                });
        }
    }
}
