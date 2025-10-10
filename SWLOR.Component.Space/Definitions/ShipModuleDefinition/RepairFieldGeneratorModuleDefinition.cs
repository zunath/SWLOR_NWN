using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Space.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Component.Combat.Contracts;
using SWLOR.Shared.Domain.Communication.Contracts;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Skill.Enums;
using SWLOR.Shared.Domain.Space.Contracts;
using SWLOR.Shared.Domain.Space.Enums;
using SWLOR.Shared.Domain.Space.Events;
using SWLOR.Shared.Domain.Space.ValueObjects;
using SWLOR.Shared.Events.Events.Player;

namespace SWLOR.Component.Space.Definitions.ShipModuleDefinition
{
    public class RepairFieldGeneratorModuleDefinition : IShipModuleListDefinition
    {
        private readonly IDatabaseService _db;
        private readonly IServiceProvider _serviceProvider;
        private readonly IShipModuleBuilder _builder;
        private readonly IEventAggregator _eventAggregator;
        
        // Lazy-loaded services to break circular dependencies
        private readonly Lazy<ISpaceService> _spaceService;
        private readonly Lazy<IEnmityService> _enmityService;
        private readonly Lazy<ICombatPointService> _combatPointService;
        private readonly Lazy<IMessagingService> _messagingService;
        
        private ISpaceService SpaceService => _spaceService.Value;
        private IEnmityService EnmityService => _enmityService.Value;
        private ICombatPointService CombatPointService => _combatPointService.Value;
        private IMessagingService MessagingService => _messagingService.Value;

        public RepairFieldGeneratorModuleDefinition(IDatabaseService db, IServiceProvider serviceProvider, IShipModuleBuilder builder, IEventAggregator eventAggregator)
        {
            _db = db;
            _serviceProvider = serviceProvider;
            _builder = builder;
            _eventAggregator = eventAggregator;
            
            // Initialize lazy services
            _spaceService = new Lazy<ISpaceService>(() => _serviceProvider.GetRequiredService<ISpaceService>());
            _enmityService = new Lazy<IEnmityService>(() => _serviceProvider.GetRequiredService<IEnmityService>());
            _combatPointService = new Lazy<ICombatPointService>(() => _serviceProvider.GetRequiredService<ICombatPointService>());
            _messagingService = new Lazy<IMessagingService>(() => _serviceProvider.GetRequiredService<IMessagingService>());
        }

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            RepairFieldGenerator("repairfield", "Repair Field Generator", "Rep Field Gen", "A suite of welding lasers and other advanced devices serves to repair hull.", 10);

            return _builder.Build();
        }

        private void RepairFieldGenerator(
            string itemTag,
            string name,
            string shortName,
            string description,
            int repairAmount)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Type(ShipModuleType.RepairFieldGenerator)
                .Texture("iit_ess_074")
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

                    ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffectType.Vfx_Dur_Aura_Pulse_Red_White), activator, 12.0f);

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
                            ApplyEffectToObject(DurationType.Temporary, EffectBeam(VisualEffectType.Vfx_Beam_Disintegrate, activator, BodyNodeType.Chest), nearby, 1.0f);
                            SpaceService.RestoreHull(nearby, nearbyStatus, recovery);

                            if (GetIsPC(nearby) && !GetIsDM(nearby) && !GetIsDMPossessed(nearby))
                            {
                                var playerId = GetObjectUUID(nearby);
                                var dbPlayer = _db.Get<Player>(playerId);
                                var dbShip = _db.Get<PlayerShip>(dbPlayer.ActiveShipId);

                                if (dbShip != null)
                                {
                                    dbShip.Status = nearbyStatus;
                                    _db.Set(dbShip);

                                    _eventAggregator.Publish(new OnPlayerHullAdjusted(), nearby);
                                    _eventAggregator.Publish(new OnPlayerTargetUpdated(), nearby);
                                }
                            }

                            count++;
                        }

                        nearby = GetNextObjectInShape(ShapeType.Sphere, Distance, GetLocation(activator), true, ObjectType.Creature);
                    }

                    EnmityService.ModifyEnmityOnAll(activator, 100 + repairAmount);
                    MessagingService.SendMessageNearbyToPlayers(activator, $"{GetName(activator)} begins restoring {recovery} armor HP to nearby ships.");
                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.Piloting);
                });
        }
    }
}


