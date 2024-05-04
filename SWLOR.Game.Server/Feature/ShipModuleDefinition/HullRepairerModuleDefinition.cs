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
            HullRepairer("hull_rep_b", "Basic Hull Repairer", "B. Hull Rep.", "Restores targeted or user ship's hull HP by 10.", 1, 10, 10);
            HullRepairer("hull_rep_1", "Hull Repairer I", "Hull Rep. I", "Restores targeted or user ship's hull HP by 14.", 2, 14, 14);
            HullRepairer("hull_rep_2", "Hull Repairer II", "Hull Rep. II", "Restores targeted or user ship's hull HP by 18.", 3, 18, 18);
            HullRepairer("hull_rep_3", "Hull Repairer III", "Hull Rep. III", "Restores targeted or user ship's hull HP by 22.", 4, 22, 22);
            HullRepairer("hull_rep_4", "Hull Repairer IV", "Hull Rep. IV", "Restores targeted or user ship's hull HP by 26.", 5, 26, 26);

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
                .Recast(18f)
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