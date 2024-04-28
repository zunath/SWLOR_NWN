using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SpaceService;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class HypermatterInjectorModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new ShipModuleBuilder();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            HypermatterInjector("cap_inject1", "Hypermatter Injector I", "Hyperfuel 1", "Consumes a fuel capsule to restore 8 capacitor.", 1, 8);
            HypermatterInjector("cap_inject2", "Hypermatter Injector II", "Hyperfuel 2", "Consumes a fuel capsule to restore 14 capacitor.", 2, 14);
            HypermatterInjector("cap_inject3", "Hypermatter Injector III", "Hyperfuel 3", "Consumes a fuel capsules to restore 20 capacitor.", 3, 20);
            HypermatterInjector("cap_inject4", "Hypermatter Injector IV", "Hyperfuel 4", "Consumes a fuel capsules to restore 26 capacitor.", 4, 26);
            HypermatterInjector("cap_inject5", "Hypermatter Injector V", "Hyperfuel 5", "Consumes a fuel capsules to restore 32 capacitor.", 5, 32);

            return _builder.Build();
        }

        private void HypermatterInjector(string tag, string name, string shortName, string description, int requiredLevel, int capRestore)
        {
            _builder.Create(tag)
                .Name(name)
                .ShortName(shortName)
                .Description(description)
                .ValidTargetType(ObjectType.Creature)
                .PowerType(ShipModulePowerType.High)
                .Texture("iit_ess_032")
                .Type(ShipModuleType.HypermatterInjector)
                .RequirePerk(Service.PerkService.PerkType.DefensiveModules, requiredLevel)
                .Recast(60f)
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
                    
                    var recovery = capRestore + (moduleBonus + activatorShipStatus.Industrial) * 2;
                    Space.RestoreCapacitor(target, targetShipStatus, recovery);

                    Messaging.SendMessageNearbyToPlayers(activator, $"{GetName(activator)} restores {recovery} capacitor charge to {GetName(target)}'s ship.");
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Piloting);
                });
        }
    }
}
