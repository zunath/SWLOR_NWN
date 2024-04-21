using SWLOR.Core.NWScript.Enum;
using SWLOR.Core.Service;
using SWLOR.Core.Service.AbilityService;
using SWLOR.Core.Service.PerkService;
using SWLOR.Core.Service.StatusEffectService;

namespace SWLOR.Core.Feature.AbilityDefinition.Leadership
{
    public class SoldiersPrecisionAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            SoldiersPrecision();

            return _builder.Build();
        }

        private void SoldiersPrecision()
        {
            _builder.Create(FeatType.SoldiersPrecision, PerkType.SoldiersPrecision)
                .Name("Soldier's Precision")
                .Level(1)
                .HasRecastDelay(RecastGroup.SoldiersPrecision, 60f)
                .HasActivationDelay(4f)
                .UnaffectedByHeavyArmor()
                .IsCastedAbility()
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasActivationAction((activator, target, level, location) =>
                {
                    return Ability.ToggleAura(activator, StatusEffectType.SoldiersPrecision);
                })
                .HasImpactAction((activator, target, level, location) =>
                {
                    Ability.ApplyAura(activator, StatusEffectType.SoldiersPrecision, false, true, false);
                });
        }
    }
}
