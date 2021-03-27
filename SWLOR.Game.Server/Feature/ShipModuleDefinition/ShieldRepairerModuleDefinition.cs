using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SpaceService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class ShieldRepairerModuleDefinition: IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new ShipModuleBuilder();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            ShieldRepairer("shld_rep_b", "Basic Shield Repairer", "B. Shld. Rep.", "Restores your target's shield HP.", 1, 10f, 8, 8);
            ShieldRepairer("shld_rep_1", "Shield Repairer I", "Shld. Rep. I", "Restores your target's shield HP.", 2, 12f, 9, 10);
            ShieldRepairer("shld_rep_2", "Shield Repairer II", "Shld. Rep. II", "Restores your target's shield HP.", 3, 14f, 10, 12);
            ShieldRepairer("shld_rep_3", "Shield Repairer III", "Shld. Rep. III", "Restores your target's shield HP.", 4, 16f, 11, 14);
            ShieldRepairer("shld_rep_4", "Shield Repairer IV", "Shld. Rep. IV", "Restores your target's shield HP.", 5, 18f, 12, 16);

            return _builder.Build();
        }


        private void ShieldRepairer(string itemTag, string name, string shortName, string description, int requiredLevel, float recast, int capacitor, int baseRecovery)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Texture("iit_ess_040")
                .Type(ShipModuleType.ShieldRepairer)
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

                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), target);

                    targetShipStatus.Shield += baseRecovery;
                    if (targetShipStatus.Shield > targetShipStatus.MaxShield)
                        targetShipStatus.Shield = targetShipStatus.MaxShield;

                    Messaging.SendMessageNearbyToPlayers(activator, $"{GetName(activator)} restores {baseRecovery} shield HP to {GetName(target)}'s ship.");
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Piloting);
                });
        }
    }
}
