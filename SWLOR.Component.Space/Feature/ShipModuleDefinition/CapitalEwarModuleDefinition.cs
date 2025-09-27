using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Space.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Communication.Contracts;
using SWLOR.Shared.Domain.Space.Contracts;
using SWLOR.Shared.Domain.Space.Enums;
using SWLOR.Shared.Domain.Space.ValueObjects;

namespace SWLOR.Component.Space.Feature.ShipModuleDefinition
{
    public class CapitalEwarModuleDefinition : IShipModuleListDefinition
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IShipModuleBuilder _builder;
        
        // Lazy-loaded services to break circular dependencies
        private ISpaceService SpaceService => _serviceProvider.GetRequiredService<ISpaceService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IMessagingService MessagingService => _serviceProvider.GetRequiredService<IMessagingService>();

        public CapitalEwarModuleDefinition(IServiceProvider serviceProvider, IShipModuleBuilder builder)
        {
            // Services are now lazy-loaded via IServiceProvider
            _builder = builder;
        }

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

                    ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffectType.Vfx_Dur_Aura_Pulse_Blue_White), activator, 12.0f);
                    ApplyEffectToObject(DurationType.Temporary, EffectAbilityIncrease(AbilityType.Vitality, 4),activator, 12.0f);

                    const float Distance = 20f;
                    var nearby = GetFirstObjectInShape(ShapeType.Sphere, Distance, GetLocation(activator), true, ObjectType.Creature);
                    var count = 1;

                    while (GetIsObjectValid(nearby) && count <= 10)
                    {
                        if (GetIsEnemy(nearby, activator) &&
                            !GetIsDead(activator) &&
                            SpaceService.GetShipStatus(nearby) != null &&
                            nearby != activator)
                        {
                            var nearbyStatus = SpaceService.GetShipStatus(nearby);

                            ApplyEffectToObject(DurationType.Temporary, EffectBeam(VisualEffectType.Vfx_Beam_Cold, activator, BodyNodeType.Chest), nearby, 2.0f);
                            ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffectType.Vfx_Dur_Aura_Pulse_Blue_White), nearby, 2.0f);
                            EnmityService.ModifyEnmity(activator, nearby, enmityAmount);

                            count++;
                        }

                        nearby = GetNextObjectInShape(ShapeType.Sphere, Distance, GetLocation(activator), true, ObjectType.Creature);
                        
                    }

                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.Piloting);
                    MessagingService.SendMessageNearbyToPlayers(activator, $"{GetName(activator)} activates their E-War device and begins to draw fire.");
                });
        }
    }
}
