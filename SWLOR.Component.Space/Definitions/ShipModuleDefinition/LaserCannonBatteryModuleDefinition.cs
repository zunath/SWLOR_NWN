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
    public class LaserCannonBatteryModuleDefinition : IShipModuleListDefinition
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IShipModuleBuilder _builder;
        
        // Lazy-loaded services to break circular dependencies
        private IRandomService Random => _serviceProvider.GetRequiredService<IRandomService>();
        private ISpaceService SpaceService => _serviceProvider.GetRequiredService<ISpaceService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IMessagingService MessagingService => _serviceProvider.GetRequiredService<IMessagingService>();
        private ICombatCalculationService CombatCalculationService => _serviceProvider.GetRequiredService<ICombatCalculationService>();
        private ICombatMessagingService CombatMessagingService => _serviceProvider.GetRequiredService<ICombatMessagingService>();

        public LaserCannonBatteryModuleDefinition(IServiceProvider serviceProvider, IShipModuleBuilder builder)
        {
            _serviceProvider = serviceProvider;
            // Services are now lazy-loaded via IServiceProvider
            _builder = builder;
        }

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            LaserCannonBattery("lasbattery1", "Laser Cannon Battery I", "Las Cann Bat 1", "Fires at 75% of nearby targets for 30 DMG. Fires once every 2 seconds for 18 seconds.", 30);

            LaserCannonBattery("npcautolas1", "NPC Laser Cannon Battery 1", "npcautolas1", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 12);
            LaserCannonBattery("npcautolas2", "NPC Laser Cannon Battery 2", "npcautolas2", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 17);
            LaserCannonBattery("npcautolas3", "NPC Laser Cannon Battery 3", "npcautolas3", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 22);
            LaserCannonBattery("npcautolas4", "NPC Laser Cannon Battery 4", "npcautolas4", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 25);
            LaserCannonBattery("npcautolas5", "NPC Laser Cannon Battery 5", "npcautolas5", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 30);
            LaserCannonBattery("npcautolas6", "NPC Laser Cannon Battery 6", "npcautolas6", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 33);
            LaserCannonBattery("npcautolas7", "NPC Laser Cannon Battery 7", "npcautolas7", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 36);
            LaserCannonBattery("npcautolas8", "NPC Laser Cannon Battery 8", "npcautolas8", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 39);
            LaserCannonBattery("npcautolas9", "NPC Laser Cannon Battery 9", "npcautolas9", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 41);
            LaserCannonBattery("npcautolas10", "NPC Laser Cannon Battery 10", "npcautolas10", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 43);
            LaserCannonBattery("npcautolas11", "NPC Laser Cannon Battery 11", "npcautolas11", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 45);
            LaserCannonBattery("npcautolas12", "NPC Laser Cannon Battery 12", "npcautolas12", "Fires at 75% of nearby targets. Fires once every 2 seconds for 18 seconds.", 50);

            return _builder.Build();
        }

        private void LaserCannonBattery(
            string itemTag,
            string name,
            string shortName,
            string description,
            int dmg)
        {
        _builder.Create(itemTag)
            .Name(name)
            .ShortName(shortName)
            .Type(ShipModuleType.LaserBattery)
            .Texture("iit_ess8_088")
            .Description(description)
            .ValidTargetType(ObjectType.Creature)
            .MaxDistance(20f)
            .PowerType(ShipModulePowerType.High)
            .RequirePerk(PerkType.OffensiveModules, 5)
            .Recast(18f)
            .Capacitor(15)
            .CapitalClassModule()
            .CanTargetSelf()
            .ActivatedAction((activator, activatorShipStatus, target, _, moduleBonus) =>
            {
                var attackBonus = activatorShipStatus.ThermalDamage;
                var attackerStat = SpaceService.GetAttackStat(activator);
                var attack = SpaceService.GetShipAttack(activator, attackBonus);

                var moduleDMG = dmg + moduleBonus;

                // Determine attacker stat type (Willpower or Perception based on Intuitive Piloting feat)
                var wil = GetAbilityScore(activator, AbilityType.Willpower);
                var per = GetAbilityScore(activator, AbilityType.Perception);
                var attackerStatType = (GetHasFeat(FeatType.IntuitivePiloting, activator) && wil > per)
                    ? AbilityType.Willpower
                    : AbilityType.Perception;
                var missile = EffectVisualEffect(VisualEffectType.Mirv_StarWars_Bolt2);

                for (var i = 0; i < 9; i++)
                {
                    var delay = i * 2f;
                    DelayCommand(delay, () =>
                    {
                        if (!GetIsDead(activator))
                        {
                            var nearbyTarget = GetFirstObjectInShape(ShapeType.Sphere, 20f, GetLocation(activator), true, ObjectType.Creature);
                            while (GetIsObjectValid(nearbyTarget))
                            {
                                if (nearbyTarget != activator && 
                                    Random.D4(1) != 1 && 
                                    GetIsEnemy(nearbyTarget, activator) && 
                                    SpaceService.GetShipStatus(nearbyTarget) != null)
                                {
                                    var nearbyShipStatus = SpaceService.GetShipStatus(nearbyTarget);
                                    var nearbyDefenseBonus = nearbyShipStatus.ThermalDefense * 2;
                                    var nearbyDefense = SpaceService.GetShipDefense(nearbyTarget, nearbyDefenseBonus);
                                    var nearbyDefenderStat = GetAbilityScore(nearbyTarget, AbilityType.Vitality);
                                    var damage = CombatCalculationService.CalculateAbilityDamage(
                        activator,
                        target,
                        moduleDMG,
                        CombatDamageType.Thermal,
                        SkillType.Piloting,
                        attackerStatType,
                        AbilityType.Vitality);
                                    var chanceToHit = SpaceService.CalculateChanceToHit(activator, nearbyTarget);
                                    var roll = Random.D100(1);
                                    var isHit = roll <= chanceToHit;
                                    ApplyEffectToObject(DurationType.Instant, missile, nearbyTarget);
                                    if (isHit)
                                    {
                                        SpaceService.ApplyShipDamage(activator, nearbyTarget, damage);
                                    }

                                    var attackId = isHit ? 1 : 4;
                                    var combatLogMessage = CombatMessagingService.BuildCombatLogMessage(activator, target, attackId, chanceToHit);
                                    MessagingService.SendMessageNearbyToPlayers(nearbyTarget, combatLogMessage, 60f);

                                    EnmityService.ModifyEnmity(activator, nearbyTarget, damage);
                                    CombatPointService.AddCombatPoint(activator, nearbyTarget, SkillType.Piloting);
                                }
                                nearbyTarget = GetNextObjectInShape(ShapeType.Sphere, 20f, GetLocation(activator), true, ObjectType.Creature);
                            }
                        }
                    });
                }
            });
        }
    }
}




