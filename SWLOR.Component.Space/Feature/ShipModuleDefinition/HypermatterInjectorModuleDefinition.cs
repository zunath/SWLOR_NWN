using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Space.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Communication.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;
using SWLOR.Shared.Domain.Space.Contracts;
using SWLOR.Shared.Domain.Space.Enums;
using SWLOR.Shared.Domain.Space.ValueObjects;

namespace SWLOR.Component.Space.Feature.ShipModuleDefinition
{
    public class HypermatterInjectorModuleDefinition : IShipModuleListDefinition
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IShipModuleBuilder _builder;
        
        // Lazy-loaded services to break circular dependencies
        private ISpaceService SpaceService => _serviceProvider.GetRequiredService<ISpaceService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IMessagingService MessagingService => _serviceProvider.GetRequiredService<IMessagingService>();

        public HypermatterInjectorModuleDefinition(IServiceProvider serviceProvider, IShipModuleBuilder builder)
        {
            _serviceProvider = serviceProvider;
            _builder = builder;
        }

        private const string FuelCapsuleItemResref = "ship_fuelcapsule";

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            HypermatterInjector("cap_inject1", "Basic Hypermatter Injector", "Basic Fuel Inj", "Consumes a fuel capsule to restore 8 capacitor.", 1, 8);
            HypermatterInjector("cap_inject2", "Hypermatter Injector I", "Fuel Inj 1", "Consumes a fuel capsule to restore 14 capacitor.", 2, 14);
            HypermatterInjector("cap_inject3", "Hypermatter Injector II", "Fuel Inj 2", "Consumes a fuel capsules to restore 20 capacitor.", 3, 20);
            HypermatterInjector("cap_inject4", "Hypermatter Injector III", "Fuel Inj 3", "Consumes a fuel capsules to restore 26 capacitor.", 4, 26);
            HypermatterInjector("cap_inject5", "Hypermatter Injector IV", "Fuel Inj 4", "Consumes a fuel capsules to restore 32 capacitor.", 5, 32);

            return _builder.Build();
        }

        private void HypermatterInjector(string tag, string name, string shortName, string description, int requiredLevel, int capRestore)
        {
            _builder.Create(tag)
                .Name(name)
                .ShortName(shortName)
                .Description(description)
                .Type(ShipModuleType.HypermatterInjector)
                .CanTargetSelf()
                .ValidTargetType(ObjectType.Creature)
                .PowerType(ShipModulePowerType.High)
                .Texture("iit_ess_032")
                .RequirePerk(PerkType.DefensiveModules, requiredLevel)
                .Recast(60f)
                .ValidationAction((activator, _, _, _, _) =>
                {
                    var item = GetItemPossessedBy(activator, FuelCapsuleItemResref);
                    var stackSize = GetItemStackSize(item);
                    if (stackSize <= 0 && GetIsPC(activator))
                    {
                        return "You need a fuel capsule to activate this module.";
                    }

                    return string.Empty;
                })
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
                            var beam = EffectBeam(VisualEffectType.Vfx_Beam_Mind, activator, BodyNodeType.Chest);
                            ApplyEffectToObject(DurationType.Temporary, beam, target, 1.0f);
                        });
                    }

                    var item = GetItemPossessedBy(activator, FuelCapsuleItemResref);
                    var stackSize = GetItemStackSize(item);
                    if (stackSize <= 1)
                    {
                        DestroyObject(item);
                    }
                    else
                    {
                        SetItemStackSize(item, stackSize - 1);
                    }

                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Breach), target);
                    
                    var recovery = capRestore + (moduleBonus + activatorShipStatus.Industrial) * 2;
                    SpaceService.RestoreCapacitor(target, targetShipStatus, recovery);

                    MessagingService.SendMessageNearbyToPlayers(activator, $"{GetName(activator)} restores {recovery} capacitor charge to {GetName(target)}'s ship.");
                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.Piloting);
                });
        }
    }
}
