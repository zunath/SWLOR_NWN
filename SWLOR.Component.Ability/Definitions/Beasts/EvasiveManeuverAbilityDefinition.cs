using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Perk.Enums;

namespace SWLOR.Component.Ability.Definitions.Beasts
{
    public class EvasiveManeuverAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public EvasiveManeuverAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            EvasiveManeuver1(builder);
            EvasiveManeuver2(builder);
            EvasiveManeuver3(builder);
            EvasiveManeuver4(builder);
            EvasiveManeuver5(builder);

            return builder.Build();
        }

        private void Impact(uint activator)
        {
            var master = GetMaster(activator);
            var beastmasterStat = GetAbilityModifier(AbilityType.Agility, master) / 2;
            var beastStat = GetAbilityModifier(AbilityType.Agility, activator) / 2;
            var totalStat = beastmasterStat + beastStat;

            var duration = 5 * 60f + totalStat * 10;
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Fnf_Howl_Odd), activator);
        }

        private void EvasiveManeuver1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.EvasiveManeuver1, PerkType.EvasiveManeuver)
                .Name("Evasive Maneuver I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.EvasiveManeuver, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator);
                });
        }
        private void EvasiveManeuver2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.EvasiveManeuver2, PerkType.EvasiveManeuver)
                .Name("Evasive Maneuver II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.EvasiveManeuver, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator);
                });
        }
        private void EvasiveManeuver3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.EvasiveManeuver3, PerkType.EvasiveManeuver)
                .Name("Evasive Maneuver III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.EvasiveManeuver, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator);
                });
        }
        private void EvasiveManeuver4(IAbilityBuilder builder)
        {
            builder.Create(FeatType.EvasiveManeuver4, PerkType.EvasiveManeuver)
                .Name("Evasive Maneuver IV")
                .Level(4)
                .HasRecastDelay(RecastGroupType.EvasiveManeuver, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(4)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator);
                });
        }
        private void EvasiveManeuver5(IAbilityBuilder builder)
        {
            builder.Create(FeatType.EvasiveManeuver5, PerkType.EvasiveManeuver)
                .Name("Evasive Maneuver V")
                .Level(5)
                .HasRecastDelay(RecastGroupType.EvasiveManeuver, 60f)
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
