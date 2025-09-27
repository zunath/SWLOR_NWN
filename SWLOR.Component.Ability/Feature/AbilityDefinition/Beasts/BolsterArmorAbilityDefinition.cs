using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.StatusEffect.Contracts;
using SWLOR.Shared.Domain.StatusEffect.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Beasts
{
    public class BolsterArmorAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public BolsterArmorAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IStatusEffectService StatusEffectService => _serviceProvider.GetRequiredService<IStatusEffectService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            BolsterArmor1(builder);
            BolsterArmor2(builder);
            BolsterArmor3(builder);
            BolsterArmor4(builder);
            BolsterArmor5(builder);

            return builder.Build();
        }

        private void Impact(uint activator, StatusEffectType statusEffect)
        {
            var master = GetMaster(activator);
            var beastmasterStat = GetAbilityModifier(AbilityType.Vitality, master) / 2;
            var beastStat = GetAbilityModifier(AbilityType.Vitality, activator) / 2;
            var totalStat = beastmasterStat + beastStat;

            var duration = 5 * 60f + totalStat * 10;
            StatusEffectService.Apply(activator, activator, statusEffect, duration);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Ac_Bonus), activator);
        }

        private void BolsterArmor1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.BolsterArmor1, PerkType.BolsterArmor)
                .Name("Bolster Armor I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.BolsterArmor, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, StatusEffectType.BolsterArmor1);
                });
        }
        private void BolsterArmor2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.BolsterArmor2, PerkType.BolsterArmor)
                .Name("Bolster Armor II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.BolsterArmor, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, StatusEffectType.BolsterArmor2);
                });
        }
        private void BolsterArmor3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.BolsterArmor3, PerkType.BolsterArmor)
                .Name("Bolster Armor III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.BolsterArmor, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, StatusEffectType.BolsterArmor3);
                });
        }
        private void BolsterArmor4(IAbilityBuilder builder)
        {
            builder.Create(FeatType.BolsterArmor4, PerkType.BolsterArmor)
                .Name("Bolster Armor IV")
                .Level(4)
                .HasRecastDelay(RecastGroupType.BolsterArmor, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(4)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, StatusEffectType.BolsterArmor4);
                });
        }
        private void BolsterArmor5(IAbilityBuilder builder)
        {
            builder.Create(FeatType.BolsterArmor5, PerkType.BolsterArmor)
                .Name("Bolster Armor V")
                .Level(5)
                .HasRecastDelay(RecastGroupType.BolsterArmor, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, StatusEffectType.BolsterArmor5);
                });
        }

    }
}
