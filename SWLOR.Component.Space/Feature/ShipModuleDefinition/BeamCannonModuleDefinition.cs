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

namespace SWLOR.Component.Space.Feature.ShipModuleDefinition
{
    public class BeamCannonModuleDefinition : IShipModuleListDefinition
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IShipModuleBuilder _builder;
        
        // Lazy-loaded services to break circular dependencies
        private IRandomService Random => _serviceProvider.GetRequiredService<IRandomService>();
        private ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();
        private ISpaceService SpaceService => _serviceProvider.GetRequiredService<ISpaceService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IMessagingService MessagingService => _serviceProvider.GetRequiredService<IMessagingService>();

        public BeamCannonModuleDefinition(IServiceProvider serviceProvider, IShipModuleBuilder builder)
        {
            _serviceProvider = serviceProvider;
            // Services are now lazy-loaded via IServiceProvider
            _builder = builder;
        }

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            BeamCannon("beamcannon1", "Basic Beam Cannon", "Basic Beam C.", "A stream of high energy particles deals damage over time, three attacks are made over the course of one second, each tick doing 3 thermal DMG on a hit.", 3, 8, 1);
            BeamCannon("beamcannon2", "Beam Cannon I", "Beam Cann. 1", "A stream of high energy particles deals damage over time, three attacks are made over the course of one second, each tick doing 6 thermal DMG on a hit.", 6, 10, 2);
            BeamCannon("beamcannon3", "Beam Cannon II", "Beam Cann. 2", "A stream of high energy particles deals damage over time, three attacks are made over the course of one second, each tick doing 9 thermal DMG on a hit.", 9, 12, 3);
            BeamCannon("beamcannon4", "Beam Cannon III", "Beam Cann. 3", "A stream of high energy particles deals damage over time, three attacks are made over the course of one second, each tick doing 12 thermal DMG on a hit.", 12, 14, 4);
            BeamCannon("beamcannon5", "Beam Cannon IV", "Beam Cann. 4", "A stream of high energy particles deals damage over time, three attacks are made over the course of one second, each tick doing 15 thermal DMG on a hit.", 15, 16, 5);

            return _builder.Build();
        }

        private void BeamCannon(
            string itemTag,
            string name,
            string shortName,
            string description,
            int dmg,
            int capacitor,
            int requiredLevel)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Type(ShipModuleType.BeamLaser)
                .Texture("iit_ess_017")
                .Description(description)
                .MaxDistance(20f)
                .ValidTargetType(ObjectType.Creature)
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.OffensiveModules, requiredLevel)
                .Recast(10f)
                .Capacitor(capacitor)
                .ActivatedAction((activator, activatorShipStatus, target, targetShipStatus, moduleBonus) =>
                {
                    var attackBonus = activatorShipStatus.ThermalDamage;
                    var attackerStat = SpaceService.GetAttackStat(activator);
                    var attack = SpaceService.GetShipAttack(activator, attackBonus);
                    var moduleDamage = dmg + moduleBonus / 3;
                    var defenseBonus = targetShipStatus.ThermalDefense * 2;
                    var defense = SpaceService.GetShipDefense(target, defenseBonus);
                    var defenderStat = GetAbilityScore(target, AbilityType.Vitality);

                    var beam = EffectBeam(VisualEffectType.Vfx_Beam_Holy, activator, BodyNodeType.Chest);

                    for (var i = 0; i < 3; i++)
                    {
                        var delay = i * 0.33f;
                        DelayCommand(delay, () =>
                        {
                            var chanceToHit = SpaceService.CalculateChanceToHit(activator, target);
                            var roll = Random.D100(1);
                            var isHit = roll <= chanceToHit;
                            var damage = CombatService.CalculateDamage(
                                attack,
                                moduleDamage,
                                attackerStat,
                                defense,
                                defenderStat,
                                0);
                            if (!GetIsDead(activator))
                            {
                                if (isHit)
                                {
                                    AssignCommand(activator, () =>
                                    {
                                        ApplyEffectToObject(DurationType.Temporary, beam, target, 0.2f);
                                        SpaceService.ApplyShipDamage(activator, target, damage);
                                    });
                                }
                                else
                                {
                                    AssignCommand(activator, () =>
                                    {
                                        ApplyEffectToObject(DurationType.Temporary, beam, target, 0.2f);
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
