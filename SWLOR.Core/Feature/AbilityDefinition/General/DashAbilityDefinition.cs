using SWLOR.Core.NWScript.Enum;
using SWLOR.Core.Service;
using SWLOR.Core.Service.AbilityService;
using SWLOR.Core.Service.PerkService;

namespace SWLOR.Core.Feature.AbilityDefinition.General
{
    public class DashAbilityDefinition: IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Dash();

            return _builder.Build();
        }

        [NWNEventHandler("space_enter")]
        public static void EnterSpace()
        {
            var player = OBJECT_SELF;
            Ability.ToggleAbility(player, AbilityToggleType.Dash, false);
        }

        private void Dash()
        {
            _builder.Create(FeatType.Dash, PerkType.Dash)
                .Name("Dash")
                .HideActivationMessage()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    var toggle = !Ability.IsAbilityToggled(target, AbilityToggleType.Dash);
                    Ability.ToggleAbility(target, AbilityToggleType.Dash, toggle);
                });
        }
    }
}
