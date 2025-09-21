using System.Collections.Generic;
using SWLOR.Game.Server.Service;


using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Leadership
{
    public class DedicationAbilityDefinition: IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();
        private readonly IAbilityService _abilityService;

        public DedicationAbilityDefinition(IAbilityService abilityService)
        {
            _abilityService = abilityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Dedication();

            return _builder.Build();
        }

        private void Dedication()
        {
            _builder.Create(FeatType.Dedication, PerkType.Dedication)
                .Name("Dedication")
                .Level(1)
                .HasRecastDelay(RecastGroup.Dedication, 60f)
                .HasActivationDelay(4f)
                .UnaffectedByHeavyArmor()
                .IsCastedAbility()
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasActivationAction((activator, target, level, location) =>
                {
                    return _abilityService.ToggleAura(activator, StatusEffectType.Dedication);
                })
                .HasImpactAction((activator, target, level, location) =>
                {
                    _abilityService.ApplyAura(activator, StatusEffectType.Dedication, true, true, false);
                });
        }
    }
}
