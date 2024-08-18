﻿using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class BulwarkShieldGeneratorModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            BulwarkShieldGenerator("bulwarkgen", "Bulwark Shield Generator", "Bulwark Gen", "An advanced suite of shield generators project at a wide range, protecting allied ships and reinforcing their defenses, and forcing most nearby foes to target the user. Lasts 12 seconds.", 10);

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
                .Type(ShipModuleType.RepairFieldGenerator)
                .Texture("iit_ess_075")
                .Description(description)
                .PowerType(ShipModulePowerType.High)
                .Capacitor(25)
                .Recast(12f)
                .CapitalClassModule()
                .CanTargetSelf()
                .ActivatedAction((activator, activatorShipStatus, _, _, moduleBonus) =>
                {
                    repairAmount += (activatorShipStatus.Industrial + moduleBonus) / 2;

                    ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Blue_White), activator, 2.0f);

                    const float Distance = 8f;
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
                            
                            ApplyEffectToObject(DurationType.Temporary, EffectBeam(VisualEffect.Vfx_Beam_Cold, activator, BodyNode.Chest), nearby, 2.0f);
                            ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Blue_White), nearby, 2.0f);
                            ApplyEffectToObject(DurationType.Temporary, EffectAbilityIncrease(AbilityType.Vitality, 4), nearby, 2.0f);
                        
                            Space.RestoreShield(nearby, nearbyStatus, repairAmount);

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
                        }

                        nearby = GetNextObjectInShape(Shape.Sphere, Distance, GetLocation(activator), true, ObjectType.Creature);
                        count++;
                    }

                    Enmity.ModifyEnmityOnAll(activator, 250 + (repairAmount * count));
                    Messaging.SendMessageNearbyToPlayers(activator, $"{GetName(activator)} begins restoring {repairAmount * count} shield HP to nearby ships, and reinforces their shield integrity.");
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Piloting);
                });
        }
    }
}
