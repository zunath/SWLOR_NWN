using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class ShieldRepairerModuleDefinition: IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new ShipModuleBuilder();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            ShieldRepairer("shld_rep_b", "Basic Shield Repairer", "B. Shld. Rep.", "Restores another ship's shield HP by 8.", 1, 10f, 8, 8);
            ShieldRepairer("shld_rep_1", "Shield Repairer I", "Shld. Rep. I", "Restores another ship's shield HP by 10.", 2, 12f, 9, 10);
            ShieldRepairer("shld_rep_2", "Shield Repairer II", "Shld. Rep. II", "Restores another ship's shield HP by 12.", 3, 14f, 10, 12);
            ShieldRepairer("shld_rep_3", "Shield Repairer III", "Shld. Rep. III", "Restores another ship's shield HP by 14.", 4, 16f, 11, 14);
            ShieldRepairer("shld_rep_4", "Shield Repairer IV", "Shld. Rep. IV", "Restores another ship's shield HP by 16.", 5, 18f, 12, 16);

            return _builder.Build();
        }


        private void ShieldRepairer(string itemTag, string name, string shortName, string description, int requiredLevel, float recast, int capacitor, int baseRecovery)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Texture("iit_ess_040")
                .Type(ShipModuleType.ShieldRepairer)
                .CanTargetSelf()
                .MaxDistance(20f)
                .ValidTargetType(ObjectType.Creature)
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

                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), target);

                    var recovery = baseRecovery + moduleBonus * 2;
                    Space.RestoreShield(target, targetShipStatus, recovery);

                    Messaging.SendMessageNearbyToPlayers(activator, $"{GetName(activator)} restores {baseRecovery} shield HP to {GetName(target)}'s ship.");
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Piloting);
                });
        }
    }
}
