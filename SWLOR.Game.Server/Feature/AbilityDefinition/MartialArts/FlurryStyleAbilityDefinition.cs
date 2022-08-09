using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.MartialArts
{
    public class FlurryStyleAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            FlurryStyle();

            return _builder.Build();
        }

        private void DoToggle(uint activator, AbilityToggleType type)
        {
            if(Ability.IsAbilityToggled(activator, AbilityToggleType.CrushingStyle))
                Ability.ToggleAbility(activator, AbilityToggleType.CrushingStyle, false);
            if(Ability.IsAbilityToggled(activator, AbilityToggleType.MasteredCrushing))
                Ability.ToggleAbility(activator, AbilityToggleType.MasteredCrushing, false);

            var isToggled = !Ability.IsAbilityToggled(activator, type);
            Ability.ToggleAbility(activator, type, isToggled);

            if (isToggled)
            {
                SendMessageToPC(activator, ColorToken.Green("Flurry Style activated."));
            }
            else
            {
                SendMessageToPC(activator, ColorToken.Red("Flurry Style deactivated."));
            }
        }

        private void FlurryStyle()
        {
            _builder.Create(FeatType.FlurryStyle, PerkType.FlurryStyle)
                .Name("Flurry Style")
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HideActivationMessage()
                .HasImpactAction((activator, target, level, location) =>
                {
                    if (level == 1)
                        DoToggle(activator, AbilityToggleType.FlurryStyle);
                    else
                        DoToggle(activator, AbilityToggleType.MasteredFlurry);
                });
        }
    }
}
