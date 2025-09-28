using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Space.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Communication.Contracts;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Skill.Enums;
using SWLOR.Shared.Domain.Space.Contracts;
using SWLOR.Shared.Domain.Space.Enums;
using SWLOR.Shared.Domain.Space.ValueObjects;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.Events.Player;

namespace SWLOR.Component.Space.Feature.ShipModuleDefinition
{
    public class BulwarkShieldGeneratorModuleDefinition : IShipModuleListDefinition
    {
        private readonly IDatabaseService _db;
        private readonly IServiceProvider _serviceProvider;
        private readonly IShipModuleBuilder _builder;
        private readonly IEventAggregator _eventAggregator;
        
        // Lazy-loaded services to break circular dependencies
        private ISpaceService SpaceService => _serviceProvider.GetRequiredService<ISpaceService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IMessagingService Messaging => _serviceProvider.GetRequiredService<IMessagingService>();

        public BulwarkShieldGeneratorModuleDefinition(
            IDatabaseService db, 
            IServiceProvider serviceProvider,
            IShipModuleBuilder builder,
            IEventAggregator eventAggregator)
        {
            _db = db;
            _builder = builder;
            _eventAggregator = eventAggregator;
            // Services are now lazy-loaded via IServiceProvider
        }

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            BulwarkShieldGenerator("bulwarkgen", "Bulwark Shield Generator", "Bulwark Gen", "An advanced suite of shield generators project at a wide range, protecting allied ships and reinforcing their shields.", 10);

            return _builder.Build();
        }

        private void BulwarkShieldGenerator(
            string itemTag,
            string name,
            string shortName,
            string description,
            int repairAmount)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Type(ShipModuleType.BulwarkShieldGenerator)
                .Texture("iit_ess_075")
                .Description(description)
                .PowerType(ShipModulePowerType.High)
                .Capacitor(25)
                .Recast(12f)
                .CapitalClassModule()
                .CanTargetSelf()
                .ActivatedAction((activator, activatorShipStatus, _, _, moduleBonus) =>
                {
                    var bonusRecovery = activatorShipStatus.Industrial * moduleBonus;
                    var recovery = repairAmount + bonusRecovery;

                    ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffectType.Vfx_Dur_Aura_Pulse_Blue_White), activator, 12.0f);

                    const float Distance = 20f;
                    var nearby = GetFirstObjectInShape(ShapeType.Sphere, Distance, GetLocation(activator), true, ObjectType.Creature);
                    var count = 1;

                    while (GetIsObjectValid(nearby) && count <= 6)
                    {
                        if (!GetIsEnemy(nearby, activator) && 
                            !GetIsDead(activator) && 
                            SpaceService.GetShipStatus(nearby) != null &&
                            nearby != activator)
                        {
                            var nearbyStatus = SpaceService.GetShipStatus(nearby);
                            
                            ApplyEffectToObject(DurationType.Temporary, EffectBeam(VisualEffectType.Vfx_Beam_Cold, activator, BodyNodeType.Chest), nearby, 2.0f);
                            ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffectType.Vfx_Dur_Aura_Pulse_Blue_White), nearby, 2.0f);
                            ApplyEffectToObject(DurationType.Temporary, EffectAbilityIncrease(AbilityType.Vitality, 4), nearby, 10.0f);
                        
                            SpaceService.RestoreShield(nearby, nearbyStatus, recovery);

                            if (GetIsPC(nearby) && !GetIsDM(nearby) && !GetIsDMPossessed(nearby))
                            {
                                var playerId = GetObjectUUID(nearby);
                                var dbPlayer = _db.Get<Player>(playerId);
                                var dbShip = _db.Get<PlayerShip>(dbPlayer.ActiveShipId);

                                if (dbShip != null)
                                {
                                    dbShip.Status = nearbyStatus;
                                    _db.Set(dbShip);

                                    _eventAggregator.Publish(new OnPlayerShieldAdjusted(), nearby);
                                    _eventAggregator.Publish(new OnPlayerTargetUpdated(), nearby);
                                }
                            }

                            count++;
                        }

                        nearby = GetNextObjectInShape(ShapeType.Sphere, Distance, GetLocation(activator), true, ObjectType.Creature);
                    }

                    EnmityService.ModifyEnmityOnAll(activator, 100 + repairAmount);
                    Messaging.SendMessageNearbyToPlayers(activator, $"{GetName(activator)} begins restoring {recovery} shield HP to nearby ships reinforcing their shield integrity.");
                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.Piloting);
                });
        }
    }
}
