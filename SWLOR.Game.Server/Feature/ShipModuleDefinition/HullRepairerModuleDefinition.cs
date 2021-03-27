using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SpaceService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class HullRepairerModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new ShipModuleBuilder();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            HullRepairer("hull_rep_b", "Basic Hull Repairer", "B. Hull Rep.", "Restores your target's hull HP.", 1, 10f, 8, 6);
            HullRepairer("hull_rep_1", "Hull Repairer I", "Hull Rep. I", "Restores your target's hull HP.", 2, 12f, 9, 8);
            HullRepairer("hull_rep_2", "Hull Repairer II", "Hull Rep. II", "Restores your target's hull HP.", 3, 14f, 10, 10);
            HullRepairer("hull_rep_3", "Hull Repairer III", "Hull Rep. III", "Restores your target's hull HP.", 4, 16f, 11, 12);
            HullRepairer("hull_rep_4", "Hull Repairer IV", "Hull Rep. IV", "Restores your target's hull HP.", 5, 18f, 12, 14);

            return _builder.Build();
        }


        private void HullRepairer(string itemTag, string name, string shortName, string description, int requiredLevel, float recast, int capacitor, int baseRecovery)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Texture("iit_ess_020")
                .Type(ShipModuleType.HullRepairer)
                .ValidTargetType(ObjectType.Creature)
                .Description(description)
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.DefensiveModules, requiredLevel)
                .Recast(recast)
                .Capacitor(capacitor)
                .ActivatedAction((activator, activatorShipStatus, target, targetShipStatus) =>
                {
                    if (!GetIsObjectValid(target))
                    {
                        target = activator;
                        targetShipStatus = activatorShipStatus;
                    }

                    if (activator != target)
                    {
                        AssignCommand(activator, () =>
                        {
                            var beam = EffectBeam(VisualEffect.Vfx_Beam_Mind, activator, BodyNode.Chest);
                            ApplyEffectToObject(DurationType.Temporary, beam, target, 1.0f);
                        });
                    }

                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Breach), target);

                    targetShipStatus.Hull += baseRecovery;
                    if (targetShipStatus.Hull > targetShipStatus.MaxHull)
                        targetShipStatus.Hull = targetShipStatus.MaxHull;

                    Messaging.SendMessageNearbyToPlayers(activator, $"{GetName(activator)} restores {baseRecovery} hull HP to {GetName(target)}'s ship.");
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Piloting);
                });

        }
    }
}