using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.OneHanded
{
    public class StrongStyleLightsaberAbilityDefinition : IAbilityListDefinition
    {
        private readonly IAbilityService _abilityService;

        public StrongStyleLightsaberAbilityDefinition(IAbilityService abilityService)
        {
            _abilityService = abilityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            StrongStyleLightsaber(builder);

            return builder.Build();
        }

        private void DoToggle(uint activator, AbilityToggleType type)
        {
            var isToggled = !_abilityService.IsAbilityToggled(activator, type);
            _abilityService.ToggleAbility(activator, type, isToggled);

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
