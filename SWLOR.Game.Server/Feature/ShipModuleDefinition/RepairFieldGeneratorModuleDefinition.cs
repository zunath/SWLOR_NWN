using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using System.Collections.Generic;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Data.Entity;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class RepairFieldGeneratorModuleDefinition : IShipModuleListDefinition
    {
        private readonly IDatabaseService _db;
        private readonly ISpaceService _spaceService;
        private readonly IEnmityService _enmityService;
        private readonly ICombatPointService _combatPointService;
        private readonly IMessagingService _messagingService;
        private readonly ShipModuleBuilder _builder = new();

        public RepairFieldGeneratorModuleDefinition(IDatabaseService db, ISpaceService spaceService, IEnmityService enmityService, ICombatPointService combatPointService, IMessagingService messagingService)
        {
            _db = db;
            _spaceService = spaceService;
            _enmityService = enmityService;
            _combatPointService = combatPointService;
            _messagingService = messagingService;
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

                    ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Red_White), activator, 12.0f);

                    const float Distance = 20f;
                    var nearby = GetFirstObjectInShape(Shape.Sphere, Distance, GetLocation(activator), true, ObjectType.Creature);
                    var count = 1;

                    while (GetIsObjectValid(nearby) && count <= 6)
                    {
                        if (!GetIsEnemy(nearby, activator) && 
                            !GetIsDead(activator) && 
                            _spaceService.GetShipStatus(nearby) != null &&
                            nearby != activator)
                        {
                            var nearbyStatus = _spaceService.GetShipStatus(nearby);
                            ApplyEffectToObject(DurationType.Temporary, EffectBeam(VisualEffect.Vfx_Beam_Disintegrate, activator, BodyNode.Chest), nearby, 1.0f);
                            _spaceService.RestoreHull(nearby, nearbyStatus, recovery);

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

                        nearby = GetNextObjectInShape(Shape.Sphere, Distance, GetLocation(activator), true, ObjectType.Creature);
                    }

                    _enmityService.ModifyEnmityOnAll(activator, 100 + repairAmount);
                    _messagingService.SendMessageNearbyToPlayers(activator, $"{GetName(activator)} begins restoring {recovery} armor HP to nearby ships.");
                    _combatPointService.AddCombatPointToAllTagged(activator, SkillType.Piloting);
                });
        }
    }
}
