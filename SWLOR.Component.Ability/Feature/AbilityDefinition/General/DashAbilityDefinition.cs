using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.Enums;
using SWLOR.Component.Ability.Model;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.General
{
    public class DashAbilityDefinition: IAbilityListDefinition
    {
        private readonly IAbilityService _abilityService;

        public DashAbilityDefinition(IAbilityService abilityService)
        {
            _abilityService = abilityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Dash(builder);

            return builder.Build();
        }

        [ScriptHandler(ScriptName.OnSpaceEnter)]
        public void EnterSpace()
        {
            var player = OBJECT_SELF;
            _abilityService.ToggleAbility(player, AbilityToggleType.Dash, false);
        }

        private void Dash(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Dash, PerkType.Dash)
                .Name("Dash")
                .HideActivationMessage()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    var toggle = !_abilityService.IsAbilityToggled(target, AbilityToggleType.Dash);
                    _abilityService.ToggleAbility(target, AbilityToggleType.Dash, toggle);
                });
        }
    }
}
