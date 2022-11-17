using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.General
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
