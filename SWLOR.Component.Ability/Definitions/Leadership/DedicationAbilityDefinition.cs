using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Perk.Enums;

namespace SWLOR.Component.Ability.Definitions.Leadership
{
    public class DedicationAbilityDefinition: IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public DedicationAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IAbilityService AbilityService => _serviceProvider.GetRequiredService<IAbilityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Dedication(builder);

            return builder.Build();
        }

        private void Dedication(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Dedication, PerkType.Dedication)
                .Name("Dedication")
                .Level(1)
                .HasRecastDelay(RecastGroupType.Dedication, 60f)
                .HasActivationDelay(4f)
                .UnaffectedByHeavyArmor()
                .IsCastedAbility()
                .UsesAnimation(AnimationType.FireForgetTaunt)
                .HasActivationAction((activator, target, level, location) =>
                {
                    return false;
                })
                .HasImpactAction((activator, target, level, location) =>
                {
                });
        }
    }
}
