using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Infrastructure;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.General
{
    public class DashAbilityDefinition: IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();
        private readonly IAbilityService _abilityService;

        public DashAbilityDefinition(IAbilityService abilityService)
        {
            _abilityService = abilityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Dash();

            return _builder.Build();
        }

        [ScriptHandler(ScriptName.OnSpaceEnter)]
        public static void EnterSpace()
        {
            var player = OBJECT_SELF;
            var abilityService = ServiceContainer.GetService<IAbilityService>();
            abilityService.ToggleAbility(player, AbilityToggleType.Dash, false);
        }

        private void Dash()
        {
            _builder.Create(FeatType.Dash, PerkType.Dash)
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
