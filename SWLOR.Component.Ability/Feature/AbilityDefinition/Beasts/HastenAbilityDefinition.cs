using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Common.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Beasts
{
    public class HastenAbilityDefinition: IAbilityListDefinition
    {
        public const string HastenEffectTag = "BEAST_HASTEN";
        private readonly IServiceProvider _serviceProvider;

        public HastenAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Hasten1(builder);
            Hasten2(builder);
            Hasten3(builder);

            return builder.Build();
        }

        private void Impact(uint activator, int numAttacks, bool applyToBeastmaster)
        {
            var beastmaster = GetMaster(activator);
            var beastmasterStat = GetAbilityModifier(AbilityType.Agility, beastmaster);
            var beastStat = GetAbilityModifier(AbilityType.Agility, activator);
            var bonusDuration = 3f * (beastmasterStat + beastStat);

            var effect = EffectModifyAttacks(numAttacks);
            effect = TagEffect(effect, HastenEffectTag);

            ApplyEffectToObject(DurationType.Temporary, effect, activator, 30f + bonusDuration);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Haste), activator);

            if (applyToBeastmaster)
            {
                effect = EffectModifyAttacks(1);
                effect = TagEffect(effect, HastenEffectTag);

                ApplyEffectToObject(DurationType.Temporary, effect, beastmaster, 30f + bonusDuration);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Haste), beastmaster);
            }

            EnmityService.ModifyEnmityOnAll(activator, 300 * numAttacks);
        }

        private void Hasten1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Hasten1, PerkType.Hasten)
                .Name("Hasten I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.Hasten, 120f)
                .HasActivationDelay(1f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, 1, false);
                });
        }

        private void Hasten2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Hasten2, PerkType.Hasten)
                .Name("Hasten II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.Hasten, 120f)
                .HasActivationDelay(1f)
                .RequirementStamina(6)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, 2, false);
                });
        }

        private void Hasten3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Hasten3, PerkType.Hasten)
                .Name("Hasten III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.Hasten, 120f)
                .HasActivationDelay(1f)
                .RequirementStamina(7)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, 2, true);
                });
        }
    }
}
