using SWLOR.Core.NWScript.Enum;
using SWLOR.Core.NWScript.Enum.VisualEffect;
using SWLOR.Core.Service;
using SWLOR.Core.Service.AbilityService;
using SWLOR.Core.Service.PerkService;
using SWLOR.Core.Service.StatusEffectService;

namespace SWLOR.Core.Feature.AbilityDefinition.NPC
{
    public class VenomAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Venom();

            return _builder.Build();
        }

        private void Venom()
        {
            _builder.Create(FeatType.Venom, PerkType.Invalid)
                .Name("Venom")
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.Venom, 35f)
                .IsCastedAbility()
                .RequirementStamina(3)
                .HasImpactAction((activator, target, level, location) =>
                {
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Disease_S), target);
                    StatusEffect.Apply(activator, target, StatusEffectType.Poison, 120f);
                });
        }

    }
}
