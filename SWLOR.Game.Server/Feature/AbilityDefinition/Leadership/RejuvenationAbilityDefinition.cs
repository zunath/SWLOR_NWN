﻿using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Leadership
{
    public class RejuvenationAbilityDefinition : AuraBaseAbilityDefinition, IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Rejuvenation();

            return _builder.Build();
        }

        private void Rejuvenation()
        {
            _builder.Create(FeatType.Rejuvenation, PerkType.Rejuvenation)
                .Name("Rejuvenation")
                .Level(1)
                .HasRecastDelay(RecastGroup.Rejuvenation, 180f)
                .HasActivationDelay(4f)
                .UnaffectedByHeavyArmor()
                .IsCastedAbility()
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasActivationAction((activator, target, level, location) =>
                {
                    return OnAuraActivation(activator, StatusEffectType.Rejuvenation);
                })
                .HasImpactAction((activator, target, level, location) =>
                {
                    ApplyAura(activator, StatusEffectType.Rejuvenation, false, true, false);
                });
        }
    }
}
