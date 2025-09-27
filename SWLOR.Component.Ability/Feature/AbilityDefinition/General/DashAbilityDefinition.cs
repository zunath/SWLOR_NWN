using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Perk.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.General
{
    public class DashAbilityDefinition: IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public DashAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IAbilityService AbilityService => _serviceProvider.GetRequiredService<IAbilityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Dash(builder);

            return builder.Build();
        }

        public void EnterSpace()
        {
            var player = OBJECT_SELF;
            AbilityService.ToggleAbility(player, AbilityToggleType.Dash, false);
        }

        private void Dash(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Dash, PerkType.Dash)
                .Name("Dash")
                .HideActivationMessage()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    var toggle = !AbilityService.IsAbilityToggled(target, AbilityToggleType.Dash);
                    AbilityService.ToggleAbility(target, AbilityToggleType.Dash, toggle);
                });
        }
    }
}
