using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Perk.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Force
{
    public class CreepingTerrorAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public CreepingTerrorAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            CreepingTerror1(builder);
            CreepingTerror2(builder);
            CreepingTerror3(builder);

            return builder.Build();
        }

        private void CreepingTerror1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.CreepingTerror1, PerkType.CreepingTerror)
                .Name("Creeping Terror I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.CreepingTerror, 30f)
                .HasActivationDelay(2f)
                .RequirementFP(4)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .IsHostileAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                });
        }

        private void CreepingTerror2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.CreepingTerror2, PerkType.CreepingTerror)
                .Name("Creeping Terror II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.CreepingTerror, 30f)
                .HasActivationDelay(2f)
                .RequirementFP(6)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .IsHostileAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                });
        }

        private void CreepingTerror3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.CreepingTerror3, PerkType.CreepingTerror)
                .Name("Creeping Terror III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.CreepingTerror, 30f)
                .HasActivationDelay(2f)
                .RequirementFP(8)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .IsHostileAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                });
        }
    }
}
