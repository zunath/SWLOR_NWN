using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.Enums;
using SWLOR.Component.Ability.Model;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.NPC
{
    public class IronShellAbilityDefinition: IAbilityListDefinition
    {
        private readonly IStatusEffectService _statusEffectService;

        public IronShellAbilityDefinition(IStatusEffectService statusEffectService)
        {
            _statusEffectService = statusEffectService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            IronShell(builder);

            return builder.Build();
        }

        private void IronShell(IAbilityBuilder builder)
        {
            builder.Create(FeatType.IronShell, PerkType.Invalid)
                .Name("Iron Shell")
                .HasActivationDelay(2.0f)
                .HasRecastDelay(RecastGroup.IronShell, 60f)
                .IsCastedAbility()
                .RequirementStamina(5)
                .HasImpactAction((activator, target, level, location) =>
                {
                    ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Magenta), activator, 1.0f);
                    _statusEffectService.Apply(activator, activator, StatusEffectType.IronShell, 45f);
                });
        }
    }
}
