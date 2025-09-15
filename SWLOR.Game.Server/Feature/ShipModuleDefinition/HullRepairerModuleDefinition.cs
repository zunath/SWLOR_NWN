using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class HullRepairerModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            HullRepairer("hull_rep_b", "Basic Hull Repairer", "B. Hull Rep.", "Restores targeted or user ship's hull HP by 7.", 1, 8, 7);
            HullRepairer("hull_rep_1", "Hull Repairer I", "Hull Rep. I", "Restores targeted or user ship's hull HP by 14.", 2, 12, 14);
            HullRepairer("hull_rep_2", "Hull Repairer II", "Hull Rep. II", "Restores targeted or user ship's hull HP by 21.", 3, 16, 21);
            HullRepairer("hull_rep_3", "Hull Repairer III", "Hull Rep. III", "Restores targeted or user ship's hull HP by 28.", 4, 20, 28);
            HullRepairer("hull_rep_4", "Hull Repairer IV", "Hull Rep. IV", "Restores targeted or user ship's hull HP by 35.", 5, 24, 35);
            HullRepairer("npc_hull_r1", "NPC Hull Repairer I", "NPC HullR 1", "Restores targeted or user ship's hull HP by 20", 0, 5, 20);
            HullRepairer("npc_hull_r2", "NPC Hull Repairer II", "NPC HullR 2", "Restores targeted or user ship's hull HP by 25", 0, 5, 25);
            HullRepairer("npc_hull_r3", "NPC Hull Repairer III", "NPC HullR 3", "Restores targeted or user ship's hull HP by 30", 0, 5, 30);
            HullRepairer("npc_hull_r4", "NPC Hull Repairer IV", "NPC HullR 4", "Restores targeted or user ship's hull HP by 35", 0, 5, 35);
            HullRepairer("npc_hull_r5", "NPC Hull Repairer V", "NPC HullR 5", "Restores targeted or user ship's hull HP by 40", 0, 5, 40);

            return _builder.Build();
        }

        private void HullRepairer(string itemTag, string name, string shortName, string description, int requiredLevel, int capacitor, int baseRecovery)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Texture("iit_ess_020")
                .Type(ShipModuleType.HullRepairer)
                .ValidTargetType(ObjectType.Creature)
                .CanTargetSelf()
                .MaxDistance(20f)
                .Description(description)
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.DefensiveModules, requiredLevel)
                .Recast(12f)
                .Capacitor(capacitor)
                .ActivatedAction((activator, activatorShipStatus, target, targetShipStatus, moduleBonus) =>
                {
                    if (!GetIsObjectValid(target) || GetIsEnemy(target, activator))
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

                    var recovery = baseRecovery + (moduleBonus + activatorShipStatus.Industrial) * 2;
                    Space.RestoreHull(target, targetShipStatus, recovery);

                    Messaging.SendMessageNearbyToPlayers(activator, $"{GetName(activator)} restores {recovery} hull HP to {GetName(target)}'s ship.");
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Piloting);
                });

        }
    }
}