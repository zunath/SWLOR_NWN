using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class HullRepairerModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new ShipModuleBuilder();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            HullRepairer("hull_rep_b", "Basic Hull Repairer", "B. Hull Rep.", "Restores another ship's hull HP by 6.", 1, 10f, 8, 6);
            HullRepairer("hull_rep_1", "Hull Repairer I", "Hull Rep. I", "Restores another ship's hull HP by 8.", 2, 12f, 9, 8);
            HullRepairer("hull_rep_2", "Hull Repairer II", "Hull Rep. II", "Restores another ship's hull HP by 10.", 3, 14f, 10, 10);
            HullRepairer("hull_rep_3", "Hull Repairer III", "Hull Rep. III", "Restores another ship's hull HP by 12.", 4, 16f, 11, 12);
            HullRepairer("hull_rep_4", "Hull Repairer IV", "Hull Rep. IV", "Restores another ship's hull HP by 14.", 5, 18f, 12, 14);

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
                .CanTargetSelf()
                .MaxDistance(20f)
                .Description(description)
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.DefensiveModules, requiredLevel)
                .Recast(recast)
                .Capacitor(capacitor)
                .ActivatedAction((activator, activatorShipStatus, target, targetShipStatus, moduleBonus) =>
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

                    var recovery = baseRecovery + moduleBonus * 2;
                    Space.RestoreHull(target, targetShipStatus, recovery);

                    Messaging.SendMessageNearbyToPlayers(activator, $"{GetName(activator)} restores {recovery} hull HP to {GetName(target)}'s ship.");
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Piloting);
                });

        }
    }
}