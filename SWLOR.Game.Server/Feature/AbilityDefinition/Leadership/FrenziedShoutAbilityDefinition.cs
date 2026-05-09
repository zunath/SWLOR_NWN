using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

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
                .IsCastedAbility()
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasActivationAction((activator, target, level, location) =>
                {
                    return Ability.ToggleAura(activator, typeof(FrenziedShoutStatusEffect));
                })
                .HasImpactAction((activator, target, level, location) =>
                {
                    Ability.ApplyAura(activator, typeof(FrenziedShoutStatusEffect), false, false, true);
                });
        }
    }
}
