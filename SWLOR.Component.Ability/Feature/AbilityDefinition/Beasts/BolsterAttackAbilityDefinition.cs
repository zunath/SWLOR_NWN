using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Perk.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Beasts
{
    public class BolsterAttackAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public BolsterAttackAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            BolsterAttack1(builder);
            BolsterAttack2(builder);
            BolsterAttack3(builder);
            BolsterAttack4(builder);
            BolsterAttack5(builder);

            return builder.Build();
        }

        private void Impact(uint activator)
        {
            var master = GetMaster(activator);
            var beastmasterStat = GetAbilityModifier(AbilityType.Might, master) / 2;
            var beastStat = GetAbilityModifier(AbilityType.Might, activator) / 2;
            var totalStat = beastmasterStat + beastStat;

            var duration = 5 * 60f + totalStat * 10;
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Flame_S), activator);
        }

        private void BolsterAttack1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.BolsterAttack1, PerkType.BolsterAttack)
                .Name("Bolster Attack I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.BolsterAttack, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator);
                });
        }
        private void BolsterAttack2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.BolsterAttack2, PerkType.BolsterAttack)
                .Name("Bolster Attack II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.BolsterAttack, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator);
                });
        }
        private void BolsterAttack3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.BolsterAttack3, PerkType.BolsterAttack)
                .Name("Bolster Attack III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.BolsterAttack, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator);
                });
        }
        private void BolsterAttack4(IAbilityBuilder builder)
        {
            builder.Create(FeatType.BolsterAttack4, PerkType.BolsterAttack)
                .Name("Bolster Attack IV")
                .Level(4)
                .HasRecastDelay(RecastGroupType.BolsterAttack, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(4)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator);
                });
        }
        private void BolsterAttack5(IAbilityBuilder builder)
        {
            builder.Create(FeatType.BolsterAttack5, PerkType.BolsterAttack)
                .Name("Bolster Attack V")
                .Level(5)
                .HasRecastDelay(RecastGroupType.BolsterAttack, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator);
                });
        }

    }
}
