using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Common.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.MartialArts
{
    public class ChiAbilityDefinition: IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public ChiAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Chi1(builder);
            Chi2(builder);
            Chi3(builder);

            return builder.Build();
        }

        private void ImpactAction(uint activator, int baseRecovery)
        {
            var bonusRecovery = GetAbilityModifier(AbilityType.Willpower, activator) * 8;
            var recovery = baseRecovery + bonusRecovery;

            ApplyEffectToObject(DurationType.Instant, EffectHeal(recovery), activator);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Healing_G), activator);

            EnmityService.ModifyEnmityOnAll(activator, 300 + recovery + 10);
            CombatPointService.AddCombatPointToAllTagged(activator, SkillType.MartialArts, 3);
        }

        private void Chi1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Chi1, PerkType.Chi)
                .Name("Chi I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.Chi, 180f)
                .HasActivationDelay(1.0f)
                .RequirementStamina(4)
                .IsCastedAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, 45);
                });
        }

        private void Chi2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Chi2, PerkType.Chi)
                .Name("Chi II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.Chi, 180f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(6)
                .IsCastedAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, 115);
                });
        }

        private void Chi3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Chi3, PerkType.Chi)
                .Name("Chi III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.Chi, 180f)
                .HasActivationDelay(3.0f)
                .RequirementStamina(10)
                .IsCastedAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, 170);
                });
        }
    }
}
