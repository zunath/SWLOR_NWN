using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.OneHanded
{
    public class StrongStyleLightsaberAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public StrongStyleLightsaberAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IAbilityService AbilityService => _serviceProvider.GetRequiredService<IAbilityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            StrongStyleLightsaber(builder);

            return builder.Build();
        }

        private void DoToggle(uint activator, AbilityToggleType type)
        {
            var isToggled = !AbilityService.IsAbilityToggled(activator, type);
            AbilityService.ToggleAbility(activator, type, isToggled);

            if (isToggled)
            {
                SendMessageToPC(activator, ColorToken.Green("Strong Style (Lightsaber) enabled"));
            }
            else
            {
                SendMessageToPC(activator, ColorToken.Red("Strong Style (Lightsaber) disabled"));
            }
        }

        private void StrongStyleLightsaber(IAbilityBuilder builder)
        {
            builder.Create(FeatType.StrongStyleLightsaber, PerkType.StrongStyleLightsaber)
                .Name("Strong Style (Lightsaber)")
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HideActivationMessage()
                .HasImpactAction((activator, target, level, location) =>
                {
                    DoToggle(activator, AbilityToggleType.StrongStyleLightsaber);
                });
        }
    }
}
