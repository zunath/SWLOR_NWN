using SWLOR.Core.NWScript.Enum;
using SWLOR.Core.Service;
using SWLOR.Core.Service.AbilityService;
using SWLOR.Core.Service.PerkService;

namespace SWLOR.Core.Feature.AbilityDefinition.TwoHanded
{
    public class StrongStyleSaberstaffAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            StrongStyleSaberstaff();

            return _builder.Build();
        }

        private void DoToggle(uint activator, AbilityToggleType type)
        {
            var isToggled = !Ability.IsAbilityToggled(activator, type);
            Ability.ToggleAbility(activator, type, isToggled);

            if (isToggled)
            {
                SendMessageToPC(activator, ColorToken.Green("Strong Style (Saberstaff) enabled"));
            }
            else
            {
                SendMessageToPC(activator, ColorToken.Red("Strong Style (Saberstaff) disabled"));
            }
        }

        private void StrongStyleSaberstaff()
        {
            _builder.Create(FeatType.StrongStyleSaberstaff, PerkType.StrongStyleSaberstaff)
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
