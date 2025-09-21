using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Infrastructure;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Leadership
{
    public class FrenziedShoutAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            FrenziedShout();

            return _builder.Build();
        }

        private void FrenziedShout()
        {
            _builder.Create(FeatType.FrenziedShout, PerkType.FrenziedShout)
                .Name("Frenzied Shout")
                .Level(1)
                .HasRecastDelay(RecastGroup.FrenziedShout, 120f)
                .HasActivationDelay(4f)
                .UnaffectedByHeavyArmor()
                .IsCastedAbility()
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasActivationAction((activator, target, level, location) =>
                {
                    return AbilityService.ToggleAura(activator, StatusEffectType.FrenziedShout);
                })
                .HasImpactAction((activator, target, level, location) =>
                {
                    AbilityService.ApplyAura(activator, StatusEffectType.FrenziedShout, false, false, true);
                });
        }
    }
}
