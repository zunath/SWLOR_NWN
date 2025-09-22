using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.Enums;
using SWLOR.Component.Ability.Model;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Force
{
    public class CreepingTerrorAbilityDefinition : IAbilityListDefinition
    {
        private readonly IStatusEffectService _statusEffectService;

        public CreepingTerrorAbilityDefinition(IStatusEffectService statusEffectService)
        {
            _statusEffectService = statusEffectService;
        }

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
                .HasRecastDelay(RecastGroup.CreepingTerror, 30f)
                .HasActivationDelay(2f)
                .RequirementFP(4)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .IsHostileAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    _statusEffectService.Apply(activator, target, StatusEffectType.CreepingTerror, 24f, 1);
                });
        }

        private void CreepingTerror2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.CreepingTerror2, PerkType.CreepingTerror)
                .Name("Creeping Terror II")
                .Level(2)
                .HasRecastDelay(RecastGroup.CreepingTerror, 30f)
                .HasActivationDelay(2f)
                .RequirementFP(6)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .IsHostileAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    _statusEffectService.Apply(activator, target, StatusEffectType.CreepingTerror, 24f, 2);
                });
        }

        private void CreepingTerror3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.CreepingTerror3, PerkType.CreepingTerror)
                .Name("Creeping Terror III")
                .Level(3)
                .HasRecastDelay(RecastGroup.CreepingTerror, 30f)
                .HasActivationDelay(2f)
                .RequirementFP(8)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .IsHostileAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    _statusEffectService.Apply(activator, target, StatusEffectType.CreepingTerror, 24f, 3);
                });
        }
    }
}
