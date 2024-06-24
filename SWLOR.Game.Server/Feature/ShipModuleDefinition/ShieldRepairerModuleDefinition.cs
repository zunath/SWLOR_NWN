﻿using System.Collections.Generic;
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
        private readonly ShipModuleBuilder _builder = new();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            ShieldRepairer("shld_rep_b", "Basic Shield Repairer", "B. Shld. Rep.", "Restores targeted or user ship's shield HP by 7.", 1, 4, 7);
            ShieldRepairer("shld_rep_1", "Shield Repairer I", "Shld. Rep. I", "Restores targeted or user ship's shield HP by 14.", 2, 6, 14);
            ShieldRepairer("shld_rep_2", "Shield Repairer II", "Shld. Rep. II", "Restores targeted or user ship's shield HP by 21.", 3, 8, 21);
            ShieldRepairer("shld_rep_3", "Shield Repairer III", "Shld. Rep. III", "Restores targeted or user ship's shield HP by 28.", 4, 10, 28);
            ShieldRepairer("shld_rep_4", "Shield Repairer IV", "Shld. Rep. IV", "Restores targeted or user ship's shield HP by 35.", 5, 12, 35);

            return _builder.Build();
        }

        private void ShieldRepairer(string itemTag, string name, string shortName, string description, int requiredLevel, int capacitor, int baseRecovery)
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

                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), target);

                    var recovery = baseRecovery + (moduleBonus + activatorShipStatus.Industrial) * 2;
                    Space.RestoreShield(target, targetShipStatus, recovery);

                    Messaging.SendMessageNearbyToPlayers(activator, $"{GetName(activator)} restores {recovery} shield HP to {GetName(target)}'s ship.");
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Piloting);
                });
        }
    }
}
