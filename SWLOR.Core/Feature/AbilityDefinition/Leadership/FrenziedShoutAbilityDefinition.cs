using SWLOR.Core.NWScript.Enum;
using SWLOR.Core.Service;
using SWLOR.Core.Service.AbilityService;
using SWLOR.Core.Service.PerkService;
using SWLOR.Core.Service.StatusEffectService;

namespace SWLOR.Core.Feature.AbilityDefinition.Leadership
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
                    return Ability.ToggleAura(activator, StatusEffectType.FrenziedShout);
                })
                .HasImpactAction((activator, target, level, location) =>
                {
                    Ability.ApplyAura(activator, StatusEffectType.FrenziedShout, false, false, true);
                });
        }
    }
}
