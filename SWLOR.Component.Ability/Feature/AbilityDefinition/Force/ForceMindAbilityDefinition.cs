using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.Domain.Model;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Force
{
    public class ForceMindAbilityDefinition : IAbilityListDefinition
    {
        private readonly IStatusEffectService _statusEffectService;

        public ForceMindAbilityDefinition(IStatusEffectService statusEffectService)
        {
            _statusEffectService = statusEffectService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            ForceMind1(builder);
            ForceMind2(builder);

            return builder.Build();
        }

        private void ForceMind1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceMind1, PerkType.ForceMind)
                .Name("Force Mind I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ForceRestore, 60f * 3f)
                .IsCastedAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    _statusEffectService.Apply(activator, activator, StatusEffectType.ForceMind1, 60f);
                    ApplyEffectToObject(DurationType.Temporary, EffectAbilityDecrease(AbilityType.Willpower, 2), activator, 60f);
                });
        }
        private void ForceMind2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceMind2, PerkType.ForceMind)
                .Name("Force Mind II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ForceRestore, 60f * 3f)
                .IsCastedAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    _statusEffectService.Apply(activator, activator, StatusEffectType.ForceMind2, 60f);
                    ApplyEffectToObject(DurationType.Temporary, EffectAbilityDecrease(AbilityType.Willpower, 4), activator, 60f);
                });
        }
    }
}
