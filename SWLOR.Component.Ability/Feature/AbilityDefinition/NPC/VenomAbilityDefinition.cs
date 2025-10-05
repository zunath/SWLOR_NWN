using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Perk.Enums;



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
                .HasRecastDelay(RecastGroupType.Venom, 35f)
                .IsCastedAbility()
                .RequirementStamina(3)
                .HasImpactAction((activator, target, level, location) =>
                {
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Disease_S), target);
                });
        }

    }
}
