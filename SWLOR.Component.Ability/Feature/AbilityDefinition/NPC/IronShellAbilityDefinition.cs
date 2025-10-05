using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Perk.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.NPC
{
    public class IronShellAbilityDefinition: IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public IronShellAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            IronShell(builder);

            return builder.Build();
        }

        private void IronShell(IAbilityBuilder builder)
        {
            builder.Create(FeatType.IronShell, PerkType.Invalid)
                .Name("Iron Shell")
                .HasActivationDelay(2.0f)
                .HasRecastDelay(RecastGroupType.IronShell, 60f)
                .IsCastedAbility()
                .RequirementStamina(5)
                .HasImpactAction((activator, target, level, location) =>
                {
                    ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffectType.Vfx_Dur_Aura_Magenta), activator, 1.0f);
                });
        }
    }
}
