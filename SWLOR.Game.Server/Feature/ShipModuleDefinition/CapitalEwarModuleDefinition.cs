using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class CapitalEwarModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            CapitalEWarModule("cap_ewar", "Capital E-War Module", "Cap Ewarmod", "Sends out a pulse scrambling enemy fire systems, forcing them towards yourself.", 100);

            return _builder.Build();
        }

        private void CapitalEWarModule(
            string itemTag,
            string name,
            string shortName,
            string description,
            int enmityAmount)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Type(ShipModuleType.CapitalEwar)
                .Texture("iit_ess_253")
                .Description(description)
                .PowerType(ShipModulePowerType.High)
                .Capacitor(25)
                .Recast(12f)
                .CapitalClassModule()
                .CanTargetSelf()
                .ActivatedAction((activator, activatorShipStatus, _, _, moduleBonus) =>
                {
                    enmityAmount += moduleBonus * 25;

                    ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Blue_White), activator, 12.0f);
                    ApplyEffectToObject(DurationType.Temporary, EffectAbilityIncrease(AbilityType.Vitality, 4),activator, 12.0f);

                    const float Distance = 20f;
                    var nearby = GetFirstObjectInShape(Shape.Sphere, Distance, GetLocation(activator), true, ObjectType.Creature);
                    var count = 1;

                    while (GetIsObjectValid(nearby) && count <= 10)
                    {
                        if (GetIsEnemy(nearby, activator) &&
                            !GetIsDead(activator) &&
                            Space.GetShipStatus(nearby) != null &&
                            nearby != activator)
                        {
                            var nearbyStatus = Space.GetShipStatus(nearby);

                            ApplyEffectToObject(DurationType.Temporary, EffectBeam(VisualEffect.Vfx_Beam_Cold, activator, BodyNode.Chest), nearby, 2.0f);
                            ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Blue_White), nearby, 2.0f);
                            Enmity.ModifyEnmity(activator, nearby, enmityAmount);

                            count++;
                        }

                        nearby = GetNextObjectInShape(Shape.Sphere, Distance, GetLocation(activator), true, ObjectType.Creature);
                        
                    }

                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Piloting);
                    Messaging.SendMessageNearbyToPlayers(activator, $"{GetName(activator)} activates their E-War device and begins to draw fire.");
                });
        }
    }
}