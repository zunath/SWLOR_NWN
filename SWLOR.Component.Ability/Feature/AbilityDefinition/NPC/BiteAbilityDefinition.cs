using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.Model;
using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.Domain.Model;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.NPC
{
    public class BiteAbilityDefinition : IAbilityListDefinition
    {
        private readonly IStatusEffectService _statusEffectService;

        public BiteAbilityDefinition(IStatusEffectService statusEffectService)
        {
            _statusEffectService = statusEffectService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Bite(builder);
            return builder.Build();
        }

        private void Bite(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Bite, PerkType.Invalid)
                .Name("Bite")
                .HasActivationDelay(2f)
                .IsCastedAbility()
                .RequirementStamina(6)
                .UnaffectedByHeavyArmor()
                .HasRecastDelay(RecastGroup.Bite, 60f)
                .HasImpactAction((activator, target, level, location) =>
                {
                    _statusEffectService.Apply(activator, target, StatusEffectType.Bleed, 60f);

                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Com_Chunk_Red_Small), target);
                });
        }

    }
}
