using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.Enums;
using SWLOR.Component.Ability.Model;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.NPC
{
    public class VenomAbilityDefinition : IAbilityListDefinition
    {
        private readonly IStatusEffectService _statusEffectService;

        public VenomAbilityDefinition(IStatusEffectService statusEffectService)
        {
            _statusEffectService = statusEffectService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Venom(builder);

            return builder.Build();
        }

        private void Venom(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Venom, PerkType.Invalid)
                .Name("Venom")
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.Venom, 35f)
                .IsCastedAbility()
                .RequirementStamina(3)
                .HasImpactAction((activator, target, level, location) =>
                {
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Disease_S), target);
                    _statusEffectService.Apply(activator, target, StatusEffectType.Poison, 120f);
                });
        }

    }
}
