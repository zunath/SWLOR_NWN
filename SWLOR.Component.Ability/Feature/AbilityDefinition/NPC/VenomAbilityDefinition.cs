using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Common.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.NPC
{
    public class VenomAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public VenomAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IStatusEffectService StatusEffectService => _serviceProvider.GetRequiredService<IStatusEffectService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Venom(builder);

            return builder.Build();
        }

        private void Venom(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Venom, PerkType.Invalid)
                .Name("Venom")
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.Venom, 35f)
                .IsCastedAbility()
                .RequirementStamina(3)
                .HasImpactAction((activator, target, level, location) =>
                {
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Disease_S), target);
                    StatusEffectService.Apply(activator, target, StatusEffectType.Poison, 120f);
                });
        }

    }
}
