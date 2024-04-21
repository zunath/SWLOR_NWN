using SWLOR.Core.NWScript.Enum;
using SWLOR.Core.Service;
using SWLOR.Core.Service.AbilityService;
using SWLOR.Core.Service.PerkService;
using SWLOR.Core.Service.StatusEffectService;

namespace SWLOR.Core.Feature.AbilityDefinition.Leadership
{
    public class SoldiersSpeedAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            SoldiersSpeed();

            return _builder.Build();
        }

        private void SoldiersSpeed()
        {
            _builder.Create(FeatType.SoldiersSpeed, PerkType.SoldiersSpeed)
                .Name("Soldier's Speed")
                .Level(1)
                .HasRecastDelay(RecastGroup.SoldiersSpeed, 60f)
                .HasActivationDelay(4f)
                .UnaffectedByHeavyArmor()
                .IsCastedAbility()
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasActivationAction((activator, target, level, location) =>
                {
                    return Ability.ToggleAura(activator, StatusEffectType.SoldiersSpeed);
                })
                .HasImpactAction((activator, target, level, location) =>
                {
                    Ability.ApplyAura(activator, StatusEffectType.SoldiersSpeed, false, true, false);
                });
        }
    }
}
