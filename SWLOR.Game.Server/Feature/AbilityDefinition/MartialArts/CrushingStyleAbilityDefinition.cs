using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.MartialArts
{
    public class CrushingStyleAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            CrushingStyle();

            return _builder.Build();
        }

        private void DoToggle(uint activator, AbilityToggleType type)
        {
            if(Ability.IsAbilityToggled(activator, AbilityToggleType.FlurryStyle) || Ability.IsAbilityToggled(activator, AbilityToggleType.MasteredFlurry))
            {
                Ability.ToggleAbility(activator, AbilityToggleType.FlurryStyle, false);
                Ability.ToggleAbility(activator, AbilityToggleType.MasteredFlurry, false);
            }
            var isToggled = !Ability.IsAbilityToggled(activator, type);
            Ability.ToggleAbility(activator, type, isToggled);

            if (isToggled)
            {
                SendMessageToPC(activator, ColorToken.Green("Crushing Style activated."));
            }
            else
            {
                SendMessageToPC(activator, ColorToken.Red("Crushing Style deactivated."));
            }
        }

        private void CrushingStyle()
        {
            _builder.Create(FeatType.CrushingStyle, PerkType.CrushingStyle)
                .Name("Crushing Style")
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HideActivationMessage()
                .HasImpactAction((activator, target, level, location) =>
                {
                    if (level == 1)
                        DoToggle(activator, AbilityToggleType.CrushingStyle);
                    else
                        DoToggle(activator, AbilityToggleType.MasteredCrushing);
                });
        }
    }
}
