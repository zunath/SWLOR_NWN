using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class RepairFieldGeneratorModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new();

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
                            Space.GetShipStatus(nearby) != null &&
                            nearby != activator)
                        {
                            var nearbyStatus = Space.GetShipStatus(nearby);
                            ApplyEffectToObject(DurationType.Temporary, EffectBeam(VisualEffect.Vfx_Beam_Disintegrate, activator, BodyNode.Chest), nearby, 1.0f);
                            Space.RestoreHull(nearby, nearbyStatus, recovery);

                            if (GetIsPC(nearby) && !GetIsDM(nearby) && !GetIsDMPossessed(nearby))
                            {
                                var playerId = GetObjectUUID(nearby);
                                var dbPlayer = DB.Get<Player>(playerId);
                                var dbShip = DB.Get<PlayerShip>(dbPlayer.ActiveShipId);

                                if (dbShip != null)
                                {
                                    dbShip.Status = nearbyStatus;
                                    DB.Set(dbShip);

                                    ExecuteScript("pc_target_upd", nearby);
                                }
                            }

                            count++;
                        }

                        nearby = GetNextObjectInShape(Shape.Sphere, Distance, GetLocation(activator), true, ObjectType.Creature);
                    }

                    Enmity.ModifyEnmityOnAll(activator, 100 + repairAmount);
                    Messaging.SendMessageNearbyToPlayers(activator, $"{GetName(activator)} begins restoring {recovery} armor HP to nearby ships.");
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Piloting);
                });
        }
    }
}
