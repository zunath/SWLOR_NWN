using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Perk.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Leadership
{
    public class SoldiersPrecisionAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public SoldiersPrecisionAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IAbilityService AbilityService => _serviceProvider.GetRequiredService<IAbilityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            SoldiersPrecision(builder);

            return builder.Build();
        }

        private void SoldiersPrecision(IAbilityBuilder builder)
        {
            builder.Create(FeatType.SoldiersPrecision, PerkType.SoldiersPrecision)
                .Name("Soldier's Precision")
                .Level(1)
                .HasRecastDelay(RecastGroupType.SoldiersPrecision, 60f)
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
