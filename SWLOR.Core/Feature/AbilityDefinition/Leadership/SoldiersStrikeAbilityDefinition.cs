using SWLOR.Core.NWScript.Enum;
using SWLOR.Core.Service;
using SWLOR.Core.Service.AbilityService;
using SWLOR.Core.Service.PerkService;
using SWLOR.Core.Service.StatusEffectService;

namespace SWLOR.Core.Feature.AbilityDefinition.Leadership
{
    public class SoldiersStrikeAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            SoldiersStrike();

            return _builder.Build();
        }

        private void SoldiersStrike()
        {
            _builder.Create(FeatType.SoldiersStrike, PerkType.SoldiersStrike)
                .Name("Soldier's Strike")
                .Level(1)
                .HasRecastDelay(RecastGroup.SoldiersStrike, 60f)
                .HasActivationDelay(4f)
                .UnaffectedByHeavyArmor()
                .IsCastedAbility()
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasActivationAction((activator, target, level, location) =>
                {
                    return Ability.ToggleAura(activator, StatusEffectType.SoldiersStrike);
                })
                .HasImpactAction((activator, target, level, location) =>
                {
                    Ability.ApplyAura(activator, StatusEffectType.SoldiersStrike, false, true, false);
                });
        }
    }
}
