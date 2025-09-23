using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.Model;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.Domain.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.TwoHanded
{
    public class StrongStyleSaberstaffAbilityDefinition : IAbilityListDefinition
    {
        private readonly IAbilityService _abilityService;

        public StrongStyleSaberstaffAbilityDefinition(IAbilityService abilityService)
        {
            _abilityService = abilityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            StrongStyleSaberstaff(builder);

            return builder.Build();
        }

        private void DoToggle(uint activator, AbilityToggleType type)
        {
            var isToggled = !_abilityService.IsAbilityToggled(activator, type);
            _abilityService.ToggleAbility(activator, type, isToggled);

            if (isToggled)
            {
                SendMessageToPC(activator, ColorToken.Green("Strong Style (Saberstaff) enabled"));
            }
            else
            {
                SendMessageToPC(activator, ColorToken.Red("Strong Style (Saberstaff) disabled"));
            }
        }

        private void StrongStyleSaberstaff(IAbilityBuilder builder)
        {
            builder.Create(FeatType.StrongStyleSaberstaff, PerkType.StrongStyleSaberstaff)
                .Name("Strong Style (Saberstaff)")
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HideActivationMessage()
                .HasImpactAction((activator, target, level, location) =>
                {
                    DoToggle(activator, AbilityToggleType.StrongStyleSaberstaff);
                });
        }
    }
}
