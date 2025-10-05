using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Perk.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.NPC
{
    public class BiteAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public BiteAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Bite(builder);
            return builder.Build();
        }

        private void Bite(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Bite, PerkType.Invalid)
                .Name("Bite")
                .HasActivationDelay(2f)
                .IsCastedAbility()
                .RequirementStamina(6)
                .UnaffectedByHeavyArmor()
                .HasRecastDelay(RecastGroupType.Bite, 60f)
                .HasImpactAction((activator, target, level, location) =>
                {

                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Com_Chunk_Red_Small), target);
                });
        }

    }
}
