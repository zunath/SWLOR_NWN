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

namespace SWLOR.Component.Space.Feature.ShipModuleDefinition
{
    public class RepairFieldGeneratorModuleDefinition : IShipModuleListDefinition
    {
        private readonly IDatabaseService _db;
        private readonly IServiceProvider _serviceProvider;
        private readonly IShipModuleBuilder _builder;
        
        // Lazy-loaded services to break circular dependencies
        private ISpaceService SpaceService => _serviceProvider.GetRequiredService<ISpaceService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IMessagingService MessagingService => _serviceProvider.GetRequiredService<IMessagingService>();

        public RepairFieldGeneratorModuleDefinition(IDatabaseService db, IServiceProvider serviceProvider, IShipModuleBuilder builder)
        {
            _db = db;
            // Services are now lazy-loaded via IServiceProvider
            _builder = builder;
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

                                    ExecuteScript(ScriptName.OnPlayerHullAdjusted, nearby);
                                    ExecuteScript(ScriptName.OnPlayerTargetUpdated, nearby);
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
